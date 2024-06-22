namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Net;
using System.Net.Sockets;
using OSCData;

public sealed class OscUdpClient {
    public event Action<OSCMessage, IPEndPoint>? OnReceive;
    public int BoundPort => boundAddress?.Port ?? throw new InvalidOperationException();
    
    private readonly IPEndPoint localEndpoint;
    private volatile UdpClient? socket;
    private IPEndPoint? boundAddress;

    public OscUdpClient(IPAddress localAddress) {
        localEndpoint = new IPEndPoint(localAddress, 0);
    }
    
    public void Start() {
        if (socket != null) return;
        socket = new UdpClient(localEndpoint);
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
        UdpClient activeSocket = (UdpClient)result.AsyncState!;
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
