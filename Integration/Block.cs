using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

record struct Block(ushort Value)
{
	public ushort BlockId => (ushort)(Value & 0x7FF);
	public bool PlayerPlaced => (Value & 0x800) >> 11 != 0;
	public StageData.BlockInstance.ChiselType Chisel => (StageData.BlockInstance.ChiselType)((Value & 0xF000) >> 12);
}