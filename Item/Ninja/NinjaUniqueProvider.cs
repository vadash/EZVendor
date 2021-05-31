using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
            Task.Run(() => UpdateCheapUniques());
        }

        private void UpdateCheapUniques()
        {
            _cheap0LUniques = LoadDataFromFile(_db0LName, out var age1);
            _cheap6LUniques = LoadDataFromFile(_db6LName, out var age2);
            if (age1 <= 24 && age2 <= 24) return;
            if (!GetDataOnline(out var data0L, false, _unique0LChaosCutoff)) return;
            if (!GetDataOnline(out var data6L, true, _unique6LChaosCutoff)) return;
            _cheap0LUniques = data0L;
            _cheap6LUniques = data6L;
            SaveData(data0L, _db0LName);
            SaveData(data6L, _db6LName);
        }

        private HashSet<string> LoadDataFromFile(string dbName, out double databaseAgeHours)
        {
            try
            {
                if (File.Exists(_db0LName))
                {
                    var dif = DateTime.Now - File.GetLastWriteTime(_db0LName);
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

        private bool GetDataOnline(out HashSet<string> onlineData, bool only6L, int cutoff)
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
                            if (only6L && (!int.TryParse((string) token?["links"], out var links) || links < 6)) continue;
                            var chaosValueStr = ((string) token?["chaosValue"])?.Split('.')[0];
                            if (double.TryParse(chaosValueStr, out var chaosValue) &&
                                chaosValue <= cutoff)
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