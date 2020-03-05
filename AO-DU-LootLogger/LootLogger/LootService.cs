using LootLogger.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace LootLogger
{
    public class LootService : ILootService
    {
        private const string url = "";

        private List<Player> players;
        private readonly HttpClient client;

        private DateTime lastUploadDate = DateTime.MinValue;

        public LootService()
        {
            players = new List<Player>();
            client = new HttpClient();
        }

        public void AddLootForPlayer(Loot loot, string playerName)
        {
            Player player = players.FirstOrDefault((Player p) => p.Name == playerName);
            if (player != null)
            {
                player.Loots.Add(loot);
            }
            else
            {
                players.Add(new Player
                {
                    Name = playerName,
                    Loots = new List<Loot>
                    {
                        loot
                    }
                });
            }
        }

        public string CheckString(string s) {
            if (string.IsNullOrEmpty(s))
            {
                string Combatloot = "CombatLoot";
                return Combatloot;
            }
            else {
                return s;
            }
        }
       
        public void SaveLootsToFile(string fileName)
        {
            string formattedFileName = CheckString(fileName);
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "JsonLogs"));
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "FormattedLogs"));
            var jsonfile = Path.Combine(Directory.GetCurrentDirectory(), "JsonLogs", $"{formattedFileName}-{DateTime.Now.ToString("dd-MMM-HH-mm")}.json");
            string content = JsonConvert.SerializeObject(this.players, Formatting.Indented);
            using (var fs = File.Create(jsonfile))
            {
                Byte[] bytes = new UTF8Encoding(true).GetBytes(content);
                fs.Write(bytes, 0, bytes.Length);
            }

            var script = Path.Combine(Directory.GetCurrentDirectory(), "LootLogFormatter.ps1");
            var startInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy unrestricted -File {script} {jsonfile}",
                UseShellExecute = false
            };
            Process.Start(startInfo);
            System.Environment.Exit(0);

        }
    }
}
