using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EyeOfRubiss.Info.DQB2
{
    public class BGPartsInfo
    {
        // Note: Dimensions
        // -- 
        // Assuming the prop is facing north.
        // X: West-East. Start with the prop position and head east.
        // Y: Up-Down. Start with the prop position and head up.
        // Z: North-South. Start with the prop position and head north.

        // Props after 3083 are "fake blocks" and can also be used for magnetic blocks

        private const string DATABASE_PATH = "res://Info/DQB2/BGParts.json";

        private static BGPartsInfo[] _Database { get; set; }

        public ushort ID { get; set; }

        public string Name { get; set; } = "";
        public int Icon { get; set; }

        public byte Rarity { get; set; } = 0;
        public bool Connecting { get; set; } = false;
        public DyeColor Color { get; set; } = DyeColor.Plain;

        public int? Mesh { get; set; }

        public int SizeX { get; set; } = 1;
        public int SizeY { get; set; } = 1;
        public int SizeZ { get; set; } = 1;
        public Vector3I GetDimensions()
        {
            return new Vector3I(SizeX, SizeY, SizeZ);
        }

        public PartsType Block { get; set; } = PartsType.Generic;
        public ushort DQB1BGParts { get; set; } = 0;

        public bool Collision { get; set; } = false;
        public bool Effects { get; set; } = false;

        public float Sort { get; set; } = float.MaxValue;

        public bool Unknown { get; set; } = false;

        public bool IsFakeBlock() => ID > 3083;
        public ushort GetFakeBlockID() => IsFakeBlock() ? (ushort)(ID - 3084) : (ushort)0;
        public BlockInfo GetFakeBlockInfo() => IsFakeBlock() ? BlockInfo.Get((ushort)(ID - 3084)) : null;

        [JsonConstructor]
        private BGPartsInfo() { }
        private BGPartsInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
            {
                Mesh = 0;
                Name = "Unknown";
            }
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

            if (id < 3084)
            {
                return _Database.FirstOrDefault(i => i.ID == id) ?? new BGPartsInfo(id, unknown: true);
            }
            else
            {
                BlockInfo blockInfo = BlockInfo.Get((ushort)(id - 3084));
                return new(id)
                {
                    Collision = true,
                    Icon = blockInfo.Icon,
                    Color = blockInfo.Color,
                    Name = blockInfo.Name,
                    Mesh = 0
                };
            }
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