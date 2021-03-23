using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// Extension methods that are useful for sending data over the network.
	/// </summary>
	public static class NetworkingExtensions
	{
		/// <summary>
		/// Minimizes the amount of space taken up by a float value by trimming unnecessary precision.
		/// </summary>
		/// <returns>A trimmed string for sending over the network.</returns>
		public static string ToNetworkString(this float value, int maxPrecision = 2)
		{
			return value.ToString($"N{maxPrecision}").TrimEnd('0').TrimEnd('.');
		}

		/// <summary>
		/// Rounds a Vector3 to a certain point of precision.
		/// </summary>
		/// <param name="value">The vector to round</param>
		/// <param name="maxPrecision">The degree to round it. (a precision of 2 would round the number 3.432 to 3.43)</param>
		/// <returns>A rounded Vector3.</returns>
		/// <remarks>This is usually used to determine if a Vector3 has changed enough to justify sending it over the network again.</remarks>
		public static Vector3 Rounded(this Vector3 value, int maxPrecision = 2)
		{
			float scale = Mathf.Pow(10, maxPrecision);
			value *= scale;
			value = new Vector3(Mathf.Round(value.x), Mathf.Round(value.y), Mathf.Round(value.z));
			return value / scale;
		}
	}
}