using TMPro;
using UnityEngine;

/// <summary>
/// Placeholder for the herd's moo until FMOD audio exists: a faint text that fades in, holds, fades out, on repeat.
/// </summary>
// ponytail: fixed screen corner, not the real herd direction. When the herd's position matters (Level2)
// or FMOD arrives, place by direction to a herd Transform / play the FMOD event here.
public class MooPrompt : MonoBehaviour
{
    [Tooltip("Seconds between the start of one moo and the next.")]
    [SerializeField] private float interval = 6f;
    [Tooltip("Seconds to fade in, and again to fade out.")]
    [SerializeField] private float fadeTime = 0.6f;
    [Tooltip("Seconds the text stays at full strength.")]
    [SerializeField] private float holdTime = 1f;
    [Tooltip("Strongest the text gets. Low = faint.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.6f;

    TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        if (label == null) Debug.LogError($"{name}: missing TMP_Text");
    }

    void Update()
    {
        if (label == null) return;
        float t = Time.timeSinceLevelLoad % interval;   // restarts at 0 on every level load, so the level opens on a moo
        float a = 0f;
        if (t < fadeTime) a = t / fadeTime;
        else if (t < fadeTime + holdTime) a = 1f;
        else if (t < 2f * fadeTime + holdTime) a = 1f - (t - fadeTime - holdTime) / fadeTime;
        label.alpha = a * maxAlpha;
    }
}
