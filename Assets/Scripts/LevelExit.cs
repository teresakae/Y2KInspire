using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Level transition: fades in from white at level start; when both the cow and the crow are inside this trigger, saves the next scene, fades to white and loads it.
/// The scene to load must be in the File > Build Profiles scene list.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Tooltip("The cow's collider. The exit needs the cow and the crow together.")]
    [SerializeField] private Collider2D cow;
    [Tooltip("The crow's trigger collider (the CircleCollider2D with Is Trigger on).")]
    [SerializeField] private Collider2D crow;
    [Tooltip("Full-screen white Image on an overlay canvas, Raycast Target off. Only its alpha is changed.")]
    [SerializeField] private Image fade;
    [Tooltip("Seconds for a full fade, in and out.")]
    [SerializeField] private float fadeTime = 1.5f;
    [Tooltip("Scene to load at the exit. Empty = reload this scene (until the next level exists). Also saved for the menu's Continue button.")]
    [SerializeField] private string nextScene = "";

    float alpha = 1f;       // starts white, fades in
    bool cowIn;
    bool crowIn;
    bool leaving;

    void Start()
    {
        if (cow == null) Debug.LogError($"{name}: missing cow");
        if (crow == null) Debug.LogError($"{name}: missing crow");
        if (fade == null) Debug.LogError($"{name}: missing fade");
        SetAlpha();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == cow) cowIn = true;
        if (other == crow) crowIn = true;
        if (cowIn && crowIn && !leaving) StartLeaving();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == cow) cowIn = false;
        if (other == crow) crowIn = false;
    }

    void StartLeaving()
    {
        leaving = true;
        if (nextScene != "")
        {
            PlayerPrefs.SetString("SavedLevel", nextScene);
            PlayerPrefs.Save();   // WebGL only persists after Save
        }
    }

    void Update()
    {
        if (fade == null) return;
        alpha = Mathf.MoveTowards(alpha, leaving ? 1f : 0f, Time.deltaTime / fadeTime);
        SetAlpha();
        if (leaving && alpha >= 1f)
        {
            enabled = false;   // the load happens next frame; don't ask twice
            SceneManager.LoadScene(nextScene == "" ? SceneManager.GetActiveScene().name : nextScene);
        }
    }

    void SetAlpha()
    {
        if (fade == null) return;
        Color c = fade.color;
        c.a = alpha;
        fade.color = c;
    }
}
