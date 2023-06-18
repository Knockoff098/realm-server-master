﻿using RotMG.Common;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Linq;

namespace RotMG.Game.Entities
{
    public partial class Player
    {
        private const float ContainerMinimumDistance = 2f;

        private const int MaxSlotsWithoutBackpack = 12;
        private const int MaxSlotsWithBackpack = MaxSlotsWithoutBackpack + 8;

        private const byte HealthPotionSlotId = 254;
        private const byte MagicPotionSlotId = 255;

        public const int HealthPotionItemType = 2594;
        public const int MagicPotionItemType = 2595;

        private static byte[] InvalidInvSwap = GameServer.InvResult(1);
        private static byte[] ValidInvSwap = GameServer.InvResult(0);

        public int[] Inventory { get; set; }
        public int[] ItemDatas { get; set; }

        public void InitInventory(CharacterModel character)
        {
            Inventory = character.Inventory.ToArray();
            ItemDatas = character.ItemDatas.ToArray();
            UpdateInventory();
        }

        public void RecalculateEquipBonuses()
        {
            for (var i = 0; i < 8; i++)
                Boosts[i] = 0;

            for (var i = 0; i < 4; i++)
            {
                if (Inventory[i] == -1)
                    continue;

                var item = Resources.Type2Item[(ushort)Inventory[i]];
                foreach (var s in item.StatBoosts)
                    Boosts[s.Key] += s.Value;

                var data = ItemDatas[i];
                if (data == -1)
                    continue;

                Boosts[0] += (int)ItemDesc.GetStat(data, ItemData.MaxHP, 5);
                Boosts[1] += (int)ItemDesc.GetStat(data, ItemData.MaxMP, 5);
                Boosts[2] += (int)ItemDesc.GetStat(data, ItemData.Attack, 1);
                Boosts[3] += (int)ItemDesc.GetStat(data, ItemData.Defense, 1);
                Boosts[4] += (int)ItemDesc.GetStat(data, ItemData.Speed, 1);
                Boosts[5] += (int)ItemDesc.GetStat(data, ItemData.Dexterity, 1);
                Boosts[6] += (int)ItemDesc.GetStat(data, ItemData.Vitality, 1);
                Boosts[7] += (int)ItemDesc.GetStat(data, ItemData.Wisdom, 1);
            }

            UpdateStats();
        }

        public ItemDesc GetItem(int index)
        {
#if DEBUG
            if (index < 0 || index > (HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack))
                throw new Exception("GetItem index out of bounds");
#endif
            if (Inventory[index] == -1)
                return null;
            return Resources.Type2Item[(ushort)Inventory[index]];
        }

        public bool GiveItem(ushort type)
        {
            var slot = GetFreeInventorySlot();
            if (slot == -1)
                return false;
            Inventory[slot] = type;
            UpdateInventorySlot(slot);
            return true;
        }

        public int GetFreeInventorySlot()
        {
            var maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            for (var i = 4; i < maxSlots; i++)
                if (Inventory[i] == -1)
                    return i;
            return -1;
        }

        public int GetTotalFreeInventorySlots()
        {
            var count = 0;
            for (var i = 4; i < MaxSlotsWithoutBackpack; i++)
                if (Inventory[i] == -1)
                    count++;
            return count;
        }

        public void DropItem(byte slot)
        {
            UpdateInventorySlot(slot);

            if (!ValidSlot(slot))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid slot");
#endif
                return;
            }

            int item = Inventory[slot];
            int data = ItemDatas[slot];
            if (item == -1)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Nothing to drop");
#endif
                return;
            }

            Inventory[slot] = -1;
            ItemDatas[slot] = -1;
            UpdateInventorySlot(slot);

            var container = new Container(Container.PurpleBag, AccountId, 120000);
            container.Inventory[0] = item;
            container.ItemDatas[0] = data;
            container.UpdateInventorySlot(0);

