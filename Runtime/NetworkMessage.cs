using System.Collections.Generic;

namespace LMirman.Weaver
{
	/// <summary>
	/// Creates (<see cref="CreateMessage(Type, List{string})"/>) and parses (<see cref="GetParameters(string)"/>) network messages.
	/// </summary>
	public static class NetworkMessage
	{
		private static Dictionary<Type, string> typeMessageLookup;
		private static Dictionary<string, Type> messageTypeLookup;

		/// <summary>
		/// Initializes the lookup table for command types.
		/// </summary>
		static NetworkMessage()
		{
			TypeMessageEntry[] entries = new TypeMessageEntry[]
			{
				new TypeMessageEntry(Type.Okay, "1"),
				new TypeMessageEntry(Type.PlayerID, "2"),
				new TypeMessageEntry(Type.Disconnect, "3"),
				new TypeMessageEntry(Type.Create, "4"),
				new TypeMessageEntry(Type.Delete, "5"),
				new TypeMessageEntry(Type.Command, "6"),
				new TypeMessageEntry(Type.Update, "7"),
			};

			typeMessageLookup = new Dictionary<Type, string>();
			messageTypeLookup = new Dictionary<string, Type>();
			for (int i = 0; i < entries.Length; i++)
			{
				typeMessageLookup.Add(entries[i].type, entries[i].message);
				messageTypeLookup.Add(entries[i].message, entries[i].type);
			}
		}

		/// <summary>
		/// Parse a network message by retriving it's type and arguments.
		/// </summary>
		/// <param name="message">The network message to parse.</param>
		/// <returns>Returns a <see cref="Parameters"/> object.</returns>
		public static Parameters GetParameters(string message)
		{
			if (!string.IsNullOrWhiteSpace(message))
			{
				message = message.Replace('\n', ' ');

				List<string> args = new List<string>(message.Split('#'));
				if (args.Count > 0 && messageTypeLookup.TryGetValue(args[0], out Type type))
				{
					args.RemoveAt(0);
					return new Parameters(type, args);
				}
			}

			return Parameters.Null;
		}

		/// <summary>
		/// Creates a message with a type and optionally string arguments.
		/// </summary>
		/// <param name="type">The type of message to send</param>
		/// <param name="args">Arguments to the message</param>
		/// <returns>A formatted string with type and arguments. Includes the newline character.</returns>
		public static string CreateMessage(Type type, List<string> args = null)
		{
			if (typeMessageLookup.TryGetValue(type, out string value))
			{
				if (args != null)
				{
					for (int i = 0; i < args.Count; i++)
					{
						value += $"#{CleanString(args[i])}";
					}
				}

				value += "\n";
				return value;
			}
			else
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// Creates a message with a type and a string argument.
		/// </summary>
		/// <param name="type">The type of message to send</param>
		/// <param name="arg">Single argument for the message</param>
		/// <returns>A formatted string with type and arguments. Includes the newline character.</returns>
		/// <remarks>Do not manually make multiple arguments in the <paramref name="arg"/> variable. Use <see cref="CreateMessage(Type, List{string})"/> instead.</remarks>
		public static string CreateMessage(Type type, string arg)
		{
			return typeMessageLookup.TryGetValue(type, out string value) ? $"{value}#{CleanString(arg)}\n" : string.Empty;
		}

		/// <summary>
		/// Removes the newline and '#' character from <paramref name="value"/> to ensure it doesn't confuse the parser.
		/// </summary>
		private static string CleanString(string value)
		{
			try 
			{
				value = value.Replace('#', ' ');
				value = value.Replace('\n', ' ');
				return value;
			}
			catch (System.NullReferenceException)
			{
				return string.Empty;
			}
		}

		public enum Type
		{
			Null, Okay, PlayerID, Disconnect, Create, Delete, Command, Update
		}

		/// <summary>
		/// A data structure that associates a <see cref="Type"/> with a string for sending over the network.
		/// </summary>
		private class TypeMessageEntry
		{
			public Type type;
			public string message;

			public TypeMessageEntry(Type type, string message)
			{
				this.message = message;
				this.type = type;
			}
		}

		/// <summary>
		/// A parsed network message including it's <see cref="type"/> and <see cref="args"/>.
		/// </summary>
		public class Parameters
		{
			public static Parameters Null => new Parameters(Type.Null, null);

			public Type type;
			public List<string> args;

			public Parameters(Type type, List<string> args)
			{
				this.type = type;
				this.args = args;
			}
		}
	}
}