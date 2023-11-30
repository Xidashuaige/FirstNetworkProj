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
        //所有玩家跟观战者退出房间
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
            // 解析数据包过程(服务器与客户端需要严格按照一定的协议制定数据包)
            byte[] data = new byte[4];

            int length = 0;                            // message lenght
            MessageType type = MessageType.None;       // message type
            int receive = 0;                           //接收信息

            try
            {
                receive = client.Receive(data); //同步接受消息 // 这句应该会阻塞代码
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{client.RemoteEndPoint}已掉线:{ex.Message}");
                player.Offline();
                return;
            }

            //包头接收不完整
            if (receive < data.Length)
            {
                Console.WriteLine($"{client.RemoteEndPoint}已掉线");
                player.Offline();
                return;
            }

            //解析消息过程
            using (MemoryStream stream = new MemoryStream(data))
            {
                BinaryReader binary = new BinaryReader(stream, Encoding.UTF8); //UTF-8格式解析
                try
                {
                    length = binary.ReadUInt16();
                    type = (MessageType)binary.ReadUInt16();
                }
                catch (Exception)
                {
                    Console.WriteLine($"{client.RemoteEndPoint}已掉线");
                    player.Offline();
                    return;
                }
            }

            //如果有包体
            if (length - 4 > 0)
            {
                data = new byte[length - 4];
                receive = client.Receive(data);
                if (receive < data.Length)
                {
                    Console.WriteLine($"{client.RemoteEndPoint}已掉线");
                    player.Offline();
                    return;
                }
            }
            else
            {
                data = new byte[0];
                receive = 0;
            }

            Console.WriteLine($"接受到消息, 房间数:{Rooms.Count}, 玩家数:{Players.Count}, 消息类型:{type}");

            //执行回调事件
            if (_callBacks.ContainsKey(type))
            {
                CallBack callBack = new CallBack(player, data, _callBacks[type]);
                //放入回调执行线程
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