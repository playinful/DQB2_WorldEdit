using System;
using Godot;
using System.Text.Json.Serialization;

public class DioramaHeaderAssetDQB1
{
    [JsonPropertyName("m_sizeX")] public int SizeX { get; set; }
    [JsonPropertyName("m_sizeY")] public int SizeY { get; set; }
    [JsonPropertyName("m_sizeZ")] public int SizeZ { get; set; }
}