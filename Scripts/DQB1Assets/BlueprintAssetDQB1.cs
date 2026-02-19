using System;
using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;

public class BlueprintAssetDQB1
{
    [JsonPropertyName("m_SizeX")] public int SizeX { get; set; }
    [JsonPropertyName("m_SizeY")] public int SizeY { get; set; }
    [JsonPropertyName("m_SizeZ")] public int SizeZ { get; set; }

    public static BlueprintAssetDQB1 Load(string path)
    {
        return JsonSerializer.Deserialize<BlueprintAssetDQB1>(FileAccess.GetFileAsString(path));
    }

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
            [JsonPropertyName("m_uBGParts")] public byte BGParts { get; set; }
            [JsonPropertyName("m_uDir")] public byte Direction { get; set; }
        }
    }

    public ObjectStruct? GetObjectAtPosition(Vector3I position)
    {
        if (ObjectDictionary.TryGetValue(position, out ObjectStruct result))
        {
            return result;
        }
        return null;
    }

    public Dictionary<Vector3I, ObjectStruct> ObjectDictionary { get; set; }
    public void CreateObjectDictionary(bool forceReload = false)
    {
        if (!(forceReload || ObjectDictionary is null))
            return;

        ObjectDictionary = [];
        foreach (ObjectStruct obj in Objects)
        {
            ObjectDictionary[obj.GetPosition()] = obj;
        }
    }
}