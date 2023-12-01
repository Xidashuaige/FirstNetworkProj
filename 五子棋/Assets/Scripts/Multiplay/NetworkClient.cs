using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Multiplay;

/// <summary>
/// call backs
/// </summary>
public delegate void CallBack(byte[] data);

/// <summary>
/// <see langword="static"/>
/// </summary>
public static class NetworkClient
{
    private class NetworkCoroutine : MonoBehaviour
    {
        private event Action ApplicationQuitEvent;

        private static NetworkCoroutine _instance;

        /// <summary>
        /// Singleton
        /// </summary>
        public static NetworkCoroutine Instance
        {
            get
            {
                if (!_instance)
                {
                    GameObject socketClientObj = new GameObject("NetworkCoroutine");
                    _instance = socketClientObj.AddComponent<NetworkCoroutine>();
                    DontDestroyOnLoad(socketClientObj);
                }
                return _instance;
            }
        }

        public void SetQuitEvent(Action func)
        {
            if (ApplicationQuitEvent != null)
                return;
            ApplicationQuitEvent += func;
        }

        private void OnApplicationQuit()
        {
            if (ApplicationQuitEvent != null)
                ApplicationQuitEvent();
        }
    }

    private enum ClientState
    {
        None,
        Connected,
    }

    // call backs
    private static Dictionary<MessageType, CallBack> _callBacks = new Dictionary<MessageType, CallBack>();

    // message waiting list
    private static Queue<byte[]> _messages;

    private static ClientState _curState;

    private static TcpClient _client;

    private static NetworkStream _stream;

    private static IPAddress _address;

    private static int _port;

    // Heart beat 
    private const float HEARTBEAT_TIME = 3;         // heart beat interval, in second
    private static float _timer = HEARTBEAT_TIME;   // timer
    public static bool Received = true;             // if receive heart beat reply

    private static IEnumerator Connect()
    {
        _client = new TcpClient();

        // async connect 
        IAsyncResult async = _client.BeginConnect(_address, _port, null, null);
        while (!async.IsCompleted)
        {
            Debug.Log("connecting to server...");
            yield return null;
        }

        // catch erro
        try
        {
            _client.EndConnect(async);
        }
        catch (Exception ex)
        {
            Info.Instance.Print("connect server faild:" + ex.Message, true);
            yield break;
        }

        // get message stream
        try
        {
            _stream = _client.GetStream();
        }
        catch (Exception ex)
        {
            Info.Instance.Print("connect server faild:" + ex.Message, true);
            yield break;
        }
        if (_stream == null)
        {
            Info.Instance.Print("connect server faild: data was null", true);
            yield break;
        }

        _curState = ClientState.Connected;
        _messages = new Queue<byte[]>();
        Info.Instance.Print("connct server successful!");

        NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.Enroll);// change to name page

        // Setup clients async system

        NetworkCoroutine.Instance.StartCoroutine(Send());

        NetworkCoroutine.Instance.StartCoroutine(Receive());

