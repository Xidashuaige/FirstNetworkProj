using UnityEngine;
using Multiplay;

public class Network : MonoBehaviour
{
    private Network() { }
    public static Network Instance { get; private set; }

    public void EnrollRequest(string name)
    {
        Enroll request = new Enroll();
        request.Name = name;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.Enroll, data);
    }

    public void CreatRoomRequest(int roomId)
    {
        CreatRoom request = new CreatRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.CreatRoom, data);
    }

    public void EnterRoomRequest(int roomId)
    {
        EnterRoom request = new EnterRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.EnterRoom, data);
    }

    public void ExitRoomRequest(int roomId)
    {
        ExitRoom request = new ExitRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.ExitRoom, data);
    }

    public void StartGameRequest(int roomId)
    {
        StartGame request = new StartGame();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.StartGame, data);
    }

    public void PlayChessRequest(int roomId)
    {
        //进行棋盘操作检测
        Vec2 pos = NetworkGameplay.Instance.PlayChess();

        if (pos.X == -1) return;

        PlayChess request = new PlayChess();
        request.RoomId = roomId;
        request.Chess = NetworkPlayer.Instance.Chess;
        request.X = pos.X;
        request.Y = pos.Y;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.PlayChess, data);
    }

    public void SendMessageRequest(int roomId,string message)
    {
        SendMessage request = new SendMessage();

        request.RoomId = roomId;
        request.Message = message;
        request.Owner = NetworkPlayer.Instance.Name;
        request.Suc = false;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.SendMessage, data);
    }

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        NetworkClient.Register(MessageType.HeartBeat, Heartbeat);
        NetworkClient.Register(MessageType.Enroll, Enroll);
        NetworkClient.Register(MessageType.CreatRoom, CreatRoom);
        NetworkClient.Register(MessageType.EnterRoom, EnterRoom);
        NetworkClient.Register(MessageType.ExitRoom, ExitRoom);
        NetworkClient.Register(MessageType.StartGame, StartGame);
        NetworkClient.Register(MessageType.PlayChess, PlayChess);
        NetworkClient.Register(MessageType.SendMessage, SendMessage);
    }

    #region Send message callback

    private void Heartbeat(byte[] data)
    {
        NetworkClient.Received = true;
        Debug.Log("receive heart beat");
    }

    private void Enroll(byte[] data)
    {
        Enroll result = NetworkUtils.Deserialize<Enroll>(data);
        if (result.Suc)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.CreateRoom);

            NetworkPlayer.Instance.OnNameChange(result.Name);

            Info.Instance.Print("Enroll successful");
        }
        else
        {
            Info.Instance.Print("Enroll faild");
        }
    }

    private void CreatRoom(byte[] data)
    {
        CreatRoom result = NetworkUtils.Deserialize<CreatRoom>(data);

        if (result.Suc)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.InRoom);

            NetworkPlayer.Instance.OnRoomIdChange(result.RoomId);

            Info.Instance.Print(string.Format("Create room successful, your room id is: {0}", NetworkPlayer.Instance.RoomId));
        }
        else
        {
            Info.Instance.Print("Create room faild");
        }
    }

    private void EnterRoom(byte[] data)
    {
        EnterRoom result = NetworkUtils.Deserialize<EnterRoom>(data);

        if (result.result == Multiplay.EnterRoom.Result.Player)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.InRoom);

            Info.Instance.Print("Join room successful, you're a player");
        }
        else if (result.result == Multiplay.EnterRoom.Result.Observer)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.InRoom);

            Info.Instance.Print("Join room successful, you're a observer");
        }
        else
        {
            Info.Instance.Print("Join room faild");
            return;
        }

        // Jooin room
        NetworkPlayer.Instance.OnRoomIdChange(result.RoomId);
    }

    private void ExitRoom(byte[] data)
    {
        ExitRoom result = NetworkUtils.Deserialize<ExitRoom>(data);

        if (result.Suc)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.CreateRoom);

            // Change room id to 0
            NetworkPlayer.Instance.OnRoomIdChange(0);
            // Cange player state
            NetworkPlayer.Instance.OnPlayingChange(false);

            Info.Instance.Print("Leave room successful");
        }
        else
        {
            Info.Instance.Print("Leave room faild");
        }
    }

    private void StartGame(byte[] data)
    {
        StartGame result = NetworkUtils.Deserialize<StartGame>(data);

        if (result.Suc)
        {
            // Start game
            NetworkPlayer.Instance.OnPlayingChange(true);

            // Observer case
            if (result.Watch)
            {
                NetworkPlayer.Instance.OnStartGame(Chess.None);
            }
            // Player case
            else
            {
                // Who start
                if (result.First)
                    NetworkPlayer.Instance.OnStartGame(Chess.Black);
                else
                    NetworkPlayer.Instance.OnStartGame(Chess.White);
            }
        }
        else
        {
            Info.Instance.Print("Start game faild");
        }
    }

    private void PlayChess(byte[] data)
    {
        PlayChess result = NetworkUtils.Deserialize<PlayChess>(data);

        if (!result.Suc)
        {
            Info.Instance.Print("下棋操作失败");
            return;
        }

        switch (result.Challenger)
        {
            case Chess.None:
                break;
            case Chess.Black:
                NetworkPlayer.Instance.OnPlayingChange(false);
                Info.Instance.Print("黑棋胜利");
                break;
            case Chess.White:
                NetworkPlayer.Instance.OnPlayingChange(false);
                Info.Instance.Print("白棋胜利");
                break;
            case Chess.Draw:
                NetworkPlayer.Instance.OnPlayingChange(false);
                Info.Instance.Print("平局");
                break;
        }

        //实例化棋子
        NetworkGameplay.Instance.InstChess(result.Chess, new Vec2(result.X, result.Y));
    }

    private void SendMessage(byte[] data)
    {
        SendMessage result = NetworkUtils.Deserialize<SendMessage>(data);

        if (!result.Suc)
        {
            Info.Instance.Print("Message receive faild");
            return;
        }

        // Print test
        NetworkGameplay.Instance.ShowText(result.Owner + ": " + result.Message);
    }

    #endregion
}