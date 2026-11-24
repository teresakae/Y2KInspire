using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Esc or P pauses the game (Time.timeScale 0) and shows the settings panel over a dim backdrop; closing the panel resumes, Save and quit goes to the menu.</summary>
public class PauseMenu : MonoBehaviour
{
    [Tooltip("Dim backdrop, parent of the settings panel. Inactive at start.")]
    [SerializeField] private GameObject pauseScreen;
    [Tooltip("Settings panel (child of the pause screen). Its Back button calls SettingsPanel.Close, which resumes the game.")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Scene loaded by Save and quit. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string menuScene = "MainMenu";

    bool paused;

    void Start()
    {
        if (pauseScreen == null) Debug.LogError($"{name}: missing pauseScreen");
        if (settingsPanel == null) Debug.LogError($"{name}: missing settingsPanel");
    }

    void Update()
    {
        if (pauseScreen == null || settingsPanel == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        // P as well because browsers swallow Esc while fullscreen
        if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
        {
            SetPaused(!paused);
            return;
        }

        // The Back button closes the panel itself, so a closed panel while paused means resume.
        if (paused && !settingsPanel.activeSelf) SetPaused(false);
    }

    void SetPaused(bool on)
    {
        paused = on;
        Time.timeScale = on ? 0f : 1f;
        settingsPanel.SetActive(on);
        pauseScreen.SetActive(on);
    }

    // Saves the level, not the spot in it: Start in the menu begins this level again.
    public void SaveAndQuit()
    {
        PlayerPrefs.SetString("SavedLevel", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();   // WebGL only persists after Save
        SceneManager.LoadScene(menuScene);
    }

    // timeScale survives scene loads, so never leave it at 0 behind us.
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
