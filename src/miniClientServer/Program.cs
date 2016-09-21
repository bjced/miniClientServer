using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{

    public static void Main()
    {
        Console.WriteLine("Welcome, press s for server or anything else for client");
        
        if (Console.ReadLine() == "s")
        {
            StartServer();
        }
        else 
        {
            StartClient();
        }
    }


    static void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("0.0.0.0"), 1234);
        listener.Start();
        Console.WriteLine("Server started");
        Task<TcpClient> client = listener.AcceptTcpClientAsync();
        client.Wait();
        var reader = new StreamReader(client.Result.GetStream(), Encoding.ASCII);
        
        while (true)
        {
            Console.WriteLine(reader.ReadLine());
        }
    }
    
    static void StartClient()
    {
        TcpClient client = new TcpClient();
        client.ConnectAsync("localhost", 1234).Wait();
        NetworkStream stream = client.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true };
        Console.WriteLine("Welcome to dot net core demo 2016, write something to send it to the server side..");
        while (true)
        {
            writer.WriteLine(Console.ReadLine());
        }
    }
}
