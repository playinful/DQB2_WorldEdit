using Godot;

namespace EyeOfRubiss.Info.DQB2
{
    public static class SongName
    {
        private const string DATABASE_PATH = "res://Info/DQB2/Songs.txt";

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
        public static string[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return [.. _Database];
        }
    }
}