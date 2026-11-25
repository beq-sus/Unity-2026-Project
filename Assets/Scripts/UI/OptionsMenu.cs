using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Options panel: fullscreen, resolution, damage numbers, screen shake.
/// Changes are pending until APPLY; closing the panel (BACK) discards them. RESET fills in defaults (still needs APPLY).
/// Note: fullscreen/resolution changes only take effect in a built player, not in the editor Game view.
/// </summary>
public class OptionsMenu : MonoBehaviour
{
    // Common 16:9 sizes; only those that fit the monitor are shown, plus the monitor's native size.
    private static readonly Vector2Int[] CommonResolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
    };

    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle damageNumbersToggle;
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Button applyButton;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    private struct State
    {
        public bool fullscreen;
        public int resolution;
        public bool damageNumbers;
        public bool screenShake;
    }

    private State applied;
    private State pending;

    private void OnEnable()
    {
        BuildResolutionList();
        applied = new State
        {
            fullscreen = GameSettings.Fullscreen,
            resolution = ClosestResolution(GameSettings.Resolution),
            damageNumbers = GameSettings.ShowDamageNumbers,
            screenShake = GameSettings.ScreenShake,
        };
        pending = applied;
        SyncUI();
    }

    // ---- called by the controls (only change pending values) ----

    public void SetFullscreen(bool on) { pending.fullscreen = on; UpdateApplyButton(); }
    public void SetResolution(int index) { pending.resolution = index; UpdateApplyButton(); }
    public void SetShowDamageNumbers(bool on) { pending.damageNumbers = on; UpdateApplyButton(); }
    public void SetScreenShake(bool on) { pending.screenShake = on; UpdateApplyButton(); }

    public void ResetToDefaults()
    {
        pending = new State
        {
            fullscreen = true,
            resolution = 0, // list is sorted largest first, so 0 = native
            damageNumbers = GameSettings.DefaultShowDamageNumbers,
            screenShake = GameSettings.DefaultScreenShake,
        };
        SyncUI();
    }

    public void Apply()
    {
        bool displayChanged = pending.fullscreen != applied.fullscreen || pending.resolution != applied.resolution;
        if (displayChanged && pending.resolution >= 0 && pending.resolution < resolutions.Count)
            GameSettings.SetDisplay(resolutions[pending.resolution], pending.fullscreen);

        if (pending.damageNumbers != applied.damageNumbers) GameSettings.ShowDamageNumbers = pending.damageNumbers;
        if (pending.screenShake != applied.screenShake) GameSettings.ScreenShake = pending.screenShake;

        applied = pending;
        UpdateApplyButton();
    }

    // ---- helpers ----

    private void SyncUI()
    {
        if (fullscreenToggle) fullscreenToggle.SetIsOnWithoutNotify(pending.fullscreen);
        if (damageNumbersToggle) damageNumbersToggle.SetIsOnWithoutNotify(pending.damageNumbers);
        if (screenShakeToggle) screenShakeToggle.SetIsOnWithoutNotify(pending.screenShake);
        if (resolutionDropdown)
        {
            resolutionDropdown.SetValueWithoutNotify(pending.resolution);
            resolutionDropdown.RefreshShownValue();
        }
        UpdateApplyButton();
    }

    private void UpdateApplyButton()
    {
        if (applyButton) applyButton.interactable = !pending.Equals(applied);
    }

    private void BuildResolutionList()
    {
        var display = Screen.mainWindowDisplayInfo;
        var native = new Vector2Int(display.width, display.height);
        if (native.x <= 0 || native.y <= 0)
            native = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);

        resolutions.Clear();
        resolutions.AddRange(CommonResolutions.Where(r => r.x <= native.x && r.y <= native.y));
        if (!resolutions.Contains(native)) resolutions.Add(native);
        resolutions.Sort((a, b) => (b.x * b.y).CompareTo(a.x * a.y)); // largest first

        if (resolutionDropdown == null) return;
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutions.Select(v => $"{v.x} x {v.y}").ToList());
    }

    private int ClosestResolution(Vector2Int size)
    {
        int best = 0;
        for (int i = 1; i < resolutions.Count; i++)
        {
            if ((resolutions[i] - size).sqrMagnitude < (resolutions[best] - size).sqrMagnitude)
                best = i;
        }
        return best;
    }
}
