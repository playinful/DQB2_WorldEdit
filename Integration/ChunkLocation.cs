using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

record struct ChunkLocation(int X32, int Z32)
{
	public int X => X32 * 32;
	public int Z => Z32 * 32;

	public static ChunkLocation FromPosition(Vector3I position)
	{
		int x32 = position.X / 32;
		int z32 = position.Z / 32;
		return new ChunkLocation(x32, z32);
	}
}