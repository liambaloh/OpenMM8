﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.OpenMM8.Scripts.Gameplay.Items
{
    class BaseItem
    {
        public ItemData ItemData;

        public BaseItem(ref ItemData itemData)
        {
            ItemData = itemData;
        }

        virtual public ItemInteractResult InteractWithDoll(Character player)
        {
            return ItemInteractResult.Invalid;
        }

        /*public bool IsEquippable()
        {
            return ItemData.EquipType == EquipType.WeaponOneHanded ||
                ItemData.EquipType == EquipType.WeaponTwoHanded ||
                ItemData.EquipType == EquipType.WeaponDualWield ||
                ItemData.EquipType == EquipType.Wand ||
                ItemData.EquipType == EquipType.Missile ||
                ItemData.EquipType == EquipType.Armor ||
                ItemData.EquipType == EquipType.Shield ||
                ItemData.EquipType == EquipType.Helmet ||
                ItemData.EquipType == EquipType.Belt ||
                ItemData.EquipType == EquipType.Cloak ||
                ItemData.EquipType == EquipType.Gauntlets ||
                ItemData.EquipType == EquipType.Boots ||
                ItemData.EquipType == EquipType.Ring ||
                ItemData.EquipType == EquipType.Amulet;
        }

        public bool IsConsumable()
        {

        }

        public bool IsLearnable()
        {

        }

        public bool IsReadable()
        {

        }

        public bool IsCastable()
        {

        }

        public bool IsInteractibleWithDoll()
        {

        }

        public Vector2Int GetInventorySize()
        {

        }*/
    }
}
