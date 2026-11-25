using System;
using UnityEngine;

/// <summary>
/// Saved gameplay settings (PlayerPrefs). Read these from gameplay code, e.g.
///   if (GameSettings.ShowDamageNumbers) SpawnDamagePopup(...);
///   if (GameSettings.ScreenShake) CameraShake(...);
/// Subscribe to Changed to react while the game is running.
/// Display settings (fullscreen + resolution) are saved here too and re-applied when the game starts.
/// </summary>
public static class GameSettings
{
    private const string DamageNumbersKey = "settings.showDamageNumbers";
    private const string ScreenShakeKey = "settings.screenShake";
    private const string FullscreenKey = "settings.fullscreen";
    private const string ResolutionWidthKey = "settings.resolutionWidth";
    private const string ResolutionHeightKey = "settings.resolutionHeight";

    public const bool DefaultShowDamageNumbers = true;
    public const bool DefaultScreenShake = true;

    public static event Action Changed;

    public static bool ShowDamageNumbers
    {
        get => GetBool(DamageNumbersKey, DefaultShowDamageNumbers);
        set => SetBool(DamageNumbersKey, value);
    }

    public static bool ScreenShake
    {
        get => GetBool(ScreenShakeKey, DefaultScreenShake);
        set => SetBool(ScreenShakeKey, value);
    }

    // ---- Display ----

    /// <summary>Saved fullscreen choice; falls back to the current screen mode if never saved.</summary>
    public static bool Fullscreen => GetBool(FullscreenKey, Screen.fullScreenMode != FullScreenMode.Windowed);

    /// <summary>Saved resolution; falls back to the current window size if never saved.</summary>
    public static Vector2Int Resolution => new Vector2Int(
        PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width),
        PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height));

    /// <summary>Saves and applies fullscreen + resolution. (Has no visible effect in the editor Game view.)</summary>
    public static void SetDisplay(Vector2Int resolution, bool fullscreen)
    {
        PlayerPrefs.SetInt(ResolutionWidthKey, resolution.x);
        PlayerPrefs.SetInt(ResolutionHeightKey, resolution.y);
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        ApplyDisplay(resolution, fullscreen);
        Changed?.Invoke();
    }

    private static void ApplyDisplay(Vector2Int resolution, bool fullscreen) =>
        Screen.SetResolution(resolution.x, resolution.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

    /// <summary>Re-applies the saved display settings when the game starts.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedDisplayOnStartup()
    {
        if (Application.isEditor || !PlayerPrefs.HasKey(ResolutionWidthKey)) return;
        ApplyDisplay(Resolution, Fullscreen);
    }

    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(DamageNumbersKey);
        PlayerPrefs.DeleteKey(ScreenShakeKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    private static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
