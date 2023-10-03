using UnityEngine;
using Multiplay;

public class Network : MonoBehaviour
{
    private Network() { }
    public static Network Instance { get; private set; }

    /// <summary>
    /// ע��
    /// </summary>
    public void EnrollRequest(string name)
    {
        Enroll request = new Enroll();
        request.Name = name;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.Enroll, data);
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void CreatRoomRequest(int roomId)
    {
        CreatRoom request = new CreatRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.CreatRoom, data);
    }

    /// <summary>
    /// ���뷿��
    /// </summary>
    public void EnterRoomRequest(int roomId)
    {
        EnterRoom request = new EnterRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.EnterRoom, data);
    }

    /// <summary>
    /// �˳�����
    /// </summary>
    public void ExitRoomRequest(int roomId)
    {
        ExitRoom request = new ExitRoom();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.ExitRoom, data);
    }

    /// <summary>
    /// ��ʼ��Ϸ
    /// </summary>
    public void StartGameRequest(int roomId)
    {
        StartGame request = new StartGame();
        request.RoomId = roomId;
        byte[] data = NetworkUtils.Serialize(request);
        NetworkClient.Enqueue(MessageType.StartGame, data);
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void PlayChessRequest(int roomId)
    {
        //�������̲������
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
        NetworkClient.Register(MessageType.HeartBeat, _Heartbeat);
        NetworkClient.Register(MessageType.Enroll, _Enroll);
        NetworkClient.Register(MessageType.CreatRoom, _CreatRoom);
        NetworkClient.Register(MessageType.EnterRoom, _EnterRoom);
        NetworkClient.Register(MessageType.ExitRoom, _ExitRoom);
        NetworkClient.Register(MessageType.StartGame, _StartGame);
        NetworkClient.Register(MessageType.PlayChess, _PlayChess);
        NetworkClient.Register(MessageType.SendMessage, _SendMessage);
    }

    #region ������Ϣ�ص��¼�

    private void _Heartbeat(byte[] data)
    {
        NetworkClient.Received = true;
        Debug.Log("�յ���������Ӧ");
    }

    private void _Enroll(byte[] data)
    {
        Enroll result = NetworkUtils.Deserialize<Enroll>(data);
        if (result.Suc)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.CreateRoom);

            NetworkPlayer.Instance.OnNameChange(result.Name);

            Info.Instance.Print("ע��ɹ�");
        }
        else
        {
            Info.Instance.Print("ע��ʧ��");
        }
    }

    private void _CreatRoom(byte[] data)
    {
        CreatRoom result = NetworkUtils.Deserialize<CreatRoom>(data);

        if (result.Suc)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.InRoom);

            NetworkPlayer.Instance.OnRoomIdChange(result.RoomId);

            Info.Instance.Print(string.Format("��������ɹ�, ��ķ������{0}", NetworkPlayer.Instance.RoomId));
        }
        else
        {
            Info.Instance.Print("��������ʧ��");
        }
    }

    private void _EnterRoom(byte[] data)
    {
        EnterRoom result = NetworkUtils.Deserialize<EnterRoom>(data);

        if (result.result == EnterRoom.Result.Player)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.InRoom);

            Info.Instance.Print("���뷿��ɹ�, ����һ�����");
        }
        else if (result.result == EnterRoom.Result.Observer)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.InRoom);

            Info.Instance.Print("���뷿��ɹ�, ����һ���۲���");
        }
        else
        {
            Info.Instance.Print("���뷿��ʧ��");
            return;
        }

        //���뷿��
        NetworkPlayer.Instance.OnRoomIdChange(result.RoomId);
    }

    private void _ExitRoom(byte[] data)
    {
        ExitRoom result = NetworkUtils.Deserialize<ExitRoom>(data);

        if (result.Suc)
        {
            NetworkGameplay.Instance.ChangePage(NetworkGameplay.Pages.CreateRoom);

            //����ű�ΪĬ��
            NetworkPlayer.Instance.OnRoomIdChange(0);
            //���״̬�ı�
            NetworkPlayer.Instance.OnPlayingChange(false);

            Info.Instance.Print("�˳�����ɹ�");
        }
        else
        {
            Info.Instance.Print("�˳�����ʧ��");
        }
    }

    private void _StartGame(byte[] data)
    {
        StartGame result = NetworkUtils.Deserialize<StartGame>(data);

        if (result.Suc)
        {
            //��ʼ��Ϸ�¼�
            NetworkPlayer.Instance.OnPlayingChange(true);

            //�ǹ۲���
            if (result.Watch)
            {
                NetworkPlayer.Instance.OnStartGame(Chess.None);
            }
            //�����
            else
            {
                //�Ƿ�����(����ִ����, ����ִ����)
                if (result.First)
                    NetworkPlayer.Instance.OnStartGame(Chess.Black);
                else
                    NetworkPlayer.Instance.OnStartGame(Chess.White);
            }
        }
        else
        {
            Info.Instance.Print("��ʼ��Ϸʧ��");
        }
    }

    private void _PlayChess(byte[] data)
    {
        PlayChess result = NetworkUtils.Deserialize<PlayChess>(data);

        if (!result.Suc)
        {
            Info.Instance.Print("�������ʧ��");
            return;
        }

        switch (result.Challenger)
        {
            case Chess.None:
                break;
            case Chess.Black:
                NetworkPlayer.Instance.OnPlayingChange(false);
                Info.Instance.Print("����ʤ��");
                break;
            case Chess.White:
                NetworkPlayer.Instance.OnPlayingChange(false);
                Info.Instance.Print("����ʤ��");
                break;
            case Chess.Draw:
                NetworkPlayer.Instance.OnPlayingChange(false);
                Info.Instance.Print("ƽ��");
                break;
        }

        //ʵ��������
        NetworkGameplay.Instance.InstChess(result.Chess, new Vec2(result.X, result.Y));
    }

    private void _SendMessage(byte[] data)
    {
        SendMessage result = NetworkUtils.Deserialize<SendMessage>(data);

        if (!result.Suc)
        {
            Info.Instance.Print("��Ϣ����ʧ��");
            return;
        }

        // ��ʾ��Ϣ
        NetworkGameplay.Instance.ShowText(result.Owner + ": " + result.Message);
    }

    #endregion
}