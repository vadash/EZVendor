using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ExileCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EZVendor.Item.Ninja
{
    internal class NinjaUniqueProvider : INinjaProvider
    {
        private readonly List<string> _ninjaUniquesUrls;
        private readonly string _db0LName;
        private readonly string _db6LName;
        private readonly int _unique0LChaosCutoff;
        private readonly int _unique6LChaosCutoff;
        private HashSet<string> _cheap0LUniques;
        private HashSet<string> _cheap6LUniques;

        public NinjaUniqueProvider(
            int unique0LChaosCutoff,
            int unique6LChaosCutoff,
            string directoryFullName,
            string leagueName)
        {
            _unique0LChaosCutoff = unique0LChaosCutoff;
            _unique6LChaosCutoff = unique6LChaosCutoff;
            _db0LName = Path.Combine(directoryFullName, "ninja0L.json");
            _db6LName = Path.Combine(directoryFullName, "ninja6L.json");
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
            Task.Run(UpdateCheapUniquesAsync);
        }

        private async Task UpdateCheapUniquesAsync()
        {
            _cheap0LUniques = await GetDataOnlineAsync(false, _unique0LChaosCutoff) ?? LoadDataFromFile(_db0LName, out _);
            _cheap6LUniques = await GetDataOnlineAsync(true, _unique6LChaosCutoff) ?? LoadDataFromFile(_db6LName, out _);
            SaveData(_cheap0LUniques, _db0LName);
            DebugWindow.LogMsg($"[EZV] Loaded {_cheap0LUniques.Count} < 6L uniques. Cutoff {_unique0LChaosCutoff}");
            SaveData(_cheap6LUniques, _db6LName);
            DebugWindow.LogMsg($"[EZV] Loaded {_cheap6LUniques.Count} 6L uniques. Cutoff {_unique6LChaosCutoff}");
        }


        private static HashSet<string> LoadDataFromFile(string dbName, out double databaseAgeHours)
        {
            try
            {
                if (File.Exists(dbName))
                {
                    var dif = DateTime.Now - File.GetLastWriteTime(dbName);
                    databaseAgeHours = dif.TotalHours;
                    var json = File.ReadAllText(dbName);
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

        private async Task<HashSet<string>> GetDataOnlineAsync(bool only6L, int cutoff)
        {
            var onlineData = new HashSet<string>();
            try
            {
                var result = new List<string>();
                using var httpClient = new HttpClient();
                foreach (var url in _ninjaUniquesUrls)
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
                    var jToken = JObject.Parse(json)["lines"];
                    if (jToken == null) return onlineData;
                    foreach (var token in jToken)
                    {
                        if (only6L && (!int.TryParse((string)token?["links"], out var links) || links < 6)) continue;
                        var chaosValueStr = ((string)token?["chaosValue"])?.Split('.')[0];
                        if (double.TryParse(chaosValueStr, out var chaosValue) && chaosValue <= cutoff)
                        {
                            result.Add((string)token?["name"]);
                        }
                    }
                }
                onlineData = result.ToHashSet();
                return onlineData;
            }
            catch (Exception)
            {
                return onlineData;
            }
        }


        private static void SaveData(HashSet<string> data, string filename)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                File.WriteAllText(filename, json);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public IEnumerable<string> GetCheap0LUniques() => _cheap0LUniques;

        public IEnumerable<string> GetCheap6LUniques() => _cheap6LUniques;
    }
}