using ArchipelagoDredge.Game.Managers;
using ArchipelagoDredge.Network;
using HarmonyLib;
using UnityEngine;
using Winch.Core;

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

            new Harmony("alextric234.archipelagodredge").PatchAll();

            GameManager.Instance.OnGameStarted += OnGameStarted;
            GameManager.Instance.OnGameEnded += OnGameEnded;
        }

        private static void OnGameStarted()
        {
            ArchipelagoItemManager.Initialize();
        }

        private static void OnGameEnded()
        {
            ArchipelagoClient.Disconnect();
            ArchipelagoClient.GameReady = false;
        }
	}
}