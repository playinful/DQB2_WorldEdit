using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info.DQB2
{
    public class BlockInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB2/Blocks.json";

        private static BlockInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; } = "";
        public int Icon { get; set; } = -1;

        public string[] Tags { get; set; } = [];
        public float Sort { get; set; } = float.MaxValue;

        public ulong Voxel { get; set; } = 0;

        public bool Unknown { get; set; } = false;

        public Dictionary<string, ushort> Variants { get; set; }
        public ushort? BaseVariant { get; set; }

        public FluidType FluidType { get; set; } = FluidType.Air;
        public FluidLevel FluidLevel { get; set; } = FluidLevel.None;

        public byte DQB1Block { get; set; } = 0;

        public byte Rarity { get; set; } = 0;
        public DyeColor Color { get; set; } = DyeColor.Plain;

        [JsonConstructor]
        private BlockInfo() { }
        private BlockInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
            {
                Voxel = 2;
                Name = "Unknown";
            }
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

        public static IEnumerable<BlockInfo> SearchByText(string text)
        {
            if (_Database is null)
                LoadDatabase();
            
            if (string.IsNullOrEmpty(text))
                return GetAll();
            
            string searchText = text.ToLowerInvariant().Trim().Replace(" ", "");
            return _Database.Where(info =>
            {
                string nameKey = info.Name.ToLowerInvariant().Trim().Replace(" ", "");
                return nameKey.Contains(searchText);
            });
        }

        public AtlasTexture GetIcon() => Util.GetItemIcon(Icon);

        public PartsType GetPartsType()
        {
            // Prop shells start at 1158
            // Liquid types: 8
            // Liquid variations: 11 types per variation
            // 8 * 11 + 1 (air) = 89

            if (ID < 1158)
                return PartsType.None;
            else
                return (PartsType)((ID - 1158) / 89);
        }
    }
}