using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// Used to get objects defined in the <see cref="NetworkContract"/> at runtime.
	/// </summary>
	public static class NetworkItems
	{
		/// <summary>
		/// A dictionary of network game objects with a key of integer ContractID and value of GameObject.
		/// </summary>
		public static Dictionary<int, GameObject> Lookup { get; private set; } = new Dictionary<int, GameObject>();

		/// <summary>
		/// A convenient dictionary to convert <see cref="NetworkContract.Item.lookupKey"/> to it's <see cref="NetworkContract.Item.id"/>.
		/// </summary>
		private static Dictionary<string, int> IndexLookup { get; set; } = new Dictionary<string, int>();

		/// <summary>
		/// The object to be spawned when a player connects to the server.
		/// </summary>
		public static GameObject PlayerObject { get; private set; }

		/// <summary>
		/// Try to get an integer id for an object in the <see cref="Lookup"/> dictionary.
		/// </summary>
		/// <param name="key">The string key defined in <see cref="NetworkContract.Item.lookupKey"/></param>
		/// <param name="id">The id found by the function when successful. Otherwise is meaninglessly -9999.</param>
		/// <returns>True when <paramref name="id"/> has been successfully found. Otherwise returns false.</returns>
		public static bool TryGetIndex(string key, out int id)
		{
			id = -9999;
			return !string.IsNullOrWhiteSpace(key) && IndexLookup.TryGetValue(key, out id);
		}

		//Initializes the Dictionary before it is used. See static constructors for more information.
		static NetworkItems()
		{
			NetworkContract contract = Resources.Load<NetworkContract>("Network Contract");
			if (contract)
			{
				contract.UpdateContractID();
				PlayerObject = contract.PlayerObject;

				foreach (NetworkContract.Item item in contract.ContractItems)
				{
					Lookup.Add(item.id, item.value);
					IndexLookup.Add(item.lookupKey, item.id);
				}
			}
			else
			{
				throw new NullReferenceException("No Network Contract found. Please create a Network Contract in Resources folder with the exact name \"Network Contract\".");
			}
		}
	}
}