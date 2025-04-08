namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using OSCData;

public delegate void ReceiveClientHandler(SimpleClient client);
public delegate void ScoreHandler(float score);

public class GameNetwork {
    public event ReceiveClientHandler OnReceiveClient;

    private List<SimpleClient> clients = new();
    private static readonly IPEndPoint SUPERCOLLIDER_ENDPOINT = new IPEndPoint(IPAddress.Loopback, 57120); //25440
    
    private readonly OscDiscoveryClient discovery = new OscDiscoveryClient();
    private readonly OscUdpClient oscClient = new OscUdpClient(IPAddress.Any, true);
    private readonly OscUdpClient sc = new OscUdpClient(IPAddress.Loopback);
    private readonly OscTcpServer server = new OscTcpServer(IPAddress.Any);
    
    private readonly Dictionary<string, Action<OSCMessage, IPEndPoint>> routes = new();
    
    private readonly string lobbyName;

    private volatile bool listening;
    private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CHECK_TIME = TimeSpan.FromSeconds(3);

    private Thread rttThread;

    public GameNetwork(string lobbyName) {
        this.lobbyName = lobbyName;
        SetUpSockets();
        
        routes.Add("/keyOn", ReceiveKey);
        routes.Add("/keyOff", ReceiveKey);
        routes.Add("/join", ReceiveJoin);
        routes.Add("/rtt/response", ReceiveRTTResponse);
        
        /*
        routes.Add("/start", ReceiveStartMusic);
        routes.Add("/config", ReceiveOctaveConfig);
        routes.Add("/discovery/response", ReceiveDiscoveryResponse);
        routes.Add("/discovery/request", ReceiveDiscoveryRequest);
        routes.Add("/join", ReceiveJoin);
        */
    }

    private void SetUpSockets() {
        oscClient.OnReceive += OnOSCReceive;
        server.OnMessageReceived += OnOSCReceive;
        
        sc.Start();
        oscClient.Start();
        server.Start();
        discovery.Start(CreateDiscoveryInformation());
        listening = true;
        rttThread = new Thread(CheckRTT);
        rttThread.Start();
    }

    private OSCMessage CreateDiscoveryInformation() {
        OSCMessage message = new OSCMessage("/discovery/promotion");
        
        message.Append(oscClient.BoundPort);
        message.Append(server.BoundPort);
        message.Append(lobbyName);

        return message;
    }
    
    private void OnOSCReceive(OSCMessage message, IPEndPoint endPoint) {
        if (routes.TryGetValue(message.Address, out Action<OSCMessage, IPEndPoint>? action)) {
            action.Invoke(message, endPoint);
        }
    }

    public void SendMidiFile(string fileName, byte[] data) {
        OSCMessage message = new OSCMessage("/midi/init");
        message.Append(fileName);
        message.Append(data);

        server.Send(message);
    }
    
    public void SendStartMusic() {
        OSCMessage message = new OSCMessage("/start");
        server.Send(message);
    }

    public void SendOctaveConfig(int numOfOctaves, int startOctave, IPEndPoint endPoint) {
        OSCMessage message = new OSCMessage("/config");
        message.Append(numOfOctaves);
        message.Append(startOctave);

        server.Send(message, endPoint);
    }

    private void SendRTTRequest(long rtt, int sequenceCounter, IPEndPoint client) {
        OSCMessage message = new OSCMessage("/rtt/request");
        message.Append(rtt);
        message.Append(sequenceCounter);
        
        oscClient.Send(message, client);
    }

    private void SendRTTRequest(SimpleClient client) {
        client.HeartbeatTime = DateTime.Now;
        SendRTTRequest(client.RTT.Ticks, ++client.SequenceCounter, client.UdpEndPoint);
    }
    
    private void ReceiveJoin(OSCMessage message, IPEndPoint tcpEndPoint) {
        string deviceName = (string) message.Data[0];
        int udpPort = (int) message.Data[1];

        IPEndPoint udpEndpoint = new IPEndPoint(tcpEndPoint.Address, udpPort);
        SimpleClient client = new SimpleClient(deviceName, tcpEndPoint, udpEndpoint);
        
        clients.Add(client);
        Console.WriteLine("Join Received: " + deviceName + " " + udpEndpoint);
        OnReceiveClient?.Invoke(client);
        lock (client) SendRTTRequest(client);
    }

    public void ReceiveKey(OSCMessage message, IPEndPoint endPoint) {
        Console.WriteLine("Received Key");
        sc.Send(message, SUPERCOLLIDER_ENDPOINT);
    }
    
    private void ReceiveRTTResponse(OSCMessage message, IPEndPoint endPoint) {
        int sequenceCounter = (int)message.Data[0];
        SimpleClient? client;
        lock (clients) client = clients.Find(c => c.UdpEndPoint.Equals(endPoint));
        if (client != null && sequenceCounter == client.SequenceCounter) {
            DateTime current = DateTime.Now;
            client.LastResponse = current;
            TimeSpan rtt = current - client.HeartbeatTime;
            client.RTT = rtt;
        }
    }
    
    private void CheckRTT() {
        while (listening) {
            DateTime current = DateTime.Now;

            lock (clients) {
                foreach (var client in clients) {
                    lock (client) {
                        if (current - client.LastResponse >= TIMEOUT) {
                            server.Disconnect(client.TcpEndPoint);
                            continue;
                        }

                        if (current - client.HeartbeatTime >= CHECK_TIME) {
                            SendRTTRequest(client);
                        }
                    }
                }
            }

            Thread.Sleep(1000);
        }
    }
}
