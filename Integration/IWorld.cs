using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

interface IWorld
{
	bool HasData(Box box);

	Block GetBlockAtPosition(Vector3I position);

	/// <summary>
	/// When using an IWorld, the camera coordinates will match the Zylann coordinates.
	/// So this property allows the IWorld to declare where the camera should start.
	/// </summary>
	/// <remarks>
	/// Other code applies an XZ transform of -1024,-1024 to the VoxelTerrain node
	/// so that when the camera is at 0,0 it's looking at XZ=1024,1024.
	/// This works nicely for DQB2 StageData because that will always put you near
	/// the center of the island, but it would be slightly confusing for an
	/// integration to have to know this and adjust to put its build near 1024,1024.
	/// </remarks>
	Vector3I InitialCameraPosition { get; }
}
