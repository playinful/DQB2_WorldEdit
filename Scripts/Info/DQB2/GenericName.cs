using Godot;

namespace EyeOfRubiss.Info.DQB2
{
    public static class GenericName
    {
        private const string DATABASE_PATH_MALE = "res://Info/DQB2/MaleNames.txt";
        private const string DATABASE_PATH_FEMALE = "res://Info/DQB2/FemaleNames.txt";

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
}