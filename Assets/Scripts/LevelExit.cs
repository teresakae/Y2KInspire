using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Level transition: fades in from white at level start; when the cow enters this trigger, fades to white and loads the next scene.
/// The scene to load must be in the File > Build Profiles scene list.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Tooltip("The cow's collider. Only the cow triggers the exit.")]
    [SerializeField] private Collider2D cow;
    [Tooltip("Full-screen white Image on an overlay canvas, Raycast Target off. Only its alpha is changed.")]
    [SerializeField] private Image fade;
    [Tooltip("Seconds for a full fade, in and out.")]
    [SerializeField] private float fadeTime = 1.5f;
    [Tooltip("Scene to load at the exit. Empty = reload this scene (until the next level exists).")]
    [SerializeField] private string nextScene = "";

    float alpha = 1f;       // starts white, fades in
    bool leaving;

    void Start()
    {
        if (cow == null) Debug.LogError($"{name}: missing cow");
        if (fade == null) Debug.LogError($"{name}: missing fade");
        SetAlpha();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == cow) leaving = true;
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
