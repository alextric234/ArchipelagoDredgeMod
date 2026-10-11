using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using Winch.Core;

namespace ArchipelagoDredge.Utils;

public class MainThreadDispatcher : MonoBehaviour
{
    private const int MaxActionsPerFrame = 50;

    private static readonly ConcurrentQueue<Action> _actions = new();
    private static MainThreadDispatcher _instance;
    private static int _mainThreadId = -1;

    public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        var count = Math.Min(_actions.Count, MaxActionsPerFrame);
        for (var i = 0; i < count && _actions.TryDequeue(out var action); i++)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                WinchCore.Log.Error($"MainThreadDispatcher error: {e}");
            }
        }
    }

    public static void Enqueue(Action action)
    {
        if (action == null)
        {
            return;
        }

        _actions.Enqueue(action);
    }
    public static void RunOnMainThread(Action action)
    {
        if (action == null)
        {
            return;
        }

        if (IsMainThread)
        {
            action();
            return;
        }

        _actions.Enqueue(action);
    }
}