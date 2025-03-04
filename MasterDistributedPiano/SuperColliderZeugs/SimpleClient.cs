namespace MasterDistributedPiano.SuperColliderZeugs;

using System.Net;

public class SimpleClient {
     
   
    public string DeviceName { get; set; }
    public IPEndPoint TcpEndPoint { get; set; }
    public IPEndPoint UdpEndPoint { get; set; }
    public DateTime LastResponse { get; set; }
    public TimeSpan RTT { get; set; }
    public DateTime HeartbeatTime { get; set; }
    public int SequenceCounter { get; set; }
    
    public SimpleClient(string deviceName, IPEndPoint tcpEndPoint, IPEndPoint udpEndPoint) {
        this.DeviceName = deviceName;
        this.TcpEndPoint = tcpEndPoint;
        this.UdpEndPoint = udpEndPoint;
        this.LastResponse = DateTime.Now;
    }
}
