namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using OSCData;

public sealed class OscDiscoveryClient {
    private static readonly IPEndPoint DISCOVERY_ENDPOINT = new IPEndPoint(IPAddress.Broadcast, 50001);
    private volatile UdpClient? socket;
    private byte[] discoveryInfo = Array.Empty<byte>();
    private Thread? beaconThread;
    private readonly object lockObj = new();

    public void Start(OSCMessage discoveryInfo) {
        lock (lockObj) {
            if (socket != null) return;
            this.discoveryInfo = discoveryInfo.ToByteArray();
            socket = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            socket.EnableBroadcast = true;

            beaconThread = new Thread(SendBeaconSignal);
            beaconThread.Start();
        }
    }

    public void Stop() {
        lock (lockObj) {
            if (socket == null) return;
            UdpClient activeSocket = socket;
            socket = null;
            beaconThread!.Interrupt();
            beaconThread!.Join();
            activeSocket.Dispose();
        }
    }

    private void SendBeaconSignal() {
        try {
            while (true) {
                UdpClient? active = socket;
                if (active == null) break;
                active.Send(discoveryInfo, discoveryInfo.Length, DISCOVERY_ENDPOINT);
                Thread.Sleep(1000);
            }
        } catch(Exception e) {
            Console.Error.WriteLine(e);
        }
    }
}