        NetworkCoroutine.Instance.SetQuitEvent(() => { _client.Close(); _curState = ClientState.None; });
    }

    private static IEnumerator Send()
    {
        while (_curState == ClientState.Connected)
        {
            _timer += Time.deltaTime;

            // If there has any message in queue
            if (_messages.Count > 0)
            {
                byte[] data = _messages.Dequeue();
                yield return Write(data);
            }

            // every X time we will sent heart beat
            if (_timer >= HEARTBEAT_TIME)
            {
                // if don't receive last heart beat reply, then disconest from server
                if (!Received)
                {
                    _curState = ClientState.None;
                    Info.Instance.Print("heart beat doesn't replay, disconnecting...", true);
                    yield break;
                }
                _timer = 0;

                //pakage message
                byte[] data = Pack(MessageType.HeartBeat);

                // Send message
                yield return Write(data);

                Debug.Log("Heart beat sent");
            }
            yield return null;
        }
    }

    private static IEnumerator Receive()
    {
        while (_curState == ClientState.Connected)
        {
            // The server and client need to formulate data packages strictly in accordance with certain protocols
            byte[] data = new byte[4]; // firstly we only read the first 4 byte, which will be message length (2bytes) & message type (2 bytes)

            int length;         // message length
            MessageType type;   // message type
            int receive = 0;    // length received

            IAsyncResult async = _stream.BeginRead(data, 0, data.Length, null, null); //BeginRead doesn't block thread

            // while until receive message
            while (!async.IsCompleted)
            {
                yield return null;
            }

            // catch error
            try
            {
                receive = _stream.EndRead(async);
            }
            catch (Exception ex)
            {
                _curState = ClientState.None;
                Info.Instance.Print("Header read faild: " + ex.Message, true);
                yield break;
            }
            if (receive < data.Length)
            {
                _curState = ClientState.None;
                Info.Instance.Print("Header read faild", true);
                yield break;
            }

            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader binary = new BinaryReader(stream, Encoding.UTF8);  // read as UTF-8 format 
                try
                {
                    length = binary.ReadUInt16();
                    type = (MessageType)binary.ReadUInt16();
                }
                catch (Exception)
                {
                    _curState = ClientState.None;
                    Info.Instance.Print("header receive faild", true);
                    yield break;
                }
            }

            // If there has body
            if (length - 4 > 0)
            {
                data = new byte[length - 4];

                // Then read it
                async = _stream.BeginRead(data, 0, data.Length, null, null);
                while (!async.IsCompleted)
                {
                    yield return null;
                }
                // catch error
                try
                {
                    receive = _stream.EndRead(async);
                }
                catch (Exception ex)
                {
                    _curState = ClientState.None;
                    Info.Instance.Print("message body reveive faild: " + ex.Message, true);
                    yield break;
                }
                if (receive < data.Length)
                {
                    _curState = ClientState.None;
                    Info.Instance.Print("message body reveive faild", true);
                    yield break;
                }
            }
            // If there no message body
            else
            {
                data = new byte[0];
                receive = 0;
            }

            if (_callBacks.ContainsKey(type))
            {
                // execute the callback
                CallBack method = _callBacks[type];
                method(data);
            }
            else
            {
                Debug.Log("cannot find correspond callback: " + type);
            }
        }
    }

    private static IEnumerator Write(byte[] data)
    {
        if (_curState != ClientState.Connected || _stream == null)
        {
            Info.Instance.Print("Connect faild,cannot send message", true);
            yield break;
        }

        IAsyncResult async = _stream.BeginWrite(data, 0, data.Length, null, null);
        while (!async.IsCompleted)
        {
            yield return null;
        }
        try
        {
            _stream.EndWrite(async);
        }
        catch (Exception ex)
        {
            _curState = ClientState.None;
            Info.Instance.Print("message send faild: " + ex.Message, true);
        }
    }

    public static void Connect(string address = null, int port = 8848)
    {
        if (_curState == ClientState.Connected)
        {
            Info.Instance.Print("You're already in server");
            return;
        }
        if (address == null)
            address = NetworkUtils.GetLocalIPv4();

        if (!IPAddress.TryParse(address, out _address))
        {
            Info.Instance.Print("IP error, try again", true);
            return;
        }

        _port = port;

        // connect to server
        NetworkCoroutine.Instance.StartCoroutine(Connect()); //(Successful connection of IP and port number does not guarantee successful establishment of network flow)
        // use NetworkCoroutine to start coroutine, because NetworkClient doesn't inheritance from MonoBehavior
    }

    public static void Register(MessageType type, CallBack method)
    {
        if (!_callBacks.ContainsKey(type))
            _callBacks.Add(type, method);
        else
            Debug.LogWarning("this callback is already registered: " + type);
    }

    /// <summary>
    /// Add message to the queue
    /// </summary>
    public static void Enqueue(MessageType type, byte[] data = null)
    {
        // pack the message
        byte[] bytes = Pack(type, data);

        if (_curState == ClientState.Connected)
        {
            // join to message queue and waiting for sent                                
            _messages.Enqueue(bytes);
        }
    }

    /// <summary>
    /// Pack the message
    /// </summary>
    private static byte[] Pack(MessageType type, byte[] data = null)
    {
        MessagePacker packer = new MessagePacker();
        if (data != null)
        {
            packer.Add((ushort)(4 + data.Length)); // message lenght , 4 mean lenght + type (2(ushort) + 2(ushort))
            packer.Add((ushort)type);              // message type
            packer.Add(data);                      // message context
        }
        else
        {
            packer.Add(4);                         // message lenght
            packer.Add((ushort)type);              // message type
        }
        return packer.Package;
    }
}