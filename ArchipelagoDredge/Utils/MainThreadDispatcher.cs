using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoDredge.Utils;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _actions = new();
    private static MainThreadDispatcher _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
                try
                {
                    _actions.Dequeue()?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError("MainThreadDispatcher Error: " + e);
                }
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_actions)
        {
            _actions.Enqueue(action);
        }
    }
}