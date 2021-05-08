using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace EZVendor
{
    public class EZVendorSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public string LeagueName4 = "Ultimatum";
        
        #region Main
        
        [Menu("Sell garbage hotkey")] 
        public HotkeyNode MainHotkey2 { get; set; } = new HotkeyNode(Keys.F3);        

        [Menu("Chaos Unique Cutoff")]
        public RangeNode<int> ChaosUniqueCutoff2 { get; set; } = new RangeNode<int>(10, 1, 30);

        [Menu("Click TANE to trade")]
        public ToggleNode AutoOpenTrade2 { get; set; } = new ToggleNode(true);

        [Menu("Click sell window accept button")]
        public ToggleNode AutoClickAcceptButton2 { get; set; } = new ToggleNode(true);

        [Menu("Sell ALL influenced gear")]
        public ToggleNode VendorInfluenced2 { get; set; } = new ToggleNode(true);

        [Menu("Sell ALL 6 links")]
        public ToggleNode Sell6Links2 { get; set; } = new ToggleNode(true);
        
        [Menu("Sell transmutes")]
        public ToggleNode VendorTransmutes2 { get; set; } = new ToggleNode(true);

        [Menu("Sell scraps/whetstones")]
        public ToggleNode VendorScraps2 { get; set; } = new ToggleNode(true);

        [Menu("Less garbage (enable after first week of league)")]
        public ToggleNode StricterFiltering2 { get; set; } = new ToggleNode(true);

        [Menu("Sell ALL rares")]
        public ToggleNode VendorAllRares2 { get; set; } = new ToggleNode(true);
        
        #endregion

        #region Other

        [Menu("Stop hotkey")] 
        public HotkeyNode StopHotkey2 { get; set; } = new HotkeyNode(Keys.Space);        

        [Menu("Click cancel button instead (debug)")]
        public ToggleNode AutoClickDebug2 { get; set; } = new ToggleNode(false);

        [Menu("Bypass broken ItemMods component (debug)")]
        public ToggleNode BypassBrokenItemMods2 { get; set; } = new ToggleNode(false);

        [Menu("Extra log (debug)")] 
        public ToggleNode DebugLog2 { get; set; } = new ToggleNode(false);

        [Menu("Delay after mouse move, ms")]
        public RangeNode<int> Delay1AfterMouseMove2 { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Delay after click, ms")]
        public RangeNode<int> Delay2AfterClick2 { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Debug copy inventory item stats under cursor hotkey")]
        public HotkeyNode CopyStatsHotkey2 { get; set; } = new HotkeyNode(Keys.NumPad7);
        
        #endregion
       
    }
}