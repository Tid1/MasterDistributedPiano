namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Net;
using System.Net.Sockets;
using OSCData;

public delegate void ReceiveClientHandler(SimpleClient client);
public delegate void ScoreHandler(float score);

public class GameNetwork {
    public event ReceiveClientHandler OnReceiveClient;
    public event ScoreHandler OnReceiveScore;

    private List<SimpleClient> clients = new();
    private static readonly IPEndPoint SUPERCOLLIDER_ENDPOINT = new IPEndPoint(IPAddress.Loopback, 57120); //25440
    
    private readonly OscDiscoveryClient discovery = new OscDiscoveryClient();
    private readonly OscUdpClient oscClient = new OscUdpClient(IPAddress.Any);
    private readonly OscUdpClient sc = new OscUdpClient(IPAddress.Loopback);
    private readonly OscTcpServer server = new OscTcpServer(IPAddress.Any);
    private readonly Dictionary<string, Action<OSCMessage, IPEndPoint>> routes = new();
    private readonly string lobbyName;

    public GameNetwork(string lobbyName) {
        this.lobbyName = lobbyName;
        SetUpSockets();
        routes.Add("/keyOn", ReceiveKey);
        routes.Add("/keyOff", ReceiveKey);
        routes.Add("/join", ReceiveJoin);
        routes.Add("/point", ReceivePoints);
        
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
    }

    private OSCMessage CreateDiscoveryInformation() {
        OSCMessage message = new OSCMessage("/discovery/promotion");
        
        message.Append(oscClient.BoundPort);
        message.Append(server.BoundPort); //TODO in Unity einfügen damit richtig gelesen wird
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
    
    public void SendStartMusic(long time) {
        OSCMessage message = new OSCMessage("/start");
        message.Append(time);

        server.Send(message);
    }

    public void SendOctaveConfig(int numOfOctaves, int startOctave, IPEndPoint endPoint) {
        OSCMessage message = new OSCMessage("/config");
        message.Append(numOfOctaves);
        message.Append(startOctave);

        server.Send(message, endPoint);
    }
    
    private void ReceiveJoin(OSCMessage message, IPEndPoint tcpEndPoint) {
        string deviceName = (string) message.Data[0];
        int udpPort = (int) message.Data[1];

        IPEndPoint udpEndpoint = new IPEndPoint(tcpEndPoint.Address, udpPort);
        SimpleClient client = new SimpleClient(deviceName, tcpEndPoint, udpEndpoint);

        clients.Add(client);
        Console.WriteLine("Join Received: " + deviceName + " " + udpEndpoint);
        OnReceiveClient?.Invoke(client);
    }
    
    private void ReceivePoints(OSCMessage message, IPEndPoint endPoint) {
        float score = (float) message.Data[0];
        OnReceiveScore?.Invoke(score);
    }

    public void ReceiveKey(OSCMessage message, IPEndPoint endPoint) {
        //TODO check ob Client in Lobby. Vllt andere Stelle?
        sc.Send(message, SUPERCOLLIDER_ENDPOINT);
    }
    
}
