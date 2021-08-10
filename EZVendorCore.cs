using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
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
    internal class MyItem
    {
        internal long? InitialInvItemItemAddress { get; }
        internal NormalInventoryItem InvItem { get; }

        public MyItem(NormalInventoryItem invItem)
        {
            InvItem = invItem;
            InitialInvItemItemAddress = InvItem?.Item?.Address;
        }
    }
    
    public class EZVendorCore : BaseSettingsPlugin<EZVendorSettings>
    {
        private const int MaxIDTimes = 2;
        private const int MaxVendorTimes = 3;
        private const int MaxSameBases = 4;
        private const string MainCoroutineName = "EZV_Main";
        private const string TwoStoneBase1 = @"Metadata/Items/Rings/Ring12";
        private const string TwoStoneBase2 = @"Metadata/Items/Rings/Ring13";
        private const string TwoStoneBase3 = @"Metadata/Items/Rings/Ring14";
        private IItemFactory _itemFactory;
        private INinjaProvider _ninja;
        private int Latency => GameController?.IngameState?.ServerData?.Latency ?? 50;

        public override bool Initialise()
        {
            Input.RegisterKey(Keys.LControlKey);
            Input.RegisterKey(Keys.LShiftKey);
            Input.RegisterKey(Settings.MainHotkey2);
            Input.RegisterKey(Settings.CopyStatsHotkey2);
            Input.RegisterKey(Settings.StopHotkey2);
            _ninja = new NinjaUniqueProvider(
                Settings.Unique0LChaosCutoff2,
                Settings.Unique6LChaosCutoff2,
                DirectoryFullName,
                Settings.LeagueNameExpedition);
            _itemFactory = new ItemFactory(
                GameController,
                _ninja,
                Settings.VendorTransmutes2,
                Settings.VendorScraps2,
                Settings.BypassBrokenItemMods2,
                Settings.VendorInfluenced2,
                Settings.VendorAllRares2,
                Settings.SellNonUnique6Links,
                Settings.SaveVeiledHelmets,
                Settings.SaveEnchantedHelmets
                );
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
            ImGui.BulletText("6L: keep expensive uniques, 6S: vendor");
            ImGui.BulletText("Transmutes: vendor");
            ImGui.NewLine();
            ImGui.Text("This plugin will sort 95 percent of rare garbage");
            ImGui.Text("Use tiered pricing tabs to auto price and sell rest. Example 1exa -> 50c -> 25c -> vendor");
            ImGui.Text("If non influenced item dropped and plugin tries to vendor it or \r\n" +
                       "it doesnt sell trash item then move this item to player inventory, \r\n" +
                       "mouse over it, press debug key and send me msg");
            ImGui.NewLine();
            ImGui.InputText("League name", ref Settings.LeagueNameExpedition, 255);
            base.DrawSettings();
            if (ImGui.Button("Delete ninja cache (after you change settings)"))
            {
                File.Delete(Path.Combine(DirectoryFullName, "ninja0L.json"));
                File.Delete(Path.Combine(DirectoryFullName, "ninja6L.json"));
            }
        }

        public override void ReceiveEvent(string eventId, object args)
        {
            if (!Settings.Enable.Value)
            {
                return;
            }

            switch (eventId)
            {
                case "start_ezv":
                    StartMainCoroutine();
                    break;
            }
        }

        #region Tracking item under cursor

        private readonly Stopwatch _cursorStuckWithGarbageTimer = new Stopwatch();

        private bool IsCursorWithItem()
        {
            if (GameController?.Game?.IsPreGame == true) return false;
            try
            {
                var playerInventories = GameController
                    ?.Game
                    ?.IngameState
                    ?.ServerData
                    ?.PlayerInventories;
                var cursorItems =
                    (from playerInventory in playerInventories
                        select playerInventory?.Inventory
                        into inventory
                        where inventory?.InventType == InventoryTypeE.Cursor
                        select inventory)
                    .FirstOrDefault();
                if (cursorItems?.Items?.Count != 1) return false;
                var cursorItem = cursorItems?.Items?[0];
                return !string.IsNullOrEmpty(cursorItem?.Path);
            }
            catch (Exception) // ok
            {
                return false;
            }
        }

        private void UpdateCursorStuckWithGarbageTimer()
        {
            if (IsCursorWithItem())
                _cursorStuckWithGarbageTimer.Start();
            else if (_cursorStuckWithGarbageTimer.IsRunning) 
                _cursorStuckWithGarbageTimer.Reset();
        }

        #endregion
        
        public override Job Tick()
        {
            #region Stop if we have stuck item under cursor

            UpdateCursorStuckWithGarbageTimer();
            if (_cursorStuckWithGarbageTimer.ElapsedMilliseconds > 5000)
            {
                Core.ParallelRunner?.FindByName(MainCoroutineName)?.Done(true);
                return null;
            }

            #endregion
            
            #region start main routine

            if (Settings.MainHotkey2.PressedOnce())
            {
                StartMainCoroutine();
            }

            if (Settings.StopHotkey2.PressedOnce() &&
                Core.ParallelRunner?.FindByName(MainCoroutineName)?.Done(true) == true)
            {
                LogMessage("[EZV] aborted");
                PublishEvent("ezv_finished", null);
            }

            #endregion

            #region debug item mods

            if (Settings.CopyStatsHotkey2.PressedOnce())
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
                
                try
                {
                    if (itemComponent.ItemRarity == ItemRarity.Unique)
                        stats += $"Internal name: {itemComponent?.UniqueName} " + Environment.NewLine;
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
            if (Core.ParallelRunner.FindByName(MainCoroutineName) != null) return;
            LogMessage("[EZV] started");
            Core.ParallelRunner?.Run(new Coroutine(MainRoutine(), this, MainCoroutineName));
            PublishEvent("ezv_started", null);
        }

        private IEnumerator MainRoutine()
        {
            for (var iteration = 0; iteration < MaxIDTimes; iteration++)
            {
                if (Settings.DebugLog2) LogMessage("[EZV] OpenNPCTrade");
                yield return OpenNPCTrade();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitForOpenInventory");
                yield return WaitForOpenInventory();
                if (Settings.DebugLog2) LogMessage("[EZV] GetUnidentifiedItems");
                GetUnidentifiedItems(out var unidList);
                if (Settings.DebugLog2) LogMessage("[EZV] Unid");
                yield return DoUnid(unidList);
            }
            for (var iteration = 0; iteration < MaxVendorTimes; iteration++)
            {
                if (Settings.DebugLog2) LogMessage("[EZV] OpenNPCTrade");
                yield return OpenNPCTrade();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitForOpenInventory");
                yield return WaitForOpenInventory();

                #region AnalyzeIdentifiedItems

                IList<MyItem> vendorList;
                IDictionary<string, int> vendorBases;
                try
                {
                    if (Settings.DebugLog2) LogMessage("[EZV] AnalyzeIdentifiedItems");
                    AnalyzeIdentifiedItems(out vendorList, out vendorBases);
                }
                catch (Exception e)
                {
                    LogError($"[EZV] AnalyzeIdentifiedItems " + e.StackTrace, 30);
                    yield break;
                }                

                #endregion

                #region RemoveBadVendorRecipes

                var badVendorRecipes = false;
                try
                {
                    if (Settings.DebugLog2) LogMessage("[EZV] RemoveBadVendorRecipes");
                    badVendorRecipes = RemoveBadVendorRecipes(vendorList, vendorBases);
                }
                catch (Exception e)
                {
                    LogError($"[EZV] RemoveBadVendorRecipes " + e.StackTrace, 30);
                }                

                #endregion
               
                if (Settings.DebugLog2) LogMessage("[EZV] VendorGarbage");
                yield return DoVendorGarbage(vendorList);
                if (Settings.DebugLog2) LogMessage("[EZV] ClickSellWindowAcceptButton");
                yield return ClickSellWindowAcceptButton();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitForClosedInventory");
                yield return WaitForClosedInventory();
                if (!badVendorRecipes) break;
            }

            // Make sure keys are not stuck
            Input.KeyUp(Keys.LShiftKey);
            Input.KeyUp(Keys.LControlKey);
            PublishEvent("ezv_finished", null);
            LogMessage("[EZV] finished");
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
                    if (Settings.DebugLog2) LogMessage($"[EZV] Finished waiting for {condition.Method.Name}");
                    yield return new WaitTime(100 + Latency);
                    yield break;
                }

                if (Settings.DebugLog2) LogMessage($"[EZV] Waiting for {condition.Method.Name}");
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
            out IList<MyItem> vendorList,
            out IDictionary<string, int> pathCount)
        {
            vendorList = new List<MyItem>();
            pathCount = new Dictionary<string, int>();
            try
            {
                foreach (var invItem in GetInventoryItems())
                {
                    try
                    {
                        if (invItem.Item.ComponentList == 0 ||
                            invItem.Item.Rarity == MonsterRarity.Error ||
                            !invItem.Item.HasComponent<Base>() ||
                            _itemFactory.Evaluate(invItem) == Actions.Vendor)
                        {
                            vendorList.Add(new MyItem(invItem));
                            var key = invItem.Item.Path;
                            if (pathCount.ContainsKey(key))
                                pathCount[key]++;
                            else
                                pathCount.Add(key, 1);
                        }                   
                    }
                    catch (Exception e)
                    {
                        LogMessage($"[EZV] Found krangled item. Selling " + e.StackTrace, 30);
                        vendorList.Add(new MyItem(invItem));
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void GetUnidentifiedItems(
            out IList<MyItem> unidList)
        {
            try
            {
                var query =
                    from invItem in GetInventoryItems()
                    let item = invItem.Item
                    let modsComponent = item.HasComponent<Mods>() ? item.GetComponent<Mods>() : null
                    where item.Type == EntityType.Error || modsComponent?.Identified == false
                    let socketsComponent = item.HasComponent<Sockets>() ? item.GetComponent<Sockets>() : null
                    let sixSockets = socketsComponent?.NumberOfSockets == 6
                    let sixLinks = socketsComponent?.LargestLinkSize == 6
                    where !sixSockets || sixLinks // skip 6S
                    let mapComponent = item.HasComponent<Map>()
                    where !mapComponent // skip maps
                    select new MyItem(invItem);
                
                unidList = query.ToList();
            }
            catch (Exception)
            {
                unidList = new List<MyItem>();
            }
        }

        /// <summary>
        ///     Removes bad vendor recipes like 5 same bases
        /// </summary>
        /// <param name="vendorList"></param>
        /// <param name="basesCount"></param>
        /// <returns>true - removed something</returns>
        private bool RemoveBadVendorRecipes(
            IList<MyItem> vendorList,
            IDictionary<string, int> basesCount)
        {
            var removedSomething = false;

            try
            {
                // 5 to 1 recipe
                foreach (var key in basesCount.Keys)
                {
                    var value = basesCount[key];
                    if (value <= MaxSameBases) continue;
                    removedSomething |= Remove(vendorList, key, value - MaxSameBases);
                }
            }
            catch (Exception e)
            {
                LogError($"[EZV] RemoveBadVendorRecipes #1 " + e.StackTrace, 30);
            }

            // prismatic ring
            try
            {
                for (var i = 0; i < 10; i++)
                {
                    var count = 0;
                    var r1 = vendorList.Count(myItem => myItem?.InvItem?.Item?.Path == TwoStoneBase1);
                    if (r1 > 0) count++;
                    var r2 = vendorList.Count(myItem => myItem?.InvItem?.Item?.Path == TwoStoneBase2);
                    if (r2 > 0) count++;
                    var r3 = vendorList.Count(myItem => myItem?.InvItem?.Item?.Path == TwoStoneBase3);
                    if (r3 > 0) count++;
                    if (count == 3)
                    {
                        var key = TwoStoneBase3;
                        if (r1 <= r2 && r1 <= r3) key = TwoStoneBase1;
                        else if (r2 <= r1 && r2 <= r3) key = TwoStoneBase2;
                        removedSomething |= Remove(vendorList, key);
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"[EZV] RemoveBadVendorRecipes #2 " + e.StackTrace, 30);
            }
            
            // transmute + 2 amulets
            try
            {
                if (vendorList.Count(myItem => myItem?.InvItem?.Item?.Path.Contains("Amulets") == true) >= 2)
                {
                    removedSomething |= Remove(vendorList, @"Metadata/Items/Currency/CurrencyUpgradeToMagic");
                }
            }
            catch (Exception e)
            {
                LogError($"[EZV] RemoveBadVendorRecipes #3 " + e.StackTrace, 30);
            }

            try
            {
                // weapon + whetstone
                if (vendorList.Count(myItem => myItem?.InvItem?.Item?.Path.Contains("Weapons") == true) >= 1)
                {
                    removedSomething |= Remove(vendorList, @"Metadata/Items/Currency/CurrencyWeaponQuality");
                }
            }
            catch (Exception e)
            {
                LogError($"[EZV] RemoveBadVendorRecipes #4 " + e.StackTrace, 30);
            }

            return removedSomething;
        }

        private static bool Remove(
            IList<MyItem> list,
            string key,
            int n = int.MaxValue)
        {
            var removedSomething = false;
            var removed = 0;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].InvItem.Item.Path == key)
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
        private IEnumerator DoVendorGarbage(IList<MyItem> itemList)
        {
            if (!itemList.Any()) yield break;
            LogMessage($"[EZV] Want to sell {itemList.Count} items");
            yield return ClickAll(itemList, 4, Keys.LControlKey, MouseButtons.Left);
            Input.KeyUp(Keys.LControlKey);
        }

        private IEnumerator DoUnid(IList<MyItem> itemList)
        {
            if (!itemList.Any()) yield break;
            LogMessage($"[EZV] Want to unid {itemList.Count} items");
            var scrollOfWisdom = GetInventoryItem("Metadata/Items/Currency/CurrencyIdentification");
            if (scrollOfWisdom == null)
            {
                LogMessage($"[EZV] No ID scrolls", 20);
                yield break;
            }
            Input.KeyDown(Keys.LShiftKey);
            yield return ClickItem(scrollOfWisdom, MouseButtons.Right);
            yield return ClickAll(itemList, 3, Keys.LShiftKey, MouseButtons.Left);
            Input.KeyUp(Keys.LShiftKey);
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
            IList<MyItem> itemList,
            int iterations,
            Keys keyToHold,
            MouseButtons mouseButton)
        {
            if (!itemList.Any()) yield break;
            Input.KeyDown(keyToHold);
            for (var step = 1; step <= iterations; step++)
            {
                for (var i = itemList.Count - 1; i >= 0; i--)
                {
                    if (!IsInventoryOpened() || !IsSellWindowOpened()) break;
                    var invItem = itemList?[i].InvItem;
                    if (invItem?.Item?.Type == EntityType.Error)
                    {
                        LogError($"[EZV] Selling krangled item", 30);
                        yield return ClickItem(invItem, mouseButton);
                    }
                    if (GetServerItem(itemList?[i].InitialInvItemItemAddress) == null)
                    {
                        itemList.RemoveAt(i);
                        continue;
                    }
                    yield return ClickItem(invItem, mouseButton);
                }
                if (!itemList.Any()) break;
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
            LogMessage($"[EZV] ClickItem ", 30);
            for (var j = 0; j < 10; j++) // timeout = 10 x DelayAfterMouseMove
            {
                if (!invItem.GetClientRectCache.Intersects(GetPlayerInventory().GetClientRectCache)) yield break;
                yield return Input.SetCursorPositionSmooth(invItem.GetClientRect().ClickRandom());
                yield return new WaitTime(Settings.Delay1AfterMouseMove2);
                if (GameController.IngameState.UIHoverElement.Address > 0 &&
                    GameController.IngameState.UIHoverElement.Address == invItem.Address)
                {
                    Input.Click(mouseButton);
                    yield return new WaitTime(Settings.Delay2AfterClick2);
                    break;
                }
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
            if (!Settings.AutoOpenTrade2) yield break;
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

            Input.KeyDown(Keys.LControlKey);
            yield return Input.SetCursorPositionSmooth(npc.Label.GetClientRectCache.ClickRandom());
            yield return new WaitTime(100 + Latency);
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(100 + Latency);
            Input.KeyUp(Keys.LControlKey);
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
            var btn = Settings.AutoClickDebug2
                ? sellWindows.CancelButton
                : sellWindows.AcceptButton;
            yield return Input.SetCursorPositionSmooth(btn.GetClientRectCache.ClickRandom());
            yield return new WaitTime(100 + Latency);
            if (!Settings.AutoClickAcceptButton2) yield break;
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(100 + Latency);
        }

        private bool IsInventoryOpened() =>
            GameController
                ?.IngameState
                ?.IngameUi
                ?.InventoryPanel
                ?.IsVisible == true;

        private bool IsSellWindowOpened() =>
            GameController
                ?.IngameState
                ?.IngameUi
                ?.SellWindow
                ?.IsVisible == true ||
            GameController
                ?.IngameState
                ?.IngameUi
                ?.TradeWindow
                ?.IsVisible == true;

        private NormalInventoryItem GetInventoryItem(long address) => 
            GetInventoryItems()
                ?.FirstOrDefault(item =>
                    item?.Address == address);

        private NormalInventoryItem GetInventoryItem(string path) => 
            GetInventoryItems()
                ?.FirstOrDefault(item =>
                    item?.Item?.Path == path);

        private Inventory GetPlayerInventory() => 
            GameController
                ?.IngameState
                ?.IngameUi
                ?.InventoryPanel?[InventoryIndex.PlayerInventory];

        private IEnumerable<NormalInventoryItem> GetInventoryItems() => 
            GetPlayerInventory()
                ?.VisibleInventoryItems;

        private Entity GetServerItem(long? address) => 
            GetServerItems()
                ?.FirstOrDefault(item =>
                    item?.Address == address);

        private IEnumerable<Entity> GetServerItems() =>
            GameController
                ?.IngameState
                ?.ServerData
                ?.PlayerInventories?[0]
                ?.Inventory
                ?.Items;
    }
}