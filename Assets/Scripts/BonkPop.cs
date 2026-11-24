using TMPro;
using UnityEngine;

/// <summary>Floating word (e.g. "bonk!"). Punches in, shakes, drifts up, fades, then destroys itself.
/// Put it on a GameObject with a TextMeshPro (3D) component.</summary>
public class BonkPop : MonoBehaviour
{
    [Tooltip("The word. Usually the TextMeshPro on this object.")]
    [SerializeField] private TMP_Text label;
    [Tooltip("Seconds until it is gone.")]
    [SerializeField] private float life = 0.8f;
    [Tooltip("Scale over its life (0 to 1 of the life).")]
    [SerializeField] private AnimationCurve pop = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.12f, 1.5f), new Keyframe(0.3f, 0.9f), new Keyframe(0.45f, 1.1f), new Keyframe(0.6f, 1f), new Keyframe(1f, 1f));
    [Tooltip("Units it drifts up over its life. Eases out.")]
    [SerializeField] private float rise = 0.9f;
    [Tooltip("Degrees it shakes each way. Fades out with the life.")]
    [SerializeField] private float shake = 12f;
    [Tooltip("Speed of the shake (radians per second).")]
    [SerializeField] private float shakeSpeed = 50f;
    [Tooltip("Fraction of the life when the fade starts.")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeStart = 0.6f;
    [Tooltip("Degrees of random tilt each way, picked once at the start.")]
    [SerializeField] private float startTilt = 12f;

    float timer, tilt;
    Vector3 baseScale, startPos;

    void Awake()
    {
        if (label == null)
        {
            Debug.LogError($"{name}: missing label");
            enabled = false;
            return;
        }
        baseScale = transform.localScale;
        startPos = transform.position;
        tilt = Random.Range(-startTilt, startTilt);
    }

    void Update()
    {
        timer += Time.deltaTime;   // deltaTime, so it freezes with the pause
        float t = Mathf.Clamp01(timer / life);

        transform.localScale = baseScale * pop.Evaluate(t);
        transform.position = startPos + Vector3.up * (rise * (1f - (1f - t) * (1f - t)));
        transform.rotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(timer * shakeSpeed) * shake * (1f - t));
        // Max guards fadeStart = 1 from dividing by zero.
        label.alpha = 1f - Mathf.Clamp01((t - fadeStart) / Mathf.Max(1f - fadeStart, 0.0001f));

        if (timer >= life) Destroy(gameObject);
    }
}
