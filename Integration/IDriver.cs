using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

interface IDriver : IDisposable
{
	public event EventHandler<WorldUpdatedEventArgs> WorldUpdated;
	public IWorld World { get; }
}

sealed class WorldUpdatedEventArgs
{
	public IWorld World { get; init; }
}