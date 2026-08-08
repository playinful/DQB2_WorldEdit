using System;
using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace EyeOfRubiss
{
    public abstract class ParamData : SaveData
    {
        public const int HEADER_LENGTH = 0;

        public const int HOTBAR_ITEM_COUNT = 15;
        public const int EQUIPMENT_COUNT = 16;
        public const int BAG_ITEM_COUNT = 192;
        public const int EQUIPMENT_BAG_COUNT = 64;

        abstract public uint WorldChecksum { get; set; }

        abstract public byte StageID { get; set; }

        abstract public ushort PlayerMaximumHP { get; set; }
        abstract public ushort PlayerHP { get; set; }

        abstract public ushort PlayerMaxSatiety { get; set; }
        abstract public ushort PlayerSatiety { get; set; }

        abstract public ushort PlayerStrength { get; set; }
        abstract public ushort PlayerResilience { get; set; }

        abstract public float PlayerPositionX { get; set; }
        abstract public float PlayerPositionY { get; set; }
        abstract public float PlayerPositionZ { get; set; }
        public Vector3 GetPlayerPosition() => new(PlayerPositionX, PlayerPositionY, PlayerPositionZ);

        abstract public float PlayerRotation { get; set; }

        abstract public bool YoshiExists { get; }

        abstract public float YoshiPositionX { get; set; }
        abstract public float YoshiPositionY { get; set; }
        abstract public float YoshiPositionZ { get; set; }
        public Vector3 GetYoshiPosition() => new(YoshiPositionX, YoshiPositionY, YoshiPositionZ);

        abstract public float YoshiRotation { get; set; }

        abstract public float Time { get; set; }
        abstract public ushort Weather { get; set; }

        abstract public uint Score { get; set; }

        abstract public ushort ElapsedDays { get; set; }
        abstract public ushort CraftCount { get; set; }
        abstract public byte DeadCount { get; set; }

        abstract public bool AcquireAttackSkill { get; set; } // Spinning Slice

        #region Inventory
        abstract public InventoryItem GetHotbarItem(int index);
        public IEnumerable<InventoryItem> GetHotbarItems(int index = 0, int count = HOTBAR_ITEM_COUNT)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < HOTBAR_ITEM_COUNT; i++)
                yield return GetHotbarItem(i + index);
        }

        abstract public InventoryItem GetBagItem(int index);
        public IEnumerable<InventoryItem> GetBagItems(int index = 0, int count = BAG_ITEM_COUNT)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < BAG_ITEM_COUNT; i++)
                yield return GetBagItem(i + index);
        }

        abstract public InventoryItem GetEquipment(int index);
        public IEnumerable<InventoryItem> GetEquipments(int index = 0, int count = EQUIPMENT_COUNT)
        {
            if (index < 0 || index >= EQUIPMENT_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < EQUIPMENT_COUNT; i++)
                yield return GetEquipment(i + index);
        }
        
        abstract public InventoryItem GetBagEquipment(int index);
        public IEnumerable<InventoryItem> GetBagEquipments(int index = 0, int count = EQUIPMENT_BAG_COUNT)
        {
            if (index < 0 || index >= EQUIPMENT_BAG_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < EQUIPMENT_BAG_COUNT; i++)
                yield return GetBagEquipment(i + index);
        }
        #endregion
        
        #region Residents
        public abstract class Resident(ParamData saveData, int index)
        {
            public const int LENGTH = 0x40;
            public const int MAXIMUM = 13; // TODO check

            public readonly ParamData SaveData = saveData;
            public readonly int Index = index;

            abstract public int GetAddress();

            abstract public float PositionX { get; set; }
            abstract public float PositionY { get; set; }
            abstract public float PositionZ { get; set; }

            public Vector3 GetPosition() => new(PositionX, PositionY, PositionZ);

            abstract public float Rotation { get; set; }

            abstract public ushort ResidentID { get; set; }
            abstract public ushort Type { get; set; }

            abstract public ushort HP { get; set; }
            
            abstract public byte State1 { get; set; }
            abstract public byte State2 { get; set; }

            public Span<byte> GetBytes() => SaveData.GetBytes(GetAddress(), LENGTH);

            public void Clear()
            {
                SaveData.Fill(0, GetAddress(), LENGTH);
            }
        }

        abstract public Resident GetResident(int index);
        public IEnumerable<Resident> GetResidents(int index = 0, int count = Resident.MAXIMUM)
        {
            if (index < 0 || index >= Resident.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Resident.MAXIMUM; i++)
                yield return GetResident(i + index);   
        }
        #endregion

        #region Block entities
        public abstract class BlockEntity(ParamData saveData)
        {
            public readonly ParamData SaveData = saveData;

            abstract public ushort X { get; set; }
            abstract public byte Y { get; set; }
            abstract public ushort Z { get; set; }

            public virtual Vector3I GetPosition() => new(X, Y, Z);

            public virtual void Clear() {}
        }
        public void ClearBlockEntitiesAtPosition(Vector3I position)
        {
            if (GetColossalCofferAtPosition(position) is ColossalCoffer coffer)
                coffer.Clear();
            foreach (Storage storage in GetStorages())
                if (storage.Enabled && storage.GetPosition() == position)
                    storage.Clear();
            foreach (ItemDisplay display in GetAllItemDisplays())
                if (display.Enabled && display.GetPosition() == position)
                    display.Clear();
            foreach (Signpost signpost in GetSignposts())
                if (signpost.Enabled && signpost.GetPosition() == position)
                    signpost.Clear();
            foreach (Teleportal teleportal in GetTeleportals())
                if (teleportal.Enabled && teleportal.GetPosition() == position)
                    teleportal.Clear();
            foreach (Naviglobe globe in GetNaviglobes())
                if (globe.Enabled && globe.GetPosition() == position)
                    globe.Clear();
            if (GetSharingStoneAtPosition(position) is SharingStone sharingStone)
                sharingStone.Clear();
            foreach (SummoningStone stone in GetSummoningStones())
                if (stone.Enabled && stone.GetPosition() == position)
                    stone.Clear();
            if (GetAncientTeleportalAtPosition(position) is AncientTeleportal ancientTeleportal)
                ancientTeleportal.Clear();
        }
        public void ClearAllBlockEntities()
        {
            GetColossalCoffer().Clear();
            foreach (Storage storage in GetStorages())
                storage.Clear();
            foreach (ItemDisplay display in GetAllItemDisplays())
                display.Clear();
            foreach (Signpost signpost in GetSignposts())
                signpost.Clear();
            foreach (Teleportal teleportal in GetTeleportals())
                teleportal.Clear();
            foreach (Naviglobe globe in GetNaviglobes())
                globe.Clear();
            GetSharingStone().Clear();
            foreach (SummoningStone stone in GetSummoningStones())
                stone.Clear();
            GetAncientTeleportal().Clear();
        }

        public abstract class ColossalCoffer(ParamData saveData) : BlockEntity(saveData)
        {
            public const int ADDRESS = 0x10D8;
            public const int LENGTH = 8;

            abstract public bool Enabled { get; set; }

            public override void Clear()
            {
                SaveData.Fill(0, ADDRESS, LENGTH);
            }
        }
        abstract public ColossalCoffer GetColossalCoffer();
        public ColossalCoffer GetColossalCofferAtPosition(Vector3I position)
        {
            ColossalCoffer coffer = GetColossalCoffer();
            if (coffer.Enabled && coffer.GetPosition() == position)
                return coffer;
            return null;
        }

        public abstract class Storage(ParamData saveData, int metadataAddress, int contentsAddress) : BlockEntity(saveData)
        {
            public const int START_ADDRESS_METADATA = 0x10E0;
            public const int LENGTH_METADATA = 8;
            public const int LENGTH_CONTENTS = ITEM_COUNT * 4;
            public const int MAXIMUM = 32;
            public const int ITEM_COUNT = 32;

            public readonly int MetadataAddress = metadataAddress;
            public readonly int ContentsAddress = contentsAddress;

            abstract public bool Enabled { get; set; }

            abstract public InventoryItem GetItem(int index);
            public IEnumerable<InventoryItem> GetItems(int index = 0, int count = ITEM_COUNT)
            {
                if (index < 0 || index >= ITEM_COUNT)
                    throw new IndexOutOfRangeException();

                for (int i = 0; i < count && i + index < ITEM_COUNT; i++)
                    yield return GetItem(i + index);
            }

            public override void Clear()
            {
                SaveData.Fill(0, MetadataAddress, LENGTH_METADATA);
                SaveData.Fill(0, ContentsAddress, LENGTH_CONTENTS);
            }
        }
        abstract public Storage GetStorage(int index);
        public IEnumerable<Storage> GetStorages(int index = 0, int count = Storage.MAXIMUM)
        {
            if (index < 0 || index >= Storage.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Storage.MAXIMUM; i++)
                yield return GetStorage(i + index);
        }
        public Storage GetStorageAtPosition(Vector3I position)
        {
            return GetStorages().FirstOrDefault(storage => storage.Enabled && storage.GetPosition() == position);
        }
        
        public abstract class ItemDisplay(ParamData saveData, int metadataAddress, int contentsAddress) : BlockEntity(saveData)
        {
            public const int LENGTH_METADATA = 8;
            public const int LENGTH_CONTENTS = 4;

            public const int START_ADDRESS_ITEM_DISPLAY_METADATA = 0x11E0;
            public const int ITEM_DISPLAY_MAXIMUM = 32;

            public const int START_ADDRESS_EQUIPMENT_DISPLAY_METADATA = 0x12E0;
            public const int EQUIPMENT_DISPLAY_MAXIMUM = 32;

            public readonly int MetadataAddress = metadataAddress;
            public readonly int ContentsAddress = contentsAddress;

            abstract public bool Enabled { get; set; }

            abstract public InventoryItem Item { get; }

            public override void Clear()
            {
                SaveData.Fill(0, MetadataAddress, LENGTH_METADATA);
                SaveData.Fill(0, ContentsAddress, LENGTH_CONTENTS);
            }
        }
        abstract public ItemDisplay GetItemDisplayGeneric(int index);
        public IEnumerable<ItemDisplay> GetItemDisplaysGeneric(int index = 0, int count = ItemDisplay.ITEM_DISPLAY_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.ITEM_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.ITEM_DISPLAY_MAXIMUM; i++)
                yield return GetItemDisplayGeneric(i + index);
        }
        abstract public ItemDisplay GetEquipmentDisplay(int index);
        public IEnumerable<ItemDisplay> GetEquipmentDisplays(int index = 0, int count = ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
        {
            if (index < 0 || index >= ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < ItemDisplay.EQUIPMENT_DISPLAY_MAXIMUM; i++)
                yield return GetEquipmentDisplay(i + index);
        }
        public IEnumerable<ItemDisplay> GetAllItemDisplays()
        {
            foreach (ItemDisplay display in GetItemDisplaysGeneric())
                yield return display;
            foreach (ItemDisplay display in GetEquipmentDisplays())
                yield return display;
        }
        public ItemDisplay GetItemDisplayAtPosition(Vector3I position)
        {
            return GetAllItemDisplays().FirstOrDefault(display => display.Enabled && display.GetPosition() == position);
        }

        public abstract class Signpost(ParamData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x14A8;
            public const int LENGTH = 0x88;
            public const int MAXIMUM = 20;

            public readonly int Address = address;

            abstract public string Text { get; set; }

            abstract public bool Enabled { get; set; }
            abstract public bool Written { get; set; }

            abstract public byte Language { get; set; }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        abstract public Signpost GetSignpost(int index);
        public IEnumerable<Signpost> GetSignposts(int index = 0, int count = Signpost.MAXIMUM)
        {
            if (index < 0 || index >= Signpost.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Signpost.MAXIMUM; i++)
                yield return GetSignpost(i + index);
        }
        public Signpost GetSignpostAtPosition(Vector3I position)
        {
            return GetSignposts().FirstOrDefault(signpost => signpost.Enabled && signpost.GetPosition() == position);
        }
        
        public abstract class Teleportal(ParamData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x13F8;
            public const int LENGTH = 8;
            public const int MAXIMUM = 8; // Blue, Blue, Red, Red, Green, Green, Yellow, Yellow

            public readonly int Address = address;
            
            abstract public bool Enabled { get; set; }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        abstract public Teleportal GetTeleportal(int index);
        public IEnumerable<Teleportal> GetTeleportals(int index = 0, int count = Teleportal.MAXIMUM)
        {
            if (index < 0 || index >= Teleportal.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Teleportal.MAXIMUM; i++)
                yield return GetTeleportal(i + index);
        }
        public Teleportal GetTeleportalAtPosition(Vector3I position)
        {
            return GetTeleportals().FirstOrDefault(teleportal => teleportal.Enabled && teleportal.GetPosition() == position);
        }
        
        public abstract class Naviglobe(ParamData saveData, int address) : BlockEntity(saveData)
        {
            public const int START_ADDRESS = 0x13E0;
            public const int LENGTH = 8;
            public const int MAXIMUM = 3; // Red, green, blue

            public readonly int Address = address;
            
            abstract public bool Enabled { get; set; }

            public override void Clear()
            {
                SaveData.Fill(0, Address, LENGTH);
            }
        }
        abstract public Naviglobe GetNaviglobe(int index);
        public IEnumerable<Naviglobe> GetNaviglobes(int index = 0, int count = Naviglobe.MAXIMUM)
        {
            if (index < 0 || index >= Naviglobe.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < Naviglobe.MAXIMUM; i++)
                yield return GetNaviglobe(i + index);
        }
        public Naviglobe GetNaviglobeAtPosition(Vector3I position)
        {
            return GetNaviglobes().FirstOrDefault(globe => globe.Enabled && globe.GetPosition() == position);
        }
        
        public abstract class SharingStone(ParamData saveData) : BlockEntity(saveData)
        {
            abstract public bool Enabled { get; set; }
        }
        abstract public SharingStone GetSharingStone();
        public SharingStone GetSharingStoneAtPosition(Vector3I position)
        {
            SharingStone stone = GetSharingStone();
            if (stone.Enabled && stone.GetPosition() == position)
                return stone;
            return null;
        }

        public abstract class SummoningStone(ParamData saveData, int address) : BlockEntity(saveData)
        {
            public const int MAXIMUM = 127;

            public readonly int Address = address;

            abstract public bool Enabled { get; set; }
        }
        abstract public SummoningStone GetSummoningStone(int index);
        public IEnumerable<SummoningStone> GetSummoningStones(int index = 0, int count = SummoningStone.MAXIMUM)
        {
            if (index < 0 || index >= SummoningStone.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            for (int i = 0; i < count && i + index < SummoningStone.MAXIMUM; i++)
                yield return GetSummoningStone(i + index);
        }
        public SummoningStone GetSummoningStoneAtPosition(Vector3I position)
        {
            return GetSummoningStones().FirstOrDefault(stone => stone.Enabled && stone.GetPosition() == position);
        }
        
        public abstract class AncientTeleportal(ParamData saveData) : BlockEntity(saveData)
        {
            public int ADDRESS = 0x1438;
            public const int LENGTH = 8;

            abstract public bool Enabled { get; set; }

            public override void Clear()
            {
                SaveData.Fill(0, ADDRESS, LENGTH);
            }
        }
        abstract public AncientTeleportal GetAncientTeleportal();
        public AncientTeleportal GetAncientTeleportalAtPosition(Vector3I position)
        {
            AncientTeleportal teleportal = GetAncientTeleportal();
            if (teleportal.Enabled && teleportal.GetPosition() == position)
                return teleportal;
            return null;
        }
        #endregion
        
        #region Biome Map
        public class BiomeMapInfo(ParamData saveData, int index)
        {
            public const int START_ADDRESS = 0x8FD0;
            public const int LENGTH = 4;
            public const int MAXIMUM = 0x2400;

            public readonly ParamData SaveData = saveData;
            public readonly int Index = index;

            public int GetAddress() => Index * LENGTH + START_ADDRESS;

            public byte Biome { get { return SaveData.GetByte(GetAddress()); } set { SaveData.SetByte(GetAddress(), value); } }
            public byte LevelArea { get { return SaveData.GetByte(GetAddress() + 1); } set { SaveData.SetByte(GetAddress() + 1, value); } }
            public byte CaveDirFlags { get { return SaveData.GetByte(GetAddress() + 2); } set { SaveData.SetByte(GetAddress() + 2, value); } }
            public byte DioramaSearchIndex { get { return SaveData.GetByte(GetAddress() + 3); } set { SaveData.SetByte(GetAddress() + 3, value); } }
        }

        public BiomeMapInfo GetBiomeMapInfo(Vector3I position)
        {
            int x = position.X / 16;
            int z = position.Z / 16;
            int index = z + x * 96;
            return new BiomeMapInfo(this, index);
        }
        #endregion
    }
}