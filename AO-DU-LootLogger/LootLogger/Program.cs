using ExitGames.Client.Photon;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LootLogger
{

    public class Program
    {
        static void Main(string[] args)
        {
            ConsoleKeyInfo cki;
            LootLogger logger = null;
            try
            {
                logger = new LootLogger();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }
            while (true)
            {
                cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.S)
                {
                    Console.Write("Enter a file name for the new loot log (hit enter to leave it default, CombatLoot-$Date/Time):  ");
                    string fileNameInput = Console.ReadLine();
                    string fileName = Regex.Replace(fileNameInput, @"\s+", "");
                    Console.WriteLine("Saving all player loot to a new file!");
                    logger?.SaveLootsToFile(fileName);
                    Console.WriteLine("Loots saved !");
                }
                else if (cki.Key == ConsoleKey.U)
                {
                    Console.WriteLine("Discord uploads are disabled, ask Scump how to enable them (You'll need a discord channel API key)");
                    //logger?.UploadLoots();
                }
                else if (cki.Key == ConsoleKey.Escape) {
                    Console.WriteLine("Closing Loot Logger.");
                    System.Environment.Exit(0);
                }
            }
        }
    }

    public class LootLogger
    {

        private PacketHandler _eventHandler;
        private ILootService _lootService;
        private PhotonPacketHandler photonPacketHandler;

        public LootLogger()
        {
            this._lootService = new LootService();
            this._eventHandler = new PacketHandler(this._lootService);
            this.photonPacketHandler = new PhotonPacketHandler(this._eventHandler);

            new Thread(delegate ()
            {
                this.CreateListener();
            }).Start();

            Console.WriteLine(Strings.WelcomeMessage);
        }

        public void SaveLootsToFile(string fileName = "CombatLoot")
        {
            _lootService.SaveLootsToFile(fileName);
        }


        private void CreateListener()
        {
            IList<LivePacketDevice> allLocalMachine = LivePacketDevice.AllLocalMachine;
            if (allLocalMachine.Count == 0)
            {
                return;
            }
            for (int i = 0; i != allLocalMachine.Count; i++)
            {
                LivePacketDevice livePacketDevice = allLocalMachine[i];
                if (livePacketDevice.Description != null)
                {
                    Console.WriteLine("Found device! (" + livePacketDevice.Description + ")");
                }
            }
            foreach (LivePacketDevice selectedDevice in allLocalMachine.ToList())
            {
                if (!selectedDevice.Description.Contains("loopback") && !selectedDevice.Description.Contains("rpcap"))
                {
                    new Thread((ThreadStart)delegate
                    {
                        using (PacketCommunicator packetCommunicator = selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
                        {
                            if (packetCommunicator.DataLink.Kind == DataLinkKind.Ethernet)
                            {
                                using (BerkeleyPacketFilter filter = packetCommunicator.CreateFilter("ip and udp"))
                                {
                                    packetCommunicator.SetFilter(filter);
                                }
                                Console.WriteLine("Capturing on " + selectedDevice.Description + "...");
                                packetCommunicator.ReceivePackets(0, photonPacketHandler.PacketHandler);
                            }
                        }
                    }).Start();
                }
            }
        }
    }
}
