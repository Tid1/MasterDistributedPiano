namespace MasterDistributedPiano;

using SuperColliderZeugs;

public class GameHandler {
    private GameNetwork network;
    private IUserInput ui;
    private List<SimpleClient> clients = new();
    private float totalScore;
    
    public GameHandler(GameNetwork network, IUserInput ui) {
        this.network = network;
        this.ui = ui;
        RegisterEvents();
    }

    private void RegisterEvents() {
        network.OnReceiveClient += clients.Add;
        network.OnReceiveScore += AddToScore;
        ui.OnStartSignal += network.SendStartMusic;
        ui.OnMidiSend += ParseMidiToByteArray;
        ui.OnOctaveConfig += SendOctaveConfig;
    }

    private void AddToScore(float receivedScore) {
        totalScore += receivedScore;
        ui.UpdateScore(totalScore);
    }

    private void ParseMidiToByteArray(string pathToMidi, string fileName = "midiFile") {
        byte[] midiAsBytes = File.ReadAllBytes(pathToMidi);
        network.SendMidiFile(fileName, midiAsBytes);
    }

    private void SendOctaveConfig(int numOctavesPerClient) {
        int startOctave = 0;
        foreach (SimpleClient client in clients) {
            network.SendOctaveConfig(numOctavesPerClient, startOctave, client.TcpEndPoint);
            startOctave += numOctavesPerClient;
        }
    }
}
