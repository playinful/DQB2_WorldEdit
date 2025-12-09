using EyeOfRubiss;
using Godot;
using System;
using System.IO;
using System.Text.Json;
using EyeOfRubiss.Info;
using Microsoft.VisualBasic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

public partial class DQB1DioramaTest : Node3D
{
	public void _On_LoadBlueprint_Button_Pressed()
    {
        string path = GetNode<LineEdit>("Window/VBoxContainer/HBoxContainer/LineEdit").Text;

		DQB1BlueprintAsset blueprint = JsonSerializer.Deserialize<DQB1BlueprintAsset>(Godot.FileAccess.GetFileAsString(path));
		GetNode<VoxelTerrain>("VoxelTerrain").Stream = new VoxelStreamDQB1BlueprintAsset(blueprint);

		PropGridHacky propGrid = GetNode<PropGridHacky>("PropGrid");
		propGrid.Clear();
		propGrid.ClearSubGrid();
		foreach (var obj in blueprint.Objects)
        {
			EyeOfRubiss.Info.DQB1.PropInfo propInfo = EyeOfRubiss.Info.DQB1.PropInfo.Get(obj.Data.Prop);
			if (propInfo.MeshID is int meshId)
            	propGrid.SetCellItemDelegated(obj.GetPosition(), meshId, Util.GridMapRotationFromDirection(obj.Data.Rotation));
        }
    }
	public void _On_LoadDiorama_Button_Pressed()
    {
		string path = GetNode<LineEdit>("Window/VBoxContainer/HBoxContainer2/LineEdit").Text;

        string headerPath = path + "_header.json";
		string dataPath = path + "_data.json";

		var header = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(headerPath));
		var data = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(dataPath));

		int sizeX = header.RootElement.GetProperty("m_sizeX").GetInt32();
		int sizeY = header.RootElement.GetProperty("m_sizeY").GetInt32();
		int sizeZ = header.RootElement.GetProperty("m_sizeZ").GetInt32();
		byte[] blocks = Array.ConvertAll(data.RootElement.GetProperty("m_blocks").EnumerateArray().ToArray(), element => element.GetByte());
		//int[] props = Array.ConvertAll(data.RootElement.GetProperty("m_bgParts").EnumerateArray().ToArray(), element => element.GetInt32());
		BGPartsStruct[] props = JsonSerializer.Deserialize<BGPartsStruct[]>(data.RootElement.GetProperty("m_bgParts"));

		GetNode<VoxelTerrain>("VoxelTerrain").Stream = new VoxelStreamDQB1DioramaAsset(sizeX, sizeY, sizeZ, blocks);
		PropGridHacky propGrid = GetNode<PropGridHacky>("PropGrid");
		propGrid.Clear();
		propGrid.ClearSubGrid();
		foreach (var prop in props)
        {
			EyeOfRubiss.Info.DQB1.PropInfo propInfo = EyeOfRubiss.Info.DQB1.PropInfo.Get(prop.PropID);
			if (propInfo.MeshID is int meshId)
            	propGrid.SetCellItemDelegated(prop.GetPosition(), meshId, Util.GridMapRotationFromDirection(prop.Direction));
        }
    }

	
        public struct BGPartsStruct
        {
            [JsonPropertyName("posX")] public int X { get; set; }
            [JsonPropertyName("posY")] public int Y { get; set; }
            [JsonPropertyName("posZ")] public int Z { get; set; }
			public Vector3I GetPosition() => new Vector3I(X, Y, Z);

			[JsonPropertyName("dir")] public int Direction { get; set; }

			[JsonPropertyName("bgPartsId")] public ushort PropID { get; set; }
        }
}
