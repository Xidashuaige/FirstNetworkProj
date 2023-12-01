using System;
using UnityEngine;
using Multiplay;

/// <summary>
/// һ����Ϸ�ͻ���ֻ�ܴ���һ���������
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    //����
    private NetworkPlayer() { }
    public static NetworkPlayer Instance { get; private set; }

    [HideInInspector]
    public Chess Chess;                     //��������
    [HideInInspector]
    public int RoomId = 0;                  //�������
    [HideInInspector]
    public bool Playing = false;            //������Ϸ
    [HideInInspector]
    public string Name;                     //����

    public Action<int> OnRoomIdChange;      //����ID�ı�
    public Action<bool> OnPlayingChange;    //��Ϸ״̬�ı�
    public Action<Chess> OnStartGame;       //��ʼ��Ϸ
    public Action<string> OnNameChange;     //���ָı�

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        OnRoomIdChange += (roomId) => RoomId = roomId;

        OnPlayingChange += (playing) => Playing = playing;

        OnStartGame += (chess) => Chess = chess;

        OnNameChange += (name) => Name = name;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Playing)
        {
            Network.Instance.PlayChessRequest(RoomId);
        }
    }
}