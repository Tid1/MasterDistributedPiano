namespace MasterDistributedPiano;

public delegate void StartSignalHandler(long time);
public delegate void MidiHandler(string pathToFile, string fileName = "midiFile");
public delegate void OctaveHandler(int numOctavesPerClient);

public interface IUserInput {
    event StartSignalHandler OnStartSignal;
    event MidiHandler OnMidiSend;
    event OctaveHandler OnOctaveConfig;
    void UpdateScore(float score);
}
