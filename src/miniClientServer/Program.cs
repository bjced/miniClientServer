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


    //Using the SSDP multicast address and port  here
    //(I know, this will break stuff, bit it is for a good cause :-)
    //Using it since it means that messages will probably be forwarded even with IGMP snooping active
    const string multiCastIp = "239.255.255.250";
    const int MultiCastPortNo = 1900;

    const int TcpPortNo = 1234;
    static string TcpServerIp = "localhost"; 

    

    public static string GetLocalIp()
    {
        var addr = Dns.GetHostAddressesAsync(Dns.GetHostName());
        addr.Wait();
        return (addr.Result.First(a => a.AddressFamily == AddressFamily.InterNetwork).ToString());
    }

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
                    //MultiCastServer();
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
                MultiCastClient();
            }
            else
            {
                Console.WriteLine("invalid argument, try \"client\" or \"server\" (or \"quit\")");
            }
        }
    }

    static void MultiCastServer()
    {
        UdpClient udpclient = new UdpClient();

        IPAddress multicastaddress = IPAddress.Parse(multiCastIp);
        udpclient.JoinMulticastGroup(multicastaddress);
        IPEndPoint remoteep = new IPEndPoint(multicastaddress, MultiCastPortNo);
        Byte[] buffer = Encoding.ASCII.GetBytes(TcpServerIp);
        while(true)
        {
            udpclient.SendAsync(buffer, buffer.Length, remoteep);
            //Console.WriteLine("Sent " + ServerIp);
            Thread.Sleep(1000);
        }
    }

    static void MultiCastClient()
    {
        UdpClient client = new UdpClient();

        client.ExclusiveAddressUse = false;
        IPEndPoint localEp = new IPEndPoint(IPAddress.Any, MultiCastPortNo);

        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.ExclusiveAddressUse = false;
        client.Client.Bind(localEp);

        IPAddress multicastaddress = IPAddress.Parse(multiCastIp);
        client.JoinMulticastGroup(multicastaddress);

        while (true)
        {
            var data = client.ReceiveAsync();
            string strData = Encoding.ASCII.GetString(data.Result.Buffer);
            if (strData.StartsWith("192"))
            {
                Console.WriteLine("Found server at " + strData);
                TcpServerIp = strData;
                return;
            }
            Console.WriteLine(strData);
        }
    }

    static void StartServer()
    {
        TcpServerIp = GetLocalIp();
        Task readTask = Task.Run(() =>
        {
            MultiCastServer();
        });
        
        TcpListener listener = new TcpListener(IPAddress.Parse(TcpServerIp), TcpPortNo);
        Console.WriteLine("Local server started on address " + listener.LocalEndpoint.ToString());

        listener.Start();

        while (true)
        {
            Task<TcpClient> client;
            client = listener.AcceptTcpClientAsync();
            client.Wait();
            Task connectionTask = Task.Run(() =>
            {
                NetworkStream stream = client.Result.GetStream();
                StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

                StreamReader reader = new StreamReader(stream, Encoding.ASCII);
                Console.WriteLine("New connection from " + ((IPEndPoint)client.Result.Client.RemoteEndPoint).Address.ToString());

                writer.WriteLine("Welcome! Please enter your name:");
                string clientName = reader.ReadLine();
                writerList.Add(new ClientWriter(clientName, writer));
                writer.WriteLine("Hello " + clientName);
                writer.WriteLine("There are " + (writerList.Count - 1) + " other clients connected");
                foreach (var item in writerList.Where(x => x.Name != clientName))
                {
                    writer.WriteLine(item.Name);
                }
                writerList.Where(x => x.Name != clientName).ToList().ForEach(x => x.Writer.WriteLine(clientName + " connected"));
                while (true)
                {
                    string inputLine = "";
                    while (inputLine != null)
                    {
                        inputLine = reader.ReadLine();
                        string msg = clientName + " : " + inputLine;

                        //TODO: make threadsafe
                        writerList.Where(x => x.Name != clientName).ToList().ForEach(x => x.Writer.WriteLine(msg));
                        Console.WriteLine(msg);
                    }
                    Console.WriteLine("Server saw disconnect from " + clientName);
                }
            });
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

    static void StartClient()
    {

        Console.WriteLine("Starting client and waiting for server...");
        MultiCastClient();

        int port = TcpPortNo;
        TcpClient client = new TcpClient();
        Task tsk = client.ConnectAsync(TcpServerIp, port);
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
