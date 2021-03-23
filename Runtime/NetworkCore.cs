//NOTE: This file is split between NetworkCore.cs, NetworkCoreServer.cs, and NetworkCoreClient.cs
using NetworkEngine.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// Responsible for managing low level functionality of the network.
	/// </summary>
	/// <remarks>When connected use <see cref="ActiveNetwork"/> to act upon the network.</remarks>
	public partial class NetworkCore : MonoBehaviour
	{
		public static NetworkCore ActiveNetwork { get; private set; }

		[SerializeField, DisableInPlayMode, Range(1, 128), Header("Network Settings")]
		private int maxConnections = 32;
		[SerializeField, DisableInPlayMode, Tooltip("Objects created, with server ownership, when the server is started.")]
		private int[] initialObjects = new int[0];
		public const int UpdateRate = 50;
		public const float UpdateDelta = 1f / UpdateRate;

		public Dictionary<int, TCPConnection> Connections { get; private set; } = new Dictionary<int, TCPConnection>();
		public Dictionary<int, NetworkID> NetObjects { get; private set; } = new Dictionary<int, NetworkID>();
		public event Action<int> ClientConnected = delegate { };
		public event Action<int> ClientDisconnected = delegate { };
		public event Action NetworkTick = delegate { };

		#region Variables
		private Coroutine serverListener;
		private Socket listenerTCP;

		// Network State
		public int LocalPlayerId { get; set; } = -1;
		public bool IsServer { get; private set; } = false;
		public bool IsClient { get; private set; } = false;
		private bool CurrentlyConnecting { get; set; } = false;
		public bool IsConnected { get; private set; } = false;
		public int ConnectionCount { get; set; } = 0;
		public int ObjectCount { get; set; } = 0;

		// Message
		public bool MessageWaiting { get; set; } = false;
		public string MasterMessage { get; set; } = string.Empty;

		// Network configuration
		public string IPAddress { get; private set; } = "127.0.0.1";
		public int Port { get; private set; } = 9001;
		#endregion

		private void Awake()
		{
			ActiveNetwork = this;
		}

		public void OnApplicationQuit()
		{
			LeaveGame();
		}

		/// <summary>
		/// Handles individual commands based on the message's <see cref="NetworkMessage.Parameters"/>
		/// </summary>
		public void HandleCommand(string command)
		{
			NetworkMessage.Parameters param = NetworkMessage.GetParameters(command);
			if (param.type == NetworkMessage.Type.PlayerID)
			{
				if (IsClient && param.args.Count > 0 && int.TryParse(param.args[0], out int id))
				{
					LocalPlayerId = id;
					Connections[0].PlayerID = id;
				}
			}
			else if (param.type == NetworkMessage.Type.Disconnect)
			{
				if (IsServer && int.TryParse(param.args[0], out int disconnectID))
				{
					DisconnectUser(disconnectID);
				}
				else if (IsClient)
				{
					LeaveGame();
				}
			}
			else if (param.type == NetworkMessage.Type.Create)
			{
				if (IsClient && param.args.Count >= 3)
				{
					Vector3 position = Vector3.zero;
					if (param.args.Count >= 6)
					{
						try
						{
							position = new Vector3(float.Parse(param.args[3]), float.Parse(param.args[4]), float.Parse(param.args[5]));
						}
						catch
						{
							position = Vector3.zero;
						}
					}

					Quaternion rotation = Quaternion.identity;
					if (param.args.Count >= 10)
					{
						try
						{
							rotation = new Quaternion(float.Parse(param.args[6]), float.Parse(param.args[7]), float.Parse(param.args[8]), float.Parse(param.args[9]));
						}
						catch
						{
							rotation = Quaternion.identity;
						}
					}

					if (int.TryParse(param.args[0], out int contractID) && int.TryParse(param.args[1], out int owner) && int.TryParse(param.args[2], out int netID))
					{
						GameObject newGameObject = Instantiate(contractID == -1 ? NetworkItems.PlayerObject : NetworkItems.Lookup[contractID], position, rotation);

						if (newGameObject.TryGetComponent(out NetworkID networkID))
						{
							networkID.Owner = owner;
							networkID.NetId = netID;
							NetObjects[netID] = networkID;
						}
					}
				}
			}
			else if (param.type == NetworkMessage.Type.Delete)
			{
				if (IsClient && param.args.Count > 0 && int.TryParse(param.args[0], out int netID))
				{
					DestroyNetworkObject(netID);
				}
			}
			else if ((param.type == NetworkMessage.Type.Command && IsServer) || (param.type == NetworkMessage.Type.Update && IsClient))
			{
				if (param.args.Count >= 2 && int.TryParse(param.args[0], out int netID) && NetObjects.ContainsKey(netID))
				{
					string type = param.args[1];
					param.args.RemoveRange(0, 2);
					NetObjects[netID].NetworkMessage(type, param.args);
				}
			}
		}

		/// <summary>
		/// Leave the game by disconnecting or closing the server.
		/// </summary>
		public void LeaveGame()
		{
			if (IsClient && IsConnected && !Connections[0].IsDisconnecting)
			{
				StartCoroutine(LeaveGameClient());
			}

			if (IsServer && IsConnected && !serverIsDisconnecting)
			{
				StartCoroutine(LeaveGameServer());
			}
		}

		/// <summary>
		/// Destroy an object on the network.
		/// </summary>
		/// <param name="netID">The ID of the object to destroy.</param>
		/// <remarks>
		/// Can work on server and client, however only the server can propogate the message to clients. 
		/// Therefore, on clients this should only be used as a response to a server command.
		/// </remarks>
		public void DestroyNetworkObject(int netID)
		{
			if (NetObjects.ContainsKey(netID))
			{
				Destroy(NetObjects[netID].gameObject);
				NetObjects.Remove(netID);
			}

			if (IsServer)
			{
				MasterMessage += NetworkMessage.CreateMessage(NetworkMessage.Type.Delete, netID.ToString());
			}
		}

		/// <summary>
		/// Send messages to active connections when a message is ready.
		/// </summary>
		private IEnumerator NetworkUpdate()
		{
			float updateTimer = 0;
			while (IsConnected)
			{
				//Compose Master Message
				foreach (NetworkID id in NetObjects.Values)
				{
					MasterMessage += id.EnqueuedMessage;
					id.ClearMessage();
				}

				//Send Master Message
				List<int> invalidConnections = new List<int>();
				if (MasterMessage != string.Empty)
				{
					byte[] byteData = Encoding.ASCII.GetBytes(MasterMessage);
					foreach (KeyValuePair<int, TCPConnection> connection in Connections)
					{
						try
						{
							connection.Value.Send(byteData);
						}
						catch
						{
							invalidConnections.Add(connection.Key);
						}
					}

					MasterMessage = string.Empty;
					MessageWaiting = false;

					foreach (int invalidConnection in invalidConnections)
					{
						DisconnectUser(invalidConnection);
					}
				}

				do
				{
					updateTimer += UpdateDelta;

					while (updateTimer > 0)
					{
						updateTimer = Mathf.MoveTowards(updateTimer, UpdateDelta * -5, Time.deltaTime);
						yield return null;
					}

					NetworkTick();
				}
				while (!MessageWaiting && MasterMessage == string.Empty);
			}
		}

		#region UI Methods
		public void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#else
			Application.Quit();
#endif
		}

		public void SetIP(string value)
		{
			if (!IsConnected)
			{
				IPAddress = value;
			}
		}

		public void SetPort(int value)
		{
			if (!IsConnected)
			{
				Port = value;
			}
		}
		#endregion

		#region OnQuit Handling
		private void OnEnable()
		{
			Application.wantsToQuit += Application_wantsToQuit;
		}

		private void OnDisable()
		{
			Application.wantsToQuit -= Application_wantsToQuit;
		}

		private float forceQuitTime = 0;
		private bool Application_wantsToQuit()
		{
			if (IsConnected && forceQuitTime < Time.unscaledTime)
			{
				forceQuitTime = Time.unscaledTime + 1f; //If stuck, the user can press twice quickly to force close.
				LeaveGame();
				return false;
			}
			else
			{
				return true;
			}
		}
		#endregion
	}
}
