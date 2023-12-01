using UnityEngine;
using UnityEngine.UI;

public class NetworkUIEvent : MonoBehaviour
{
    [SerializeField]
    private GameObject _hide;             //������UI

    [SerializeField]
    private InputField _ipAddressIpt;     //������IP�����
    [SerializeField]
    private InputField _roomIdIpt;        //������������
    [SerializeField]
    private InputField _nameIpt;          //���������
    [SerializeField]
    private InputField _messageIpt;       //��Ϣ�����

    [SerializeField]
    private Button _connectServerBtn;     //���ӷ�������ť
    [SerializeField]
    private Button _enrollBtn;            //ע�ᰴť
    [SerializeField]
    private Button _creatRoomBtn;         //�������䰴ť
    [SerializeField]
    private Button _enterRoomBtn;         //���뷿�䰴ť
    [SerializeField]
    private Button _exitRoomBtn;          //�˳����䰴ť
    [SerializeField]
    private Button _startGameBtn;         //��ʼ��Ϸ��ť
    [SerializeField]
    private Button _sendMessage;          //������Ϣ��ť

    [SerializeField]
    private Text _gameStateTxt;           //��Ϸ״̬�ı�
    [SerializeField]
    private Text _roomIdTxt;              //��������ı�
    [SerializeField]
    private Text _nameTxt;                //�����ı�

    private void Start()
    {
        // add listener for buttons
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
                _gameStateTxt.text = "Gaming";
            else
                _gameStateTxt.text = "Waiting";
        };

        NetworkPlayer.Instance.OnRoomIdChange += (roomId) =>
        {
            _roomIdTxt.text = "" + roomId;
        };

        NetworkPlayer.Instance.OnStartGame += (chess) =>
        {
            _hide.SetActive(false);
            Info.Instance.Print(string.Format("Game Start: You're {0} chess!", chess));
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
            Info.Instance.Print("ip cannot be empty");
        }
    }

    private void EnrollBtn()
    {
        if (_nameIpt.text != string.Empty)
            Network.Instance.EnrollRequest(_nameIpt.text);
        else
        {
            Info.Instance.Print("name cannot be empty");
        }
    }

    private void CreatRoomBtn()
    {
        int.TryParse(_roomIdIpt.text, out int roomId);

        if (roomId != 0)
            Network.Instance.CreatRoomRequest(roomId);
        else
        {
            Info.Instance.Print("cannot use 0 for room id");
        }
    }

    private void EnterRoomBtn()
    {
        int.TryParse(_roomIdIpt.text, out int roomId);

        if (roomId != 0)
            Network.Instance.EnterRoomRequest(roomId);
        else
        {
            Info.Instance.Print("cannot use 0 for room id");
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