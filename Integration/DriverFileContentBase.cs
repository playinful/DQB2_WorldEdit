using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

static class IntegrationTypeConstants
{
	public const string FSWatcher = "FSWatcher";
}

class DriverFileContentBase
{
	/// <summary>
	/// Must be one of the <see cref="IntegrationTypeConstants"/> values.
	/// </summary>
	public required string IntegrationType { get; init; }
}
