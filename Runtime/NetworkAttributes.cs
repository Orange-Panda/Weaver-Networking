using UnityEngine;

namespace NetworkEngine.Attributes
{
	/// <summary>
	/// Prevents a property from ever being modified in the inspector.
	/// </summary>
	public class ReadOnlyAttribute : PropertyAttribute { }

	/// <summary>
	/// Prevents a property from being modified in the inspector when the application is playing.
	/// </summary>
	public class DisableInPlayModeAttribute : PropertyAttribute { }
}