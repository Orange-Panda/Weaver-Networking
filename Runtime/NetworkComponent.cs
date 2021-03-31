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

		private const string DirtyCommand = "1";
		private const string UndirtyCommand = "2";

		public bool IsLocalPlayer => networkID.NetworkReady && NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.LocalPlayerId == networkID.Owner;
		protected bool IsClient => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsClient;
		protected bool IsServer => NetworkCore.ActiveNetwork && NetworkCore.ActiveNetwork.IsServer;
		public int Owner => networkID.Owner;
		/// <summary>
		/// Identifies this network component, avoiding conflicting messages on the same object.
		/// </summary>
		public virtual string ComponentID => "0";
		/// <summary>
		/// The instance ID of the object on the server. Essentially the identifier for this particular object.
		/// </summary>
		public int NetId => networkID.NetId;

		protected bool Dirty { get; private set; }

		/// <summary>
		/// Called after the network engine is ready. It is safe to use the network engine within here.
		/// </summary>
		protected virtual IEnumerator NetworkUpdate() { yield break; }

		/// <summary>
		/// When a special packet has been sent towards this network object, this method is called to behave off of it.
		/// </summary>
		/// <param name="command">The type of message that has been sent to this network object.</param>
		/// <param name="args">The parameter(s) of the message.</param>
		public virtual void HandleMessage(string command, List<string> args) { }

		/// <summary>
		/// Handles packets sent to this particular network object. If it is specially defined it will go to <see cref="HandleMessage(string, List{string})"/>
		/// </summary>
		/// <param name="command">The type of message that has been sent to this network object.</param>
		/// <param name="args">The parameter(s) of the message.</param>
		public void MessageReceived(string command, List<string> args)
		{
			if (IsServer && command.Equals(DirtyCommand))
			{
				Dirty = true;
			}
			else if (IsClient && command.Equals(UndirtyCommand))
			{
				Dirty = false;
				DeserializeData(args);
			}
			else
			{
				HandleMessage(command, args);
			}
		}

		/// <summary>
		/// Traditional Awake() functionality. Called after <see cref="networkID"/> is defined.
		/// </summary>
		/// <remarks>May be called before the network is ready. Use <see cref="NetworkUpdate"/> if you need to ensure network functionality.</remarks>
		protected virtual void OnAwake() { }

		protected void Awake()
		{
			networkID = GetComponent<NetworkID>();
			OnAwake();
		}

		/// <summary>
		/// Traditional Start() functionality.
		/// </summary>
		/// <remarks>May be called before the network is ready. Use <see cref="NetworkUpdate"/> if you need to ensure network functionality.</remarks>
		protected virtual void OnStart() { }

		protected IEnumerator Start()
		{
			OnStart();
			yield return new WaitUntil(() => networkID.NetworkReady);
			MarkDirty();
			StartCoroutine(NetworkUpdate());
		}

		/// <summary>
		/// Send a dirty message to the server.
		/// </summary>
		protected void MarkDirty()
		{
			Dirty = true;
			
			if (IsClient)
			{
				networkID.AddMessage(NetworkMessage.CreateMessage(NetworkMessage.Type.Command, new List<string>() { NetId.ToString(), ComponentID, DirtyCommand }));
			}
		}

		protected virtual void OnEnable()
		{
			NetworkCore.ActiveNetwork.NetworkTick += OnNetworkTick;
		}

		protected virtual void OnDisable()
		{
			NetworkCore.ActiveNetwork.NetworkTick -= OnNetworkTick;
		}

		protected virtual void OnNetworkTick()
		{
			if (Dirty && IsServer)
			{
				Dirty = false;
				SendToClient(UndirtyCommand, SerializeData());
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
				args.Insert(1, ComponentID);
				args.Insert(2, type);
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
				args.Insert(1, ComponentID);
				args.Insert(2, type);
			}

			if (IsServer)
			{
				networkID.AddMessage(NetworkMessage.CreateMessage(NetworkMessage.Type.Update, args));
			}
		}

		/// <summary>
		/// Creates a List of string arguments for the data to be sent over the server when dirty.
		/// </summary>
		protected virtual List<string> SerializeData()
		{
			return null;
		}

		/// <summary>
		/// Deserializes the data sent from <see cref="SerializeData()">
		/// </summary>
		protected virtual void DeserializeData(List<string> args) { }
	}
}
