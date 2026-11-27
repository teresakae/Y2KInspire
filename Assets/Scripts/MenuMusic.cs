using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keeps the menu music going across the menu and cutscene scenes instead of restarting it each load.</summary>
public class MenuMusic : MonoBehaviour
{
    [Tooltip("Scenes this music keeps playing in. Any other scene stops it.")]
    [SerializeField] private string[] keepInScenes = { "MainMenu", "Intro", "HomeScreen" };

    void Awake()
    {
        // A copy carried over from the last scene is already playing, so this scene's copy steps aside.
        if (FindObjectsByType<MenuMusic>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (System.Array.IndexOf(keepInScenes, scene.name) < 0) Destroy(gameObject);
    }
}
