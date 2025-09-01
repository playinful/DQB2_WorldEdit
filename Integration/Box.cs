using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

record struct Box(Vector3I Start, Vector3I Size)
{
	public Vector3I End => new Vector3I(Start.X + Size.X, Start.Y + Size.Y, Start.Z + Size.Z);
}