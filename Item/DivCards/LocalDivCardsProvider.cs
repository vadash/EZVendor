using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EZVendor.Item.DivCards
{
    public class LocalDivCardsProvider : IDivCardsProvider
    {
        private readonly string _settingsLimitedUsername;
        private readonly string _settingsFilterName;

        public LocalDivCardsProvider(
            string settingsLimitedUsername,
            string settingsFilterName
            )
        {
            _settingsLimitedUsername = settingsLimitedUsername;
            _settingsFilterName = settingsFilterName;
            _sellDivCardsList = new Lazy<List<string>>(GetSellDivsListValue);
        }
        
        private readonly Lazy<List<string>> _sellDivCardsList;

        public List<string> GetSellDivCardsList() => _sellDivCardsList.Value;

        private List<string> GetSellDivsListValue()
        {
            if (!ReadLocalFilter(out var allLines)) return null;
            if (!FilterOnlyDivCardsSection(ref allLines)) return null;
            if (!RemoveComments(ref allLines)) return null;
            if (!SplitByGroup(allLines, out var keepDivs, out var sellDivs)) return null;
            if (!LogDebug(keepDivs, sellDivs, out _)) return null;
            return keepDivs.Count > 1 && sellDivs.Count > 1 ? sellDivs : null;
        }
        
        private bool ReadLocalFilter(out List<string> fileContent)
        {
            fileContent = null;
            var path = $@"C:\Users\{_settingsLimitedUsername}\Documents\My Games\Path of Exile\{_settingsFilterName}.filter";
            if (!File.Exists(path)) return false;
            fileContent = File.ReadAllLines(path).ToList();
            return true;
        }
        
        private static bool FilterOnlyDivCardsSection(ref List<string> fileContent)
        {
            var divSectionStartRegex = new Regex(@"^# \[\[.* Divination Cards");
            var divSectionEndRegex = new Regex(@"^# \[\[.*");
            var startId = -1;
            var endId = -1;
            for (var i = 250; i < fileContent.Count; i++) // skip ToC
            {
                var line = fileContent[i];
                if (startId == -1 && divSectionStartRegex.IsMatch(line)) startId = i;
                else if (startId != -1 && endId == -1 && divSectionEndRegex.IsMatch(line)) endId = i;
                else if (startId != -1 && endId != - 1) break;
            }
            fileContent = fileContent.GetRange(startId, endId - startId);
            return fileContent.Count > 50;
        }

        private static bool RemoveComments(ref List<string> fileContent)
        {
            var commentRegex = new Regex(@"^#.*");
            for (var i = fileContent.Count - 1; i >= 0; i--)
            {
                var line = fileContent[i];
                if (commentRegex.IsMatch(line)) fileContent.RemoveAt(i);
            }
            return fileContent.Count > 25;
        }
        
        private static bool SplitByGroup(List<string> fileContent, out List<string> keepDivs, out List<string> sellDivs)
        {
            keepDivs = new List<string>();
            sellDivs = new List<string>();
            var isHide = false;
            var isShow = false;
            foreach (var line in fileContent)
            {
                if (line.StartsWith(@"Show"))
                {
                    isShow = true;
                    isHide = false;
                }
                else if (line.StartsWith(@"Hide"))
                {
                    isHide = true;
                    isShow = false;
                }
                else if (line.Contains(@"BaseType =="))
                {
                    var div = line.Replace(@"BaseType == ", "");
                    var divs = div.Split(new[] { "\" \"" }, StringSplitOptions.None);
                    if (isHide)
                    {
                        sellDivs.AddRange(divs.Select(card => 
                            card.Replace("\"", "").Trim()));
                        isHide = false;
                    }
                    else if (isShow)
                    {
                        keepDivs.AddRange(divs.Select(card =>
                            card.Replace("\"", "").Trim()));
                        isShow = false;
                    }
                    else return false;
                }
            }
            return true;
        }
        
        private static bool LogDebug(IEnumerable<string> keepDivs, IEnumerable<string> sellDivs, out string result)
        {
            result = "good:" + Environment.NewLine;
            result = keepDivs.Aggregate(result, (current, card) => current + card + ",");
            result += Environment.NewLine + "bad:" + Environment.NewLine;
            result = sellDivs.Aggregate(result, (current, card) => current + card + ",");
            return true;
        }
    }
}