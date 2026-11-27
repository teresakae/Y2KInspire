using UnityEngine;

/// <summary>
/// One camera for both players: follows their midpoint and holds them inside the view.
/// </summary>
public class SharedCamera : MonoBehaviour
{
    [Tooltip("Cow and crow. The camera follows their midpoint.")]
    [SerializeField] private Rigidbody2D[] players;
    [Tooltip("Seconds the camera takes to catch up. Higher = lazier.")]
    [SerializeField] private float smoothTime = 0.3f;
    [Tooltip("Lowest camera centre (x, y). Keeps the view from showing past the level's bottom-left edge.")]
    [SerializeField] private Vector2 minPosition = Vector2.zero;
    [Tooltip("Highest camera centre (x, y). Keeps the view from showing past the level's top-right edge.")]
    [SerializeField] private Vector2 maxPosition = new Vector2(17.8f, 0f);
    [Tooltip("How far inside the view edge the players are held, in units.")]
    [SerializeField] private float edgeMargin = 0.5f;

    [Tooltip("Seconds a shake takes to fade out.")]
    [SerializeField] private float shakeTime = 0.25f;

    Camera cam;
    Vector3 velocity;   // SmoothDamp's working memory
    Vector3 shakeOffset;   // what the shake added last frame; taken off again before following
    float shakeStrength, shakeTimer;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) Debug.LogError($"{name}: missing Camera");
        if (players == null || players.Length == 0) Debug.LogError($"{name}: missing players");
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f) return;   // paused: no shake, and SmoothDamp divides by deltaTime
        if (cam == null || players == null || players.Length == 0) return;
        transform.position -= shakeOffset;   // follow from the true position so the shake never drifts the camera
        Vector2 mid = Vector2.zero;
        foreach (var p in players) mid += p.position;
        mid /= players.Length;
        mid.x = Mathf.Clamp(mid.x, minPosition.x, maxPosition.x);
        mid.y = Mathf.Clamp(mid.y, minPosition.y, maxPosition.y);
        Vector3 target = new Vector3(mid.x, mid.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);

        shakeOffset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            shakeOffset = (Vector3)(Random.insideUnitCircle * shakeStrength * Mathf.Clamp01(shakeTimer / shakeTime));
            transform.position += shakeOffset;
        }
    }

    /// <summary>Short decaying random jolt, strength in units. A new shake never replaces a bigger one still running.</summary>
    public void Shake(float strength)
    {
        strength *= PlayerPrefs.GetFloat("Shake", 1f);   // Screen shake setting (SettingsPanel): 0 = off
        if (strength <= 0f) return;
        if (shakeTimer > 0f && strength < shakeStrength * Mathf.Clamp01(shakeTimer / shakeTime)) return;
        shakeStrength = strength;
        shakeTimer = shakeTime;
    }

    // ponytail: setting rb.position is a hard invisible wall; fine for 2 players on 1 screen.
    void FixedUpdate()
    {
        if (cam == null || players == null) return;
        float halfH = cam.orthographicSize - edgeMargin;
        float halfW = cam.orthographicSize * cam.aspect - edgeMargin;
        Vector2 c = transform.position - shakeOffset;   // clamp against the true view, not the shaken one
        foreach (var p in players)
        {
            Vector2 pos = p.position;
            pos.x = Mathf.Clamp(pos.x, c.x - halfW, c.x + halfW);
            pos.y = Mathf.Clamp(pos.y, c.y - halfH, c.y + halfH);
            p.position = pos;
        }
    }
}
