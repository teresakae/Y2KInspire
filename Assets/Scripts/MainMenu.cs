using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: start, continue from the saved level, settings panel (fullscreen, volume), quit.
/// Buttons call the public methods through their OnClick lists in the Inspector.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Scene loaded by Start. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string firstLevel = "Level1";
    [Tooltip("Continue button object. Hidden when there is no save.")]
    [SerializeField] private GameObject continueButton;
    [Tooltip("Exit button object. Hidden on WebGL, where Quit does nothing.")]
    [SerializeField] private GameObject exitButton;
    [Tooltip("Settings panel. Inactive in the scene.")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Fullscreen toggle inside the settings panel.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Volume slider inside the settings panel. Min 0, Max 1.")]
    [SerializeField] private Slider volumeSlider;

    void Start()
    {
        if (continueButton == null) Debug.LogError($"{name}: missing continueButton");
        if (exitButton == null) Debug.LogError($"{name}: missing exitButton");
        if (settingsPanel == null) Debug.LogError($"{name}: missing settingsPanel");
        if (fullscreenToggle == null) Debug.LogError($"{name}: missing fullscreenToggle");
        if (volumeSlider == null) Debug.LogError($"{name}: missing volumeSlider");
        if (continueButton == null || exitButton == null || settingsPanel == null
            || fullscreenToggle == null || volumeSlider == null) return;

        continueButton.SetActive(PlayerPrefs.HasKey("SavedLevel"));
        if (Application.platform == RuntimePlatform.WebGLPlayer) exitButton.SetActive(false);

        // WithoutNotify: setting the UI must not fire the OnValueChanged saves below
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        float v = PlayerPrefs.GetFloat("Volume", 1f);
        volumeSlider.SetValueWithoutNotify(v);
        AudioListener.volume = v;
    }

    public void StartGame()
    {
        SceneManager.LoadScene(firstLevel);
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene(PlayerPrefs.GetString("SavedLevel", firstLevel));
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetFullscreen(bool on)
    {
        Screen.fullScreen = on;
    }

    public void SetVolume(float v)
    {
        // ponytail: FMOD not wired yet; route to an FMOD bus when audio arrives
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("Volume", v);
        PlayerPrefs.Save();   // WebGL only persists after Save
    }
}
