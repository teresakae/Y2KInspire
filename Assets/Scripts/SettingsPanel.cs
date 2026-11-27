using UnityEngine;
using UnityEngine.UI;

/// <summary>Settings panel shared by the main menu and the pause screen: fullscreen, volume, screen shake and motion. Saved in PlayerPrefs.</summary>
public class SettingsPanel : MonoBehaviour
{
    [Tooltip("Fullscreen toggle inside the panel.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Volume slider inside the panel. Min 0, Max 1.")]
    [SerializeField] private Slider volumeSlider;
    [Tooltip("Screen shake slider. Min 0 (off), Max 1 (full). SharedCamera.Shake multiplies by it.")]
    [SerializeField] private Slider shakeSlider;
    [Tooltip("Motion slider. Min 0 (still), Max 1 (full). Scales title bob, grass and tree sway, and mist drift.")]
    [SerializeField] private Slider motionSlider;

    void OnEnable()
    {
        if (fullscreenToggle == null) Debug.LogError($"{name}: missing fullscreenToggle");
        if (volumeSlider == null) Debug.LogError($"{name}: missing volumeSlider");
        if (shakeSlider == null) Debug.LogError($"{name}: missing shakeSlider");
        if (motionSlider == null) Debug.LogError($"{name}: missing motionSlider");
        if (fullscreenToggle == null || volumeSlider == null || shakeSlider == null || motionSlider == null) return;

        // WithoutNotify: setting the UI must not fire the OnValueChanged saves below
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume", 1f));
        shakeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Shake", 1f));
        motionSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Motion", 1f));
    }

    /// <summary>Applies the saved volume and motion. The panel starts inactive, so MainMenu calls this at start. Shake is read when it happens.</summary>
    public static void ApplySaved()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("Volume", 1f);
        // shaders and MistDrift read this global; it survives scene loads
        Shader.SetGlobalFloat("_ReduceMotion", 1f - PlayerPrefs.GetFloat("Motion", 1f));
    }

    public void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
    }

    public void SetVolume(float v)
    {
        AudioListener.volume = v;   // master volume for every AudioSource
        PlayerPrefs.SetFloat("Volume", v);
        PlayerPrefs.Save();   // WebGL only persists after Save
    }

    public void SetShake(float v)
    {
        PlayerPrefs.SetFloat("Shake", v);
        PlayerPrefs.Save();
    }

    public void SetMotion(float v)
    {
        PlayerPrefs.SetFloat("Motion", v);
        PlayerPrefs.Save();
        ApplySaved();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
