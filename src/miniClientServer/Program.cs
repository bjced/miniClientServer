using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    const int PortNo = 1234;
    //const string SERVER_IP = "127.0.0.1";
       
    const string ServerIp = "192.168.0.100"; //use localhost or the ip of your server

    public static void Main()
    {
        Console.WriteLine("Please pick either \"server\" (s) or \"client\" (c)");
        string arg = "";
        while (arg != "q")
        {
            arg = Console.ReadKey().KeyChar.ToString();
            Console.WriteLine();
            if (arg == "s")
            {
                Console.WriteLine("Starting echo server...");
                try
                {
                    StartServer();
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed starting server, already started?");
                    throw;
                }
            }
            else if (arg == "c")
            {
                Console.WriteLine("Starting client...");
                StartClient();
            }
            else
            {
                Console.WriteLine("invalid argument, try \"client\" or \"server\" (or \"quit\")");
            }
        }
    }

    static void StartServer()
    {
        TcpListener listener = new TcpListener(IPAddress.Parse(ServerIp), PortNo);
        Console.WriteLine("Local server started on address " + listener.LocalEndpoint.ToString());
        listener.Start();

        while (true)
        {
            Task<TcpClient> client;
            client = listener.AcceptTcpClientAsync();
            client.Wait();
            ThreadPool.QueueUserWorkItem(ClientHandlerThread, client);
        }
    }
    
    class ClientWriter
    {
        public readonly StreamWriter Writer;
        public readonly string Name;

        public ClientWriter(string name, StreamWriter writer)
        {
            Name = name;
            Writer = writer;
        }
    }

    static List<ClientWriter> writerList = new List<ClientWriter>();

    static void ClientHandlerThread(object obj)
    {
        var client = (Task<TcpClient>)obj;
        NetworkStream stream = client.Result.GetStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
        
        StreamReader reader = new StreamReader(stream, Encoding.ASCII);
        Console.WriteLine("New connection from " + ((IPEndPoint)client.Result.Client.RemoteEndPoint).Address.ToString());

        writer.WriteLine("Welcome! Please enter your name:");
        string clientName = reader.ReadLine();
        writerList.Add(new ClientWriter(clientName, writer));
        writer.WriteLine("Hello " + clientName);
        writer.WriteLine("There are "+ writerList.Count + " clients connected");
        foreach (var item in writerList)
        {
            writer.WriteLine(item.Name);
        }

        while (true)
        {
            string inputLine = "";
            while (inputLine != null)
            {
                inputLine = reader.ReadLine();
                string msg = clientName + " : " + inputLine;

                //TODO: make threadsafe
                writerList.Where(x=>x.Name != clientName).ToList().ForEach(x => x.Writer.WriteLine(msg));
                Console.WriteLine(msg);
            }
            Console.WriteLine("Server saw disconnect from " + clientName);
        }
    }

    static void StartClient()
    {

        Console.WriteLine("Starting client...");

        int port = PortNo;
        TcpClient client = new TcpClient();
        Task tsk = client.ConnectAsync(ServerIp, port);
        tsk.Wait();
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        //Setup name
        Console.WriteLine(reader.ReadLine());
        string name = Console.ReadLine();
        writer.WriteLine(name);

        Task readTask = Task.Run(() =>
        {
            while (true)
            {
                string lineReceived = reader.ReadLine();
                //clear name for incoming message
                Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r"); 
                Console.WriteLine(lineReceived);
                Console.Write(name + ": ");
            }
        });

        while (true)
        {
            Console.Write(name+": ");
            string lineToSend = Console.ReadLine();
            //Console.WriteLine("Sending to server: " + lineToSend);
            writer.WriteLine(lineToSend);

        }
    }
}
