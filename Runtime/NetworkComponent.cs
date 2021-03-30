using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.Weaver
{
	/// <summary>
	/// An abstract component which, when inherited from, causes a monobehavior to have network functionality.
	/// </summary>
	/// <remarks>
	///	Requires a <see cref="NetworkID"/> component to be on the gameObject as well.
	/// </remarks>
	[RequireComponent(typeof(NetworkID))]
	public abstract class NetworkComponent : MonoBehaviour
	{
		protected NetworkID networkID;
		protected const string DirtyCommand = "D";

		public bool IsLocalPlayer => networkID.NetworkReady && NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.LocalPlayerId == networkID.Owner;
		protected bool IsClient => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsClient;
		protected bool IsServer => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsServer;
		public int Owner => networkID.Owner;

		/// <summary>
		/// The instance ID of the object on the server. Essentially the identifier for this particular object.
		/// </summary>
		protected int NetId => networkID.NetId;

		/// <summary>
		/// Called after the network engine is ready. It is safe to use the network engine within here.
		/// </summary>
		protected abstract IEnumerator NetworkUpdate();

		/// <summary>
		/// When a packet has been sent towards this network object, this method is called to behave off of it.
		/// </summary>
		/// <param name="command">The type of message that has been sent to this network object.</param>
		/// <param name="args">The parameter(s) of the message.</param>
		public abstract void HandleMessage(string command, List<string> args);

		/// <summary>
		/// Traditional Awake() functionality. Called after <see cref="networkID"/> is defined.
		/// </summary>
		/// <remarks>May be called before the network is ready. Use <see cref="NetworkUpdate"/> if you need to ensure network functionality.</remarks>
		protected virtual void OnAwake() { }

		/// <summary>
		/// Traditional Start() functionality.
		/// </summary>
		/// <remarks>May be called before the network is ready. Use <see cref="NetworkUpdate"/> if you need to ensure network functionality.</remarks>
		protected virtual void OnStart() { }

		protected void Awake()
		{
			networkID = GetComponent<NetworkID>();
			OnAwake();
		}

		protected IEnumerator Start()
		{
			OnStart();
			yield return new WaitUntil(() => networkID.NetworkReady);
			StartCoroutine(NetworkUpdate());
		}

		/// <summary>
		/// Send a dirty message to the server. Dirty must be implemented on a per object basis.
		/// </summary>
		protected void FlagDirtyToServer()
		{
			if (IsClient)
			{
				networkID.AddMessage(NetworkMessage.CreateMessage(NetworkMessage.Type.Command, new List<string>() { NetId.ToString(), DirtyCommand }));
			}
		}

		/// <summary>
		/// Send a message to the server. Only functions if <see cref="IsClient"/> and <see cref="IsLocalPlayer"/>.
		/// </summary>
		/// <param name="type">The type of message to send to clients.</param>
		/// <param name="arg">The value of the message.</param>
		protected void SendToServer(string type, string arg)
		{
			SendToServer(type, new List<string>() { arg });
		}

		/// <summary>
		/// Send a message to the server. Only functions if <see cref="IsClient"/> and <see cref="IsLocalPlayer"/>.
		/// </summary>
		/// <param name="type">The type of message to send to clients.</param>
		/// <param name="args">The value(s) of the message.</param>
		protected void SendToServer(string type, List<string> args = null)
		{
			if (args == null)
			{
				args = new List<string>()
				{
					NetId.ToString(),
					type
				};
			}
			else
			{
				args.Insert(0, NetId.ToString());
				args.Insert(1, type);
			}

			if (IsClient && IsLocalPlayer)
			{
				networkID.AddMessage(NetworkMessage.CreateMessage(NetworkMessage.Type.Command, args));
			}
		}

		/// <summary>
		/// Send a message to all client(s) currently connected. Only functions if <see cref="IsServer"/> is true.
		/// </summary>
		/// <param name="type">The type of message to send to clients.</param>
		/// <param name="arg">The value of the message.</param>
		protected void SendToClient(string type, string arg)
		{
			SendToClient(type, new List<string>() { arg });
		}

		/// <summary>
		/// Send a message to all client(s) currently connected. Only functions if <see cref="IsServer"/> is true.
		/// </summary>
		/// <param name="type">The type of message to send to clients.</param>
		/// <param name="args">The value(s) of the message.</param>
		protected void SendToClient(string type, List<string> args = null)
		{
			if (args == null)
			{
				args = new List<string>()
				{
					NetId.ToString(),
					type
				};
			}
			else
			{
				args.Insert(0, NetId.ToString());
				args.Insert(1, type);
			}

			if (IsServer)
			{
				networkID.AddMessage(NetworkMessage.CreateMessage(NetworkMessage.Type.Update, args));
			}
		}
	}
}
