using UnityEditor;
using UnityEngine;

namespace LMirman.Weaver.Attributes
{
	/// <summary>
	/// A custom editor window that makes modifying the <see cref="NetworkContract"/> significantly easier.
	/// </summary>
	/// <remarks>Should this custom editor ever fail, use the inspector's debug mode to modify variables that cause trouble.</remarks>
	[CustomEditor(typeof(NetworkContract))]
	public class NetworkContractEditor : Editor
	{
		SerializedProperty playerObjectIndex, contractItems;
		private int? enqueueRemoval;

		private void OnEnable()
		{
			playerObjectIndex = serializedObject.FindProperty("playerObjectIndex");
			contractItems = serializedObject.FindProperty("contractItems");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.LabelField("Network Contract", EditorStyles.boldLabel);
			if (EditorApplication.isPlaying)
			{
				EditorGUILayout.LabelField("The network contract can't be modified during play mode.");
			}
			else
			{
				EditorGUILayout.PropertyField(playerObjectIndex);

				EditorGUILayout.Space();

				DrawContractItems();

				EditorGUILayout.Space();

				if (GUILayout.Button("Add New Contract Item"))
				{
					contractItems.InsertArrayElementAtIndex(contractItems.arraySize);
				}

				if (GUILayout.Button("Force Contract Update"))
				{
					((NetworkContract)target).UpdateContractID();
				}

				EditorGUILayout.Space();

				DrawVerification();

				if (enqueueRemoval != null)
				{
					int value = (int)enqueueRemoval;
					enqueueRemoval = null;
					contractItems.DeleteArrayElementAtIndex(value);
				}

				serializedObject.ApplyModifiedProperties();
			}
		}

		private void DrawVerification()
		{
			EditorGUILayout.LabelField("Verification", EditorStyles.boldLabel);
			bool valid = true;

			//Verify playerObjectIndex
			if (!VerifyIndex(playerObjectIndex.intValue, contractItems))
			{
				valid = false;
				EditorGUILayout.HelpBox("The player object index is invalid. Verify the index is a non-negative integer and it exists in the contract items.", MessageType.Error);
			}
			else
			{
				EditorGUILayout.LabelField("✔ Network player object index is valid.");
			}

			//Verify contractItems
			MessageType messageType = MessageType.None;
			string message = string.Empty;
			if (!((NetworkContract)target).ValidContractItems(ref message, ref messageType))
			{
				valid = false;
				EditorGUILayout.HelpBox(message, messageType);
			}
			else
			{ 
				EditorGUILayout.LabelField("✔ Network items are valid.");
			}

			//Confirm the entire network contract is valid.
			if (valid)
			{
				EditorGUILayout.LabelField("✔ The network contract is valid.", EditorStyles.boldLabel);
			}
			else
			{
				EditorGUILayout.LabelField("✕ The network contract did not pass validation.", EditorStyles.boldLabel);
			}
		}

		private void DrawContractItems()
		{
			EditorGUILayout.LabelField("Contract Items", EditorStyles.boldLabel);
			for (int i = 0; i < contractItems.arraySize; i++)
			{
				EditorGUILayout.BeginHorizontal();
				DrawContractItemButtons(i);
				DrawContractItemFields(i);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}
		}

		private void DrawContractItemButtons(int i)
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(20));
			if (i == 0)
			{
				GUILayout.Space(22);
			}
			else if (GUILayout.Button("↑"))
			{
				contractItems.MoveArrayElement(i, i - 1);
			}

			if (GUILayout.Button("X"))
			{
				//This has to be enqueued for removal later since we are still iterating over the array at this point.
				enqueueRemoval = i;
			}

			if (i + 1 >= contractItems.arraySize)
			{
				GUILayout.Space(22);
			}
			else if (GUILayout.Button("↓"))
			{
				contractItems.MoveArrayElement(i, i + 1);
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawContractItemFields(int i)
		{
			EditorGUILayout.BeginVertical();
			SerializedProperty item = contractItems.GetArrayElementAtIndex(i);
			SerializedProperty valueProperty = item.FindPropertyRelative("value");
			GameObject value = (GameObject)valueProperty.objectReferenceValue;

			if (value != null)
			{
				EditorGUILayout.PropertyField(valueProperty);
				EditorGUILayout.PropertyField(item.FindPropertyRelative("id"));
				SerializedProperty lookupKey = item.FindPropertyRelative("lookupKey");
				EditorGUILayout.PropertyField(lookupKey);
			}
			else
			{
				EditorGUILayout.HelpBox("No GameObject assigned.", MessageType.Error);
				EditorGUILayout.PropertyField(valueProperty);
			}
			EditorGUILayout.EndVertical();
		}

		private bool VerifyIndex(int index, SerializedProperty array)
		{
			if (index < 0)
			{
				return false;
			}

			for (int i = 0; i < array.arraySize; i++)
			{
				if (array.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue == index)
				{
					return true;
				}
			}

			return false;
		}
	}
}