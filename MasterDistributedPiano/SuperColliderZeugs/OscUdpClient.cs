namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using OSCData;

public sealed class OscUdpClient {
    public event Action<OSCMessage, IPEndPoint>? OnReceive;
    public int BoundPort => boundAddress?.Port ?? throw new InvalidOperationException();

    private readonly IPEndPoint localEndpoint;
    private volatile UdpClient? socket;
    private IPEndPoint? boundAddress;


    private bool broadcastEnabled;

    public OscUdpClient(IPAddress localAddress, bool broadcastEnabled = false) {
        localEndpoint = new IPEndPoint(localAddress, 0);
        this.broadcastEnabled = broadcastEnabled;
    }

    public void Start() {
        if (socket != null) return;
        socket = new UdpClient(localEndpoint);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            const int SIO_UDP_CONNRESET = -1744830452;
            socket.Client.IOControl(
                (IOControlCode) SIO_UDP_CONNRESET,
                new byte[] {0, 0, 0, 0},
                null
            );
        }

        socket.EnableBroadcast = broadcastEnabled;
        boundAddress = (IPEndPoint?) socket.Client.LocalEndPoint;
        socket.BeginReceive(OnUdpReceive, socket);
    }

    public void Stop() {
        if (socket == null) return;
        socket.Dispose();
        socket = null;
        boundAddress = null;
    }

    public void Send(OSCMessage message, IPEndPoint destination) {
        socket?.Send(message.ToByteArray(), destination);
    }

    private void OnUdpReceive(IAsyncResult result) {
        UdpClient activeSocket = (UdpClient) result.AsyncState!;
        if (socket != activeSocket) return;
        
        IPEndPoint? source = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = activeSocket.EndReceive(result, ref source);
        
        if (source != null && receivedData.Length > 0) {
            OSCMessage message = (OSCMessage) OSCPacket.FromByteArray(receivedData);
            OnReceive?.Invoke(message, source);
        }
        socket?.BeginReceive(OnUdpReceive, result.AsyncState);
    }
}
