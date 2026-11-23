using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>Plays the opening video, then loads the first level. Any key, or the Skip button, skips it.</summary>
[RequireComponent(typeof(VideoPlayer))]
public class Intro : MonoBehaviour
{
    [Tooltip("Video file name inside Assets/StreamingAssets.")]
    [SerializeField] private string fileName = "intro_white.mp4";
    [Tooltip("Scene loaded when the video ends or is skipped. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string nextScene = "Level1";

    VideoPlayer video;
    bool leaving;

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

    void Update()
    {
        bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        if (key) Leave();
    }

    // The Skip button calls this (After Pop).
    public void Leave()
    {
        if (leaving) return;
        leaving = true;
        SceneManager.LoadScene(nextScene);
    }
}
