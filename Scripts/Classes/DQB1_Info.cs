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
    public class BlockInfo
    {
        private const string DATABASE_PATH = "res://Info/Blocks_DQB1.json";

        private static BlockInfo[] _Database { get; set; }

        public byte ID { get; set; }
        public string Name { get; set; } = "";
        public int ImageID { get; set; } = -1;

        public float SortIndex { get; set; } = float.MaxValue;

        public ulong VoxelID { get; set; } = 0;

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

        public AtlasTexture GetIcon() => Util.GetItemIcon(ImageID);
    }
    public class PropInfo
    {
        private const string DATABASE_PATH = "res://Info/Props_DQB1.json";

        private static PropInfo[] _Database { get; set; }

        public ushort ID { get; set; }

        public string Name { get; set; } = "";
        public int Icon { get; set; }

        public int? MeshID { get; set; }

        public int DimensionX { get; set; } = 1;
        public int DimensionY { get; set; } = 1;
        public int DimensionZ { get; set; } = 1;
        public Vector3I GetDimensions()
        {
            return new Vector3I(DimensionX, DimensionY, DimensionZ);
        }

        public PropShell PropShell { get; set; } = PropShell.Generic;

        public float SortIndex { get; set; } = float.MaxValue;

        public bool Unknown { get; set; } = false;

        [JsonConstructor]
        private PropInfo() { }
        private PropInfo(ushort id, bool unknown = false)
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
                _Database = JsonSerializer.Deserialize<PropInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        public static PropInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new PropInfo(id, unknown: true);
        }
        public static PropInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }
    }
}