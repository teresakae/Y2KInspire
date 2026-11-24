using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu: Start resumes the saved level (or the first scene), settings panel.
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

    void Start()
    {
        if (settingsPanel == null) Debug.LogError($"{name}: missing settingsPanel");
        // the panel starts inactive, so apply the saved volume here
        AudioListener.volume = PlayerPrefs.GetFloat("Volume", 1f);
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
}
