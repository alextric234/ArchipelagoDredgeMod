using UnityEngine;
using Winch.Core;

namespace WinchModTemplate
{
	public class WinchModTemplate : MonoBehaviour
	{
		public void Awake()
		{
			WinchCore.Log.Debug($"{nameof(WinchModTemplate)} has loaded!");
		}
	}
}
