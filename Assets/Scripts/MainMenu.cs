using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu: Start begins a fresh game (intro, then Level1), settings panel.
/// Buttons call the public methods through their OnClick lists in the Inspector.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("Scene loaded by Start. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string firstLevel = "Level1";
    [Tooltip("Settings panel. Inactive in the scene.")]
    [SerializeField] private GameObject settingsPanel;

    void Start()
    {
        if (settingsPanel == null) Debug.LogError($"{name}: missing settingsPanel");
        // the panel starts inactive, so apply the saved volume and motion here
        SettingsPanel.ApplySaved();
    }

    // Always a fresh game: clears the saved level and the tutorial tips already seen.
    public void StartGame()
    {
        PlayerPrefs.DeleteKey("SavedLevel");
        PlayerPrefs.Save();
        TutorialTip.ForgetAll();
        SceneManager.LoadScene(firstLevel);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }
}
