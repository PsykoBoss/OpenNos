﻿/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using OpenNos.Domain;

namespace OpenNos.GameObject
{
    public class MonsterMapItem : MapItem
    {
        #region Instantiation

        public MonsterMapItem(short x, short y, short itemVNum, int amount = 1, long owner = -1) : base(x, y)
        {
            ItemVNum = itemVNum;
            if (amount < 100)
            {
                Amount = (byte)amount;
            }
            GoldAmount = amount;
            Owner = owner;
        }

        #endregion

        #region Properties

        public override byte Amount { get; set; }

        public int GoldAmount { get; set; }

        public override short ItemVNum { get; set; }

        public long? Owner { get; set; }

        #endregion

        #region Methods

        public override ItemInstance GetItemInstance()
        {
            if (_itemInstance == null)
            {
                _itemInstance = Inventory.InstantiateItemInstance(ItemVNum, Owner.Value, Amount);
            }

            return _itemInstance;
        }

        public void Rarify(ClientSession session)
        {
            ItemInstance instance = GetItemInstance();
            if (instance.Item.EquipmentSlot == EquipmentType.Armor || instance.Item.EquipmentSlot == EquipmentType.MainWeapon
                || instance.Item.EquipmentSlot == EquipmentType.SecondaryWeapon)
            {
                if (instance is WearableInstance)
                {
                    ((WearableInstance)instance).RarifyItem(session, RarifyMode.Drop, RarifyProtection.None);
                }
            }
        }

        #endregion
    }
}