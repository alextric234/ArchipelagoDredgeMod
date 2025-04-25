using UnityEngine;

namespace ArchipelagoDredge
{
	public class Loader
	{
		/// <summary>
		/// This method is run by Winch to initialize your mod
		/// </summary>
		public static void Initialize()
		{
			var gameObject = new GameObject(nameof(ArchipelagoDredge));
			gameObject.AddComponent<ArchipelagoDredge>();
			GameObject.DontDestroyOnLoad(gameObject);
		}
	}
}