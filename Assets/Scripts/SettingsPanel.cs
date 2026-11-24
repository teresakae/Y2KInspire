using UnityEngine;
using UnityEngine.UI;

/// <summary>Settings panel shared by the main menu and the pause screen: fullscreen toggle and volume slider.</summary>
public class SettingsPanel : MonoBehaviour
{
    [Tooltip("Fullscreen toggle inside the panel.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Volume slider inside the panel. Min 0, Max 1.")]
    [SerializeField] private Slider volumeSlider;

    void OnEnable()
    {
        if (fullscreenToggle == null) Debug.LogError($"{name}: missing fullscreenToggle");
        if (volumeSlider == null) Debug.LogError($"{name}: missing volumeSlider");
        if (fullscreenToggle == null || volumeSlider == null) return;

        // WithoutNotify: setting the UI must not fire the OnValueChanged saves below
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume", 1f));
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

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
