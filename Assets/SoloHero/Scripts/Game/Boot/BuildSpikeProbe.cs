// TEMP: build spike probe for story 1-09. Replaced by BootSequence in E1-04.
// Purpose: force-load the Firebase and Google Mobile Ads native libraries on device so the
// 16 KB page-size check (Android 15+) and the 64-bit build are verified at runtime, and show
// the results on screen without depending on any UI assets.
using System;
using System.Text;
using Firebase;
using GoogleMobileAds.Api;
using UnityEngine;

public sealed class BuildSpikeProbe : MonoBehaviour
{
    private readonly StringBuilder _log = new StringBuilder(2048);
    private readonly object _gate = new object();
    private string _pendingFromOtherThread;
    private GUIStyle _style;

    private void Start()
    {
        Application.targetFrameRate = 60;

        Append($"SoloHero build spike | Unity {Application.unityVersion}");
        Append($"{SystemInfo.deviceModel} | {SystemInfo.operatingSystem}");
        Append($"64-bit process: {IntPtr.Size == 8} | screen {Screen.width}x{Screen.height} dpi {Screen.dpi:F0} | safeArea {Screen.safeArea}");
        Append("google-services.json is optional for this probe; only native lib loading matters");

        InitFirebase();
        InitAds();
    }

    private async void InitFirebase()
    {
        try
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            Append($"Firebase: {status}" + (status == DependencyStatus.Available ? " (libFirebaseCppApp loaded)" : ""));
        }
        catch (Exception e)
        {
            Append($"Firebase: EXCEPTION {e.GetType().Name}: {e.Message}");
        }
    }

    private void InitAds()
    {
        try
        {
            MobileAds.Initialize(status =>
            {
                string line;
                try
                {
                    var sb = new StringBuilder("AdMob: initialized");
                    foreach (var kv in status.getAdapterStatusMap())
                        sb.Append($"\n  {kv.Key}: {kv.Value.InitializationState}");
                    line = sb.ToString();
                }
                catch (Exception e)
                {
                    line = $"AdMob: callback EXCEPTION {e.GetType().Name}: {e.Message}";
                }
                lock (_gate) _pendingFromOtherThread = line;
            });
            Append("AdMob: Initialize() called, waiting for callback");
        }
        catch (Exception e)
        {
            Append($"AdMob: EXCEPTION {e.GetType().Name}: {e.Message}");
        }
    }

    private void Update()
    {
        string pending;
        lock (_gate)
        {
            pending = _pendingFromOtherThread;
            _pendingFromOtherThread = null;
        }
        if (pending != null) Append(pending);
    }

    private void Append(string line)
    {
        _log.AppendLine(line);
        Debug.Log("[Spike] " + line);
    }

    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(18, Screen.height / 48),
                wordWrap = true,
                normal = { textColor = Color.white },
            };
        }
        var rect = new Rect(24f, 24f, Screen.width - 48f, Screen.height - 48f);
        GUI.Label(rect, _log.ToString(), _style);
    }
}
