using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;

public partial class Meshfuck : Node2D
{
	public void Run()
	{
		List<MyFirstThing> things = [];

		MeshLibrary meshLibraryA = ResourceLoader.Load<MeshLibrary>("res://Testing/PropLibraryA.tres");

		foreach (int i in meshLibraryA.GetItemList())
		{
			things.Add(AddThing(meshLibraryA, i));
		}

		MeshLibrary meshLibraryB = ResourceLoader.Load<MeshLibrary>("res://Testing/PropLibraryB.tres");

		foreach (int i in meshLibraryB.GetItemList())
		{
			things.Add(AddThing(meshLibraryB, i));
		}

		File.WriteAllText("TEST.json", JsonSerializer.Serialize(things, options: new JsonSerializerOptions{ WriteIndented = true }));
	}
	private MyFirstThing AddThing(MeshLibrary meshLibraryA, int i)
	{
		MyFirstThing thing = new();
		thing.Name = meshLibraryA.GetItemName(i);
		thing.ID = i;
		if (meshLibraryA.GetItemMesh(i) is ArrayMesh mesh)
		{
			thing.Mesh = "";//mesh.ResourcePath;
			for (int j = 0; j < mesh.GetSurfaceCount(); j++)
			{
				if (mesh.SurfaceGetMaterial(j) is StandardMaterial3D material)
				{
					MySecondThing secondThing = new();
					if (material.AlbedoTexture is not null)
						secondThing.Texture = material.AlbedoTexture.ResourcePath;
					//if (material.NormalTexture is not null)
					//	secondThing.NormalMap = material.NormalTexture.ResourcePath;
					secondThing.Color = material.AlbedoColor.ToHtml();
					secondThing.Transparent = material.Transparency != BaseMaterial3D.TransparencyEnum.Disabled;
					secondThing.BackfaceCulling = material.CullMode == BaseMaterial3D.CullModeEnum.Back;
					if (material.DetailEnabled && material.DetailMask is not null && material.DetailAlbedo is GradientTexture1D gradient)
					{
						secondThing.Mask = material.DetailMask.ResourcePath;
						secondThing.Color = gradient.Gradient.Colors[0].ToHtml();
					}
					thing.Materials.Add(secondThing);
				}
			}
		}
		
		thing.MeshOffsetX = meshLibraryA.GetItemMeshTransform(i).Origin.X;
		thing.MeshOffsetY = meshLibraryA.GetItemMeshTransform(i).Origin.Y;
		thing.MeshOffsetZ = meshLibraryA.GetItemMeshTransform(i).Origin.Z;

		return thing;
	}

	public class MyFirstThing
	{
		public long ID { get; set; }
		public string Name { get; set; }
		public string Mesh { get; set; }
		public List<MySecondThing> Materials { get; set; } = [];
		public float MeshOffsetX { get; set; }
		public float MeshOffsetY { get; set; }
		public float MeshOffsetZ { get; set; }
	}
	public class MySecondThing
	{
		public string Texture { get; set; }
		//public string NormalMap { get; set; }
		public bool Transparent { get; set; } = false;
		public bool BackfaceCulling { get; set; } = true;
		public string Mask { get; set; }
		public string Color { get; set; }
	}
}
