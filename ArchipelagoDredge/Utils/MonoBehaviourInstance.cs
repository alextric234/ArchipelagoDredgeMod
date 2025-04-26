using UnityEngine;

namespace ArchipelagoDredge.Utils
{
    public class MonoBehaviourInstance : MonoBehaviour
    {
        private static MonoBehaviourInstance _instance;

        public static MonoBehaviour Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create a hidden game object to host the instance
                    var obj = new GameObject("AP_MonoBehaviourHost");
                    _instance = obj.AddComponent<MonoBehaviourInstance>();

                    // Make persistent across scenes
                    DontDestroyOnLoad(obj);

                    // Hide in hierarchy (optional)
                    obj.hideFlags = HideFlags.HideInHierarchy;
                }
                return _instance;
            }
        }

        // Optional: Clean up when quitting
        private void OnApplicationQuit()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}