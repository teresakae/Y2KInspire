using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Settings panel shared by the main menu and the pause screen: fullscreen, music and SFX volume, screen shake and motion. Saved in PlayerPrefs.</summary>
public class SettingsPanel : MonoBehaviour
{
    [Tooltip("Fullscreen toggle inside the panel.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("AudioMixer asset with exposed parameters MusicVolume and SFXVolume (Assets/Audio/AudioMixer).")]
    [SerializeField] private AudioMixer mixer;
    [Tooltip("Music volume slider. Min 0, Max 1.")]
    [SerializeField] private Slider musicSlider;
    [Tooltip("SFX volume slider. Min 0, Max 1.")]
    [FormerlySerializedAs("volumeSlider")]
    [SerializeField] private Slider sfxSlider;
    [Tooltip("Screen shake slider. Min 0 (off), Max 1 (full). SharedCamera.Shake multiplies by it.")]
    [SerializeField] private Slider shakeSlider;
    [Tooltip("Motion slider. Min 0 (still), Max 1 (full). Scales title bob, grass and tree sway, and mist drift.")]
    [SerializeField] private Slider motionSlider;

    void OnEnable()
    {
        if (fullscreenToggle == null) Debug.LogError($"{name}: missing fullscreenToggle");
        if (mixer == null) Debug.LogError($"{name}: missing mixer");
        if (musicSlider == null) Debug.LogError($"{name}: missing musicSlider");
        if (sfxSlider == null) Debug.LogError($"{name}: missing sfxSlider");
        if (shakeSlider == null) Debug.LogError($"{name}: missing shakeSlider");
        if (motionSlider == null) Debug.LogError($"{name}: missing motionSlider");
        if (fullscreenToggle == null || musicSlider == null || sfxSlider == null || shakeSlider == null || motionSlider == null) return;

        // WithoutNotify: setting the UI must not fire the OnValueChanged saves below
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 1f));
        shakeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Shake", 1f));
        motionSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Motion", 1f));
    }

    /// <summary>Pushes the saved music and SFX volumes into the mixer. The panel starts inactive, so MainMenu calls this at start. The mixer keeps the values across scenes.</summary>
    public void ApplyVolumes()
    {
        if (mixer == null) { Debug.LogError($"{name}: missing mixer"); return; }
        AudioListener.volume = 1f;   // an old saved "Volume" pref must not leave everything quiet
        SetMixer("MusicVolume", PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetMixer("SFXVolume", PlayerPrefs.GetFloat("SFXVolume", 1f));
    }

    // Slider is linear 0..1, mixer wants decibels
    private void SetMixer(string param, float v)
    {
        mixer.SetFloat(param, Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
    }

    /// <summary>Applies the saved motion. MainMenu calls this at start. Shake is read when it happens.</summary>
    public static void ApplySaved()
    {
        // shaders and MistDrift read this global; it survives scene loads
        Shader.SetGlobalFloat("_ReduceMotion", 1f - PlayerPrefs.GetFloat("Motion", 1f));
    }

    public void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
    }

    public void SetMusicVolume(float v)
    {
        SetMixer("MusicVolume", v);
        PlayerPrefs.SetFloat("MusicVolume", v);
        PlayerPrefs.Save();   // WebGL only persists after Save
    }

    public void SetSfxVolume(float v)
    {
        SetMixer("SFXVolume", v);
        PlayerPrefs.SetFloat("SFXVolume", v);
        PlayerPrefs.Save();
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
