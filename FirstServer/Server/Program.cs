using System;

class Program
{
    static void Main(string[] args)
    {
        string ip = NetworkUtils.GetLocalIPv4();
        //ip = "192.168.1.135";
        new Network(ip);

        Console.WriteLine("Server Start!");

        Console.WriteLine($"ip: {ip}");

        Console.ReadKey();
    }
}