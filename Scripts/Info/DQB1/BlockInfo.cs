using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using System.Collections.Generic;

namespace EyeOfRubiss.Info.DQB1
{
    public class BlockInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB1/Blocks.json";

        private static BlockInfo[] _Database { get; set; }

        public byte ID { get; set; }
        public string Name { get; set; } = "";
        public int Icon { get; set; } = -1;

        public PartsType PartsType { get; set; } = PartsType.None;
        public ushort DQB2Block { get; set; } = 0;

        public float Sort { get; set; } = float.MaxValue;

        public ulong Voxel { get; set; } = 0;

        public bool Unknown { get; set; } = false;

        [JsonConstructor]
        private BlockInfo() { }
        private BlockInfo(byte id, bool unknown = false)
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

        public static BlockInfo Get(byte id)
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
    }
}