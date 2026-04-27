using System;
using Godot;
using System.Text.Json.Serialization;
using EyeOfRubiss.Info.DQB1;

public class DioramaDataAssetDQB1
{
    [JsonPropertyName("m_blocks")] public int[] Blocks { get; set; }
    [JsonPropertyName("m_bgParts")] public BGPartsStruct[] BGParts { get; set; }

    public struct BGPartsStruct
    {
        [JsonPropertyName("posX")] public int X { get; set; }
        [JsonPropertyName("posY")] public int Y { get; set; }
        [JsonPropertyName("posZ")] public int Z { get; set; }
        [JsonPropertyName("dir")] public byte Direction { get; set; }
        [JsonPropertyName("bgPartsId")] public ushort BGPartsID { get; set; }
    }
}