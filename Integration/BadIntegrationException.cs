using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeOfRubiss.Integration;

/// <summary>
/// Used when an integration sends us a bad request or bad data.
/// </summary>
sealed class BadIntegrationException : Exception
{
	public BadIntegrationException(string message) : base(message) { }
}
