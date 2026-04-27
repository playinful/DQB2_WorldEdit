using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info.DQB1
{
    public class BGPartsInfo
    {
        private const string DATABASE_PATH = "res://Info/DQB1/BGParts.json";

        private static BGPartsInfo[] _Database { get; set; }

        public ushort ID { get; set; }

        public string Name { get; set; } = "";
        public int Icon { get; set; }

        public int? Mesh { get; set; }

        public int SizeX { get; set; } = 1;
        public int SizeY { get; set; } = 1;
        public int SizeZ { get; set; } = 1;
        public Vector3I GetDimensions()
        {
            return new Vector3I(SizeX, SizeY, SizeZ);
        }

        public PartsType Block { get; set; } = PartsType.Generic;
        public byte GetPartsBlockID() => GetPartsBlockID(Block);
        public static byte GetPartsBlockID(PartsType partsType) => partsType switch
        {
            PartsType.Roof     => 249,
            PartsType.Table    => 250,
            PartsType.Lighting => 251,
            PartsType.Fixture  => 252,
            PartsType.Door     => 253,
            PartsType.Unknown  => 254,
            PartsType.Generic  => 255,
            _ => 0
        };
        public ushort DQB2BGParts { get; set; } = 0;

        public bool Collision { get; set; } = false;
        public bool Effects { get; set; } = false;

        public float Sort { get; set; } = float.MaxValue;

        public bool Unknown { get; set; } = false;

        [JsonConstructor]
        private BGPartsInfo() { }
        private BGPartsInfo(ushort id, bool unknown = false)
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
                _Database = JsonSerializer.Deserialize<BGPartsInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        public static BGPartsInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new BGPartsInfo(id, unknown: true);
        }
        public static BGPartsInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }
        
        public static IEnumerable<BGPartsInfo> SearchByText(string text)
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
    }
}