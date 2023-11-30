using System.Net.Sockets;

public class Player
{
    public Socket Socket;

    public string Name;  

    public bool InRoom;  

    public int RoomId;  

    public Player(Socket socket)
    {
        Socket = socket;
        Name = "Player Unknown";
        InRoom = false;
        RoomId = 0;
    }

    /// <summary>
    /// join the room
    /// </summary>
    public void EnterRoom(int roomId)
    {
        InRoom = true;
        RoomId = roomId;
    }

    /// <summary>
    /// leave the room
    /// </summary>
    public void ExitRoom()
    {
        InRoom = false;
    }
}