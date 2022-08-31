using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
using EZVendor.Item.DivCards;
using EZVendor.Item.Filters;
using EZVendor.Item.Ninja;
using ImGuiNET;
#pragma warning disable CS0618

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
        private IDivCardsProvider _divCardsProvider;
        private int Latency => GameController?.IngameState?.ServerData?.Latency ?? 50;
        private bool _init;

        private void Init()
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
                Settings.LeagueNameArchnemesis);
            _divCardsProvider = new LocalDivCardsProvider(
                Settings.LimitedUsername,
                Settings.FilterName
            );
            _itemFactory = new ItemFactory(
                GameController,
                _ninja,
                _divCardsProvider,
                Settings.BypassBrokenItemMods21052022,
                Settings.VendorInfluenced2,
                Settings.VendorFractured,
                Settings.SellRares6L
            );
            _init = true;
        }

        public override void DrawSettings()
        {
            try
            {
                if (!_init)
                {
                    ImGui.Text($"Loading...");
                    return;
                }
                ImGui.Text($"Welcome to EZV {Assembly.GetExecutingAssembly().GetName().Version}");

                try
                {
                    var cheapDivCards = _divCardsProvider?.GetSellDivCardsList()?.Count;
                    var zeroLinks = _ninja?.GetCheap0LUniques()?.Count(); 
                    var sixLinks = _ninja?.GetCheap6LUniques()?.Count(); 
                
                    ImGui.Text(cheapDivCards > 0
                        ? $"Loaded {cheapDivCards} div cards to sell"
                        : "CANT LOAD LOOT FILTER. Check offline filter name");
               
                    ImGui.Text(zeroLinks > 0
                        ? $"Loaded {zeroLinks} <6L cheap uniques"
                        : "CANT LOAD <6L UNIQUE LIST");

                    ImGui.Text($"Loaded {sixLinks} =6L cheap uniques");
                }
                catch (Exception)
                {
                    ImGui.Text($"EXCEPTION DURING LOADING FILTERS. DO NO USE !!!");
                }
                
                ImGui.InputText("League name", ref Settings.LeagueNameArchnemesis, 255);
                base.DrawSettings();
                ImGui.InputText("Poe username", ref Settings.LimitedUsername, 255);
                ImGui.InputText("Filter name", ref Settings.FilterName, 255);
                if (ImGui.Button("Delete ninja cache (after you change settings)"))
                {
                    File.Delete(Path.Combine(DirectoryFullName, "ninja0L.json"));
                    File.Delete(Path.Combine(DirectoryFullName, "ninja6L.json"));
                    RestartHud();
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void RestartHud()
        {
            var exeDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\..\..";
            var exeFile = exeDirectory + @"\Loader.exe";
            Process.Start("cmd.exe", $"/c taskkill /im Loader.exe & timeout 4 & taskkill /f /im Loader.exe & timeout 1 & start /d {exeDirectory} {exeFile}");
        }

        public override void ReceiveEvent(string eventId, object args)
        {
            if (!Settings.Enable.Value || !_init) return;

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
            #region Lazy init

            if (!_init && GameController.InGame)
            {
                var leagueName = GameController?.IngameState?.ServerData?.League;
                if (leagueName?.Length is >= 4 and <= 64) Settings.LeagueNameArchnemesis = leagueName;
                Init();
            }            

            #endregion
            
            #region Stop if we have stuck item under cursor

            UpdateCursorStuckWithGarbageTimer();
            if (_cursorStuckWithGarbageTimer.ElapsedMilliseconds > 5000)
            {
                Core.ParallelRunner?.FindByName(MainCoroutineName)?.Done(true);
                return null;
            }

            #endregion
            
            #region Start main routine

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

            #region Debug item mods

            if (Settings.CopyStatsHotkey2.PressedOnce())
            {
                var invItem = GetInventoryItem(GameController?.IngameState?.UIHoverElement?.Address);
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
            Core.ParallelRunner?.Run(new Coroutine(MainRoutine(), this, MainCoroutineName));
            LogMessage("[EZV] started");
            PublishEvent("ezv_started", null);
        }

        private IEnumerator MainRoutine()
        {
            #region Open inventory to update items

            if (Settings.DebugLog2) LogMessage($"[EZV] Detected {GetInventoryItems()?.Count()} items in inventory", 15);
            if (GetInventoryItems()?.Count() is null or <= 1)
            {
                if (Settings.DebugLog2) LogMessage("[EZV] OpenInventory");
                yield return OpenInventory();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitUntilItemsUpdated");
                yield return WaitUntilItemsUpdated();
                if (Settings.DebugLog2) LogMessage($"[EZV] Detected {GetInventoryItems()?.Count()} items in inventory", 15);
            }

            #endregion

            // If we need to check fractured mod this item need to be mouse over
            if (!Settings.VendorFractured)
            {
                GetUnverifiedItems(out var mouseOverList);
                if (mouseOverList.Any())
                {
                    if (Settings.DebugLog2) LogMessage("[EZV] OpenNPCTrade");
                    yield return OpenNPCTrade();
                    if (Settings.DebugLog2) LogMessage("[EZV] WaitForOpenInventory");
                    yield return WaitForOpenInventory();
                    if (Settings.DebugLog2) LogMessage("[EZV] RefreshItemCache");
                    yield return RefreshCache(mouseOverList);
                }
            }
            for (var iteration = 0; iteration < (Settings.AutoClickAcceptButton2 ? MaxIDTimes : 1); iteration++)
            {
                GetUnidentifiedItems(out var unidList);
                if (!unidList.Any()) continue;
                if (Settings.DebugLog2) LogMessage("[EZV] OpenNPCTrade");
                yield return OpenNPCTrade();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitForOpenInventory");
                yield return WaitForOpenInventory();
                if (Settings.DebugLog2) LogMessage("[EZV] Unid");
                yield return DoUnid(unidList);
            }
            for (var iteration = 0; iteration < (Settings.AutoClickAcceptButton2 ? MaxVendorTimes : 1); iteration++)
            {
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

                try
                {
                    if (Settings.DebugLog2) LogMessage("[EZV] RemoveBadVendorRecipes");
                    RemoveBadVendorRecipes(vendorList, vendorBases);
                }
                catch (Exception e)
                {
                    LogError($"[EZV] RemoveBadVendorRecipes " + e.StackTrace, 30);
                }                

                #endregion
               
                if (!vendorList.Any()) continue;
                if (Settings.DebugLog2) LogMessage("[EZV] OpenNPCTrade");
                yield return OpenNPCTrade();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitForOpenInventory");
                yield return WaitForOpenInventory();
                if (Settings.DebugLog2) LogMessage("[EZV] VendorGarbage");
                yield return DoVendorGarbage(vendorList);
                if (Settings.DebugLog2) LogMessage("[EZV] ClickSellWindowAcceptButton");
                yield return ClickSellWindowAcceptButton();
                if (Settings.DebugLog2) LogMessage("[EZV] WaitForClosedInventory");
                yield return WaitForSellWindowClosed();
            }

            if (IsInventoryOpened()) yield return CloseAllUi();
            // Make sure keys are not stuck
            Input.KeyUp(Keys.LShiftKey);
            Input.KeyUp(Keys.LControlKey);
            LogMessage("[EZV] finished");
            PublishEvent("ezv_finished", null);
        }

        private IEnumerator OpenInventory()
        {
            yield return Input.KeyPress(Settings.OpenInventoryStopHotkey);
            yield return new WaitTime(100 + Latency);
        }
        
        private IEnumerator CloseAllUi()
        {
            yield return Input.KeyPress(Settings.CloseAllUiHotkey);
            yield return new WaitTime(100 + Latency);
        }
        
        private IEnumerator WaitForOpenInventory(int timeoutMs = 5000)
        {
            yield return WaitUntil(() => IsInventoryOpened() && IsSellWindowOpened(), timeoutMs);
        }
        
        private IEnumerator WaitUntilItemsUpdated(int timeoutMs = 5000)
        {
            yield return WaitUntil(() => IsInventoryOpened() && IsItemsUpdated(), timeoutMs);
        }

        private IEnumerator WaitForSellWindowClosed(int timeoutMs = 5000)
        {
            yield return WaitUntil(() => !IsSellWindowOpened(), timeoutMs);
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
                        if (invItem.Item.Rarity == MonsterRarity.Error ||
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
                        //vendorList.Add(new MyItem(invItem));
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
        /// Some items need to be mouse over to get all the info
        /// </summary>
        /// <param name="mouseOverList"></param>
        private void GetUnverifiedItems(
            out IList<MyItem> mouseOverList)
        {
            try
            {
                var query =
                    from invItem in GetInventoryItems()
                    let item = invItem.Item
                    let modsComponent = item.HasComponent<Mods>() ? item.GetComponent<Mods>() : null
                    where item.Type == EntityType.Error || modsComponent?.ItemRarity is ItemRarity.Magic or ItemRarity.Rare
                    select new MyItem(invItem);
                
                mouseOverList = query.ToList();
            }
            catch (Exception)
            {
                mouseOverList = new List<MyItem>();
            }
        }
        
        private IEnumerator RefreshCache(IList<MyItem> itemList)
        {
            if (!itemList.Any()) yield break;
            LogMessage($"[EZV] Want to refresh cache for {itemList.Count} items");
            yield return ClickAll(itemList, 1, Keys.None, MouseButtons.Right);
        }
        
        /// <summary>
        ///     Removes bad vendor recipes like 5 same bases
        /// </summary>
        /// <param name="vendorList"></param>
        /// <param name="basesCount"></param>
        /// <returns>true - removed something</returns>
        // ReSharper disable once UnusedMethodReturnValue.Local
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
            const string liliPath = @"Metadata/NPC/Epilogue/LillyHideout";
            if (!Settings.AutoOpenTrade2) yield break;
            var npc = GameController
                ?.Game
                ?.IngameState
                ?.IngameUi
                ?.ItemsOnGroundLabels
                ?.FirstOrDefault(labelOnGround =>
                    labelOnGround?.Label?.IsVisibleLocal == true &&
                    labelOnGround?.ItemOnGround?.Metadata == liliPath);
            if (npc == null)
            {
                LogMessage("[EZV] Cant find NPC LILI nearby", 20);
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
            var btn = GetSellWindowUi()?.GetChildAtIndex(4)?.GetChildAtIndex(5)?.GetChildAtIndex(0);
            if (btn == null) yield break;
            yield return Input.SetCursorPositionSmooth(btn.GetClientRectCache.ClickRandom());
            yield return new WaitTime(100 + Latency);
            if (!Settings.AutoClickAcceptButton2) yield break;
            Input.Click(MouseButtons.Left);
            yield return new WaitTime(100 + Latency);
        }

        private bool IsItemsUpdated() => GetInventoryItems()?.Count() > 0;
        
        private bool IsInventoryOpened() =>
            GameController
                ?.IngameState
                ?.IngameUi
                ?.InventoryPanel
                ?.IsVisible == true;

        private IEnumerable<Element> GetVisibleUi()
        {
            var elements = GameController?.IngameState?.IngameUi?.Children?.Where(
                x =>
                    x is {Address: > 0, IsValid: true, IsVisible: true, IsVisibleLocal: true});
            return elements ?? new List<Element>();
        }
        
        private Element GetSellWindowUi()
        {
            #region HUD check

            try
            {
                var uiElement = GameController?.IngameState?.IngameUi?.SellWindowHideout;
                if (uiElement?.IsValid == true &&
                    uiElement?.Width > 0 &&
                    uiElement?.Height > 0 &&
                    uiElement?.ChildCount == 5 &&
                    uiElement?.AcceptButton != null &&
                    uiElement?.CancelButton != null)
                    return uiElement;
            }
            catch (Exception)
            {
                // ignored
            }
            
            try
            {
                var uiElement = GameController?.IngameState?.IngameUi?.SellWindow;
                if (uiElement?.IsValid == true &&
                    uiElement?.Width > 0 &&
                    uiElement?.Height > 0 &&
                    uiElement?.ChildCount == 5 &&
                    uiElement?.AcceptButton != null &&
                    uiElement?.CancelButton != null)
                    return uiElement;
            }
            catch (Exception)
            {
                // ignored
            }

            #endregion

            #region Alternative check

            try
            {
                var uiElement = GetVisibleUi()?.FirstOrDefault(
                    x =>
                        x?.ChildCount == 5 &&
                        (x?.GetChildAtIndex(0)?.ChildCount == 3 &&
                         x?.GetChildAtIndex(1)?.ChildCount == 1 &&
                         x?.GetChildAtIndex(3)?.ChildCount == 0) ||
                        x?.GetChildAtIndex(4)?.GetChildAtIndex(3)?.Text == @"Your Offer");
                return uiElement;
            }
            catch (Exception)
            {
                return new Element();
            }

            #endregion
        }

        private bool IsSellWindowOpened() =>
            GetSellWindowUi()?.IsVisible == true;

        private NormalInventoryItem GetInventoryItem(long? address) => 
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