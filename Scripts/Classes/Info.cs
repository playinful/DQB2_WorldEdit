using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace EyeOfRubiss.Info
{
    /// <summary> Holds information about inventory items. </summary> 
    public class ItemInfo
    {
        /// <summary> Path to the JSON file containing ItemInfo entries. </summary> 
        private const string DATABASE_PATH = "res://Info/Items.json";

        /// <summary> Array of ItemInfo entries. Loaded from DATABASE_PATH using LoadDatabase. </summary> 
        private static ItemInfo[] _Database { get; set; }

        /// <summary> The ushort ID of this item. Used as an identifier for the item in inventories. </summary> 
    	public ushort ID { get; set; }
        /// <summary> The name of the item. </summary> 
    	public string Name { get; set; } = "";
        /// <summary> Index of the icon to use when displaying this item. </summary> 
    	public int ImageID { get; set; } = -1;
        
        /// <summary> Whether or not the item is considered "connecting". Useful to know whether or not to draw the connecting symbol in regards to the item. </summary> 
        public bool Connecting { get; set; } = false;
        /// <summary> In-game "rarity" of the item, should be a value between 0 and 3, equivalent to the amount of stars displayed above an item's icon. </summary> 
        public int Rarity { get; set; } = 0;
        /// <summary> Whether or not the item should be displayed to the user in editors. </summary> 

        public bool ShowInEditor { get; set; } = true;
        /// <summary> Whether or not the item should only be displayed to the user in editors when set to Advanced Mode. </summary> 
        public bool ShowAdvanced { get; set; } = false;
        /// <summary> Index to use when sorting the item in editors. </summary> 
        public float SortIndex { get; set; } = float.MaxValue;

        /// <summary> True if the item does not exist in the database. </summary> 
        public bool Unknown { get; private set; } = false;

        /// <summary> Parameterless constructor for use with JSON. </summary> 
        [JsonConstructor]
        private ItemInfo(){}
        /// <summary>  
        /// Constructor for the ItemInfo class.
        /// </summary>  
        /// <param name="id">ID of the item.</param>  
        /// <param name="unknown">True if the item does not exist in the Database. False by default.</param>  
        private ItemInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param>  
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<ItemInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        /// <summary>  
        /// Look up an ItemInfo instance by its ID. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="id">The ID of the item to search for.</param>  
        /// <returns>The first item in the Database with the specified ID. If no item exists, returns a new ItemInfo instance with its "Unknown" field set to true.</returns>  
        public static ItemInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new ItemInfo(id, true);
        }
        /// <returns>The entire Database. Automatically calls LoadDatabase if Database is null.</returns>  
        public static ItemInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();
            
            return _Database;
        }
        
        /// <returns>The name of the item as a string with proper decoration to be applied to a RichTextLabel.</returns>  
        public string GetNameRich() => Util.ToRichText(Name);

        /// <returns>An AtlasTexture displaying the Item's icon.</returns>  
        public AtlasTexture GetIcon() => Util.GetItemIcon(ImageID);
    }
    /// <summary> Holds information about blocks. </summary> 
    public class BlockInfo
    {
        /// <summary> Path to the JSON file containing BlockInfo entries. </summary> 
        private const string DATABASE_PATH = "res://Info/Blocks.json";

        /// <summary> Array of BlockInfo entries. Loaded from DATABASE_PATH using LoadDatabase. </summary> 
        private static BlockInfo[] _Database { get; set; }

        /// <summary> The ushort ID of this block. Used as an identifier in block data. </summary> 
    	public ushort ID { get; set; }
        /// <summary> The name of this block. Used when displaying the block in GUI. </summary> 
    	public string Name { get; set; } = "";
        /// <summary> Index of the icon to use when displaying this block. </summary> 
    	public int ImageID { get; set; } = -1;

        /// <summary> An array of string tags for use in search and categorisation. </summary> 
        public string[] Tags { get; set; } = [];
        /// <summary> Index to use when sorting the block in editors. </summary> 
        public float SortIndex { get; set; } = float.MaxValue;

        /// <summary> ID of the Voxel type value to use for this block in VoxelTerrain nodes. </summary>
        public ulong VoxelID { get; set; } = 0;

        /// <summary> True if the block does not exist in the database. </summary> 
        public bool Unknown { get; set; } = false;

        /// <summary> A dictionary of variants of this block (represented by IDs). Used to consolidate UI elements. </summary> 
    	public Dictionary<string, ushort> Variants { get; set; }
        /// <summary> The ID of the "base variant" of a block (e.g. for "White Carpet", the BaseVariant would be "Carpet".) Null if block is not a variant. </summary>
    	public ushort? BaseVariant { get; set; }

        /// <summary> Parameterless constructor for use with JSON. </summary> 
        [JsonConstructor]
        private BlockInfo() { }
        /// <summary>  
        /// Constructor for the BlockInfo class.
        /// </summary>  
        /// <param name="id">ID of the block.</param>  
        /// <param name="unknown">True if the block does not exist in the Database. False by default.</param>  
        private BlockInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param>  
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = JsonSerializer.Deserialize<BlockInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
        }

        /// <summary>  
        /// Look up an BlockInfo instance by its ID. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="id">The ID of the block to search for.</param>  
        /// <returns>The first block in the Database with the specified ID. If no block exists, returns a new BlockInfo instance with its "Unknown" field set to true.</returns>  
        public static BlockInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new BlockInfo(id, true);
        }
        /// <returns>The entire Database. Automatically calls LoadDatabase if Database is null.</returns>  
        public static BlockInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }

        /// <returns>An AtlasTexture displaying the Block's icon.</returns>  
        public AtlasTexture GetIcon() => Util.GetItemIcon(ImageID);

        public PropShell PropShell { get; set; } = PropShell.None;
    }
    /// <summary> Holds information about props. </summary> 
    public class PropInfo
    {
        // Note: Dimensions
        // -- 
        // Assuming the prop is facing north.
        // X: West-East. Start with the prop position and head east.
        // Y: Up-Down. Start with the prop position and head up.
        // Z: North-South. Start with the prop position and head north.

        /// <summary> Path to the JSON file containing PropInfo entries. </summary> 
        private const string DATABASE_PATH = "res://Info/Props.json";

        /// <summary> Array of PropInfo entries. Loaded from DATABASE_PATH using LoadDatabase. </summary> 
        private static PropInfo[] _Database { get; set; }

        /// <summary> The ushort ID of this prop, used as an identifier in the StageData. </summary> 
    	public ushort ID { get; set; }
        /// <summary> The name of this prop. Used when displaying the prop in GUI. </summary> 
    	public string Name { get; set; } = "";

        [JsonIgnore] public Vector3I Dimensions { get; set; } = Vector3I.One;

        //public Vector3I Dimensions { get; set; }
        public int? MeshID { get; set; }

        /// <summary> True if the block does not exist in the database. </summary> 
        public bool Unknown { get; set; } = false;

        /// <summary> Parameterless constructor for use with JSON. </summary> 
        [JsonConstructor]
        private PropInfo() { }
        /// <summary>  
        /// Constructor for the PropInfo class.
        /// </summary>  
        /// <param name="id">ID of the prop.</param>  
        /// <param name="mesh">Path to the PropMeshResource. Null by default.</param>  
        /// <param name="unknown">True if the prop does not exist in the Database. False by default.</param>  
        private PropInfo(ushort id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param>  
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<PropInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        /// <summary>  
        /// Look up a PropInfo instance by its ID. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="id">The ID of the prop to search for.</param>  
        /// <returns>The first prop in the Database with the specified ID. If no prop exists, returns a new PropInfo instance with its "Unknown" field set to true.</returns> 
        public static PropInfo Get(ushort id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new PropInfo(id, unknown: true);
        }
        /// <returns>The entire Database. Automatically calls LoadDatabase if Database is null.</returns>  
        public static PropInfo[] GetAll()
        {
            if (_Database is null)
                LoadDatabase();

            return _Database;
        }

        public PropShell PropShell { get; set; } = PropShell.Generic;
    }
    /// <summary> Holds information about weather. </summary> 
    public class WeatherInfo
    {
        /// <summary> Path to the JSON file containing WeatherInfo entries. </summary> 
        private const string DATABASE_PATH = "res://Info/Weather.json";

        /// <summary> Array of WeatherInfo entries. Loaded from DATABASE_PATH using LoadDatabase. </summary> 
        private static WeatherInfo[] _Database { get; set; }

        /// <summary> The byte ID of this weather. </summary> 
        public byte ID { get; set; }
        /// <summary> The name of this weather, used in GUI. </summary> 
        public string Name { get; set; }

        /// <summary> True if the weather does not exist in the database. </summary> 
        public bool Unknown { get; set; } = false;

        /// <summary> Parameterless constructor for use with JSON. </summary> 
        [JsonConstructor]
        private WeatherInfo() { }
        /// <summary>  
        /// Constructor for the WeatherInfo class.
        /// </summary>  
        /// <param name="id">ID of the weather.</param>  
        /// <param name="unknown">True if the weather does not exist in the Database. False by default.</param>  
        private WeatherInfo(byte id, bool unknown = false)
        {
            ID = id;
            Unknown = unknown;
            if (unknown)
                Name = "Unknown";
        }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param>  
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
            {
                _Database = JsonSerializer.Deserialize<WeatherInfo[]>(FileAccess.GetFileAsString(DATABASE_PATH));
            }
        }

        /// <summary>  
        /// Look up a WeatherInfo instance by its ID. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="id">The ID of the weather to search for.</param>  
        /// <returns>The first weather in the Database with the specified ID. If no weather exists, returns a new WeatherInfo instance with its "Unknown" field set to true.</returns> 
        public static WeatherInfo Get(byte id)
        {
            if (_Database is null)
                LoadDatabase();

            return _Database.FirstOrDefault(i => i.ID == id) ?? new WeatherInfo(id, true);
        }
    }

    /// <summary> Static class for looking up island names. </summary> 
    public static class IslandName
    {
        /// <summary> Path to the TXT file listing island names. </summary> 
        private const string DATABASE_PATH = "res://Info/IslandName.txt";

        /// <summary> Array of names as string. Loaded from DATABASE_PATH using LoadDatabase. </summary> 
        private static string[] _Database { get; set; }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param>  
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
        }

        /// <summary>  
        /// Look up an island name by its index. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="idx">The index of the name.</param>  
        /// <returns>The name of the island at the specified index. Returns "Undefined Island" if out of bounds.</returns> 
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
    /// <summary> Static class for looking up names of important residents. </summary> 
    public static class ImportantResidentName
    {
        /// <summary> Path to the TXT file listing important resident names. </summary> 
        private const string DATABASE_PATH = "res://Info/StoryPeopleNames.txt";
        
        /// <summary> Array of names as string. Loaded from DATABASE_PATH using LoadDatabase. </summary> 
        private static string[] _Database { get; set; }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param> 
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _Database is null)
                _Database = FileAccess.GetFileAsString(DATABASE_PATH).Split("\n");
        }

        /// <summary>  
        /// Look up a resident's name by its index. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="idx">The index of the name.</param>  
        /// <returns>The resident's name at the specified index. Returns an empty string if out of bounds.</returns> 
        public static string Get(int idx)
        {
            if (_Database is null)
                LoadDatabase();

            if (idx > _Database.Length || idx < 0)
                return "";
            else
                return _Database[idx];
        }
    }
    /// <summary> Static class for looking up names of generic residents. </summary> 
    public static class GenericName
    {
        /// <summary> Path to the TXT file listing names of generic male residents. </summary> 
        private const string DATABASE_PATH_MALE = "res://Info/MaleNames.txt";
        /// <summary> Path to the TXT file listing names of generic female residents. </summary> 
        private const string DATABASE_PATH_FEMALE = "res://Info/FemaleNames.txt";
        
        /// <summary> Array of male names as string. Loaded from DATABASE_PATH_MALE using LoadDatabase. </summary> 
        private static string[] _MaleNames { get; set; }
        /// <summary> Array of female names as string. Loaded from DATABASE_PATH_FEMALE using LoadDatabase. </summary> 
        private static string[] _FemaleNames { get; set; }

        /// <summary>  
        /// Populates the Database field by loading JSON file at DATABASE_PATH.
        /// </summary>  
        /// <param name="forceReload">If true, discards any existing Database and loads it anew. Otherwise, skips loading if the Database already exists.</param> 
        public static void LoadDatabase(bool forceReload = false)
        {
            if (forceReload || _MaleNames is null)
                _MaleNames = FileAccess.GetFileAsString(DATABASE_PATH_MALE).Split("\n");
            if (forceReload || _FemaleNames is null)
                _FemaleNames = FileAccess.GetFileAsString(DATABASE_PATH_FEMALE).Split("\n");
        }

        /// <summary>  
        /// Look up a resident's name by its index. Automatically calls LoadDatabase if Database is null.
        /// </summary>  
        /// <param name="idx">The index of the name.</param>  
        /// <param name="gender">A byte corresponding to the gender of name to get. If 1, the name is selected from _MaleNames. Otherwise, the name is selected from _FemaleNames.</param>  
        /// <returns>The generic name of the specified gender at the specified index. Returns an empty string if out of bounds.</returns> 
        public static string Get(int idx, byte gender)
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