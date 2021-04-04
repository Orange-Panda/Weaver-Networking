//This file contains client exclusive functions.
//This is done to prevent NetworkCore.cs from being too long of a file.
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace LMirman.Weaver
{
	public partial class NetworkCore : MonoBehaviour
	{
		/// <summary>
		/// Start a client instance, if not currently connected.
		/// </summary>
		public void StartClient()
		{
			if (!IsConnected && !CurrentlyConnecting)
			{
				StartCoroutine(ConnectClient());
			}
		}

		/// <summary>
		/// Handles the connection of Client to the Server.
		/// </summary>
		private IEnumerator ConnectClient()
		{
			IsServer = false;
			IsClient = false;
			IsConnected = false;
			CurrentlyConnecting = true;

			//Setup our socket
			IPAddress ip = System.Net.IPAddress.Parse(IPAddress);
			IPEndPoint endPoint = new IPEndPoint(ip, Port);
			Socket clientSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			//Connect client
			clientSocket.BeginConnect(endPoint, ConnectingCallback, clientSocket);

			//Wait for the client to connect
			yield return new WaitUntil(() => IsConnected || !CurrentlyConnecting);
			if (IsConnected)
			{
				CurrentlyConnecting = false;
				StartCoroutine(Connections[0].Receive());  //It is 0 on the client because we only have 1 socket.
				StartCoroutine(NetworkUpdate());  //This will allow the client to send messages to the server.
			}
			else
			{
				IsClient = false;
				IsServer = false;
				IsConnected = false;
				CurrentlyConnecting = false;
			}
		}

		/// <summary>
		/// Fired when the client has successfully connected to the server.
		/// </summary>
		private void ConnectingCallback(IAsyncResult result)
		{
			try
			{
				TCPConnection newConnection = new TCPConnection(-1, (Socket)result.AsyncState);
				newConnection.Socket.EndConnect(result);
				Connections.Add(0, newConnection);
				IsClient = true;
				IsConnected = true;
			}
			catch (SocketException)
			{
				IsClient = false;
				IsServer = false;
				IsConnected = false;
				CurrentlyConnecting = false;
			}
		}

		/// <summary>
		/// Disconnects the client from the server.
		/// </summary>
		/// <remarks>Ususally you should use <see cref="LeaveGame"/> instead.</remarks>
		private IEnumerator LeaveGameClient()
		{
			if (IsClient)
			{
				try
				{
					Connections[0].IsDisconnecting = true;
					string disconnectMessage = NetworkMessage.CreateMessage(NetworkMessage.Type.Disconnect, Connections[0].PlayerID.ToString());
					Connections[0].Send(Encoding.ASCII.GetBytes(disconnectMessage));
				}
				catch
				{
					Debug.LogWarning("Unable to send disconnect message to the server. The server may have stopped running without the client's knowledge.");
					DisconnectClient();
					yield break;
				}

				yield return new WaitUntil(() => Connections[0].HasDisconnected);
				DisconnectClient();
			}
		}

		/// <summary>
		/// Forcibly disconnects a client from the server.
		/// </summary>
		/// <remarks>Ususally you should use <see cref="LeaveGame"/> instead.</remarks>
		private void DisconnectClient()
		{
			if (IsClient)
			{
				if (Connections.TryGetValue(0, out TCPConnection connection) && connection.Socket.Connected)
				{
					connection.Socket.Shutdown(SocketShutdown.Both);
					connection.Socket.Close();
					Connections.Remove(0);
				}

				IsClient = false;
				IsServer = false;
				IsConnected = false;
				LocalPlayerId = -10;

				foreach (NetworkID id in NetObjects.Values)
				{
					Destroy(id.gameObject);
				}

				NetObjects.Clear();
				Connections.Clear();
			}
		}
	}
}