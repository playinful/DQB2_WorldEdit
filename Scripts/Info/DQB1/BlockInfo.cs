using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info.DQB1
{
    public class BlockInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB1/Blocks.json";

        private static BlockInfo[] _Database { get; set; }

        public byte ID { get; set; }
        public string Name { get; set; } = "";
        public int Icon { get; set; } = -1;

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

        public AtlasTexture GetIcon() => Util.GetItemIcon(Icon);
    }
}