using System.Text.Json.Serialization;

namespace EyeOfRubiss
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DQB2Color : int
    {
        Plain = 0,
        White = 1,
        Black = 2,
        Purple = 3,
        Pink = 4,
        Red = 5,
        Green = 6,
        Yellow = 7,
        Blue = 8
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PropShell : int
    {
        None = -1,
        Fence = 0,
        FryingPan = 1,
        Track = 2,
        Roof = 3,
        Table = 4,
        Lighting = 5,
        Fixture = 6,
        Door = 7,
        Unknown = 8,
        Generic = 9
    }

    public enum DQB2Crop : byte
    {
        None = 0,
        Wheat = 1,
        Butterbeans = 2,
        Cabbage = 3,
        Buckwheat = 4,
        Sweetcorn = 5,
        ChilliPepper = 6,
        Sugarcane = 7,
        Strawberry = 8,
        CoffeeBeans = 9,
        Leek = 0xA,
        Potato = 0xB,
        Pumpkin = 0xC,
        Tomato = 0xD,
        Aubergette = 0xE,
        Holyhock = 0xF,
        Melon = 0x10,
        Rice = 0x11,
        Heatroot = 0x12,
        Gladiolus = 0x13,
        MedicinalShrub = 0x14,
        Sunflower = 0x15,
        Plumberry = 0x16,
        PalmTree = 0x17,
        Cedar = 0x18,
        SilverBirch = 0x19,
        Milkblossom = 0x1A,
        Coralily = 0x1B,
        Daffodaisy = 0x1C,
        Wheatgrass = 0x1D,
        Blackbell = 0x1F,
        Purplotus = 0x20,
        Scarletulip = 0x21,
        Emeraldahlia = 0x22,
        Ultramarose = 0x23,
        Cactus = 0x24,
        SnowyCedar = 0x25,
        Tingleweed = 0x27,
        PineTree = 0x28,
        Bamboo = 0x29,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FluidType : int
    {
        Air = -1,
        Water = 0,
        HotWater = 1,
        Poison = 2,
        Lava = 3,
        SwampWater = 4,
        MuddyWater = 5,
        Seawater = 6,
        Plasma = 7,
        MAXIMUM = 8
    }
    public enum FluidLevel : int
    {
        None = -1,
        Full = 0,
        Shallow = 1,
        Surface = 2,
        OneEighth = 3,
        TwoEighths = 4,
        ThreeEighths = 5,
        FourEighths = 6,
        FiveEighths = 7,
        SixEighths = 8,
        SevenEighths = 9,
        EightEighths = 10,
        MAXIMUM = 11
    }
}