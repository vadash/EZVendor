using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using EZVendor.Item;
using EZVendor.Item.Filters;
using EZVendor.Item.Ninja;
using ImGuiNET;

// ReSharper disable ConstantConditionalAccessQualifier
namespace EZVendor
{
    public class EZVendorCore : BaseSettingsPlugin<EZVendorSettings>
    {
        private const int MaxIDTimes = 3;
        private const int MaxVendorTimes = 4;
        private const int MaxSameBases = 4;
        private const string MainCoroutineName = "EZV_Main";
        private const string TwoStoneBase1 = @"Metadata/Items/Rings/Ring12";
        private const string TwoStoneBase2 = @"Metadata/Items/Rings/Ring13";
        private const string TwoStoneBase3 = @"Metadata/Items/Rings/Ring14";
        private IItemFactory _itemFactory;
        private INinjaProvider _ninja;
        private int Latency => (int) (GameController?.IngameState?.CurLatency ?? 50);

        public override bool Initialise()
        {
            Input.RegisterKey(Settings.MainHotkey);
            Input.RegisterKey(Settings.CopyStatsHotkey);
            Input.RegisterKey(Settings.StopHotkey);
            _ninja = new NinjaUniqueProvider(
                Settings.ChaosUniqueCutoff,
                DirectoryFullName,
                Settings.LeagueName2);
            _itemFactory = new ItemFactory(
                GameController,
                _ninja,
                Settings.VendorTransmutes,
                Settings.VendorScraps);
            Task.Run(() =>
            {
                LogMessage("Started loading ninja data", 10);
                _ninja.GetCheapUniques();
                LogMessage("Finished loading ninja data", 10);
            });
            return true;
        }

        public override void DrawSettings()
        {
            ImGui.Text($"Welcome to EZV {Assembly.GetExecutingAssembly().GetName().Version}");
            ImGui.Text("Clicks Tane -> unid -> vendor");
            ImGui.BulletText("Influenced: keep all (use game filter)");
            ImGui.BulletText("Rare rings, amulets, belts, gloves, boots: smart check");
            ImGui.Text("Veiled +1 weight, no defense -1 weight, no speed boots -1 weight");
            ImGui.BulletText("1h: keep +1 gems, temple DD mod");
            ImGui.BulletText("Abyss jewels: keep all T2+ Life / T2+ ES");
            ImGui.BulletText("Jewels: keep all Life% / ES% (corrupt for -1% reserved)");
            ImGui.BulletText("Other rares: vendor for alts");
            ImGui.Text("Avoid selling 5 to 1 recipe, prismatic ring recipe");
            ImGui.BulletText("Uniques: ninja sell cheap");
            ImGui.BulletText("Enchants: keep 10c+ (hard coded list)");
            ImGui.BulletText("6L: keep (except Tabula), 6S: vendor");
            ImGui.BulletText("Transmutes: vendor");
            ImGui.NewLine();
            ImGui.Text("This plugin will sort 95 percent of rare garbage");
            ImGui.Text("Use tiered pricing tabs to auto price and sell rest. Example 1exa -> 50c -> 25c -> vendor");
            ImGui.Text("If non influenced item dropped and plugin tries to vendor it or \r\n" +
                       "it doesnt sell trash item then move this item to player inventory, \r\n" +
                       "mouse over it, press debug key and send me msg");
            ImGui.NewLine();
            ImGui.InputText("League name", ref Settings.LeagueName2, 255);
            base.DrawSettings();
            if (ImGui.Button("Delete ninja cache (after you change settings)"))
            {
                File.Delete(Path.Combine(DirectoryFullName, "ninja.json"));
            }
        }

