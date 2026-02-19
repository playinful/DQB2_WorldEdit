using Godot;

namespace EyeOfRubiss.Info.DQB2
{
    public static class IslandName
    {
        private const string DATABASE_PATH = "res://Info/DQB2/IslandName.txt";

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
}