using System;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        string ip = NetworkUtils.GetLocalIPv4();
        ip = "192.168.1.135";
        new Network(ip);

        Console.WriteLine("服务器已启动!");
        Console.WriteLine($"ip地址为:{Server.GetServerIP()}");

        Console.ReadKey();
    }
}