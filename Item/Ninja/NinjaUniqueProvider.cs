using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EZVendor.Item.Ninja
{
    internal class NinjaUniqueProvider : INinjaProvider
    {
        private readonly string _dbName;
        private readonly List<string> _ninjaUniquesUrls;
        private readonly int _uniquePriceChaosCutoff;
        private HashSet<string> _cheapUniques;

        public NinjaUniqueProvider(
            int uniquePriceChaosCutoff,
            string directoryFullName,
            string leagueName)
        {
            _uniquePriceChaosCutoff = uniquePriceChaosCutoff;
            _dbName = Path.Combine(directoryFullName, "ninja.json");
            _ninjaUniquesUrls = new List<string>
            {
                @"https://poe.ninja/api/data/itemoverview?league=" + leagueName +
                @"&type=UniqueJewel&language=en",
                @"https://poe.ninja/api/data/itemoverview?league=" + leagueName +
                @"&type=UniqueFlask&language=en",
                @"https://poe.ninja/api/data/itemoverview?league=" + leagueName +
                @"&type=UniqueWeapon&language=en",
                @"https://poe.ninja/api/data/itemoverview?league=" + leagueName +
                @"&type=UniqueArmour&language=en",
                @"https://poe.ninja/api/data/itemoverview?league=" + leagueName +
                @"&type=UniqueAccessory&language=en"
            };
            Task.Run(UpdateCheapUniques);
        }

        private void UpdateCheapUniques()
        {
            _cheapUniques = LoadDataFromFile(out var databaseAgeHours);
            if (databaseAgeHours <= 24) return;
            if (!GetDataOnline(out var data)) return;
            _cheapUniques = data;
            SaveData(data);
        }

        private HashSet<string> LoadDataFromFile(out double databaseAgeHours)
        {
            try
            {
                if (File.Exists(_dbName))
                {
                    var dif = DateTime.Now - File.GetLastWriteTime(_dbName);
                    databaseAgeHours = dif.TotalHours;
                    var json = File.ReadAllText(_dbName);
                    return JsonConvert.DeserializeObject<HashSet<string>>(json);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            databaseAgeHours = double.MaxValue;
            return new HashSet<string>();
        }

        private bool GetDataOnline(out HashSet<string> onlineData)
        {
            onlineData = new HashSet<string>();
            try
            {
                var result = new List<string>();
                foreach (var url in _ninjaUniquesUrls)
                    using (var webClient = new WebClient())
                    {
                        var json = webClient.DownloadString(url);
                        var jToken = JObject.Parse(json)["lines"];
                        if (jToken == null) return false;
                        foreach (var token in jToken)
                        {
                            if (int.TryParse((string) token?["links"], out var links) &&
                                links >= 5)
                                continue;
                            var chaosValueStr = ((string) token?["chaosValue"])?.Split('.')[0];
                            if (double.TryParse(chaosValueStr, out var chaosValue) &&
                                chaosValue < _uniquePriceChaosCutoff)
                                result.Add((string) token?["name"]);
                        }
                    }

                onlineData = result.ToHashSet();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SaveData(HashSet<string> data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                File.WriteAllText(_dbName, json);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public HashSet<string> GetCheapUniques()
        {
            return _cheapUniques;
        }
    }
}