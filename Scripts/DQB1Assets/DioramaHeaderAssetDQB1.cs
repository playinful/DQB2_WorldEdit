using System;
using Godot;
using System.Text.Json.Serialization;
using System.ComponentModel;

public class DioramaHeaderAssetDQB1
{
    [JsonPropertyName("m_Name")] public string Name { get; set; } = "Diorama";
    [JsonPropertyName("m_sizeX")] public int SizeX { get; set; }
    [JsonPropertyName("m_sizeY")] public int SizeY { get; set; }
    [JsonPropertyName("m_sizeZ")] public int SizeZ { get; set; }
}