using System;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Models;

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
            if (!Item.HasComponent<Mods>()) return;
            ItemModsComponent = Item.GetComponent<Mods>();
        }

        public abstract Actions Evaluate();
    }
}