using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info.DQB1
{
    public class ItemInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB1/Items.json";

        private static ItemInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; } = "";
        public int Icon { get; set; } = -1;

        public float Sort { get; set; } = float.MaxValue;

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

        public AtlasTexture GetIcon() => Util.GetItemIcon(Icon);
    }
}