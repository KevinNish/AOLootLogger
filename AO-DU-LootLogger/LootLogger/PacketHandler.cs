using LootLogger.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LootLogger
{
    public class PacketHandler
    {
        private ILootService lootService;
        private HttpClient client;
        private const string itemsMappingUrl = "https://gist.githubusercontent.com/KevinNish/812c1d4fe64e959b74b7843d917c2864/raw/757f1477ca2d62d475ccb120fb6c746e27f6d54c/items.txt";
        public static string logTimer = DateTime.UtcNow.ToString("dd-MMM");

        private bool isInitialized;

        private Dictionary<int, string> itemDictionary = new Dictionary<int, string>();

        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public PacketHandler(ILootService lootService)
        {
            this.lootService = lootService;
            client = new HttpClient();
        }

        public async void OnEvent(byte code, Dictionary<byte, object> parameters)
        {
            if (!isInitialized)
            {
                await InitializeAsync();
            }
            if (code == 2)
            {
                return;
            }
            parameters.TryGetValue(252, out object value);
            if (value == null)
            {
                return;
            }
            int result = 0;
            if (int.TryParse(value.ToString(), out result) && result == 252)
            {
                foreach (KeyValuePair<byte, object> parameter in parameters)
                {
                    _ = parameter;
                }
                object value2 = "";
                parameters.TryGetValue(3, out value2);
                if (value2 == null)
                {
                    OnLootPicked(parameters);
                }
            }
        }

        public void OnResponse(byte operationCode, short returnCode, Dictionary<byte, object> parameters)
        {
        }

        public void OnRequest(byte operationCode, Dictionary<byte, object> parameters)
        {
            int result = 0;
            int.TryParse(parameters[253].ToString(), out result);
        }


        private void OnLootPicked(Dictionary<byte, object> parameters)
        {
            try
            {
                string text = parameters[2].ToString();
                string text2 = parameters[5].ToString();
                int num = int.Parse(parameters[4].ToString());
                string text3 = itemDictionary[num];
                string text4 = parameters[1].ToString();
                string text5 = DateTime.UtcNow.ToString();
                Loot loot = new Loot
                {
                    Id = num,
                    ItemName = text3,
                    Quantity = int.Parse(text2.ToString()),
                    PickupTime = DateTime.UtcNow,
                    BodyName = text4,
                    LooterName = text
                };
                if (!loot.IsTrash)
                {
                    lootService.AddLootForPlayer(loot, text);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "log-" + logTimer + ".csv");
                    string value = " + [" + text5 + "] " + text + " has looted " + text3 + " x " + text2 + " from " + text4;
                    string value2 = text5 + ";" + text + ";" + text3 + ";" + text2 + ";" + text4;

                    Console.WriteLine(value);
                    using (StreamWriter streamWriter = File.AppendText(path))
                    {
                        streamWriter.WriteLine(value2);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private async Task InitializeAsync()
        {
            semaphore.Wait();
            try
            {
                if (!isInitialized)
                {
                    Console.WriteLine("Please wait...");
                }
                string[] array = (await (await client.GetAsync(new Uri("https://aolootlog.com/items.txt"))).Content.ReadAsStringAsync()).Split(new string[1]
                {
                    Environment.NewLine
                }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                for (int i = 0; i < array.Length; i++)
                {
                    string[] array2 = array[i].Split(new string[1]
                    {
                        ":"
                    }, StringSplitOptions.None);
                    itemDictionary.Add(int.Parse(array2[0]), array2[1]);
                }
                isInitialized = true;
                if (itemDictionary[1308] == "T3_TRASH")
                {
                    Console.WriteLine("Items loaded.");
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
