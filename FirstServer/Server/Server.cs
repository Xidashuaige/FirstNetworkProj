using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Multiplay;

public delegate void ServerCallBack(Player client, byte[] data);

public class CallBack
{
    public Player Player;

    public byte[] Data;

    public ServerCallBack ServerCallBack;

    public CallBack(Player player, byte[] data, ServerCallBack serverCallBack)
    {
        Player = player;
        Data = data;
        ServerCallBack = serverCallBack;
    }

    public void Execute()
    {
        ServerCallBack(Player, Data);
    }
}

public class Room
{
    public enum RoomState
    {
        Await,  
        Gaming  
    }


    public int RoomId = 0;

    public GamePlay GamePlay;

    public RoomState State = RoomState.Await;

    public const int MAX_PLAYER_AMOUNT = 2;

    public const int MAX_OBSERVER_AMOUNT = 2;

    public List<Player> Players = new List<Player>(); 
    public List<Player> OBs = new List<Player>();    

    public Room(int roomId)                         
    {
        RoomId = roomId;
        GamePlay = new GamePlay();
    }

    /// <summary>
    /// Close Room: Remove room from the room list and clear players
    /// </summary>
    public void Close()
    {
        // remove all players
        foreach (var each in Players)
        {
            each.ExitRoom();
        }
        foreach (var each in OBs)
        {
            each.ExitRoom();
        }
        Server.Rooms.Remove(RoomId);
    }
}

/// <summary>
/// <see langword="static"/>
/// </summary>
public static class Server
{
    public static Dictionary<int, Room> Rooms;                  //list of rooms

    public static List<Player> Players;                         //list of players

    private static ConcurrentQueue<CallBack> _callBackQueue;    //callback queue in the main thread

    private static Dictionary<MessageType, ServerCallBack> _callBacks
        = new Dictionary<MessageType, ServerCallBack>();        //Messagetype and callback

    private static Socket _serverSocket;                        //server socket

    #region about thread

    /// <summary>
    /// Main loop
    /// </summary>
    private static void Callback()
    {
        while (true)
        {
            if (_callBackQueue.Count > 0)
            {
                if (_callBackQueue.TryDequeue(out CallBack callBack))
                {
                    //execute callback
                    callBack.Execute();
                }
            }
            //Sleep to avoid block the thread
            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Wait for new client
    /// </summary>
    private static void Await()
    {
        Socket client = null;

        while (true)
        {
            try
            {
                // waiting for client
                client = _serverSocket.Accept(); // this line will block the thread

                // get client endpoint
                string endPoint = client.RemoteEndPoint.ToString();

                // new player
                Player player = new Player(client);
                Players.Add(player);

                Console.WriteLine($"{player.Socket.RemoteEndPoint} connect");

                // create thread for new player
                ParameterizedThreadStart receiveMethod = new ParameterizedThreadStart(Receive);
                Thread listener = new Thread(receiveMethod) { IsBackground = true };
                
                // start listen to this player
                listener.Start(player);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private static void Receive(object obj)
    {
        Player player = obj as Player;
        Socket client = player.Socket;

        while (true)
        {
            // The server and client need to formulate data packages strictly in accordance with certain protocols
            byte[] data = new byte[4]; // firstly we only read the first 4 byte, which will be the message type

            int length = 0;                            // message length
            MessageType type = MessageType.None;       // message type
            int receive = 0;                           // length received

            try
            {
                receive = client.Receive(data); // this line will block the thread
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{client.RemoteEndPoint} offline :{ex.Message}");
                player.Offline();
                return;
            }

            // if read faild
            if (receive < data.Length)
            {
                Console.WriteLine($"{client.RemoteEndPoint} offline");
                player.Offline();
                return;
            }

            // convert read head byte to enum
            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader binary = new BinaryReader(stream, Encoding.UTF8); // read as UTF-8 format 
                try
                {
                    length = binary.ReadUInt16();
                    type = (MessageType)binary.ReadUInt16();
                }
                catch (Exception)
                {
                    Console.WriteLine($"{client.RemoteEndPoint} offline");
                    player.Offline();
                    return;
                }
            }

            // if have body, then read the remaining part of message
            if (length - 4 > 0)
            {
                data = new byte[length - 4];
                receive = client.Receive(data);
                if (receive < data.Length)
                {
                    Console.WriteLine($"{client.RemoteEndPoint} offline");
                    player.Offline();
                    return;
                }
            }
            else
            {
                data = new byte[0];
                receive = 0;
            }

            Console.WriteLine($"receive message, room count:{Rooms.Count}, player count:{Players.Count}, message type:{type}");

            if (_callBacks.ContainsKey(type))
            {
                CallBack callBack = new CallBack(player, data, _callBacks[type]);
                // add handle to the callbacks queue
                _callBackQueue.Enqueue(callBack);
            }
        }
    }

    #endregion

    /// <summary>
    /// Start Server
    /// </summary>
    public static void Start(string ip)
    {
        //init callback queue
        _callBackQueue = new ConcurrentQueue<CallBack>();

        Rooms = new Dictionary<int, Room>();

        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Players = new List<Player>();
        
        // Init ip adress and port
        IPEndPoint point = new IPEndPoint(IPAddress.Any, 8848);
        //IPEndPoint point = new IPEndPoint(IPAddress.Parse(ip), 8848);
             
        _serverSocket.Bind(point); 

        _serverSocket.Listen(0);

        //Start waiting client
        Thread thread = new Thread(Await) { IsBackground = true };
        thread.Start();

        //Start handle callbacks
        Thread handle = new Thread(Callback) { IsBackground = true };
        handle.Start();
    }

    public static string GetServerIP()
    {
        return _serverSocket.LocalEndPoint.ToString();
    }

    /// <summary>
    /// Register callbacks
    /// </summary>
    public static void Register(MessageType type, ServerCallBack method)
    {
        if (!_callBacks.ContainsKey(type)) _callBacks.Add(type, method);
        else Console.WriteLine("Callback has already registered");
    }

    /// <summary>
    /// packaging and send message
    /// </summary>
    public static void Send(this Player player, MessageType type, byte[] data = null)
    {
        //Pack message
        byte[] bytes = Pack(type, data);

        //Send message
        try
        {
            player.Socket.Send(bytes);
        }
        catch (Exception ex)
        {
            // client offline
            Console.WriteLine(ex.Message);
            player.Offline();
        }
    }

    /// <summary>
    /// Remove client from server
    /// </summary>
    public static void Offline(this Player player)
    {
        // remove from the list
        Players.Remove(player);

        // if this player are in the room
        if (player.InRoom)
        {
            // close the room
            Rooms[player.RoomId].Close();
        }
    }

    /// <summary>
    /// wrap message
    /// </summary>
    private static byte[] Pack(MessageType type, byte[] data = null)
    {
        MessagePacker packer = new MessagePacker();
        if (data != null)
        {
            packer.Add((ushort)(4 + data.Length)); // message lenght
            packer.Add((ushort)type);              // message type
            packer.Add(data);                      // message data
        }
        else
        {
            packer.Add(4);                         // message lenght
            packer.Add((ushort)type);              // message type
        }
        return packer.Package;
    }
}