            RecalculateEquipBonuses();
            Parent.AddEntity(container, Position + MathUtils.Position(.2f, .2f));
        }

        public void SwapItem(SlotData slot1, SlotData slot2)
        {
            var en1 = Parent.GetEntity(slot1.ObjectId);
            var en2 = Parent.GetEntity(slot2.ObjectId);

            (en1 as IContainer)?.UpdateInventorySlot(slot1.SlotId);
            (en2 as IContainer)?.UpdateInventorySlot(slot2.SlotId);
            
            //Undefined entities
            if (en1 == null || en2 == null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Undefined entities");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }
            
            //Entities which are not containers???
            if (!(en1 is IContainer) || !(en2 is IContainer))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Not containers");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            if (en1 is Player && en2 is OneWayContainer)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Tried adding to one way container");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            if (en1.Position.Distance(en2) > ContainerMinimumDistance)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Too far away from container");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            if ((en1 as Player)?.TradePartner != null ||
                (en2 as Player)?.TradePartner != null)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Tried swapping items while trading");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            //Player manipulation attempt
            if (en1 is Player && slot1.ObjectId != Id ||
                en2 is Player && slot2.ObjectId != Id)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Player manipulation attempt");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            //Container manipulation attempt
            if (en1 is Container && 
                (en1 as Container).OwnerId != -1 && 
                AccountId != (en1 as Container).OwnerId ||
             en2 is Container && 
             (en2 as Container).OwnerId != -1 && 
             AccountId != (en2 as Container).OwnerId)
            {
#if DEBUG
                Program.Print(PrintType.Error, "Container manipulation attempt");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            var con1 = en1 as IContainer;
            var con2 = en2 as IContainer;

            //Invalid slots
            if (!con1.ValidSlot(slot1.SlotId) || !con2.ValidSlot(slot2.SlotId))
            {
#if DEBUG
                Program.Print(PrintType.Error, "Invalid inv swap");
#endif
                Client.Send(InvalidInvSwap);
                return;
            }

            //Invalid slot types
            int item1 = con1.Inventory[slot1.SlotId];
            int data1 = con1.ItemDatas[slot1.SlotId];
            int item2 = con2.Inventory[slot2.SlotId];
            int data2 = con2.ItemDatas[slot2.SlotId];
            var d = Desc as PlayerDesc;
            ItemDesc d1;
            ItemDesc d2;
            Resources.Type2Item.TryGetValue((ushort)item1, out d1);
            Resources.Type2Item.TryGetValue((ushort)item2, out d2);

            if (con1 is Player)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (slot1.SlotId == i)
                    {
                        if (d1 != null && d.SlotTypes[i] != d1.SlotType ||
                            d2 != null && d.SlotTypes[i] != d2.SlotType)
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Invalid slot type");
#endif
                            Client.Send(InvalidInvSwap);
                            return;
                        }
                    }
                }
            }

            if (con2 is Player)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (slot2.SlotId == i)
                    {
                        if (d1 != null && d.SlotTypes[i] != d1.SlotType ||
                            d2 != null && d.SlotTypes[i] != d2.SlotType)
                        {
#if DEBUG
                            Program.Print(PrintType.Error, "Invalid slot type");
#endif
                            Client.Send(InvalidInvSwap);
                            return;
                        }
                    }
                }
            }

            // soulbound item into non soulbound bag
            if (con1 is Player)
            {
                if (d1 != null && d1.Soulbound && en2 is Container con && con.OwnerId != AccountId)
                {
                    DropItem(slot1.SlotId);
                    Client.Send(ValidInvSwap);
                    return;
                }
            }

            if (en1 is GiftChest)
                Database.RemoveGift(Client.Account, item1);

            con1.Inventory[slot1.SlotId] = item2;
            con1.ItemDatas[slot1.SlotId] = data2;
            con2.Inventory[slot2.SlotId] = item1;
            con2.ItemDatas[slot2.SlotId] = data1;
            con1.UpdateInventorySlot(slot1.SlotId);
            con2.UpdateInventorySlot(slot2.SlotId);
            RecalculateEquipBonuses();
            Client.Send(ValidInvSwap);
        }

        public bool ValidSlot(int slot)
        {
            var maxSlots = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            if (slot < 0 || slot >= maxSlots)
                return false;
            return true;
        }

        public void UpdateInventory()
        {
            var length = HasBackpack ? MaxSlotsWithBackpack : MaxSlotsWithoutBackpack;
            for (var k = 0; k < length; k++)
                UpdateInventorySlot(k);
        }

        public void UpdateInventorySlot(int slot)
        {
#if DEBUG
            if (!HasBackpack && slot >= MaxSlotsWithoutBackpack)
                throw new Exception("Should not be updating backpack stats when there is no backpack present.");
            if (slot < 0 || slot >= MaxSlotsWithBackpack)
                throw new Exception("Out of bounds slot update attempt.");
#endif
            switch (slot)
            {
                case 0: 
                    SetSV(StatType.Inventory0, Inventory[0]);
                    SetPrivateSV(StatType.ItemData0, ItemDatas[0]);
                    break;
                case 1: 
                    SetSV(StatType.Inventory1, Inventory[1]);
                    SetPrivateSV(StatType.ItemData1, ItemDatas[1]);
                    break;
                case 2: 
                    SetSV(StatType.Inventory2, Inventory[2]);
                    SetPrivateSV(StatType.ItemData2, ItemDatas[2]);
                    break;
                case 3: 
                    SetSV(StatType.Inventory3, Inventory[3]);
                    SetPrivateSV(StatType.ItemData3, ItemDatas[3]);
                    break;
                case 4: 
                    SetPrivateSV(StatType.Inventory4, Inventory[4]);
                    SetPrivateSV(StatType.ItemData4, ItemDatas[4]);
                    break;
                case 5: 
                    SetPrivateSV(StatType.Inventory5, Inventory[5]);
                    SetPrivateSV(StatType.ItemData5, ItemDatas[5]);
                    break;
                case 6: 
                    SetPrivateSV(StatType.Inventory6, Inventory[6]);
                    SetPrivateSV(StatType.ItemData6, ItemDatas[6]);
                    break;
                case 7: 
                    SetPrivateSV(StatType.Inventory7, Inventory[7]);
                    SetPrivateSV(StatType.ItemData7, ItemDatas[7]);
                    break;
                case 8: 
                    SetPrivateSV(StatType.Inventory8, Inventory[8]);
                    SetPrivateSV(StatType.ItemData8, ItemDatas[8]);
                    break;
                case 9: 
                    SetPrivateSV(StatType.Inventory9, Inventory[9]);
                    SetPrivateSV(StatType.ItemData9, ItemDatas[9]);
                    break;
                case 10: 
                    SetPrivateSV(StatType.Inventory10, Inventory[10]);
                    SetPrivateSV(StatType.ItemData10, ItemDatas[10]);
                    break;
                case 11: 
                    SetPrivateSV(StatType.Inventory11, Inventory[11]);
                    SetPrivateSV(StatType.ItemData11, ItemDatas[11]);
                    break;
                case 12: 
                    SetPrivateSV(StatType.Backpack0, Inventory[12]);
                    SetPrivateSV(StatType.ItemData12, ItemDatas[12]);
                    break;
                case 13: 
                    SetPrivateSV(StatType.Backpack1, Inventory[13]);
                    SetPrivateSV(StatType.ItemData13, ItemDatas[13]);
                    break;
                case 14: 
                    SetPrivateSV(StatType.Backpack2, Inventory[14]);
                    SetPrivateSV(StatType.ItemData14, ItemDatas[14]);
                    break;
                case 15: 
                    SetPrivateSV(StatType.Backpack3, Inventory[15]);
                    SetPrivateSV(StatType.ItemData15, ItemDatas[15]);
                    break;
                case 16: 
                    SetPrivateSV(StatType.Backpack4, Inventory[16]);
                    SetPrivateSV(StatType.ItemData16, ItemDatas[16]);
                    break;
                case 17: 
                    SetPrivateSV(StatType.Backpack5, Inventory[17]);
                    SetPrivateSV(StatType.ItemData17, ItemDatas[17]);
                    break;
                case 18: 
                    SetPrivateSV(StatType.Backpack6, Inventory[18]);
                    SetPrivateSV(StatType.ItemData18, ItemDatas[18]);
                    break;
                case 19: 
                    SetPrivateSV(StatType.Backpack7, Inventory[19]);
                    SetPrivateSV(StatType.ItemData19, ItemDatas[19]);
                    break;
            }
        }
    }
}