        public override Job Tick()
        {
            #region start main routine

            if (Settings.MainHotkey.PressedOnce() &&
                Core.ParallelRunner.FindByName(MainCoroutineName) == null)
            {
                LogMessage("[EZV] started");
                StartMainCoroutine();
            }

            if (Settings.StopHotkey.PressedOnce() &&
                Core.ParallelRunner?.FindByName(MainCoroutineName)?.Done(true) == true)
                LogMessage("[EZV] aborted");

            #endregion

            #region debug item mods

            if (Settings.CopyStatsHotkey.PressedOnce())
            {
                var invItem = GetInventoryItem(GameController.IngameState.UIHoverElement.Address);
                var itemComponent = invItem.Item.GetComponent<Mods>();
                var stats = itemComponent.UniqueName;
                try
                {
                    var itemClass = GameController.Files.BaseItemTypes.Translate(invItem.Item.Path).ClassName;
                    stats += " [" + itemClass + "] ";
                }
                catch (Exception e)
                {
                    LogMessage(e.StackTrace, 20);
                }

                stats += Environment.NewLine;

                stats += "Item mods: " + Environment.NewLine;
                try
                {
                    foreach (var itemMod in itemComponent.ItemMods)
                        stats += "name: [" + itemMod.Name + "] " +
                                 "group: [" + itemMod.Group + "] " +
                                 "values: " + itemMod.Value1 + " " + itemMod.Value2 +
                                 Environment.NewLine;
                }
                catch (Exception e)
                {
                    LogMessage(e.StackTrace, 20);
                }

                stats += "Human mods (this one is often broken in PoeHUD): " + Environment.NewLine;
                try
                {
                    foreach (var humanStat in itemComponent.HumanImpStats) stats += humanStat + Environment.NewLine;
                }
                catch (Exception e)
                {
                    LogMessage(e.StackTrace, 20);
                }

                try
                {
                    foreach (var humanStat in itemComponent.HumanStats) stats += humanStat + Environment.NewLine;
                }
                catch (Exception e)
                {
                    LogMessage(e.StackTrace, 20);
                }

                try
                {
                    foreach (var humanStat in itemComponent.HumanCraftedStats) stats += humanStat + Environment.NewLine;
                }
                catch (Exception e)
                {
                    LogMessage(e.StackTrace, 20);
                }

                ImGui.SetClipboardText(stats);
                LogMessage("[EZV] Saved item stats to clipboard. Send it to me if plugin misses it", 20);
            }

            #endregion

            return null;
        }

        private void StartMainCoroutine()
        {
            Core.ParallelRunner?.Run(new Coroutine(MainRoutine(), this, MainCoroutineName));
        }

        private IEnumerator MainRoutine()
        {
            for (var iteration = 0; iteration < MaxIDTimes; iteration++)
            {
                if (Settings.DebugLog) LogMessage("[EZV] OpenNPCTrade");
                yield return OpenNPCTrade();
                if (Settings.DebugLog) LogMessage("[EZV] WaitForOpenInventory");
                yield return WaitForOpenInventory();
                if (Settings.DebugLog) LogMessage("[EZV] GetUnidentifiedItems");
                GetUnidentifiedItems(out var unidList);
                if (Settings.DebugLog) LogMessage("[EZV] Unid");
                yield return DoUnid(unidList);
            }
            for (var iteration = 0; iteration < MaxVendorTimes; iteration++)
            {
                if (Settings.DebugLog) LogMessage("[EZV] OpenNPCTrade");
                yield return OpenNPCTrade();
                if (Settings.DebugLog) LogMessage("[EZV] WaitForOpenInventory");
                yield return WaitForOpenInventory();
                if (Settings.DebugLog) LogMessage("[EZV] AnalyzeIdentifiedItems");
                AnalyzeIdentifiedItems(out var vendorList, out var vendorBases);
                if (Settings.DebugLog) LogMessage("[EZV] RemoveBadVendorRecipes");
                var badVendorRecipes = RemoveBadVendorRecipes(vendorList, vendorBases);
                if (Settings.DebugLog) LogMessage("[EZV] VendorGarbage");
                yield return DoVendorGarbage(vendorList);
                if (Settings.DebugLog) LogMessage("[EZV] ClickSellWindowAcceptButton");
                yield return ClickSellWindowAcceptButton();
                if (Settings.DebugLog) LogMessage("[EZV] WaitForClosedInventory");
                yield return WaitForClosedInventory();
                if (!badVendorRecipes)
                {
                    LogMessage("[EZV] finished");
                    yield break;
                }
            }
            
            LogMessage("[EZV] timeout");
        }

        private IEnumerator WaitForOpenInventory(int timeoutMs = 5000)
        {
            yield return WaitUntil(() => IsInventoryOpened() && IsSellWindowOpened(), timeoutMs);
        }

        private IEnumerator WaitForClosedInventory(int timeoutMs = 5000)
        {
            yield return WaitUntil(() => !IsInventoryOpened() && !IsSellWindowOpened(), timeoutMs);
        }

        private IEnumerator WaitUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            const int waitTickRate = 100;
            MinMax(waitTickRate, ref timeoutMs, 10000);
            for (var i = 0; i < timeoutMs / waitTickRate; i++)
            {
                if (condition())
                {
                    if (Settings.DebugLog) LogMessage($"[EZV] Finished waiting for {condition.Method.Name}");
                    yield return new WaitTime(100 + Latency);
                    yield break;
                }

                if (Settings.DebugLog) LogMessage($"[EZV] Waiting for {condition.Method.Name}");
                yield return new WaitTime(waitTickRate);
            }
        }

        private static void MinMax(int min, ref int value, int max)
        {
            value = Math.Max(min, value);
            value = Math.Min(value, max);
        }

