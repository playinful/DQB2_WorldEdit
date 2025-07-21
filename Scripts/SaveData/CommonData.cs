using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Microsoft.VisualBasic;
using EyeOfRubiss.Info;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace EyeOfRubiss
{
    /// <summary> Class used for handling CMNDAT.BIN files, which hold DQB2 player, progress, and resident data, among other things. </summary>
    public class CommonData : SaveData
    {
        /// <summary> Length of the file header, in bytes. </summary>
        private const int HEADER_LENGTH = 0x2A444;

        private const int THUMBNAIL_ADDRESS = 0x10D;
        private const int THUMBNAIL_LENGTH = 320 * 180 * 3;

        private const int HOTBAR_ADDRESS = 0x55B28D;
        private const int HOTBAR_ITEM_COUNT = 15;
        private const int BAG_ADDRESS = 0x55B2C9;
        private const int BAG_ITEM_COUNT = 420;

        public static CommonData Instance { get; private set; }
        public static bool HasInstance() => Instance is not null && Instance.IsLoaded;

        public bool IsInstance() => this == Instance;

        public DateTime LastSaveTime { get => DateTime.FromFileTime(GetInt64(0x2A40D, header: true)); set => SetInt64(0x2A40D, value.ToFileTime(), header: true); }

        public byte FromIsland { get => GetByte(0xC9, header: true); set => SetByte(0xC9, value, header: true); } // Same as ToIsland if not sailing
        public byte ToIsland { get => GetByte(0xC8, header: true); set => SetByte(0xC8, value, header: true); }

        public string PlayerName { get => GetString(0xCD, 12, header: true); set => SetString(0xCD, value, 12, header: true); }
        public bool PlayerGender { get => GetBit(0xC4, 1, header: true); set => SetBit(0xC4, 1, value, header: true); } // False: female, True: male
        
        public byte PlayerLevel { get => GetByte(0xCA9CF); set => SetByte(0xCA9CF, value); }
        public short PlayerExperience { get => GetInt16(0x6A9D1); set => SetInt16(0x6A9D1, value); } // TODO test if signed

        public short PlayerHP { get => GetInt16(0x6A890); set => SetInt16(0x6A890, value); } // TODO test if signed, also is this current or max? </summary>
        public short PlayerAdditionalHP { get => GetInt16(0x6A892); set => SetInt16(0x6A892, value); } // TODO I think this means bonuses from seeds of life, test this please -- also test if signed
        public short PlayerHunger { get => GetInt16(0x6A896); set => SetInt16(0x6A896, value); }
        public short PlayerStamina { get => GetInt16(0x6A8A0); set => SetInt16(0x6A8A0, value); } // TODO test if signed
        public short PlayerAttack { get => GetInt16(0x6A898); set => SetInt16(0x6A898, value); } // TODO test if signed
        public short PlayerDefence { get => GetInt16(0x6A89A); set => SetInt16(0x6A89A, value); } // TODO test if signed

        public ushort PlayerHairColor { get { return GetUInt16(0x6A876); } } // TODO check

        public bool UnlockBag { get => GetBit(0x635, 0); set => SetBit(0x635, 0, value); }
        public bool UnlockWindbraker { get => GetBit(0x6A8A2, 1); set => SetBit(0x6A8A2, 1, value); }
        public bool UnlockFlipper { get => GetBit(0x6A8A3, 1); set => SetBit(0x6A8A3, 1, value); } // TODO: What does this do?
        public bool UnlockBigBash { get => GetBit(0x506, 1); set => SetBit(0x506, 1, value); }
        public bool UnlockBiggerBash { get => GetBit(0x502, 3); set => SetBit(0x502, 3, value); }
        public bool BottomlessPotUse { get => GetBit(0x504, 2); set => SetBit(0x504, 2, value); } // TODO: What does this do?
        public bool BottomlessPot { get => GetBit(0x67D, 1); set => SetBit(0x67D, 1, value); } // TODO: What does this do?
        public bool UnlockBuildnoculars { get => GetBit(0x502, 7); set => SetBit(0x502, 7, value); }

        public bool CarFly { get => GetBit(0x506, 6); set => SetBit(0x506, 6, value); }
        public bool CarBeam { get => GetBit(0x506, 7); set => SetBit(0x506, 7, value); }
        public bool CarLight { get => GetBit(0x506, 5); set => SetBit(0x506, 5, value); }
        
        public bool Transform { get => GetBit(0x500, 6); set => SetBit(0x500, 6, value); } // TODO: What does this do?
        public bool Expression { get => GetBit(0x501, 1); set => SetBit(0x501, 1, value); } // TODO: What does this do?

        public byte MiniMedals { get => GetByte(0x226E40); set => SetByte(0x226E40, value); }
        public byte MiniMedalsConsigned { get => GetByte(0x226E44); set => SetByte(0x226E44, value); }

        /// <summary> TODO </summary>
        public bool MaterialBonusCord { get => GetBit(0x22C75A, 4); set => SetBit(0x22C75A, 4, value); }
        /// <summary> TODO </summary>
        public bool MaterialBonusGrassFibre { get => GetBit(0x22CD8A, 4); set => SetBit(0x22CD8A, 4, value); }
        /// <summary> TODO </summary>
        public bool MaterialBonusWood { get => GetBit(0x22C757, 4); set => SetBit(0x22C757, 4, value); }
        /// <summary> TODO </summary>
        public bool MaterialBonusDryGrass { get => GetBit(0x22CD3E, 4); set => SetBit(0x22CD3E, 4, value); }

        /// <summary> Buildertopia appears on map. TODO: research </summary>
        public bool PioneerLand { get => GetBit(0x22E785, 3); set => SetBit(0x22E785, 3, value); }

        public string IsleOfAwakeningName { get => GetString(0x226E10, 30); set => SetString(0x226E10, value, 30); }
        public string Buildertopia1Name { get => GetString(0x52A667, 30); set => SetString(0x52A667, value, 30); }
        public string Buildertopia2Name { get => GetString(0x52A69F, 30); set => SetString(0x52A69F, value, 30); }
        public string Buildertopia3Name { get => GetString(0x52A6D7, 30); set => SetString(0x52A6D7, value, 30); }

        public byte Buildertopia1Type { get => GetByte(0xED, header: true); set => SetByte(0xED, value, header: true); }
        public byte Buildertopia2Type { get => GetByte(0xEE, header: true); set => SetByte(0xEE, value, header: true); }
        public byte Buildertopia3Type { get => GetByte(0xEF, header: true); set => SetByte(0xEF, value, header: true); }

        public int Buildertopia1Seed { get => GetInt32(0x52A697); set => SetInt32(0x52A697, value); }
        public int Buildertopia2Seed { get => GetInt32(0x52A6CF); set => SetInt32(0x52A6CF, value); }
        public int Buildertopia3Seed { get => GetInt32(0x52A707); set => SetInt32(0x52A707, value); }

        public byte Buildertopia1Size { get => GetByte(0x52A69B); set => SetByte(0x52A69B, value); }
        public byte Buildertopia2Size { get => GetByte(0x52A6D3); set => SetByte(0x52A6D3, value); }
        public byte Buildertopia3Size { get => GetByte(0x52A70B); set => SetByte(0x52A70B, value); }

        public byte Buildertopia1Gratitude { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } // TODO
        public byte Buildertopia2Gratitude { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } // TODO 52A74B
        public byte Buildertopia3Gratitude { get => throw new NotImplementedException(); set => throw new NotImplementedException(); } // TODO

        public InventoryItem PlayerWeapon => new(this, 0x55B959);
        public InventoryItem PlayerArmour => new(this, 0x55B989);
        public InventoryItem PlayerShield => new(this, 0x55B985);
        public InventoryItem PlayerHammer => new(this, 0x55B95D);

        public InventoryItem GlamourHammer => new(this, 0x6A8C6);
        public InventoryItem GlamourWeapon => new(this, 0x6A8C2);
        public InventoryItem GlamourArmour => new(this, 0x6A8CA);
        public InventoryItem GlamourShield => new(this, 0x6A8CE);
        public InventoryItem GlamourHeadwear => new(this, 0x6A8D2);
        public InventoryItem GlamourAccessory1 => new(this, 0x6A8D6);
        public InventoryItem GlamourAccessory2 => new(this, 0x6A8DA);
        public InventoryItem GlamourAccessory3 => new(this, 0x6A8DE);

        // TODO: are these addresses correct???
        public float PlayerPositionX { get { return GetSingle(0x6A866 + 0x16); } set { SetSingle(0x6A866 + 0x16, value); } }
        public float PlayerPositionY { get { return GetSingle(0x6A866 + 0x1A); } set { SetSingle(0x6A866 + 0x1A, value); } }
        public float PlayerPositionZ { get { return GetSingle(0x6A866 + 0x1E); } set { SetSingle(0x6A866 + 0x1E, value); } }
        public float PlayerRotation { get { return GetSingle(0x6A866 + 0x24); } set { SetSingle(0x6A866 + 0x24, value); } }
        public Vector3 GetPlayerPosition => new(PlayerPositionX, PlayerPositionY, PlayerPositionZ);

        public static CommonData TryLoadAndSet(string path)
        {
            if (TryLoad(path) is CommonData commonData)
            {
                return Instance = commonData;
            }
            else return null;
        }
        public static CommonData TryLoad(string path)
        {
            CommonData commonData = new();
            if (commonData._TryLoad(path, HEADER_LENGTH))
            {
                return commonData;
            }
            else return null;
        }
        public static CommonData QuickLoad(string path)
        {
            CommonData commonData = new();
            if (commonData._QuickLoad(path, HEADER_LENGTH))
            {
                return commonData;
            }
            else return null;
        }

        public static void Close()
        {
            Instance = null;
        }

        public Image GetThumbnail()
        {
            Image image = Image.CreateEmpty(320, 180, false, Image.Format.Rgb8);
            var xspan = GetBytes(THUMBNAIL_ADDRESS, THUMBNAIL_LENGTH, header: true);
            for (int y = 0; y < 180; y++)
            {
                for (int x = 0; x < 320; x++)
                {
                    image.SetPixel(x, y, new Color()
                    {
                        B8 = xspan[(y * 320 * 3) + (x * 3)],
                        G8 = xspan[(y * 320 * 3) + (x * 3) + 1],
                        R8 = xspan[(y * 320 * 3) + (x * 3) + 2]
                    });
                }
            }
            return image;
        }

        public InventoryItem GetHotbarItem(int index)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, index * 4 + HOTBAR_ADDRESS);
        }
        public IEnumerable<InventoryItem> GetHotbarItems(int index = 0, int count = HOTBAR_ITEM_COUNT)
        {
            if (index < 0 || index >= HOTBAR_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < HOTBAR_ITEM_COUNT; i++)
                yield return GetHotbarItem(i + index);
        }

        public InventoryItem GetBagItem(int index)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            return new InventoryItem(this, index * 4 + BAG_ADDRESS);
        }
        public IEnumerable<InventoryItem> GetBagItems(int index = 0, int count = BAG_ITEM_COUNT)
        {
            if (index < 0 || index >= BAG_ITEM_COUNT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < BAG_ITEM_COUNT; i++)
                yield return GetBagItem(i + index);
        }

        public void ClearHotbar()
        {
            foreach (InventoryItem item in GetHotbarItems())
            {
                item.ItemID = 0;
                item.Count = 0;
            }
        }
        public void ClearBag()
        {
            foreach (InventoryItem item in GetBagItems())
            {
                item.ItemID = 0;
                item.Count = 0;
            }
        }

        #region Residents
        public class Resident(CommonData saveData, int index)
        {
            public const int START_ADDRESS = 0x6ACC8;
            public const int LENGTH = 608;
            public const int MAXIMUM_IMPORTANT = 1023;
            public const int MAXIMUM_GENERIC = 238;
            public const int MAXIMUM = MAXIMUM_IMPORTANT + MAXIMUM_GENERIC;

            public const int INVENTORY_COUNT = 15;

            public readonly CommonData SaveData = saveData;
            public readonly int Index = index;

            public int GetAddress() => (Index - 1) * LENGTH + START_ADDRESS;

            public string Name { get => SaveData.GetString(GetAddress(), 30).Replace("\0", ""); set => SaveData.SetString(GetAddress(), value, 30); }
            public bool UseCustomName { get => SaveData.GetBit(GetAddress() + 0x12D, 7); set => SaveData.SetBit(GetAddress() + 0x12D, 7, value); }
            public byte GenericName { get => SaveData.GetByte(GetAddress() + 0x112); set => SaveData.SetByte(GetAddress() + 0x112, value); }

            public byte Sex { get => SaveData.GetByte(GetAddress() + 0x102); set => SaveData.SetByte(GetAddress() + 0x102, value); } // 1: male, 0: female

            // TODO: Maximum or current??
            public short HP { get => SaveData.GetInt16(GetAddress() + 0x92); set => SaveData.SetInt16(GetAddress() + 0x92, value); }

            public ushort Type { get => SaveData.GetUInt16(GetAddress() + 0x90); set => SaveData.SetUInt16(GetAddress() + 0x90, value); }
            public byte Job { get => SaveData.GetByte(GetAddress() + 0x10F); set => SaveData.SetByte(GetAddress() + 0x10F, value); }

            public bool CanEquip { get => SaveData.GetBit(GetAddress() + 0x133, 1); set => SaveData.SetBit(GetAddress() + 0x133, 1, value); }
            public bool CanBattle { get => SaveData.GetBit(GetAddress() + 0x103, 1); set => SaveData.SetBit(GetAddress() + 0x103, 1, value); }

            public byte HomeIsland { get => SaveData.GetByte(GetAddress() + 0x113); set => SaveData.SetByte(GetAddress() + 0x113, value); }
            public byte CurrentIsland { get => SaveData.GetByte(GetAddress() + 0xDF); set => SaveData.SetByte(GetAddress() + 0xDF, value); }
            /// <summary> A byte value pertaining to the section of the Isle of Awakening in which this Resident lives (Green Gardens, Scarlet Sands, etc. Should be set to 0 if CurrentIsland is not the Isle of Awakening). </summary>
            public byte CurrentRegion { get => SaveData.GetByte(GetAddress() + 0x144); set => SaveData.SetByte(GetAddress() + 0x144, value); }

            public ushort Face { get => SaveData.GetUInt16(GetAddress() + 0xE5); set => SaveData.SetUInt16(GetAddress() + 0xE5, value); }
            public ushort Hair { get => SaveData.GetUInt16(GetAddress() + 0xE7); set => SaveData.SetUInt16(GetAddress() + 0xE7, value); }
            public ushort Body { get => SaveData.GetUInt16(GetAddress() + 0xE9); set => SaveData.SetUInt16(GetAddress() + 0xE9, value); }
            public ushort EyeColor { get => SaveData.GetUInt16(GetAddress() + 0xEB); set => SaveData.SetUInt16(GetAddress() + 0xEB, value); }
            public ushort HairColor { get => SaveData.GetUInt16(GetAddress() + 0xED); set => SaveData.SetUInt16(GetAddress() + 0xED, value); }
            public ushort SkinColor { get => SaveData.GetUInt16(GetAddress() + 0xEF); set => SaveData.SetUInt16(GetAddress() + 0xEF, value); }
            public bool LockGraphic { get => SaveData.GetBit(GetAddress() + 0x12E, 4); set => SaveData.SetBit(GetAddress() + 0x12E, 4, value); }

            public byte MessageType { get => SaveData.GetByte(GetAddress() + 0x10A); set => SaveData.SetByte(GetAddress() + 0x10A, value); }
            public byte VoiceType { get => SaveData.GetByte(GetAddress() + 0x10B); set => SaveData.SetByte(GetAddress() + 0x10B, value); }

            public byte RoomSize { get => SaveData.GetByte(GetAddress() + 0x107); set => SaveData.SetByte(GetAddress() + 0x107, value); }
            public byte RoomFanciness { get => SaveData.GetByte(GetAddress() + 0x108); set => SaveData.SetByte(GetAddress() + 0x108, value); }
            public byte RoomAmbience { get => SaveData.GetByte(GetAddress() + 0x109); set => SaveData.SetByte(GetAddress() + 0x109, value); }

            public float PositionX { get => SaveData.GetSingle(GetAddress() + 0x5C); set => SaveData.SetSingle(GetAddress() + 0x5C, value); }
            public float PositionY { get => SaveData.GetSingle(GetAddress() + 0x60); set => SaveData.SetSingle(GetAddress() + 0x60, value); }
            public float PositionZ { get => SaveData.GetSingle(GetAddress() + 0x64); set => SaveData.SetSingle(GetAddress() + 0x64, value); }
            public float Rotation { get => SaveData.GetSingle(GetAddress() + 0x8C); set => SaveData.SetSingle(GetAddress() + 0x8C, value); }

            public bool Hidden { get => SaveData.GetBit(GetAddress() + 0x133, 3); set => SaveData.SetBit(GetAddress() + 0x133, 3, value); }

            // TODO ask Sapphire what this is
            public bool TypeLock { get { return SaveData.GetBit(GetAddress() + 0x12E, 4); } set { SaveData.SetBit(GetAddress() + 0x12E, 4, value); } }

            public InventoryItem Weapon => new(SaveData, GetAddress() + 0xC7);
            public InventoryItem Armour => new(SaveData, GetAddress() + 0xCF);
            public InventoryItem GetInventoryItem(int index)
            {
                if (index < 0 || index >= INVENTORY_COUNT)
                    throw new IndexOutOfRangeException();

                return new InventoryItem(SaveData, GetAddress() + 0x20 + index * 4);
            }
            public IEnumerable<InventoryItem> GetInventory(int index = 0, int count = INVENTORY_COUNT)
            {
                if (index < 0 || index >= INVENTORY_COUNT)
                    throw new IndexOutOfRangeException();

                for (int i = index; i < count; i++)
                    yield return GetInventoryItem(i);
            }

            public bool Clothed { get { return SaveData.GetBit(GetAddress() + 0x9C, 6); } set { SaveData.SetBit(GetAddress() + 0x9C, 6, value); } }
            public bool InRags { get { return SaveData.GetBit(GetAddress() + 0x9C, 1); } set { SaveData.SetBit(GetAddress() + 0x9C, 1, value); } }

            public bool IsImportant() => (Index - 1) <= MAXIMUM_IMPORTANT;

            public Vector3 GetPosition()
            {
                return new Vector3(PositionX, PositionY, PositionZ);
            }
            public string GetDisplayName()
            {
                if (!string.IsNullOrEmpty(Name))
                    return Name;

                if (IsImportant())
                    return ImportantResidentName.Get(Index);

                return Name;
                // TODO generic names
            }
        }

        public Resident GetResident(int index)
        {
            if (index < 0 || index >= Resident.MAXIMUM)
                throw new IndexOutOfRangeException();
            
            return new Resident(this, index);
        }
        public IEnumerable<Resident> GetResidents(int index = 0, int count = Resident.MAXIMUM)
        {
            if (index < 0 || index >= Resident.MAXIMUM)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Resident.MAXIMUM; i++)
                yield return GetResident(i + index);
        }
        public IEnumerable<Resident> GetImportantResidents(int index = 0, int count = Resident.MAXIMUM_IMPORTANT)
        {
            if (index < 0 || index >= Resident.MAXIMUM_IMPORTANT)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Resident.MAXIMUM_IMPORTANT; i++)
                yield return GetResident(i + index);
        }
        public IEnumerable<Resident> GetGenericResidents(int index = 0, int count = Resident.MAXIMUM_GENERIC)
        {
            if (index < 0 || index >= Resident.MAXIMUM_GENERIC)
                throw new IndexOutOfRangeException();

            for (int i = 0; i < count && i + index < Resident.MAXIMUM_GENERIC; i++)
                yield return GetResident(i + index + Resident.MAXIMUM_IMPORTANT);
        }
        #endregion

        public class Blueprint(CommonData saveData, int index)
        {
            private const int START_ADDRESS = 0x136DE8;
            private const int LENGTH = 0x30008;

            public readonly CommonData SaveData = saveData;
            public readonly int Index = index;

            public int GetAddress() => START_ADDRESS + Index * LENGTH;

            public ushort Width { get { return SaveData.GetUInt16(GetAddress() + 0x30000); } set { SaveData.SetUInt16(GetAddress() + 0x30000, value); } }
            public ushort Height { get { return SaveData.GetUInt16(GetAddress() + 0x30002); } set { SaveData.SetUInt16(GetAddress() + 0x30002, value); } }
            public ushort Depth { get { return SaveData.GetUInt16(GetAddress() + 0x30004); } set { SaveData.SetUInt16(GetAddress() + 0x30004, value); } }
        }

        /// <summary> TODO </summary>
        public class Craft(CommonData saveData, int address, uint id)
        {
            public CommonData SaveData = saveData;
            public int Address = address;
            public uint ID = id;

            public int GetAddress() => Address;

            // TODO: No clue what these are
            public bool Recipe { get { return SaveData.GetBit(GetAddress(), 0); } set { SaveData.SetBit(GetAddress(), 0, value); } }
            public bool Build { get { return SaveData.GetBit(GetAddress(), 1); } set { SaveData.SetBit(GetAddress(), 1, value); } }
            public bool New { get { return SaveData.GetBit(GetAddress(), 2); } set { SaveData.SetBit(GetAddress(), 2, value); } }
            public bool Infinite { get { return SaveData.GetBit(GetAddress(), 4); } set { SaveData.SetBit(GetAddress(), 4, value); } }
        }

        /// <summary> TODO </summary>
        public class Crop(CommonData saveData, int address, uint id)
        {
            public CommonData SaveData = saveData;
            public int Address = address;
            public uint ID = id;

            public int GetAddress() => Address;

            public uint Count { get { return SaveData.GetUInt32(GetAddress()); } set { SaveData.SetUInt32(GetAddress(), value); } }
            public bool Harvest { get { return SaveData.GetBit(GetAddress() + 4, 0); } set { SaveData.SetBit(GetAddress() + 4, 0, value); } }
            public bool Plant { get { return SaveData.GetBit(GetAddress() + 4, 2); } set { SaveData.SetBit(GetAddress() + 4, 2, value); } }
            public bool Growth { get { return SaveData.GetBit(GetAddress() + 4, 3); } set { SaveData.SetBit(GetAddress() + 4, 3, value); } }
        }

        /// <summary> TODO </summary>
        public class MaterialIsland(CommonData saveData, int address, uint id)
        {
            public CommonData SaveData = saveData;
            public int Address = address;
            public uint ID = id;

            public const int CheckedItemCount = 48;

            public int GetAddress() => Address;

            public bool GetCheckedItem(int index)
            {
                if (index < 0 || index >= CheckedItemCount)
                    throw new IndexOutOfRangeException();

                return SaveData.GetBit(GetAddress() + index / 8, index % 8);
            }
            public void GetCheckedItem(int index, bool value)
            {
                if (index < 0 || index >= CheckedItemCount)
                    throw new IndexOutOfRangeException();

                SaveData.SetBit(GetAddress() + index / 8, index % 8, value);
            }

            public byte State { get { return (byte)SaveData.GetNumberBitwise(GetAddress() + 6, 1, 2); } set { SaveData.SetNumberBitwise(GetAddress() + 6, 1, 2, value); } }
        }

        /// <summary> TODO </summary>
        public class PartyMember(CommonData saveData, int address)
        {
            public CommonData SaveData = saveData;
            public int Address = address;

            public int GetAddress() => Address;

            // TODO nu clue what this is
            public ushort ID { get { return SaveData.GetUInt16(GetAddress()); } set { SaveData.SetUInt16(GetAddress(), value); } }
            public ushort Type { get { return SaveData.GetUInt16(GetAddress() + 2); } set { SaveData.SetUInt16(GetAddress() + 2, value); } }
        }

        /// <summary> TODO </summary>
        public class Scenery(CommonData saveData, int address)
        {
            public CommonData SaveData = saveData;
            public int Address = address;

            public int GetAddress() => Address;

            public bool Visit { get { return SaveData.GetByte(GetAddress()) == 1; } set { SaveData.SetByte(GetAddress(), (byte)(value ? 1 : 0)); } }
        }

        /// <summary> TODO </summary>
        public class StoryIsland(CommonData saveData, int address)
        {
            public CommonData SaveData = saveData;
            public int Address = address;

            public int GetAddress() => Address;

            public bool Map { get { return SaveData.GetBit(GetAddress(), 0); } set { SaveData.SetBit(GetAddress(), 0, value); } }
            public bool Move { get { return SaveData.GetBit(GetAddress(), 1); } set { SaveData.SetBit(GetAddress(), 1, value); } }
            public bool New { get { return SaveData.GetBit(GetAddress(), 2); } set { SaveData.SetBit(GetAddress(), 2, value); } }
            public bool Clear { get { return SaveData.GetBit(GetAddress(), 3); } set { SaveData.SetBit(GetAddress(), 3, value); } }
        }
    }
}