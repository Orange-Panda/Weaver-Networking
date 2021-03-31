using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.Weaver
{
	/// <summary>
	/// A convenient and built-in way to synchronize transforms over the network.
	/// </summary>
	/// <remarks>Simply add this component to a gameObject and its transform rotation and position will be synchronized over the server.</remarks>
	public class NetworkTransform : NetworkComponent
	{
		public override string ComponentID => "T";

		[Tooltip("The visual component of the gameObject. This is hidden before the object position has been set, preventing it from appearing at the center of the world briefly.")]
		public GameObject visual;

		//Client
		private Vector3 positionObjective;
		private Vector3 positionVelocity;
		private Quaternion rotationObjective;
		private Quaternion rotationVelocity;

		//Server
		private Vector3 lastPositionSent;
		private Quaternion lastRotationSent;

		protected override void OnAwake()
		{
			base.OnAwake();
			SetVisualState(false);
		}

		protected override IEnumerator NetworkUpdate()
		{
			MarkDirty();

			float timer = 0.5f;
			while (timer > 0 && Dirty)
			{
				timer -= Time.deltaTime;
				yield return null;
			}

			SetVisualState(true);
		}

		private void Update()
		{
			if (IsClient)
			{
				transform.position = Vector3.Distance(transform.position, positionObjective) > 2 ? positionObjective : Vector3.SmoothDamp(transform.position, positionObjective, ref positionVelocity, NetworkCore.UpdateDelta);
				transform.rotation = QuaternionUtility.SmoothDamp(transform.rotation, rotationObjective, ref rotationVelocity, NetworkCore.UpdateDelta);
			}
		}

		protected override void OnNetworkTick()
		{
			base.OnNetworkTick();

			//Send the transform information to clients if it has been modified.
			if (IsServer)
			{
				Vector3 roundedPosition = transform.position.Rounded();
				if (roundedPosition != lastPositionSent || Quaternion.Angle(lastRotationSent, transform.rotation) > 2)
				{
					lastPositionSent = roundedPosition;
					lastRotationSent = transform.rotation;
					MarkDirty();
				}
			}
		}

		private void SetVisualState(bool value)
		{
			if (visual)
			{
				visual.SetActive(value);
			}
		}

		protected override void DeserializeData(List<string> args)
		{
			if (args.Count >= 3)
			{
				positionObjective = new Vector3(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
			}

			if (args.Count >= 7)
			{
				rotationObjective = new Quaternion(float.Parse(args[3]), float.Parse(args[4]), float.Parse(args[5]), float.Parse(args[6]));
			}
		}

		protected override List<string> SerializeData()
		{
			Vector3 roundedPosition = transform.position.Rounded();
			return new List<string>()
			{
				roundedPosition.x.ToNetworkString(),
				roundedPosition.y.ToNetworkString(),
				roundedPosition.z.ToNetworkString(),
				transform.rotation.x.ToNetworkString(),
				transform.rotation.y.ToNetworkString(),
				transform.rotation.z.ToNetworkString(),
				transform.rotation.w.ToNetworkString()
			};
		}
	}
}