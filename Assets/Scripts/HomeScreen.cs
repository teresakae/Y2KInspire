using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Ending cutscene: text fades in over white, fades out, the end picture fades in, then the Back to menu button appears.</summary>
public class HomeScreen : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Scene loaded by the button. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string menuScene = "MainMenu";

    [Header("Groups")]
    [Tooltip("Parent of Title and Subtitle.")]
    [SerializeField] private CanvasGroup textGroup;
    [Tooltip("The full-screen end picture.")]
    [SerializeField] private CanvasGroup picture;
    [Tooltip("Parent of the Back to menu button.")]
    [SerializeField] private CanvasGroup buttons;

    [Header("Timing (seconds)")]
    [Tooltip("Text fades in.")]
    [SerializeField] private float textFadeIn = 1f;
    [Tooltip("Text stays fully visible.")]
    [SerializeField] private float textHold = 2.5f;
    [Tooltip("Text fades out.")]
    [SerializeField] private float textFadeOut = 1f;
    [Tooltip("End picture fades in (starts when the text is gone).")]
    [SerializeField] private float pictureFadeIn = 1.5f;
    [Tooltip("Wait after the picture is fully shown before the button starts to appear.")]
    [SerializeField] private float buttonDelay = 1f;
    [Tooltip("Button fades in.")]
    [SerializeField] private float buttonFadeIn = 0.5f;

    private float elapsed;

    private void Awake()
    {
        if (textGroup == null) Debug.LogError($"{name}: missing Text Group");
        if (picture == null) Debug.LogError($"{name}: missing Picture");
        if (buttons == null) Debug.LogError($"{name}: missing Buttons");
        if (textGroup == null || picture == null || buttons == null) enabled = false;
    }

    private void Start()
    {
        textGroup.alpha = 0f;
        picture.alpha = 0f;
        buttons.alpha = 0f;
        // Not clickable until it starts to appear.
        buttons.interactable = false;
        buttons.blocksRaycasts = false;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = elapsed;
        float textIn = Mathf.Clamp01(t / textFadeIn);
        t -= textFadeIn + textHold;
        float textOut = Mathf.Clamp01(t / textFadeOut);
        textGroup.alpha = textIn - textOut;

        t -= textFadeOut;
        picture.alpha = Mathf.Clamp01(t / pictureFadeIn);

        t -= pictureFadeIn + buttonDelay;
        buttons.alpha = Mathf.Clamp01(t / buttonFadeIn);
        bool buttonsOn = t >= 0f;
        buttons.interactable = buttonsOn;
        buttons.blocksRaycasts = buttonsOn;
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(menuScene);
    }
}
