using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace EyeOfRubiss
{
    public class ParamDataPS4 : ParamData
    {
        public const int HOTBAR_ADDRESS = 0x45020;
        public const int EQUIPMENT_ADDRESS = 0x4505C;
        public const int BAG_ADDRESS = 0x4509C;
        public const int EQUIPMENT_BAG_ADDRESS = 0x4539C;

        public override uint WorldChecksum { get { return GetUInt32(0x8); } set { SetUInt32(0x8, value); } }

        public override byte StageID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override ushort PlayerMaximumHP { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ushort PlayerHP { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override ushort PlayerMaxSatiety { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ushort PlayerSatiety { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override ushort PlayerStrength { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ushort PlayerResilience { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override float PlayerPositionX { get => GetSingle(0x1D360); set => SetSingle(0x1D360, value); }
        public override float PlayerPositionY { get => GetSingle(0x1D364); set => SetSingle(0x1D364, value); }
        public override float PlayerPositionZ { get => GetSingle(0x1D368); set => SetSingle(0x1D368, value); }

        public override float PlayerRotation { get => GetSingle(0x1D370); set => SetSingle(0x1D370, value); }

        public override bool YoshiExists => false;

        public override float YoshiPositionX { get { return 0; } set {} }
        public override float YoshiPositionY { get { return 0; } set {} }
        public override float YoshiPositionZ { get { return 0; } set {} }

        public override float YoshiRotation { get { return 0; } set {} }

        public override float Time { get => GetSingle(0x1E528); set => SetSingle(0x1E528, value); }
        public override ushort Weather { get => GetUInt16(0x1E52C); set => SetUInt16(0x1E52C, value); }

        public override uint Score { get => GetUInt32(0x2604); set { SetUInt32(0x2604, value); SetUInt32(0x2608, value); } }
        
        public override ushort ElapsedDays { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ushort CraftCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override byte DeadCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override bool AcquireAttackSkill { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public static bool TryLoad(string path, out ParamDataPS4 result)
        {
            result = null;
            ParamDataPS4 paramData = new();
            if (paramData._TryLoad(path, HEADER_LENGTH, decompress: false))
            {
                result = paramData;
                return true;
            }
            else return false;
        }
        public override void Save(string path = null)
        {
            path ??= Path;
            Path = path;

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            file.StoreBuffer(_Buffer);

            UnsavedChanges = false;
        }

        #region Inventory
        public override InventoryItem GetHotbarItem(int index)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, HOTBAR_ADDRESS + index * InventoryItem.LENGTH);
        }
        public override InventoryItem GetBagItem(int index)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, BAG_ADDRESS + index * InventoryItem.LENGTH);
        }
        public override InventoryItem GetEquipment(int index)
        {
            if (index < 0 || index >= EQUIPMENT_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, EQUIPMENT_ADDRESS + index * InventoryItem.LENGTH);
        }
        public override InventoryItem GetBagEquipment(int index)
        {
            if (index < 0 || index >= EQUIPMENT_BAG_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, EQUIPMENT_BAG_ADDRESS + index * InventoryItem.LENGTH);
        }
        #endregion
        
        #region Residents
        new public class Resident(ParamDataPS4 saveData, int index) : ParamData.Resident(saveData, index)
        {
            public const int START_ADDRESS = 0x2620;

            public override int GetAddress() => START_ADDRESS + Index * LENGTH;

            public override float PositionX { get => SaveData.GetSingle(GetAddress()); set => SaveData.SetSingle(GetAddress(), value); }
            public override float PositionY { get => SaveData.GetSingle(GetAddress() + 4); set => SaveData.SetSingle(GetAddress() + 4, value); }
            public override float PositionZ { get => SaveData.GetSingle(GetAddress() + 8); set => SaveData.SetSingle(GetAddress() + 8, value); }

            public override float Rotation { get => SaveData.GetSingle(GetAddress() + 0x10); set => SaveData.SetSingle(GetAddress() + 0x10, value); }

            public override ushort ResidentID { get => SaveData.GetUInt16(GetAddress() + 0x14); set => SaveData.SetUInt16(GetAddress() + 0x14, value); }
            public override ushort Type { get => SaveData.GetUInt16(GetAddress() + 0x16); set => SaveData.SetUInt16(GetAddress() + 0x16, value); }

            public override ushort HP { get => SaveData.GetUInt16(GetAddress() + 0x18); set => SaveData.SetUInt16(GetAddress() + 0x18, value); }

            public override byte State1 { get => SaveData.GetByte(GetAddress() + 0x1A); set => SaveData.SetByte(GetAddress() + 0x1A, value); }
            public override byte State2 { get => SaveData.GetByte(GetAddress() + 0x1B); set => SaveData.SetByte(GetAddress() + 0x1B, value); }
        }

        public override Resident GetResident(int index)
        {
            if (index < 0 || index >= ParamData.Resident.MAXIMUM)
                throw new IndexOutOfRangeException();

            return new Resident(this, index);
        }
        #endregion

        #region Block entities
        new public class ColossalCoffer(ParamDataPS4 saveData) : ParamData.ColossalCoffer(saveData)
        {
            public override ushort X { get { return SaveData.GetUInt16(ADDRESS); } set { SaveData.SetUInt16(ADDRESS, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(ADDRESS + 2); } set { SaveData.SetUInt16(ADDRESS + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(ADDRESS + 4); } set { SaveData.SetByte(ADDRESS + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(ADDRESS + 5) == 1; } set { SaveData.SetByte(ADDRESS + 5, (byte)(value ? 1 : 0)); } }
            // bytes 7 + 8 are always 01 00 (?)
        }
        public override ColossalCoffer GetColossalCoffer()
        {
            return new ColossalCoffer(this);
        }

        new public class Storage(ParamDataPS4 saveData, int metadataAddress, int contentsAddress) : ParamData.Storage(saveData, metadataAddress, contentsAddress)
        {
            public const int START_ADDRESS_CONTENTS = 0x4549C;

            public override ushort X { get { return SaveData.GetUInt16(MetadataAddress); } set { SaveData.SetUInt16(MetadataAddress, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(MetadataAddress + 2); } set { SaveData.SetUInt16(MetadataAddress + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(MetadataAddress + 4); } set { SaveData.SetByte(MetadataAddress + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(MetadataAddress + 5) == 1; } set { SaveData.SetByte(MetadataAddress + 5, (byte)(value ? 1 : 0)); } }
            // bytes 7 + 8 are always 02 00 (?)

            public override InventoryItem GetItem(int index)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                return new(SaveData, ContentsAddress + index * InventoryItem.LENGTH);
            }
        }
        public override Storage GetStorage(int index)
        {
            if (index < 0 || index >= ParamData.Storage.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Storage(this,
                ParamData.Storage.START_ADDRESS_METADATA + ParamData.Storage.LENGTH_METADATA * index,
                Storage.START_ADDRESS_CONTENTS + ParamData.Storage.LENGTH_CONTENTS * index);
        }
        
        new public class ItemDisplay(ParamDataPS4 saveData, int metadataAddress, int contentsAddress) : ParamData.ItemDisplay(saveData, metadataAddress, contentsAddress)
        {
            public const int START_ADDRESS_ITEM_DISPLAY_CONTENTS = 0x4649C;
            public const int START_ADDRESS_EQUIPMENT_DISPLAY_CONTENTS = 0x4651C;
            
            public override ushort X { get { return SaveData.GetUInt16(MetadataAddress); } set { SaveData.SetUInt16(MetadataAddress, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(MetadataAddress + 2); } set { SaveData.SetUInt16(MetadataAddress + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(MetadataAddress + 4); } set { SaveData.SetByte(MetadataAddress + 4, value); } }

            public override bool Enabled { get { return SaveData.GetByte(MetadataAddress + 5) == 1; } set { SaveData.SetByte(MetadataAddress + 5, (byte)(value ? 1 : 0)); } }
            // bytes 7 + 8 are always 00 03 if item display, 00 04 if equip display

            public override InventoryItem Item => new(SaveData, ContentsAddress);
        }
        public override ItemDisplay GetItemDisplayGeneric(int index)
        {
            if (index < 0 || index >= ParamData.ItemDisplay.ITEM_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new ItemDisplay(this,
                ParamData.ItemDisplay.START_ADDRESS_ITEM_DISPLAY_METADATA + ParamData.ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_ITEM_DISPLAY_CONTENTS + ParamData.ItemDisplay.LENGTH_CONTENTS * index);
        }
        public override ItemDisplay GetEquipmentDisplay(int index)
        {
            if (index < 0 || index >= ParamData.ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new ItemDisplay(this,
                ParamData.ItemDisplay.START_ADDRESS_EQUIPMENT_DISPLAY_METADATA + ParamData.ItemDisplay.LENGTH_METADATA * index,
                ItemDisplay.START_ADDRESS_EQUIPMENT_DISPLAY_CONTENTS + ParamData.ItemDisplay.LENGTH_CONTENTS * index);
        }

        new public class Signpost(ParamDataPS4 saveData, int address) : ParamData.Signpost(saveData, address)
        {
            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }

            public override string Text { get { return SaveData.GetString(Address + 8, 0x80); } set { SaveData.SetString(Address + 8, value, 0x80); } }

            public override bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }
            public override bool Written { get { return SaveData.GetByte(Address + 6) == 0x80; } set { SaveData.SetByte(Address + 6, (byte)(value ? 0x80 : 0)); } }

            public override byte Language { get { return SaveData.GetByte(Address + 7); } set { SaveData.SetByte(Address + 7, value); } } // unused maybe?
        }
        public override Signpost GetSignpost(int index)
        {
            if (index < 0 || index >= ParamData.Signpost.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Signpost(this, ParamData.Signpost.START_ADDRESS + ParamData.Signpost.LENGTH * index);
        }
        
        new public class Teleportal(ParamDataPS4 saveData, int address) : ParamData.Teleportal(saveData, address)
        {
            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }
            
            public override bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }
        }
        public override Teleportal GetTeleportal(int index)
        {
            if (index < 0 || index >= ParamData.Teleportal.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Teleportal(this, ParamData.Teleportal.START_ADDRESS + ParamData.Teleportal.LENGTH * index);
        }
        
        new public class Naviglobe(ParamDataPS4 saveData, int address) : ParamData.Naviglobe(saveData, address)
        {
            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }
            
            public override bool Enabled { get { return SaveData.GetByte(Address + 5) == 1; } set { SaveData.SetByte(Address + 5, (byte)(value ? 1 : 0)); } }
        }
        public override Naviglobe GetNaviglobe(int index)
        {
            if (index < 0 || index >= ParamData.Naviglobe.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Naviglobe(this, ParamData.Naviglobe.START_ADDRESS + ParamData.Naviglobe.LENGTH * index);
        }
        
        new public class SharingStone(ParamDataPS4 saveData) : ParamData.SharingStone(saveData)
        {
            public const int ADDRESS = 0x12078;
            public const int LENGTH = 0x124;

            public override ushort X { get { return SaveData.GetUInt16(ADDRESS); } set { SaveData.SetUInt16(ADDRESS, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(ADDRESS + 2); } set { SaveData.SetUInt16(ADDRESS + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(ADDRESS + 4); } set { SaveData.SetByte(ADDRESS + 4, value); } }

            public override bool Enabled { get { return SaveData.GetBit(ADDRESS + 6, 0); } set { SaveData.SetBit(ADDRESS + 6, 0, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, ADDRESS, LENGTH);
            }
        }
        public override SharingStone GetSharingStone()
        {
            return new SharingStone(this);
        }

        new public class SummoningStone(ParamDataPS4 saveData, int address) : ParamData.SummoningStone(saveData, address)
        {
            public const int START_ADDRESS = 0x1219C;
            public const int LENGTH = 0x128;

            public override ushort X { get { return SaveData.GetUInt16(Address); } set { SaveData.SetUInt16(Address, value); } }
            public override ushort Z { get { return SaveData.GetUInt16(Address + 2); } set { SaveData.SetUInt16(Address + 2, value); } }
            public override byte Y { get { return SaveData.GetByte(Address + 4); } set { SaveData.SetByte(Address + 4, value); } }

            public override bool Enabled { get { return SaveData.GetBit(Address + 7, 0); } set { SaveData.SetBit(Address + 7, 0, value); } }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        public override SummoningStone GetSummoningStone(int index)
        {
            if (index < 0 || index >= ParamData.SummoningStone.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new SummoningStone(this, SummoningStone.START_ADDRESS + SummoningStone.LENGTH * index);
        }
        
        // new public class AncientTeleportal(ParamDataPS4 saveData) : ParamData.AncientTeleportal(saveData)
        // {
        //    // TODO
        // }
        public override AncientTeleportal GetAncientTeleportal()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}