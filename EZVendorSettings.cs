using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace EZVendor
{
    public class EZVendorSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new(false);

        public string LeagueNameArchnemesis = "";
        
        #region Main
        
        [Menu("Sell garbage hotkey")] 
        public HotkeyNode MainHotkey2 { get; set; } = new HotkeyNode(Keys.F3);        

        [Menu("Chaos not 6L Unique Cutoff")]
        public RangeNode<int> Unique0LChaosCutoff2 { get; set; } = new RangeNode<int>(5, 5, 30);
        
        [Menu("Chaos 6L Unique Cutoff")]
        public RangeNode<int> Unique6LChaosCutoff2 { get; set; } = new RangeNode<int>(20, 20, 50);
        
        [Menu("Click LILI to trade")]
        public ToggleNode AutoOpenTrade2 { get; set; } = new ToggleNode(true);

        [Menu("Click sell window accept button")]
        public ToggleNode AutoClickAcceptButton2 { get; set; } = new ToggleNode(true);

        [Menu("Sell ALL influenced gear")]
        public ToggleNode VendorInfluenced2 { get; set; } = new ToggleNode(true);
       
        [Menu("Save veiled helmets (+1 skele craft)")]
        public ToggleNode SaveVeiledHelmets { get; set; } = new ToggleNode(false);
        
        [Menu("Save top enchanted helmets")]
        public ToggleNode SaveEnchantedHelmets { get; set; } = new ToggleNode(false);
        
        #endregion

        #region Other

        [Menu("Open inventory hotkey")] 
        public HotkeyNode OpenInventoryStopHotkey { get; set; } = new HotkeyNode(Keys.I);
        
        [Menu("Close all UI hotkey")] 
        public HotkeyNode CloseAllUiHotkey { get; set; } = new HotkeyNode(Keys.End);   
        
        [Menu("Stop hotkey")] 
        public HotkeyNode StopHotkey2 { get; set; } = new HotkeyNode(Keys.Space);        

        [Menu("Bypass broken ItemMods component (debug)")]
        public ToggleNode BypassBrokenItemMods21052022 { get; set; } = new ToggleNode(false);

        [Menu("Extra log (debug)")] 
        public ToggleNode DebugLog2 { get; set; } = new ToggleNode(false);

        [Menu("Delay after mouse move, ms")]
        public RangeNode<int> Delay1AfterMouseMove2 { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Delay after click, ms")]
        public RangeNode<int> Delay2AfterClick2 { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Debug copy inventory item stats under cursor hotkey")]
        public HotkeyNode CopyStatsHotkey2 { get; set; } = new HotkeyNode(Keys.NumPad7);
        
        #endregion

        #region Div cards
        
        public string LimitedUsername = "liza";
        
        public string FilterName = "Blight";

        #endregion
    }
}