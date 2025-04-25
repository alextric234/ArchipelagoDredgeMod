using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge
{
	public class ArchipelagoDredge : MonoBehaviour
	{
		public void Awake()
		{
			WinchCore.Log.Debug($"{nameof(ArchipelagoDredge)} has loaded!");
		}
	}
}
