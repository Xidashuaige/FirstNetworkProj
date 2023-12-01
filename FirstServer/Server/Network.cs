using System;
using Multiplay;

public class Network
{
    /// <summary>
    /// Init Network and Start server
    /// </summary>
    /// <param name="ip">IPv4地址</param>
    public Network(string ip)
    {
        //Register callbacks
        Server.Register(MessageType.HeartBeat, HeartBeat);
        Server.Register(MessageType.Enroll, Enroll);
        Server.Register(MessageType.CreatRoom, CreatRoom);
        Server.Register(MessageType.EnterRoom, EnterRoom);
        Server.Register(MessageType.ExitRoom, ExitRoom);
        Server.Register(MessageType.StartGame, StartGame);
        Server.Register(MessageType.PlayChess, PlayChess);
        Server.Register(MessageType.SendMessage, SendMessage);

        //Start Server
        Server.Start(ip);
    }

    private void HeartBeat(Player player, byte[] data)
    {
        // just for check if server is still runing
        player.Send(MessageType.HeartBeat);
    }

    private void Enroll(Player player, byte[] data)
    {
        Enroll result = new Enroll();

        Enroll receive = NetworkUtils.Deserialize<Enroll>(data);

        Console.WriteLine($"Player {player.Name} Change name to: {receive.Name}");
        //Set player name
        player.Name = receive.Name;

        // send succes message to player
        result.Suc = true;
        result.Name = receive.Name;
        data = NetworkUtils.Serialize(result);
        player.Send(MessageType.Enroll, data);
    }

