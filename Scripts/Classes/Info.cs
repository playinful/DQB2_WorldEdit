using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info
{
    public class ItemInfo
    {
        private const string DATABASE_PATH = "res://Info/Items.json";

        private static ItemInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; } = "";
        public int Image { get; set; } = -1;

        public bool Connecting { get; set; } = false;
        public int Rarity { get; set; } = 0;

        public bool ShowInEditor { get; set; } = true;
        public bool ShowAdvanced { get; set; } = false;
        public float SortIndex { get; set; } = float.MaxValue;

        public bool Unknown { get; private set; } = false;

        [JsonConstructor]
        private ItemInfo() { }
        private ItemInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<ItemInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        public static ItemInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new ItemInfo(id, true);
        }
        public static ItemInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }

        public string GetNameRich() => Util.ToRichText(Name);

        public AtlasTexture GetIcon() => Util.GetItemIcon(Image);
    }
    public class BlockInfo
    {
        private const string DATABASE_PATH = "res://Info/Blocks.json";

        private static BlockInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; } = "";
        public int ImageID { get; set; } = -1;

        public string[] Tags { get; set; } = [];
        public float SortIndex { get; set; } = float.MaxValue;

        public ulong VoxelID { get; set; } = 0;

        public bool Unknown { get; set; } = false;

        public Dictionary<string, ushort> Variants { get; set; }
        public ushort? BaseVariant { get; set; }

        public FluidType FluidType { get; set; } = FluidType.Air;
        public FluidLevel FluidLevel { get; set; } = FluidLevel.None;

        [JsonConstructor]
        private BlockInfo() { }
        private BlockInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<BlockInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        public static BlockInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new BlockInfo(id, true);
        }
        public static BlockInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }

        public AtlasTexture GetIcon() => Util.GetItemIcon(ImageID);

        public PropShell GetPropShell()
        {
            // Prop shells start at 1158
            // Liquid types: 8
            // Liquid variations: 11 types per variation
            // 8 * 11 + 1 (air) = 89

            if (ID < 1158)
                return PropShell.None;
            else
                return (PropShell)((ID - 1158) / 89);
        }
    }
    public class PropInfo
    {
        // Note: Dimensions
        // -- 
        // Assuming the prop is facing north.
        // X: West-East. Start with the prop position and head east.
        // Y: Up-Down. Start with the prop position and head up.
        // Z: North-South. Start with the prop position and head north.

        // Props after 3083 are "fake blocks" and can also be used for magnetic blocks

        private const string DATABASE_PATH = "res://Info/Props.json";

        private static PropInfo[] _Database { get; set; }

        public ushort ID { get; set; }

        public string Name { get; set; } = "";
        public int Icon { get; set; }

        public int Rarity { get; set; } = 0;
        public bool Connecting { get; set; } = false;
        public DQB2Color Color { get; set; } = DQB2Color.Plain;

        public int? MeshID { get; set; }

        public int DimensionX { get; set; } = 1;
        public int DimensionY { get; set; } = 1;
        public int DimensionZ { get; set; } = 1;
        public Vector3I GetDimensions()
        {
            return new Vector3I(DimensionX, DimensionY, DimensionZ);
        }

        public PropShell PropShell { get; set; } = PropShell.Generic;

        public float SortIndex { get; set; } = float.MaxValue;

        public bool Unknown { get; set; } = false;

        [JsonConstructor]
        private PropInfo() { }
        private PropInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<PropInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        public static PropInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new PropInfo(id, unknown: true);
        }
        public static PropInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }
    }
    public static class FluidConverter
    {
		private const string DATABASE_PATH = "res://Info/Fluid.json";
        private static ushort[][][] _Database;
        
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<ushort[][][]>(FileAccess.GetFileAsString(DATABASE_PATH));;
            }
        }

        public static ushort Convert(FluidType fluidType, FluidLevel fluidLevel, PropShell propShell)
        {
            LoadDatabase();
            if (fluidType == FluidType.Air)
            {
                ushort result = _Database[(int)propShell + 1][(int)FluidType.MAXIMUM][0];
                GD.Print($"In: {fluidType} ({(int)fluidType}) - {fluidLevel} ({(int)fluidLevel}) | Converting to: {propShell} ({(int)propShell}) | Out: {result} [{BlockInfo.Get(result).Name}]");
                return result;
            }
            else
            {
                ushort result = _Database[(int)propShell + 1][(int)fluidType][(int)fluidLevel];
                GD.Print($"In: {fluidType} ({(int)fluidType}) - {fluidLevel} ({(int)fluidLevel}) | Converting to: {propShell} ({(int)propShell}) | Out: {result} [{BlockInfo.Get(result).Name}]");
                return result;
            }
        }
    }

    public class WeatherInfo
    {
        private const string DATABASE_PATH = "res://Info/Weather.json";

        private static WeatherInfo[] _Database { get; set; }

        public byte ID { get; set; }
        public string Name { get; set; }

        public bool Unknown { get; set; } = false;

        [JsonConstructor]
        private WeatherInfo() { }
        private WeatherInfo(byte id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<WeatherInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        public static WeatherInfo Get(byte id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new WeatherInfo(id, true);
        }
    }

    public static class IslandName
    {
        private const string DATABASE_PATH = "res://Info/IslandName.txt";

        private static string[] _Database { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
        }

        public static string Get(byte idx)
        {
            if (_Database is null)
                LoadDatabase();

            if (idx > _Database.Length)
                return "Undefined Island";
            else
                return _Database[idx];
        }
    }
    public static class ImportantResidentName
    {
        private const string DATABASE_PATH = "res://Info/StoryPeopleNames.txt";

        private static string[] _Database { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
        }

        public static string Get(int idx)
        {
            if (_Database is null)
                LoadDatabase();

            if (idx > _Database.Length || idx < 0)
                return "";
            else
                return _Database[idx];
        }
    }
    public static class GenericName
    {
        private const string DATABASE_PATH_MALE = "res://Info/MaleNames.txt";
        private const string DATABASE_PATH_FEMALE = "res://Info/FemaleNames.txt";

        private static string[] _MaleNames { get; set; }
        private static string[] _FemaleNames { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _MaleNames is null)
                _MaleNames = FileAccess.GetFileAsString(DATABASE_PATH_MALE).Split("\n");
            if (forceReload || _FemaleNames is null)
                _FemaleNames = FileAccess.GetFileAsString(DATABASE_PATH_FEMALE).Split("\n");
        }

        public static string Get(int idx, byte gender) // Gender: 0 = Female, 1 = Male, 1< = Female
        {
            LoadDatabase();

            if (gender == 1)
            {
                if (idx > _MaleNames.Length || idx < 0)
                    return "";
                else
                    return _MaleNames[idx];
            }
            else
            {
                if (idx > _FemaleNames.Length || idx < 0)
                    return "";
                else
                    return _FemaleNames[idx];
            }
        }
    }

    public static class BodyColor
    {
        private const string DATABASE_PATH = "res://Info/color.txt";

        private static Color[] _Database { get; set; }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                string[] color_strings = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
                _Database = new Color[color_strings.Length];
                for (int i = 0; i < _Database.Length; i++)
                {
                    _Database[i] = Color.FromHtml(color_strings[i]);
                }
            }
        }

        public static Color Get(int idx)
        {
            if (_Database is null)
                LoadDatabase();

            if (idx > _Database.Length)
                return new Color(0, 0, 0);
            else
                return _Database[idx];
        }
        public static Color[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }
        public static IEnumerable<Color> GetAllUnique()
        {
            if (_Database is null)
                LoadDatabase();

            return GetAll().Distinct();
        }
    }
}