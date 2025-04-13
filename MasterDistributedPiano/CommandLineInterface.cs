namespace MasterDistributedPiano;

using System.Linq.Expressions;
using System.Runtime.InteropServices.JavaScript;

public class CommandLineInterface : IUserInput {
    public event StartSignalHandler? OnStartSignal;
    public event MidiHandler? OnMidiSend;
    public event OctaveHandler? OnOctaveConfig;
    
    public void Start() {
        string command = null;
        while (true) {
            Console.WriteLine("CommandLineInterface");
            Console.WriteLine();
            PrintCommands();
            command = Console.ReadLine()!;
            HandleInput(command);
        }
    }

    private void PrintCommands() {
        Console.WriteLine("start - Starts the music");
        Console.WriteLine();
        Console.WriteLine("config {number of Octaves per Client} - Configure the Octaves for all Clients.");
        Console.WriteLine("Example: config 4");
        Console.WriteLine();
        Console.WriteLine(
            "midi {path to midi} {filename for the clients (optional)} - Sends the MIDI file to play to the clients.");
        Console.WriteLine("Example: midi /path/to/file/file.mid");
        Console.WriteLine("Example: midi /path/to/file/file.mid CoolFileName");
        Console.WriteLine();
    }

    private void HandleInput(string input) {
        try {
            string[] parsedInputArr = input.Split(" ");
            string parsedInput = parsedInputArr[0];
            Console.Write("Current Input: ");
            foreach (string currentInput in parsedInputArr) {
                Console.Write(currentInput + ", ");
            }
            Console.WriteLine();
            Console.WriteLine("Parsed Input: " + parsedInput);
            switch (parsedInput) {
                case "start":
                    OnStartSignal?.Invoke();
                    Console.WriteLine("Sending Start...");
                    break;
                case "config":
                    int numOctavesPerClient = Int32.Parse(parsedInputArr[1]);
                    OnOctaveConfig?.Invoke(numOctavesPerClient);
                    Console.WriteLine("Sending Octave Config...");
                    break;
                case "midi":
                    if (parsedInputArr.Length == 2) {
                        string midiFolder = FindMidiFolder();
                        string pathToFile = Path.Combine(midiFolder, parsedInputArr[1]);
                        OnMidiSend?.Invoke(pathToFile);
                        Console.Write("Sending Midi...");
                        break;
                    }

                    if (parsedInputArr.Length == 3) {
                        string midiFolder = FindMidiFolder();
                        string pathToFile = Path.Combine(midiFolder, parsedInputArr[1]);
                        OnMidiSend?.Invoke(pathToFile, parsedInputArr[2]);
                        Console.Write("Sending Midi...");
                        break;
                    }
                    goto default;
                default:
                    Console.WriteLine("Invalid Input");
                    break;
            }
        } catch (Exception e){
            Console.WriteLine("Error: "  + e.Message);
        }
    }
    
    public static string FindMidiFolder()
    {
        string current = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(current))
        {
            string candidate = Path.Combine(current, "MIDIFiles");
            if (Directory.Exists(candidate))
                return candidate;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find 'MIDIFiles' directory.");
    }


    public void UpdateScore(float score) {
        throw new NotImplementedException();
    }
}
