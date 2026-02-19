using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info.DQB2
{
    public class WeatherInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB2/Weather.json";

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
}