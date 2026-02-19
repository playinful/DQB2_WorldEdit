using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

namespace EyeOfRubiss.Info.DQB2
{

    public static class BodyColor
    {
        private const string DATABASE_PATH = "res://Info/DQB2/color.txt";

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