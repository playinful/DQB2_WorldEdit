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
        None,
        Generic,
        Fixture,
        Door,
        Roof,
        Lighting,
        Table,
        Track,
        Fence,
        FryingPan,
        Unknown
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
        Heatroot = 0x12
    }
}