using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info.DQB1
{
    public class ResidentInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB1/Residents.json";

        private static ResidentInfo[] _Database { get; set; }

        public ushort ID { get; set; }
        public string Name { get; set; } = "";

        public bool Unknown { get; private set; } = false;

        [JsonConstructor]
        private ResidentInfo() { }
        private ResidentInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<ResidentInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        public static ResidentInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new ResidentInfo(id, true);
        }
        public static ResidentInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }
    }
}