using UnityEngine;
using UnityEngine.SceneManagement;

namespace LMirman.Weaver
{
	/// <summary>
	/// Provides server functionality launch options to make server distribution easier.
	/// </summary>
	public static class NetworkLaunchOptions
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void CheckForSceneChange()
		{
			string[] args = System.Environment.GetCommandLineArgs();

			//Check for scene command
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i].Equals("-scene"))
				{
					SceneManager.LoadScene(args[i + 1]);
					SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
					return;
				}
			}

			CheckForLaunchOptions();
		}

		private static void CheckForLaunchOptions()
		{
			string[] args = System.Environment.GetCommandLineArgs();

			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i].Equals("-ip"))
				{
					NetworkCore.ActiveNetwork.SetIP(args[i + 1]);
				}
				else if (args[i].Equals("-port") && int.TryParse(args[i + 1], out int port))
				{
					NetworkCore.ActiveNetwork.SetPort(port);
				}
			}

			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].Equals("-server"))
				{
					NetworkCore.ActiveNetwork.StartServer();

					if (Camera.main != null)
					{
						Camera.main.enabled = false;
					}
				}

				if (args[i].Equals("-serverCam"))
				{
					NetworkCore.ActiveNetwork.StartServer();
				}
			}
		}

		private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
		{
			CheckForLaunchOptions();
			SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
		}
	}
}