    private void CreatRoom(Player player, byte[] data)
    {
        CreatRoom result = new CreatRoom();

        CreatRoom receive = NetworkUtils.Deserialize<CreatRoom>(data);

        //Check the player doesn't have room & doesn't hace room with this id
        if (!player.InRoom && !Server.Rooms.ContainsKey(receive.RoomId))
        {
            // Create a new room
            Room room = new Room(receive.RoomId);
            Server.Rooms.Add(room.RoomId, room);
            // Add player to the room
            room.Players.Add(player);
            player.EnterRoom(receive.RoomId);

            Console.WriteLine($"Player {player.Name}: create room with successful!");

            // Send result to client
            result.Suc = true;
            result.RoomId = receive.RoomId;
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.CreatRoom, data);
        }
        else
        {
            Console.WriteLine($"Player {player.Name}: create room faild");
            // Send result to client
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.CreatRoom, data);
        }
    }

    private void EnterRoom(Player player, byte[] data)
    {
        // Result
        EnterRoom result = new EnterRoom();

        EnterRoom receive = NetworkUtils.Deserialize<EnterRoom>(data);

        // Check the player doesn't have room & exist room with this id
        if (!player.InRoom && Server.Rooms.ContainsKey(receive.RoomId))
        {
            Room room = Server.Rooms[receive.RoomId];
            // join as player 
            if (room.Players.Count < Room.MAX_PLAYER_AMOUNT && !room.Players.Contains(player))
            {
                room.Players.Add(player);
                player.EnterRoom(receive.RoomId);

                Console.WriteLine($"player :{player.Name} join room {receive.RoomId} as player");

                // send result to client
                result.RoomId = receive.RoomId;
                result.result = Multiplay.EnterRoom.Result.Player;
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.EnterRoom, data);
            }
            // join as observer
            else if (room.OBs.Count < Room.MAX_OBSERVER_AMOUNT && !room.OBs.Contains(player))
            {
                room.OBs.Add(player);
                player.EnterRoom(receive.RoomId);

                Console.WriteLine($"player {player.Name} join room {receive.RoomId} as observer");

                // send result to client
                result.RoomId = receive.RoomId;
                result.result = Multiplay.EnterRoom.Result.Observer;
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.EnterRoom, data);
            }
            // join room faild 
            else
            {
                Console.WriteLine($"player {player.Name}: join room with successful");

                result.result = Multiplay.EnterRoom.Result.None;
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.EnterRoom, data);
            }
        }
        else
        {
            Console.WriteLine($"Player {player.Name}: join room faild");
            // send result to client
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.EnterRoom, data);
        }
    }

    private void ExitRoom(Player player, byte[] data)
    {
        //result
        ExitRoom result = new ExitRoom();

        ExitRoom receive = NetworkUtils.Deserialize<ExitRoom>(data);

        // check if exist this room
        if (Server.Rooms.ContainsKey(receive.RoomId))
        {
            // check if the player is alredy in the room
            if (Server.Rooms[receive.RoomId].Players.Contains(player) ||
                Server.Rooms[receive.RoomId].OBs.Contains(player))
            {
                result.Suc = true;
                //remove player
                if (Server.Rooms[receive.RoomId].Players.Contains(player))
                {
                    Server.Rooms[receive.RoomId].Players.Remove(player);
                }
                else if (Server.Rooms[receive.RoomId].OBs.Contains(player))
                {
                    Server.Rooms[receive.RoomId].OBs.Remove(player);
                }

                if (Server.Rooms[receive.RoomId].Players.Count == 0)
                {
                    Server.Rooms.Remove(receive.RoomId); // if there is no player, remove the room
                }

                Console.WriteLine($"Player {player.Name}: leave room with successful");

                player.ExitRoom();
                // send result to client
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.ExitRoom, data);
            }
            else
            {
                Console.WriteLine($"Player {player.Name}: Leave room faild");
                // send result to client
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.ExitRoom, data);
            }
        }
        else
        {
            Console.WriteLine($"Player {player.Name}: leave room faild");
            // send result to client
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.ExitRoom, data);
        }
    }

    private void SendMessage(Player player, byte[] data)
    {
        SendMessage receive = NetworkUtils.Deserialize<SendMessage>(data);

        receive.Suc = true;

        if (Server.Rooms.ContainsKey(receive.RoomId))
        {
            data = NetworkUtils.Serialize(receive);

            foreach (var each in Server.Rooms[receive.RoomId].Players)
            {
                each.Send(MessageType.SendMessage, data);
            }
        }
        else
        {
            Console.WriteLine($"Player {player.Name}: send message faild");
            // send result to player
            player.Send(MessageType.SendMessage, data);
        }
    }

    // For Game

    private void StartGame(Player player, byte[] data)
    {
        //result
        StartGame result = new StartGame();

        StartGame receive = NetworkUtils.Deserialize<StartGame>(data);

        // check if the room with this id exist
        if (Server.Rooms.ContainsKey(receive.RoomId))
        {
            //玩家模式开始游戏
            if (Server.Rooms[receive.RoomId].Players.Contains(player) &&
                Server.Rooms[receive.RoomId].Players.Count == Room.MAX_PLAYER_AMOUNT)
            {
                //游戏开始
                Server.Rooms[receive.RoomId].State = Room.RoomState.Gaming;

                Console.WriteLine($"玩家:{player.Name}开始游戏成功");

                //遍历该房间玩家
                foreach (var each in Server.Rooms[receive.RoomId].Players)
                {
                    //开始游戏者先手
                    if (each == player)
                    {
                        result.Suc = true;
                        result.First = true;
                        data = NetworkUtils.Serialize(result);
                        each.Send(MessageType.StartGame, data);
                    }
                    else
                    {
                        result.Suc = true;
                        result.First = false;
                        data = NetworkUtils.Serialize(result);
                        each.Send(MessageType.StartGame, data);
                    }
                }

                //如果有观察者
                if (Server.Rooms[receive.RoomId].OBs.Count > 0)
                {
                    result.Suc = true;
                    result.Watch = true;
                    data = NetworkUtils.Serialize(result);
                    //向观战者发送信息
                    foreach (var each in Server.Rooms[receive.RoomId].OBs)
                    {
                        each.Send(MessageType.StartGame, data);
                    }
                }
            }
            else
            {
                Console.WriteLine($"玩家:{player.Name}开始游戏失败");
                //向玩家发送失败操作结果
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.StartGame, data);
            }
        }
        else
        {
            Console.WriteLine($"玩家:{player.Name}开始游戏失败");
            //向玩家发送失败操作结果
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.StartGame, data);
        }
    }

    private void PlayChess(Player player, byte[] data)
    {
        //结果
        PlayChess result = new PlayChess();

        PlayChess receive = NetworkUtils.Deserialize<PlayChess>(data);

        //逻辑检测(有该房间)
        if (Server.Rooms.ContainsKey(receive.RoomId))
        {
            //该房间中的玩家有资格下棋
            if (Server.Rooms[receive.RoomId].Players.Contains(player) &&
                Server.Rooms[receive.RoomId].State == Room.RoomState.Gaming &&
                receive.Chess == Server.Rooms[receive.RoomId].GamePlay.Turn)
            {
                //判断结果
                Chess chess = Server.Rooms[receive.RoomId].GamePlay.Calculate(receive.X, receive.Y);
                //检测操作:如果游戏结束
                bool over = ChessResult(chess, result);

                if (result.Suc)
                {
                    result.Chess = receive.Chess;
                    result.X = receive.X;
                    result.Y = receive.Y;

                    Console.WriteLine($"玩家:{player.Name}下棋成功");

                    //向该房间中玩家与观察者广播结果
                    data = NetworkUtils.Serialize(result);
                    foreach (var each in Server.Rooms[receive.RoomId].Players)
                    {
                        each.Send(MessageType.PlayChess, data);
                    }
                    foreach (var each in Server.Rooms[receive.RoomId].OBs)
                    {
                        each.Send(MessageType.PlayChess, data);
                    }

                    if (over)
                    {
                        Console.WriteLine("游戏结束,房间关闭");
                        Server.Rooms[receive.RoomId].Close();
                    }
                }
                else
                {
                    Console.WriteLine($"玩家:{player.Name}下棋失败");
                    //向玩家发送失败操作结果
                    data = NetworkUtils.Serialize(result);
                    player.Send(MessageType.PlayChess, data);
                }
            }
            else
            {
                Console.WriteLine($"玩家:{player.Name}下棋失败");
                //向玩家发送失败操作结果
                data = NetworkUtils.Serialize(result);
                player.Send(MessageType.PlayChess, data);
            }
        }
        else
        {
            Console.WriteLine($"玩家:{player.Name}下棋失败");
            //向玩家发送失败操作结果
            data = NetworkUtils.Serialize(result);
            player.Send(MessageType.PlayChess, data);
        }
    }

    private bool ChessResult(Chess chess, PlayChess result)
    {
        bool over = false;
        // succes
        result.Suc = true;
        switch (chess)
        {
            case Chess.Null:
                // faild
                result.Suc = false;
                break;
            case Chess.None:
                result.Challenger = Chess.None;
                break;
            case Chess.Black:
                over = true;
                result.Challenger = Chess.Black;
                break;
            case Chess.White:
                over = true;
                result.Challenger = Chess.White;
                break;
            case Chess.Draw:
                over = true;
                result.Challenger = Chess.Draw;
                break;
        }

        return over;
    }
}