        /// <summary>
        ///     AnalyzeIdentifiedItems
        /// </summary>
        /// <param name="vendorList"></param>
        /// <param name="pathCount"></param>
        private void AnalyzeIdentifiedItems(
            out IList<Tuple<long, NormalInventoryItem>> vendorList,
            out IDictionary<string, int> pathCount)
        {
            vendorList = new List<Tuple<long, NormalInventoryItem>>();
            pathCount = new Dictionary<string, int>();
            try
            {
                foreach (var invItem in GetInventoryItems())
                {
                    var action = _itemFactory.Evaluate(invItem);
                    if (action == Actions.Vendor)
                    {
                        vendorList.Add(new Tuple<long, NormalInventoryItem>(invItem.Address, invItem));
                        var key = invItem.Item.Path;
                        if (pathCount.ContainsKey(key))
                            pathCount[key]++;
                        else
                            pathCount.Add(key, 1);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void GetUnidentifiedItems(
            out IList<Tuple<long, NormalInventoryItem>> unidList)
        {
            try
            {
                var query =
                    from invItem in GetInventoryItems()
                    let item = invItem.Item
                    let modsComponent = item.HasComponent<Mods>() ? item.GetComponent<Mods>() : null
                    where modsComponent?.Identified == false
                    let socketsComponent = item.HasComponent<Sockets>() ? item.GetComponent<Sockets>() : null
                    let sixSockets = socketsComponent?.NumberOfSockets == 6
                    let sixLinks = socketsComponent?.LargestLinkSize == 6
                    where !sixSockets || sixLinks // skip 6S
                    let mapComponent = item.HasComponent<Map>()
                    where !mapComponent // skip maps
                    select new Tuple<long, NormalInventoryItem>(invItem.Address, invItem);
                
                unidList = query.ToList();
            }
            catch (Exception)
            {
                unidList = new List<Tuple<long, NormalInventoryItem>>();
            }
        }

        /// <summary>
        ///     Removes bad vendor recipes like 5 same bases
        /// </summary>
        /// <param name="vendorList"></param>
        /// <param name="basesCount"></param>
        /// <returns>true - removed something</returns>
        private static bool RemoveBadVendorRecipes(
            IList<Tuple<long, NormalInventoryItem>> vendorList,
            IDictionary<string, int> basesCount)
        {
            var removedSomething = false;
            // 5 to 1 recipe
            foreach (var key in basesCount.Keys)
            {
                var value = basesCount[key];
                if (value <= MaxSameBases) continue;
                removedSomething |= Remove(vendorList, key, value - MaxSameBases);
            }

            // prismatic ring
            for (var i = 0; i < 10; i++)
            {
                var count = 0;
                var r1 = vendorList.Count(tuple => tuple.Item2.Item.Path == TwoStoneBase1);
                if (r1 > 0) count++;
                var r2 = vendorList.Count(tuple => tuple.Item2.Item.Path == TwoStoneBase2);
                if (r2 > 0) count++;
                var r3 = vendorList.Count(tuple => tuple.Item2.Item.Path == TwoStoneBase3);
                if (r3 > 0) count++;
                if (count == 3)
                {
                    var key = TwoStoneBase3;
                    if (r1 <= r2 && r1 <= r3) key = TwoStoneBase1;
                    else if (r2 <= r1 && r2 <= r3) key = TwoStoneBase2;
                    removedSomething |= Remove(vendorList, key);
                }
            }

            return removedSomething;
        }

        private static bool Remove(
            IList<Tuple<long, NormalInventoryItem>> list,
            string key,
            int n = int.MaxValue)
        {
            var removedSomething = false;
            var removed = 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Item2.Item.Path == key)
                {
                    list.RemoveAt(i);
                    removed++;
                    removedSomething = true;
                }

                if (removed == n) break;
            }

            return removedSomething;
        }

        /// <summary>
        ///     VendorGarbage
        /// </summary>
        /// <param name="itemList"></param>
        /// <returns></returns>
        private IEnumerator DoVendorGarbage(IList<Tuple<long, NormalInventoryItem>> itemList)
        {
            LogMessage($"[EZV] Want to sell {itemList.Count} items");
            if (itemList.Count == 0) yield break;
            yield return ClickAll(itemList, 3, Keys.ControlKey, MouseButtons.Left);
        }

        private IEnumerator DoUnid(IList<Tuple<long, NormalInventoryItem>> itemList)
        {
            LogMessage($"[EZV] Want to unid {itemList.Count} items");
            if (itemList.Count == 0) yield break;
            var scrollOfWisdom = GetInventoryItem("Metadata/Items/Currency/CurrencyIdentification");
            if (scrollOfWisdom == null)
            {
                LogMessage($"[EZV] No ID scrolls", 20);
                yield break;
            }
            Input.KeyDown(Keys.ShiftKey);
            yield return ClickItem(scrollOfWisdom, MouseButtons.Right);
            yield return ClickAll(itemList, 2, Keys.ShiftKey, MouseButtons.Left);
        }

        /// <summary>
        /// Click all items
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="iterations"></param>
        /// <param name="keyToHold"></param>
        /// <param name="mouseButton"></param>
        /// <returns></returns>
        private IEnumerator ClickAll(
            IList<Tuple<long, NormalInventoryItem>> itemList,
            int iterations,
            Keys keyToHold,
            MouseButtons mouseButton)
        {
            if (itemList.Count == 0) yield break;
            Input.KeyDown(keyToHold);
            for (var step = 1; step <= iterations; step++)
            {
                for (var i = itemList.Count - 1; i >= 0; i--)
                {
                    if (!IsInventoryOpened() || !IsSellWindowOpened()) break;
                    var (initialAddress, invItem) = itemList[i];
                    if (invItem.Item == null ||
                        invItem.Address == 0 ||
                        invItem.Item.Address == 0 ||
                        GetInventoryItem(initialAddress) == null)
                    {
                        itemList.RemoveAt(i);
                        continue;
                    }
                    yield return ClickItem(invItem, mouseButton);
                }
                if (itemList.Count == 0) break;
                yield return new WaitTime(100 + Latency);
            }
            Input.KeyUp(keyToHold);
        }
        
        /// <summary>
        /// Click single item with UI hover logic
        /// </summary>
        /// <param name="invItem"></param>
        /// <param name="mouseButton"></param>
        /// <returns></returns>
        private IEnumerator ClickItem(Element invItem, MouseButtons mouseButton)
        {
            for (var j = 0; j < 25; j++) // 500 + 5 * Latency ms timeout
            {
                if (j % 5 == 0)
                {
                    Input.SetCursorPos(invItem.GetClientRect().ClickRandom());
                    Input.MouseMove();
                }
                yield return new WaitTime(20 + Latency / 5);
                if (GameController.IngameState.UIHoverElement.Address != invItem.Address) continue;
                Input.Click(mouseButton);
                break;
            }
        }

        /// <summary>
        ///     OpenNPCTrade
        /// </summary>
        /// <returns></returns>
        private IEnumerator OpenNPCTrade()
        {
            if (IsInventoryOpened() && IsSellWindowOpened()) yield break;
            const string tanePath = @"Metadata/NPC/League/Metamorphosis/MetamorphosisNPCHideout";
            if (!Settings.AutoOpenTrade) yield break;
            var npc = GameController
                ?.Game
                ?.IngameState
                ?.IngameUi
                ?.ItemsOnGroundLabels
                ?.FirstOrDefault(labelOnGround =>
                    labelOnGround?.Label?.IsVisibleLocal == true &&
                    labelOnGround?.ItemOnGround?.Metadata == tanePath);
            if (npc == null)
            {
                LogMessage("[EZV] Cant find NPC TANE nearby", 20);
                yield break;
            }

            Input.KeyDown(Keys.ControlKey);
            yield return Input.SetCursorPositionSmooth(npc.Label.GetClientRectCache.ClickRandom());
            Input.MouseMove();
            yield return new WaitTime(100 + Latency);
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(100 + Latency);
            Input.KeyUp(Keys.ControlKey);
        }

        /// <summary>
        ///     ClickSellWindowAcceptButton
        /// </summary>
        /// <returns></returns>
        private IEnumerator ClickSellWindowAcceptButton()
        {
            var sellWindows = GameController
                .IngameState
                .IngameUi
                .SellWindow;
            var btn = Settings.AutoClickDebug
                ? sellWindows.CancelButton
                : sellWindows.AcceptButton;
            yield return Input.SetCursorPositionSmooth(btn.GetClientRectCache.ClickRandom());
            yield return new WaitTime(100 + Latency);
            if (!Settings.AutoClickAcceptButton) yield break;
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(100 + Latency);
        }

        private bool IsInventoryOpened()
        {
            return GameController
                .IngameState
                .IngameUi
                .InventoryPanel
                .IsVisible;
        }

        private bool IsSellWindowOpened()
        {
            return GameController
                .IngameState
                .IngameUi
                .SellWindow
                .IsVisible;
        }

        private NormalInventoryItem GetInventoryItem(long address)
        {
            return GetInventoryItems()?.FirstOrDefault(item => item?.Address == address);
        }

        private NormalInventoryItem GetInventoryItem(string path)
        {
            return GetInventoryItems()?.FirstOrDefault(item => item?.Item?.Path?.Contains(path) == true);
        }

        
        private IEnumerable<NormalInventoryItem> GetInventoryItems()
        {
            return GameController
                .IngameState
                .IngameUi
                .InventoryPanel[InventoryIndex.PlayerInventory]
                .VisibleInventoryItems;
        }
    }
}