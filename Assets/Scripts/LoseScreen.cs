using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>Lose screen: fades to white, then shows a panel with a header, a hint and Play Again / Main Menu buttons, and freezes the game. Snakes (crow eaten) and CrowEnergy (crow drowned) call it.</summary>
public class LoseScreen : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("Full-screen white Image, Raycast Target on so nothing behind it can be clicked. Inactive at start.")]
    [SerializeField] private Image whiteout;
    [Tooltip("The panel with the texts and buttons. Inactive at start.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Big text at the top of the panel.")]
    [SerializeField] private TMP_Text header;
    [Tooltip("Hint under the header.")]
    [SerializeField] private TMP_Text subtitle;
    [Tooltip("Selected when the panel shows, so Enter or Space presses it (Play Again).")]
    [SerializeField] private GameObject firstButton;
    [Tooltip("Pause menu (PF_HUD root). Switched off while the lose screen shows, so Esc/P can't unfreeze the game behind it.")]
    [SerializeField] private PauseMenu pauseMenu;

    [Header("Timing")]
    [Tooltip("Seconds to fade to white.")]
    [SerializeField] private float fadeTime = 0.8f;
    [Tooltip("Whiteout opacity once faded. 1 = solid white; lower lets the level show through.")]
    [Range(0f, 1f)]
    [SerializeField] private float whiteAlpha = 1f;

    [Header("Eaten by a snake")]
    [Tooltip("Header text when a snake eats the crow.")]
    [SerializeField] private string eatenHeader = "Crow's been eaten!";
    [Tooltip("Hint text when a snake eats the crow.")]
    [TextArea]
    [SerializeField] private string eatenSubtitle = "Fly around the snakes. Let Cow RAM to protect!";

    [Header("Drowned")]
    [Tooltip("Header text when the crow drowns.")]
    [SerializeField] private string drownHeader = "Crow cant swim!";
    [Tooltip("Hint text when the crow drowns.")]
    [TextArea]
    [SerializeField] private string drownSubtitle = "Don't land in the water.";

    [Header("Buttons")]
    [Tooltip("Scene loaded by Main Menu. Must be in the File > Build Profiles scene list.")]
    [SerializeField] private string menuScene = "MainMenu";

    float alpha;
    bool showing;

    void Start()
    {
        if (whiteout == null) Debug.LogError($"{name}: missing Whiteout");
        if (panel == null) Debug.LogError($"{name}: missing Panel");
        if (header == null) Debug.LogError($"{name}: missing Header");
        if (subtitle == null) Debug.LogError($"{name}: missing Subtitle");
        if (firstButton == null) Debug.LogError($"{name}: missing First Button");
        if (pauseMenu == null) Debug.LogError($"{name}: missing Pause Menu");
    }

    public void ShowEaten() { Show(eatenHeader, eatenSubtitle); }

    public void ShowDrowned() { Show(drownHeader, drownSubtitle); }

    void Show(string h, string s)
    {
        if (showing || whiteout == null || panel == null) return;
        showing = true;
        if (pauseMenu != null) pauseMenu.enabled = false;
        if (header != null) header.text = h;
        if (subtitle != null) subtitle.text = s;
        alpha = 0f;
        whiteout.gameObject.SetActive(true);
        SetWhiteAlpha();
    }

    void Update()
    {
        if (!showing || panel.activeSelf) return;
        // Unscaled because the game freezes once the panel shows.
        alpha = Mathf.MoveTowards(alpha, whiteAlpha, Time.unscaledDeltaTime / fadeTime);
        SetWhiteAlpha();
        if (alpha >= whiteAlpha)
        {
            panel.SetActive(true);
            // Freeze so snakes and the cow stop behind the panel; PauseMenu.OnDestroy resets timeScale on scene load.
            Time.timeScale = 0f;
            if (EventSystem.current != null && firstButton != null) EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }

    void SetWhiteAlpha()
    {
        Color c = whiteout.color;
        c.a = alpha;
        whiteout.color = c;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        // Like Save and quit, so Start in the menu resumes this level; WebGL only persists after Save.
        PlayerPrefs.SetString("SavedLevel", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        SceneManager.LoadScene(menuScene);
    }

    // timeScale survives scene loads.
    void OnDestroy() { Time.timeScale = 1f; }
}
