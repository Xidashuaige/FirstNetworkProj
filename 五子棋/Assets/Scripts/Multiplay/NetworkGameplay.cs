using System.Collections.Generic;
using UnityEngine;
using Multiplay;
using UnityEngine.UI;

/// <summary>
/// ���������߼�
/// </summary>
public class NetworkGameplay : MonoBehaviour
{
    public enum Pages
    {
        Connect = 0,
        Enroll = 1,
        CreateRoom = 2,
        InRoom = 3
    }
    //����
    private NetworkGameplay() { }
    public static NetworkGameplay Instance { get; private set; }

    [SerializeField]
    private GameObject _blackChess;                    //��Ҫʵ�����ĺ���
    [SerializeField]
    private GameObject _whiteChess;                    //��Ҫʵ�����İ���

    [SerializeField]
    private Text _reciveText;
    [SerializeField]
    private GameObject _connectPage;                    
    [SerializeField]
    private GameObject _enrollPage;
    [SerializeField]
    private GameObject _createRoomPage;
    [SerializeField]
    private GameObject _inRoomPage;

    private List<GameObject> listPage;

    //ê��
    [SerializeField]
    private GameObject _leftTop;                       //����
    [SerializeField]
    private GameObject _leftBottom;                    //����
    [SerializeField]
    private GameObject _rightTop;                      //����

    private Vector2[,] _chessPos;                      //����������������
    private float _gridWidth;                          //������
    private float _gridHeight;                         //����߶�

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        _chessPos = new Vector2[15, 15];

        //���ĸ��ڵ㶼����������ת����Ļ����
        Vector3 leftTop = _leftTop.transform.position;
        Vector3 leftBottom = _leftBottom.transform.position;
        Vector3 rightTop = _rightTop.transform.position;

        //��ʼ��ÿһ������(һ��14��)�Ŀ����߶�
        _gridWidth = (rightTop.x - leftTop.x) / 14;
        _gridHeight = (leftTop.y - leftBottom.y) / 14;

        //��ʼ��ÿ��������λ��
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                _chessPos[i, j] = new Vector2
                (
                    leftBottom.x + _gridWidth * i,
                    leftBottom.y + _gridHeight * j
                );
            }
        }

        listPage = new List<GameObject>();

        listPage.Add(_connectPage);
        listPage.Add(_enrollPage);
        listPage.Add(_createRoomPage);
        listPage.Add(_inRoomPage);
    }

    public void Start()
    {
        
    }

    /// <summary>
    /// ����
    /// </summary>
    public Vec2 PlayChess()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //����û���������
        if (Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("ChessBoard")))
        {
            //���������λ��
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    //������������������ľ���(ֻ��x,yƽ�����)
                    float distance = _Distance(hit.point, _chessPos[i, j]);
                    //�����
                    if (distance < (_gridWidth / 2))
                    {
                        return new Vec2(i, j);
                    }
                }
            }
        }

        //δ���������
        return new Vec2(-1, -1);
    }

    /// <summary>
    /// ʵ��������
    /// </summary>
    public void InstChess(Chess chess, Vec2 pos)
    {
        Vector2 vec2 = _chessPos[pos.X, pos.Y];
        //������������:���ӵ�z���겻��������һ���ұ�������������������, ��Ȼ�п��ܻ��������ص��������Ӳ��ɼ���
        Vector3 chessPos = new Vector3(vec2.x, vec2.y, -1);

        if (chess == Chess.Black)
        {
            Instantiate(_blackChess, chessPos, Quaternion.identity);
        }
        else if (chess == Chess.White)
        {
            Instantiate(_whiteChess, chessPos, Quaternion.identity);
        }
    }

    /// <summary>
    /// ��������Vector2�ľ���
    /// </summary>
    private float _Distance(Vector2 a, Vector2 b)
    {
        Vector2 distance = b - a;
        return distance.magnitude;

        //���ɶ��������, Ҳ����ʹ������������magnitude
        //float param = Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.y - b.y, 2);
        //return Mathf.Sqrt(param);
    }

    public void ChangePage(Pages page)
    {
        foreach (var p in listPage)
        {
            p.SetActive(false);
        }

        listPage[(int)page].SetActive(true);
    }

    public void ShowText(string text)
    {
        _reciveText.text = text;
    }
}