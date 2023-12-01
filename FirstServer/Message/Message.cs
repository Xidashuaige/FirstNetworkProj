using System;

namespace Multiplay
{
    /// <summary>
    /// Chess message type
    /// </summary>
    public enum Chess
    {
        None, 
        Black,
        White,

        // for check winner
        Draw, // draw
        Null, // have a noresult
    }

    /// <summary>
    /// General message type
    /// </summary>
    public enum MessageType
    {
        // Server & special message
        None,         
        HeartBeat,    

        // Player message
        Enroll,      
        CreatRoom,    
        EnterRoom,   
        ExitRoom,    
        StartGame,   
        PlayChess,    
        SendMessage,  
    }

    [Serializable]
    public class Enroll
    {
        public string Name;

        public bool Suc;  
    }

    [Serializable]
    public class CreatRoom
    {
        public int RoomId;

        public bool Suc;  
    }

    [Serializable]
    public class EnterRoom
    {
        public int RoomId;     

        public Result result; 
        public enum Result
        {
            None,
            Player,
            Observer,
        }
    }

    [Serializable]
    public class ExitRoom
    {
        public int RoomId; 

        public bool Suc;    
    }

    [Serializable]
    public class StartGame
    {
        public int RoomId;         

        public bool Suc;            
        public bool First;           
        public bool Watch;         
    }

    [Serializable]
    public class PlayChess
    {
        public int RoomId;       
        public Chess Chess;    
        public int X;            
        public int Y;            

        public bool Suc;        
        public Chess Challenger; 
    }

    [Serializable]
    public class SendMessage
    {
        public int RoomId;      
        public string Message;  
        public string Owner;   
        public bool Suc;        
    }
}