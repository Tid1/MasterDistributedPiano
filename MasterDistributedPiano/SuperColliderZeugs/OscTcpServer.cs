namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using OSCData;

//TODO Heartbeat implementieren auf Clientseite maybe?
public class OscTcpServer {
    public event Action OnStopListening;
    public event Action<OSCMessage, IPEndPoint> OnMessageReceived;

    public int BoundPort => boundAddress?.Port ?? throw new InvalidOperationException();

    private volatile bool listening;

    private TcpListener listener;
    private readonly IPEndPoint localEndpoint;
    private IPEndPoint? boundAddress;

    //private Thread heartbeatThread;

    private List<OscTcpConnection> clients = new();

    private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CHECK_TIME = TimeSpan.FromSeconds(5);

    public OscTcpServer(IPAddress localAddress) {
        localEndpoint = new IPEndPoint(localAddress, 0);
        listener = new TcpListener(localEndpoint);
    }

    public void Start() {
        listener.Start();
        this.listening = true;
        boundAddress = (IPEndPoint?) listener.Server.LocalEndPoint;
        // heartbeatThread = new Thread(CheckHeartbeat);
        // heartbeatThread.Start();
        listener.BeginAcceptTcpClient(ConnectionRequestHandler, listener);
    }

    public void Stop() {
        listener.Stop();
        this.listening = false;
        /* heartbeatThread.Interrupt();
         heartbeatThread.Join();*/
        List<OscTcpConnection> clientCopy;

        lock (clients) {
            clientCopy = new(clients);
            clients.Clear();
        }

        foreach (var connection in clientCopy) {
            connection.responseThread.Interrupt();
            connection.responseThread.Join();
        }

        OnStopListening?.Invoke();
    }

    public void Send(OSCMessage message) {
        byte[] messageAsBytes = message.ToByteArray();
        Span<byte> dataLength = stackalloc byte[sizeof(int)];
        List<OscTcpConnection> clientsCopy;
        lock (clients) clientsCopy = new(clients);
        foreach (var oscTcpClient in clientsCopy) {
            NetworkStream stream = oscTcpClient.client.GetStream();
            BinaryPrimitives.WriteInt32BigEndian(dataLength, messageAsBytes.Length);
            stream.Write(dataLength);
            stream.Write(messageAsBytes);
        }
    }

    public void Send(OSCMessage message, IPEndPoint endPoint) {
        byte[] messageAsBytes = message.ToByteArray();
        Span<byte> dataLength = stackalloc byte[sizeof(int)];
        List<OscTcpConnection> clientsCopy;
        lock (clients) clientsCopy = new(clients);
        OscTcpConnection? oscTcpClient = clientsCopy.Find(x => Equals(x.client.Client.RemoteEndPoint, endPoint));
        if (oscTcpClient == null) return;
        NetworkStream? stream = oscTcpClient?.client.GetStream();
        BinaryPrimitives.WriteInt32BigEndian(dataLength, messageAsBytes.Length);
        stream?.Write(dataLength);
        stream?.Write(messageAsBytes);
    }

    public void Disconnect(IPEndPoint client) {
        lock (clients) {
            OscTcpConnection? tcpClient = clients.Find(c => Equals(c.client.Client.RemoteEndPoint, client));
            tcpClient?.client.Close();
        }
    }

    public void ConnectionRequestHandler(IAsyncResult result) {
        listener.BeginAcceptTcpClient(ConnectionRequestHandler, listener);
        TcpClient client = listener.EndAcceptTcpClient(result);
        OscTcpConnection connection = new OscTcpConnection();

        connection.client = client;
        //connection.lastResponse = DateTime.Now;
        connection.responseThread = new Thread(() => {
            try {
                ReceiveMessage(connection);
            } catch (Exception e) {
                lock (clients) {
                    clients.Remove(connection);
                }

                Console.WriteLine(e);
            }
        });
        connection.responseThread.Start();

        lock (clients) {
            clients.Add(connection);
        }

        Console.WriteLine("Client added from Endpoint: " + client.Client.RemoteEndPoint);

        //OnConnectionRequest?.Invoke(new OscTcpClient(client));
    }

    private void ReceiveMessage(OscTcpConnection connection) {
        NetworkStream stream = connection.client.GetStream();
        Span<byte> messageLengthArr = stackalloc byte[sizeof(int)];
        IPEndPoint? clientEndpoint = (IPEndPoint?) connection.client.Client.RemoteEndPoint;
        Console.WriteLine("Starting to receive messages from " + connection.client.Client.RemoteEndPoint);

        while (listening) {
            Console.WriteLine("Listening...");
            int numReadBytes = stream.Read(messageLengthArr);
            Console.WriteLine("Read " + numReadBytes + " bytes");

            int messageLength = BinaryPrimitives.ReadInt32BigEndian(messageLengthArr);
            Console.Write("Message length: " + messageLength);
            byte[] data = new byte[messageLength];
            int numBytes = stream.Read(data, 0, messageLength);

            if (numBytes <= 0) {
                connection.client.Close();
                continue;
            }
            
            OSCMessage receivedMessage = (OSCMessage) OSCPacket.FromByteArray(data);

            Console.WriteLine("Received Message from: " + connection.client.Client.RemoteEndPoint);
            OnMessageReceived?.Invoke(receivedMessage, clientEndpoint!);
        }
    }

    /* private void CheckHeartbeat() {
         OSCMessage message = new OSCMessage("/heartbeat");
         byte[] messageAsBytes = message.ToByteArray();
         Span<byte> messageLength = stackalloc byte[sizeof(int)];
         BinaryPrimitives.WriteInt32BigEndian(messageLength, messageAsBytes.Length);

         while (listening) {
             List<OscTcpConnection> unavailableClients = new();
             DateTime current = DateTime.Now;

             lock (clients) {
                 foreach (var connection in clients) {
                     lock (connection) {
                         if (current - connection.lastResponse >= TIMEOUT) {
                             unavailableClients.Add(connection);
                             continue;
                         }

                         if (current - connection.heartbeatTime >= CHECK_TIME) {
                             NetworkStream stream = connection.client.GetStream();

                             stream.Write(messageLength);
                             stream.Write(messageAsBytes);
                             connection.heartbeatTime = current;
                         }
                     }
                 }

                 foreach (var unavailableClient in unavailableClients) {
                     clients.Remove(unavailableClient);
                 }
             }

             Thread.Sleep(1000);
         }
     }*/


    private class OscTcpConnection {
        public TcpClient client;
        public Thread responseThread;

        //Falls client.GetStream() Probleme bereitet dann NetworkStream hier Cachen 
    }
}
