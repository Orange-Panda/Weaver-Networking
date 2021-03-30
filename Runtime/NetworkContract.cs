using LMirman.Weaver.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.Weaver
{
	/// <summary>
	/// A scriptable object that contains references to all the networked game objects used in the project.
	/// In almost all cases you should look at <see cref="NetworkItems"/> instead.
	/// </summary>
	[CreateAssetMenu(fileName = "Network Contract", menuName = "Network Contract")]
	public class NetworkContract : ScriptableObject
	{
		[DisableInPlayMode, SerializeField, Tooltip("The game object created when a player joins the server.")]
		private int playerObjectIndex;
		[DisableInPlayMode, SerializeField, Tooltip("Game objects to be synchronized over the network.")]
		private Item[] contractItems = new Item[0];

		public GameObject PlayerObject => VerifyIndex() ? contractItems[playerObjectIndex].value : null;
		public Item[] ContractItems => contractItems;

#if UNITY_EDITOR
		/// <summary>
		/// Used in the inspector to verify that all contract items are correct.
		/// </summary>
		public bool ValidContractItems(ref string errorMessage, ref UnityEditor.MessageType messageType)
		{
			UpdateContractID();

			if (contractItems.Length == 0)
			{
				messageType = UnityEditor.MessageType.Warning;
				errorMessage = "There are no items in the contract items array!";
				return false;
			}

			List<int> ids = new List<int>();
			foreach (Item item in contractItems)
			{
				if (item.value == null)
				{
					messageType = UnityEditor.MessageType.Error;
					errorMessage = "An entry does not have a gameObject assigned!";
					return false;
				}
				else if (!item.value.TryGetComponent(out NetworkID networkID))
				{
					messageType = UnityEditor.MessageType.Error;
					errorMessage = $"No NetworkID component found on {item.value.name}. Game objects without a network ID component can't be added to the Network Contract.";
					return false;
				}
				else if (ids.Contains(networkID.ContractID))
				{
					messageType = UnityEditor.MessageType.Error;
					errorMessage = $"Duplicate id detected on {item.value.name}!";
					return false;
				}

				ids.Add(item.id);
			}

			List<string> keys = new List<string>();
			bool validLookup = true;
			foreach (Item item in contractItems)
			{
				if (string.IsNullOrWhiteSpace(item.lookupKey))
				{
					messageType = UnityEditor.MessageType.Error;
					errorMessage += $"Empty lookup key detected on {item.value.name}\n";
					validLookup = false;
				}
				else if (keys.Contains(item.lookupKey))
				{
					messageType = UnityEditor.MessageType.Error;
					errorMessage += $"Duplicate lookup key detected on {item.value.name}\n";
					validLookup = false;
				}

				keys.Add(item.lookupKey);
			}

			return validLookup;
		}

		[UnityEditor.MenuItem("Network Engine/Select Network Contract")]
		public static void SelectNetworkContract()
		{
			NetworkContract contract = Resources.Load<NetworkContract>("Network Contract");
			if (contract == null)
			{
				if (UnityEditor.EditorUtility.DisplayDialog("No Network Contract Found", "There was no network contract found. Would you like to create one?", "Yes", "Cancel"))
				{
					contract = CreateInstance<NetworkContract>();
					if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Network Engine"))
					{
						UnityEditor.AssetDatabase.CreateFolder("Assets", "Network Engine");
					}
					if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Network Engine/Resources"))
					{
						UnityEditor.AssetDatabase.CreateFolder("Assets/Network Engine", "Resources");
					}
					UnityEditor.AssetDatabase.CreateAsset(contract, "Assets/Network Engine/Resources/Network Contract.asset");
					UnityEditor.AssetDatabase.SaveAssets();
					UnityEditor.Selection.activeObject = UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkContract>(UnityEditor.AssetDatabase.GetAssetPath(contract));
				}
			}
			else
			{
				UnityEditor.Selection.activeObject = UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkContract>(UnityEditor.AssetDatabase.GetAssetPath(contract));
			}
		}
#endif

		private bool VerifyIndex()
		{
			return playerObjectIndex >= 0 && playerObjectIndex < contractItems.Length;
		}

		/// <summary>
		/// Forces an update on the contract items, generating contract ids according to their <see cref="NetworkID"/> component.
		/// </summary>
		public void UpdateContractID()
		{
			foreach (Item item in contractItems)
			{
				item.id = item.value == null || !item.value.TryGetComponent(out NetworkID networkID) ? int.MaxValue : networkID.ContractID;
			}
		}

		[Serializable]
		public class Item
		{
			[ReadOnly]
			public int id;
			[Tooltip("A string lookup key that will try to find the id of a particular network item.")]
			public string lookupKey;
			public GameObject value;
		}
	}
}