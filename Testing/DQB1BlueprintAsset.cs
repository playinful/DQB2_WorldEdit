using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public class DQB1BlueprintAsset
{
    [JsonPropertyName("m_SizeX")] public int SizeX { get; set; }
    [JsonPropertyName("m_SizeY")] public int SizeY { get; set; }
    [JsonPropertyName("m_SizeZ")] public int SizeZ { get; set; }

    [JsonPropertyName("m_Objects")] public ObjectStruct[] Objects { get; set; }
    public struct ObjectStruct
    {
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("y")] public int Y { get; set; }
        [JsonPropertyName("z")] public int Z { get; set; }
        public Vector3I GetPosition() => new(X, Y, Z);

        [JsonPropertyName("data")] public ObjectDataStruct Data { get; set; }
        public struct ObjectDataStruct
        {
            [JsonPropertyName("m_uBlock")] public byte Block { get; set; }
            [JsonPropertyName("m_uBGParts")] public byte Prop { get; set; }
            [JsonPropertyName("m_uDir")] public byte Rotation { get; set; }
        }
    }

    public Dictionary<Vector3I, byte> BlockDictionary { get; set; }
    public void CreateBlockDictionary()
    {
        BlockDictionary = [];
        foreach (ObjectStruct obj in Objects)
        {
            BlockDictionary.Add(obj.GetPosition(), obj.Data.Block);
        }
    }
}