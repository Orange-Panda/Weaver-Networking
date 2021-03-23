using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace NetworkEngine
{
	/// <summary>
	/// A stable connection which is guaranteed to have successful communication, with a latency tradeoff.
	/// </summary>
	public class TCPConnection
	{
		private const int BufferSize = 2048;

		private StringBuilder stringBuilder = new StringBuilder();
		private byte[] buffer = new byte[BufferSize];
		private bool messageReady = false;

		public int PlayerID { get; set; }
		public Socket Socket { get; private set; }
		public bool IsDisconnecting { get; set; } = false;
		public bool HasDisconnected { get; set; } = false;

		public TCPConnection(int playerID, Socket socket)
		{
			PlayerID = playerID;
			Socket = socket;
		}

		/// <summary>
		/// Receive functionality for the TCP connection.
		/// </summary>
		public IEnumerator Receive()
		{
			while (Socket.Connected)
			{
				Socket.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(OnTCPReceive), this);

				// Wait until a message has been received.
				yield return new WaitUntil(() => messageReady || !Socket.Connected);

				// Pull message
				string response = stringBuilder.ToString();
				stringBuilder.Clear();
				messageReady = false;

				if (response.Length > 0 && response[response.Length - 1] == '\n')
				{
					// Parse string for commands
					string[] commands = response.Split('\n');
					for (int i = 0; i < commands.Length; i++)
					{
						NetworkCore.ActiveNetwork.HandleCommand(commands[i]);
					}
				}
			}
		}

		/// <summary>
		/// Callback for after a TCP message is received.
		/// </summary>
		private void OnTCPReceive(IAsyncResult result)
		{
			int bytesRead = Socket.EndReceive(result);
			if (bytesRead > 0)
			{
				stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
				messageReady = true;
			}
			else
			{
				Socket.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(OnTCPReceive), this);
			}
		}

		/// <summary>
		/// Send byte data over the network. Due to the nature of a TCP connection, this is guaranteed to reach the other end (at some point).
		/// </summary>
		public void Send(byte[] byteData)
		{
			Socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(OnTCPSend), Socket);
		}

		/// <summary>
		/// Callback for after a TCP message is sent.
		/// </summary>
		private void OnTCPSend(IAsyncResult result)
		{
			if (IsDisconnecting && NetworkCore.ActiveNetwork.IsClient)
			{
				HasDisconnected = true;
			}
		}
	}
}