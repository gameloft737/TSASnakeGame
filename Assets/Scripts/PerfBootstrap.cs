using UnityEngine;

/// <summary>
/// Runtime performance bootstrap — applied before the first scene loads.
///
/// This file does not need to be attached to any GameObject.
/// It uses RuntimeInitializeOnLoadMethod to configure settings that dramatically
/// improve WebGL stability and runtime performance:
///
/// 1. Disables Debug.Log/Warning output in shipped builds. Even when logs aren't
///    visible, Debug.Log allocates call-stack strings and dispatches to the logger —
///    on WebGL this is one of the largest GC offenders in a codebase that logs
///    liberally.
/// 2. Caps the target framerate so WebGL doesn't try to run at vsync'd ~120fps on
///    high-refresh displays (which doubles CPU/GC cost for little visual benefit).
/// 3. Forces a non-capped vsync so Application.targetFrameRate actually applies on
///    WebGL.
/// 4. Runs an early GC.Collect so the managed heap starts compact.
///
/// All of the above is free of cost inside the editor — guards keep editor/dev
/// builds fully verbose.
/// </summary>
public static class PerfBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        // Strip Debug.Log in shipping builds — this eliminates a huge per-frame
        // GC allocation source on WebGL. Exceptions and Errors are kept so crashes
        // are still visible in browser console.
        Debug.unityLogger.filterLogType = LogType.Error;
        // Also disable stack traces for the remaining logs (Errors/Exceptions) to
        // avoid expensive string-capture on WebGL.
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
        #endif

        // Cap framerate and disable forced vsync so the cap applies.
        // WebGL ignores QualitySettings.vSyncCount entirely, but this is still
        // correct for desktop builds and won't hurt.
        QualitySettings.vSyncCount = 0;

        #if UNITY_WEBGL && !UNITY_EDITOR
        // 60 is a good ceiling for browsers. Going higher causes the game thread
        // to compete with the browser compositor and produces stutter.
        Application.targetFrameRate = 60;
        #else
        if (Application.targetFrameRate <= 0)
        {
            Application.targetFrameRate = 60;
        }
        #endif

        // Compact the managed heap once at startup so first-gameplay GC spikes
        // are smaller.
        System.GC.Collect();
    }
}
