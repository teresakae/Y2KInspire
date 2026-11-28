using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Plays the opening video while the first level loads behind it, then fades to white and switches. Only the Skip button skips it.</summary>
[RequireComponent(typeof(VideoPlayer))]
public class Intro : MonoBehaviour
{
    [Tooltip("Video file name inside Assets/StreamingAssets.")]
    [SerializeField] private string fileName = "intro_white.mp4";
    [Tooltip("Scene loaded when the video ends or is skipped. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string nextScene = "Level1";

    [Tooltip("Full-screen white Image on top of everything, alpha 0, Raycast Target off. Only its alpha is changed.")]
    [SerializeField] private Image fade;
    [Tooltip("Seconds for the fade to white when leaving.")]
    [SerializeField] private float fadeTime = 0.5f;

    // Caps one frame's fade step so a slow frame cannot jump the fade.
    const float maxFadeStep = 0.05f;

    VideoPlayer video;
    AsyncOperation load;
    bool leaving;
    float alpha;

    void Awake()
    {
        video = GetComponent<VideoPlayer>();
        // WebGL can only play video from a URL, so the file lives in StreamingAssets, not as a VideoClip
        video.source = VideoSource.Url;
        video.url = Application.streamingAssetsPath + "/" + fileName;
        video.loopPointReached += vp => Leave();
        video.errorReceived += (vp, msg) => { Debug.LogError($"{name}: video failed: {msg}"); Leave(); };
        video.Play();
    }

    void Start()
    {
        if (fade == null) Debug.LogError($"{name}: missing fade");
        SetMenuMusicPaused(true);
        // Level1 loads behind the video; it only switches on once we are fully white.
        load = SceneManager.LoadSceneAsync(nextScene);
        if (load != null) load.allowSceneActivation = false;
    }

    void Update()
    {
        if (leaving)
        {
            alpha = Mathf.MoveTowards(alpha, 1f, Mathf.Min(Time.deltaTime, maxFadeStep) / fadeTime);
            SetAlpha();
            if (alpha >= 1f)
            {
                enabled = false;   // don't ask twice
                if (load != null) load.allowSceneActivation = true;
            }
        }
    }

    void SetAlpha()
    {
        if (fade == null) return;
        Color c = fade.color;
        c.a = alpha;
        fade.color = c;
    }

    // The menu music carries over from the menu (or this scene has its own copy). The video's sound should not fight it.
    void SetMenuMusicPaused(bool paused)
    {
        foreach (MenuMusic m in FindObjectsByType<MenuMusic>())
        {
            AudioSource music = m.GetComponent<AudioSource>();
            if (music == null) continue;
            if (paused) music.Pause();
            else music.UnPause();
        }
    }

    // The Skip button calls this (After Pop).
    public void Leave()
    {
        if (leaving) return;
        leaving = true;
        SetMenuMusicPaused(false);
        if (fade == null && load != null) load.allowSceneActivation = true;   // no fade to wait for
    }
}
