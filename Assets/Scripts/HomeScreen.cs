using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>"You're home" screen: one button back to the main menu. Saves are left alone so Start still resumes.</summary>
public class HomeScreen : MonoBehaviour
{
    [Tooltip("Scene loaded by the button. Must be in File > Build Profiles scene list.")]
    [SerializeField] private string menuScene = "MainMenu";

    public void BackToMenu()
    {
        SceneManager.LoadScene(menuScene);
    }
}
