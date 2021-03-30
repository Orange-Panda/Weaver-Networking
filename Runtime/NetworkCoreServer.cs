//This file contains server exclusive functions.
//This is done to prevent NetworkCore.cs from being too long of a file.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace LMirman.Weaver
{
	public partial class NetworkCore : MonoBehaviour
	{
		private bool serverIsDisconnecting = false;

		/// <summary>
		/// Starts the server if not currently running as a client or server already.
		/// </summary>
		public void StartServer()
		{
			if (!IsConnected)
			{
				serverListener = StartCoroutine(ServerListen());
				StartCoroutine(NetworkUpdate());
			}
		}

		/// <summary>
		/// Instantiate an object over the network.
		/// </summary>
		/// <param name="contractID">The ID of the object inside <see cref="NetworkItems"/></param>
		/// <param name="owner">The owner of the object on the network.</param>
		/// <returns>The <see cref="GameObject"/> instantiated when <see cref="IsServer"/> is true. Otherwise returns null.</returns>
		/// <remarks>Can only be called by the server (determined by <see cref="IsServer"/>)</remarks>
		public GameObject CreateNetworkObject(int contractID, int owner = -1, Vector3 position = default, Quaternion rotation = default)
		{
			if (IsServer)
			{
				GameObject newNetworkObject = Instantiate(contractID != -1 ? NetworkItems.Lookup[contractID] : NetworkItems.PlayerObject, position, rotation);
				if (newNetworkObject.TryGetComponent(out NetworkID networkID))
				{
					networkID.Owner = owner;
					networkID.NetId = ObjectCount;
					NetObjects[ObjectCount] = networkID;
					ObjectCount++;

					List<string> args = new List<string>()
						{
							contractID.ToString(),
							owner.ToString(),
							(ObjectCount - 1).ToString(),
							position.x.ToString("N2"),
							position.y.ToString("N2"),
							position.z.ToString("N2"),
							rotation.x.ToString("N2"),
							rotation.y.ToString("N2"),
							rotation.z.ToString("N2"),
							rotation.w.ToString("N2"),
						};
					MasterMessage += NetworkMessage.CreateMessage(NetworkMessage.Type.Create, args);
				}
				return newNetworkObject;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Destroy all network objects owned by a particular user.
		/// </summary>
		/// <param name="ownerID">The user ID of the client to destroy objects belonging to.</param>
		private void DestroyUserObjects(int ownerID)
		{
			if (IsServer)
			{
				List<int> invalidObjects = new List<int>();
				foreach (KeyValuePair<int, NetworkID> netObject in NetObjects)
				{
					if (netObject.Value.Owner == ownerID)
					{
						invalidObjects.Add(netObject.Key);
					}
				}

				for (int i = 0; i < invalidObjects.Count; i++)
				{
					DestroyNetworkObject(invalidObjects[i]);
				}
			}
		}

		/// <summary>
		/// Runs on the server to allow clients to connect and handle behavior when they do connect.
		/// </summary>
		private IEnumerator ServerListen()
		{
			//If we are listening then we are the server.
			IsServer = true;
			IsClient = false;
			IsConnected = true;
			LocalPlayerId = -1;
			CurrentlyConnecting = false;

			IPAddress ip = System.Net.IPAddress.Any;
			IPEndPoint endPoint = new IPEndPoint(ip, Port);

			//We could do UDP in some cases but for now we will do TCP
			listenerTCP = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try
			{
				listenerTCP.Bind(endPoint);
				listenerTCP.Listen(maxConnections);
			}
			catch (SocketException)
			{
				Debug.LogError("Address already in use.");
				IsConnected = false;
				IsServer = false;
				IsClient = false;
				CurrentlyConnecting = false;
				listenerTCP = null;
				yield break;
			}

			foreach (int initialObject in initialObjects)
			{
				CreateNetworkObject(initialObject);
			}

			while (IsConnected)
			{
				listenerTCP.BeginAccept(new AsyncCallback(ServerListenCallback), listenerTCP);
				yield return new WaitUntil(() => CurrentlyConnecting);
				CurrentlyConnecting = false;

				if (Connections.ContainsKey(ConnectionCount - 1))
				{
					string playerIDMessage = NetworkMessage.CreateMessage(NetworkMessage.Type.PlayerID, Connections[ConnectionCount - 1].PlayerID.ToString());
					Connections[ConnectionCount - 1].Send(Encoding.ASCII.GetBytes(playerIDMessage));

					//Start Server side listening for client messages.
					StartCoroutine(Connections[ConnectionCount - 1].Receive());

					//Update all current network objects
					foreach (KeyValuePair<int, NetworkID> entry in NetObjects)
					{
						List<string> args = new List<string>()
							{
								entry.Value.ContractID.ToString(),
								entry.Value.Owner.ToString(),
								entry.Value.NetId.ToString(),
								entry.Value.transform.position.x.ToString("N2"),
								entry.Value.transform.position.y.ToString("N2"),
								entry.Value.transform.position.z.ToString("N2"),
								entry.Value.transform.rotation.x.ToString("N2"),
								entry.Value.transform.rotation.y.ToString("N2"),
								entry.Value.transform.rotation.z.ToString("N2"),
								entry.Value.transform.rotation.w.ToString("N2")
							};
						string message = NetworkMessage.CreateMessage(NetworkMessage.Type.Create, args);
						Connections[ConnectionCount - 1].Send(Encoding.ASCII.GetBytes(message));
					}

					//Create NetworkPlayerManager
					CreateNetworkObject(-1, ConnectionCount - 1, Vector3.zero);

					ClientConnected(ConnectionCount - 1);
				}
			}
		}

		/// <summary>
		/// Occurs when a client connects to the server.
		/// </summary>
		private void ServerListenCallback(IAsyncResult result)
		{
			Socket listener = (Socket)result.AsyncState;
			Socket handler = listener.EndAccept(result);
			TCPConnection newConnection = new TCPConnection(ConnectionCount, handler);
			ConnectionCount++;
			Connections.Add(newConnection.PlayerID, newConnection);
			CurrentlyConnecting = true;
		}

		/// <summary>
		/// Stops running the server and disconnects all clients.
		/// </summary>
		/// <remarks>Ususally you should use <see cref="LeaveGame"/> instead.</remarks>
		private IEnumerator LeaveGameServer()
		{
			if (IsServer && IsConnected)
			{
				serverIsDisconnecting = true;

				//Send a message to all clients that the server is shutting down.
				List<int> disconnectTargets = new List<int>();
				foreach (int userID in Connections.Keys)
				{
					string disconnectMessage = NetworkMessage.CreateMessage(NetworkMessage.Type.Disconnect, "-1");
					Connections[userID].Send(Encoding.ASCII.GetBytes(disconnectMessage));
					Connections[userID].IsDisconnecting = true;
					disconnectTargets.Add(userID);
				}

				//Waits up to a few seconds for the disconnect message to be sent out. If the timer expires the server will close anyways.
				float forceCloseTimer = 3f;
				while (forceCloseTimer > 0 && Connections.Count > 0)
				{
					forceCloseTimer -= Time.unscaledDeltaTime;
					yield return null;
				}

				//Forcibly disconnects any clients who may have no gotten or responded to the disconnect message.
				foreach (int disconnectTarget in disconnectTargets)
				{
					DisconnectUser(disconnectTarget);
				}

				foreach (NetworkID obj in NetObjects.Values)
				{
					if (obj != null)
					{
						Destroy(obj.gameObject);
					}
				}

				try
				{
					listenerTCP.Close();
				}
				catch {  }

				IsConnected = false;
				IsServer = false;
				IsClient = false;
				CurrentlyConnecting = false;
				serverIsDisconnecting = false;

				NetObjects.Clear();
				Connections.Clear();
			}
		}

		/// <summary>
		/// Forcibly remove a client from this server.
		/// </summary>
		/// <remarks>Only functional on the server. Clients should use <see cref="LeaveGame()"/> instead.</remarks>
		private void DisconnectUser(int connectionID)
		{
			if (IsServer && Connections.TryGetValue(connectionID, out TCPConnection connection))
			{
				try
				{
					connection.Socket.Shutdown(SocketShutdown.Both);
					connection.Socket.Close();
				}
				catch { }

				DestroyUserObjects(connectionID);
				ClientDisconnected(connectionID);
				Connections.Remove(connectionID);
			}
		}
	}
}