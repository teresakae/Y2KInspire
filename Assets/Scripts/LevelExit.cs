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
    [Tooltip("Scene to load at the exit. Also saved, so Start in the menu resumes there.")]
    [SerializeField] private string nextScene = "";

    [Tooltip("Played in turn when both players reach the exit. Empty = silent.")]
    [SerializeField] private AudioClip[] transitionClips;

    // ponytail: static so the turn survives scene loads
    static int nextTransition;

    private AudioSource audioSource;
    float alpha = 1f;       // starts white, fades in
    bool cowIn;
    bool crowIn;
    bool leaving;
    AsyncOperation load;

    // Caps one frame's fade step: a slow frame right after a scene loads would otherwise jump the fade.
    const float maxFadeStep = 0.05f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();   // ponytail: auto-added so no scene setup is needed
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (cow == null) Debug.LogError($"{name}: missing cow");
        if (crow == null) Debug.LogError($"{name}: missing crow");
        if (fade == null) Debug.LogError($"{name}: missing fade");
        if (nextScene == "") Debug.LogError($"{name}: missing nextScene");
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
        if (transitionClips != null && transitionClips.Length > 0) audioSource.PlayOneShot(transitionClips[nextTransition++ % transitionClips.Length]);
        PlayerPrefs.SetString("SavedLevel", nextScene);
        PlayerPrefs.Save();   // WebGL only persists after Save
        // Load behind the fade; the switch waits until the screen is fully white.
        load = SceneManager.LoadSceneAsync(nextScene);
        if (load != null) load.allowSceneActivation = false;
    }

    void Update()
    {
        if (fade == null) return;
        alpha = Mathf.MoveTowards(alpha, leaving ? 1f : 0f, Mathf.Min(Time.deltaTime, maxFadeStep) / fadeTime);
        SetAlpha();
        if (leaving && alpha >= 1f)
        {
            enabled = false;   // don't ask twice
            if (load != null) load.allowSceneActivation = true;
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
