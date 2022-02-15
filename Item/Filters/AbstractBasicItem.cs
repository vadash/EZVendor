using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Enums;
#pragma warning disable CS0618

// ReSharper disable MemberCanBePrivate.Global
namespace EZVendor.Item.Filters
{
    internal abstract class AbstractBasicItem : IEvaluate
    {
        protected readonly BaseItemType BaseItemType;
        protected readonly GameController GameController;
        protected readonly Entity Item;
        protected readonly Base ItemBaseComponent;
        protected readonly Mods ItemModsComponent;
        protected readonly NormalInventoryItem NormalInventoryItem;
        protected readonly Random Random;
        protected readonly bool Veiled;
        protected readonly ItemRarity ItemRarity;

        protected AbstractBasicItem(
            GameController gameController,
            NormalInventoryItem normalInventoryItem)
        {
            Random = new Random();
            GameController = gameController;
            NormalInventoryItem = normalInventoryItem;
            Item = normalInventoryItem.Item;
            BaseItemType = GameController.Files.BaseItemTypes.Translate(normalInventoryItem.Item.Path);
            ItemBaseComponent = Item.GetComponent<Base>();
            if (Item.HasComponent<Mods>())
            {
                ItemModsComponent = Item.GetComponent<Mods>();
                Veiled = GetModByGroup("Veiled") > 0;
                ItemRarity = ItemModsComponent.ItemRarity;
            }
        }

        public abstract Actions Evaluate();
        
        private int GetModByGroup(string partialModGroup)
        {
            var total = 0;
            try
            {
                total = ItemModsComponent.ItemMods
                    .Where(mod => mod.Group.ToLower().Contains(partialModGroup.ToLower()))
                    .Sum(mod => mod.Value1);
            }
            catch (Exception)
            {
                // ignored
            }

            return Math.Abs(total);
        }
    }
}