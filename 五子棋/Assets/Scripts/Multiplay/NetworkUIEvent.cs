using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 网络UI事件
/// </summary>
public class NetworkUIEvent : MonoBehaviour
{
    [SerializeField]
    private GameObject _hide;             //可隐藏UI

    [SerializeField]
    private InputField _ipAddressIpt;     //服务器IP输入框
    [SerializeField]
    private InputField _roomIdIpt;        //房间号码输入框
    [SerializeField]
    private InputField _nameIpt;          //名字输入框
    [SerializeField]
    private InputField _messageIpt;       //消息输入框

    [SerializeField]
    private Button _connectServerBtn;     //连接服务器按钮
    [SerializeField]
    private Button _enrollBtn;            //注册按钮
    [SerializeField]
    private Button _creatRoomBtn;         //创建房间按钮
    [SerializeField]
    private Button _enterRoomBtn;         //加入房间按钮
    [SerializeField]
    private Button _exitRoomBtn;          //退出房间按钮
    [SerializeField]
    private Button _startGameBtn;         //开始游戏按钮
    [SerializeField]
    private Button _sendMessage;          //发送消息按钮

    [SerializeField]
    private Text _gameStateTxt;           //游戏状态文本
    [SerializeField]
    private Text _roomIdTxt;              //房间号码文本
    [SerializeField]
    private Text _nameTxt;                //名字文本

    private void Start()
    {
        //绑定按钮事件
        _connectServerBtn.onClick.AddListener(ConnectServerBtn);
        _enrollBtn.onClick.AddListener(EnrollBtn);
        _creatRoomBtn.onClick.AddListener(CreatRoomBtn);
        _enterRoomBtn.onClick.AddListener(EnterRoomBtn);
        _exitRoomBtn.onClick.AddListener(ExitRoomBtn);
        _startGameBtn.onClick.AddListener(StartGameBtn);
        _sendMessage.onClick.AddListener(SendMessageBtn);

        NetworkPlayer.Instance.OnPlayingChange += (playing) =>
        {
            if (playing)
                _gameStateTxt.text = "游戏";
            else
                _gameStateTxt.text = "空闲";
        };

        NetworkPlayer.Instance.OnRoomIdChange += (roomId) =>
        {
            _roomIdTxt.text = "" + roomId;
        };

        NetworkPlayer.Instance.OnStartGame += (chess) =>
        {
            _hide.SetActive(false);
            Info.Instance.Print(string.Format("开始游戏成功:这局你是{0}棋!", chess));
        };

        NetworkPlayer.Instance.OnNameChange += (name) =>
        {
            _nameTxt.text = name;
        };
    }

    private void ConnectServerBtn()
    {
        if (_ipAddressIpt.text != string.Empty)
            NetworkClient.Connect(_ipAddressIpt.text);
        else
        {
            Info.Instance.Print("IP地址不能为空");
        }
    }

    private void EnrollBtn()
    {
        if (_nameIpt.text != string.Empty)
            Network.Instance.EnrollRequest(_nameIpt.text);
        else
        {
            Info.Instance.Print("名字不能为空");
        }
    }

    private void CreatRoomBtn()
    {
        int roomId;
        int.TryParse(_roomIdIpt.text, out roomId);

        if (roomId != 0)
            Network.Instance.CreatRoomRequest(roomId);
        else
        {
            Info.Instance.Print("不能以0作为房间号");
        }
    }

    private void EnterRoomBtn()
    {
        int roomId;
        int.TryParse(_roomIdIpt.text, out roomId);

        if (roomId != 0)
            Network.Instance.EnterRoomRequest(roomId);
        else
        {
            Info.Instance.Print("不能以0作为房间号");
        }
    }

    private void ExitRoomBtn()
    {
        Network.Instance.ExitRoomRequest(NetworkPlayer.Instance.RoomId);
    }

    private void StartGameBtn()
    {
        Network.Instance.StartGameRequest(NetworkPlayer.Instance.RoomId);
    }

    private void SendMessageBtn()
    {
        Network.Instance.SendMessageRequest(NetworkPlayer.Instance.RoomId, _messageIpt.text);
        _messageIpt.text = "";
    }
}