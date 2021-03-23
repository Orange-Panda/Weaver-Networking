using UnityEditor;
using UnityEngine;

namespace NetworkEngine.Attributes
{
	/// <summary>
	/// Prevents a property from ever being modified in the inspector.
	/// </summary>
	/// <remarks>See also: <see cref="ReadOnlyAttribute"/></remarks>
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUI.enabled = false;
			EditorGUI.PropertyField(position, property, label);
			GUI.enabled = true;
		}
	}

	/// <summary>
	/// Prevents a property from being modified in the inspector when the application is playing.
	/// </summary>
	/// <remarks>See also: <see cref="DisableInPlayModeAttribute"/></remarks>
	[CustomPropertyDrawer(typeof(DisableInPlayModeAttribute))]
	public class DisableInPlayModePropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			GUI.enabled = !EditorApplication.isPlaying;
			EditorGUI.PropertyField(position, property, label);
			GUI.enabled = true;
		}
	}
}