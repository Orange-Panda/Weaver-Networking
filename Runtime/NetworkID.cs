using LMirman.Weaver.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.Weaver
{
	/// <summary>
	/// Used to identify a specific object on the network.
	/// </summary>
	/// <remarks>Not to be confused with <see cref="NetId"/> which is the integer identifier, while this is the Monobehavior component that references it.</remarks>
	public sealed class NetworkID : MonoBehaviour
	{
		[DisableInPlayMode, SerializeField, Min(0), Tooltip("The unique contract id for this gameObject. Allows all clients to create the same object on each instance.")]
		private int contractID;

		/// <summary>
		/// All NetworkComponents on this gameObject.
		/// </summary>
		/// <remarks>This is initialized on Start(), therefore adding new NetworkComponents must be done before Start occurs.</remarks>
		private NetworkComponent[] components;

		// Properties
		/// <summary>
		/// The identifier for this particular instance of an object over the network.
		/// </summary>
		public int NetId { get; set; } = -10;
		/// <summary>
		/// The network user responsible for controlling this gameObject.
		/// </summary>
		public int Owner { get; set; } = -10;
		public bool NetworkReady { get; private set; }
		/// <summary>
		/// Enqueued messages are sent on the next <see cref="NetworkCore.NetworkTick"/>.
		/// </summary>
		public string EnqueuedMessage { get; private set; } = string.Empty;

		// Get properties
		/// <summary>
		/// The unique contract id for this gameObject. Allows all clients to create the same object on each instance.
		/// </summary>
		public int ContractID => contractID;
		private bool IsServer => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsServer;
		private bool IsClient => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsClient;

		private IEnumerator Start()
		{
			yield return new WaitUntil(() => IsServer || IsClient);

			if (IsClient && NetId == -10)
			{
				Destroy(gameObject);
			}
			else if (IsServer && NetId == -10)
			{
				if (NetworkItems.Lookup.ContainsKey(contractID))
				{
					Owner = -1;
					NetId = NetworkCore.ActiveNetwork.ObjectCount;
					NetworkCore.ActiveNetwork.ObjectCount++;
					NetworkCore.ActiveNetwork.NetObjects.Add(NetId, this);
				}
				else
				{
					throw new System.Exception("FATAL - Game Object not found in network contract!");
				}
			}

			yield return new WaitUntil(() => Owner != -10 && NetId != -10);

			components = GetComponents<NetworkComponent>();
			NetworkReady = true;
		}

		/// <summary>
		/// Add a message to <see cref="EnqueuedMessage"/> to be sent over the network.
		/// </summary>
		/// <param name="msg">A formatted message to send over the network. Usually this should be created by <see cref="NetworkMessage.GetParameters(string)"/></param>
		public void AddMessage(string msg)
		{
			EnqueuedMessage += msg;
			NetworkCore.ActiveNetwork.MessageWaiting = true;
		}

		/// <summary>
		/// Clears <see cref="EnqueuedMessage"/>.
		/// </summary>
		/// <remarks>Ususally exclusively done after the message has been sent by <see cref="NetworkCore"/>.</remarks>
		public void ClearMessage()
		{
			EnqueuedMessage = string.Empty;
		}

		/// <summary>
		/// Handle an incoming message from the network on this particular network object.
		/// </summary>
		/// <param name="command">The local command defined on a per <see cref="NetworkComponent"/> basis.</param>
		/// <param name="args">Argument(s) for the command defined.</param>
		public void NetworkMessage(string componentID, string command, List<string> args)
		{
			if (NetworkReady)
			{
				if (IsServer && NetworkCore.ActiveNetwork.Connections.ContainsKey(Owner) == false && Owner != -1)
				{
					NetworkCore.ActiveNetwork.DestroyNetworkObject(NetId);
				}
				else
				{
					foreach (NetworkComponent component in components)
					{
						if (component.ComponentID == componentID)
						{
							component.MessageReceived(command, args);
						}
					}
				}
			}
		}
	}
}