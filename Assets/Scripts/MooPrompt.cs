using UnityEngine;

/// <summary>
/// The herd's moo as a speech bubble on the HUD: it pops up every few seconds and points toward the goal, sitting on it when it is on screen and on the screen edge when it is not.
/// </summary>
// ponytail: FMOD positional moo goes here later (play the event when a moo starts).
public class MooPrompt : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds between the start of one moo and the next.")]
    [SerializeField] private float interval = 6f;
    [Tooltip("Seconds to fade in, and again to fade out.")]
    [SerializeField] private float fadeTime = 0.6f;
    [Tooltip("Seconds the bubble stays at full strength.")]
    [SerializeField] private float holdTime = 1f;
    [Tooltip("Strongest the bubble gets. Low = faint.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.6f;

    [Header("Scene references")]
    [Tooltip("The bubble body. Moved around the screen; stays upright.")]
    [SerializeField] private RectTransform bubble;
    [Tooltip("Small arrow child of the bubble. Turns to point at the goal.")]
    [SerializeField] private RectTransform tail;
    [Tooltip("CanvasGroup on the bubble. Its alpha is the fade.")]
    [SerializeField] private CanvasGroup group;
    [Tooltip("Camera that sees the level. Empty = Main Camera.")]
    [SerializeField] private Camera cam;
    [Tooltip("What the moo points at: the exit, or the next goal.")]
    [SerializeField] private Transform target;

    [Header("Placement")]
    [Tooltip("Pixels (at 1920x1080) kept between the bubble and the screen edge.")]
    [SerializeField] private float edgePadding = 80f;
    [Tooltip("Pixels (at 1920x1080) the bubble floats above the goal when the goal is on screen.")]
    [SerializeField] private float onScreenOffset = 60f;
    [Tooltip("Degrees added to the tail's turn. Set it so the tail art points at the goal. 0 = art points right.")]
    [SerializeField] private float tailAngleOffset = 0f;
    [Tooltip("Pixels (at 1920x1080) from the bubble centre to the tail. About half the bubble's size.")]
    [SerializeField] private float tailDistance = 60f;

    Canvas canvas;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
    }

    void Start()
    {
        if (bubble == null) Debug.LogError($"{name}: missing bubble");
        if (tail == null) Debug.LogError($"{name}: missing tail");
        if (group == null) Debug.LogError($"{name}: missing group");
        if (cam == null) Debug.LogError($"{name}: missing cam");
        if (canvas == null) Debug.LogError($"{name}: missing Canvas in parents");
        if (target == null) Debug.LogError($"{name}: missing target");
    }

    /// <summary>Point the moo at a new goal, e.g. when an area opens.</summary>
    public void SetTarget(Transform t)
    {
        target = t;
    }

    // LateUpdate, not Update: the camera smooths in its own LateUpdate; placing earlier lags a frame behind it.
    void LateUpdate()
    {
        if (bubble == null || tail == null || group == null || cam == null || canvas == null) return;
        if (target == null)
        {
            group.alpha = 0f;
            return;
        }

        float t = Time.timeSinceLevelLoad % interval;   // restarts at 0 on every level load, so the level opens on a moo
        float a = 0f;
        if (t < fadeTime) a = t / fadeTime;
        else if (t < fadeTime + holdTime) a = 1f;
        else if (t < 2f * fadeTime + holdTime) a = 1f - (t - fadeTime - holdTime) / fadeTime;
        group.alpha = a * maxAlpha;
        if (a <= 0f) return;   // hidden: skip the placement maths

        float scale = canvas.scaleFactor;   // pixels below are written for the 1920x1080 reference
        float pad = edgePadding * scale;
        Vector2 centre = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 half = centre - new Vector2(pad, pad);   // half-size of the screen rect shrunk by the padding
        Vector3 sp = cam.WorldToScreenPoint(target.position);
        Vector2 dir = (Vector2)sp - centre;
        bool onScreen = sp.z > 0f && Mathf.Abs(dir.x) <= half.x && Mathf.Abs(dir.y) <= half.y;

        if (onScreen)
        {
            bubble.position = new Vector2(sp.x, sp.y + onScreenOffset * scale);
            dir = Vector2.down;   // bubble floats above the goal, so the tail points down at it
        }
        else
        {
            // Shrink dir until it touches the padded rect: the smaller of the x and y factors.
            float sx = Mathf.Abs(dir.x) > 0.001f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
            float sy = Mathf.Abs(dir.y) > 0.001f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
            bubble.position = centre + dir * Mathf.Min(sx, sy);
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        tail.localEulerAngles = new Vector3(0f, 0f, angle + tailAngleOffset);
        tail.anchoredPosition = dir.normalized * tailDistance;   // orbit the bubble edge on the goal's side
    }
}
