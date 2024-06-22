// See https://aka.ms/new-console-template for more information

using MasterDistributedPiano;
using MasterDistributedPiano.SuperColliderZeugs;
using MasterDistributedPiano.SuperColliderZeugs.OSCData;

Console.WriteLine("Hello, World!");

GameNetwork network = new GameNetwork("Test");

CommandLineInterface cli = new CommandLineInterface();
GameHandler handler = new GameHandler(network, cli);
cli.Start();

OSCMessage messageOne = new OSCMessage("/keyOn");
messageOne.Append(69);
OSCMessage messageTwo = new OSCMessage("/keyOn");
messageTwo.Append(72);
OSCMessage messageThree = new OSCMessage("/keyOn");
messageThree.Append(76);
network.ReceiveKey(messageOne, null);
network.ReceiveKey(messageTwo, null);
network.ReceiveKey(messageThree, null);

