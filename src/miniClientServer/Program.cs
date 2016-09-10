using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    const int PORT_NO = 1234;
    const string SERVER_IP = "127.0.0.1";

    public static void Main()
    {
        StartServerOrClient();
    }

    static void StartServerOrClient()
    {

        Console.WriteLine("Please pick either \"client\" or \"server\"");
        string arg = "";
        while (arg != "quit")
        {
            arg = Console.ReadLine();
            if (arg == "server")
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
            else if (arg == "client")
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
        int port = PORT_NO;
        TcpListener listener = new TcpListener(IPAddress.Parse(SERVER_IP), port);
        listener.Start();
        Task<TcpClient> client;

        client = listener.AcceptTcpClientAsync();
        client.Wait();

        NetworkStream stream = client.Result.GetStream();
        StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
        StreamReader reader = new StreamReader(stream, Encoding.ASCII);

        while (true)
        {
            string inputLine = "";
            while (inputLine != null)
            {
                inputLine = reader.ReadLine();
                writer.WriteLine("Echoing string: " + inputLine);
                Console.WriteLine("Echoing string: " + inputLine);
            }
            Console.WriteLine("Server saw disconnect from client.");
        }
    }

    static void StartClient()
    {

        Console.WriteLine("Starting echo client...");

        int port = PORT_NO;
        TcpClient client = new TcpClient();
        Task tsk = client.ConnectAsync(SERVER_IP, port);
        tsk.Wait();
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

        while (true)
        {
            Console.Write("Enter text to send: ");
            string lineToSend = Console.ReadLine();
            Console.WriteLine("Sending to server: " + lineToSend);
            writer.WriteLine(lineToSend);
            string lineReceived = reader.ReadLine();
            Console.WriteLine("Received from server: " + lineReceived);
        }
    }
}
