using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: Start resumes the saved level (or the first scene), settings panel (fullscreen, volume), credits.
/// Buttons call the public methods through their OnClick lists in the Inspector.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Scene loaded by Start when there is no save. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string firstLevel = "Level1";
    [Tooltip("Name of the end scene. LevelExit saves the next scene, so a finished game saves this; Start then begins again instead of resuming it.")]
    [SerializeField] private string endScene = "HomeScreen";
    [Tooltip("Settings panel. Inactive in the scene.")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Fullscreen toggle inside the settings panel.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Volume slider inside the settings panel. Min 0, Max 1.")]
    [SerializeField] private Slider volumeSlider;

    void Start()
    {
        if (settingsPanel == null) Debug.LogError($"{name}: missing settingsPanel");
        if (fullscreenToggle == null) Debug.LogError($"{name}: missing fullscreenToggle");
        if (volumeSlider == null) Debug.LogError($"{name}: missing volumeSlider");
        if (settingsPanel == null || fullscreenToggle == null || volumeSlider == null) return;

        // WithoutNotify: setting the UI must not fire the OnValueChanged saves below
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        float v = PlayerPrefs.GetFloat("Volume", 1f);
        volumeSlider.SetValueWithoutNotify(v);
        AudioListener.volume = v;
    }

    public void StartGame()
    {
        string saved = PlayerPrefs.GetString("SavedLevel", "");
        if (saved != "" && saved != endScene) SceneManager.LoadScene(saved);
        else SceneManager.LoadScene(firstLevel);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
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
