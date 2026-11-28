using UnityEngine;

/// <summary>
/// The herd's moo as a speech bubble on the HUD: it pops up every few seconds and points toward the goal, sitting on it when it is on screen and on the screen edge when it is not.
/// </summary>
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

    [Header("Moo juice")]
    [Tooltip("Bubble scale over the pop time: starts small, overshoots, settles at 1.")]
    [SerializeField] private AnimationCurve popCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1.12f), new Keyframe(0.8f, 0.97f), new Keyframe(1f, 1f));
    [Tooltip("Seconds the pop takes at the start of each moo.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float popTime = 0.35f;
    [Tooltip("Degrees the bubble wiggles each way while it shows.")]
    [Range(0f, 15f)]
    [SerializeField] private float wiggleAngle = 4f;
    [Tooltip("Wiggle speed (radians per second).")]
    [SerializeField] private float wiggleSpeed = 14f;

    [Header("Sound")]
    [Tooltip("The herd's moos. Played in turn at the start of each bubble, panned toward the goal. Empty = silent.")]
    [SerializeField] private AudioClip[] mooClips;
    [Tooltip("Lowest random pitch per moo.")]
    [SerializeField] private float pitchMin = 0.95f;
    [Tooltip("Highest random pitch per moo.")]
    [SerializeField] private float pitchMax = 1.05f;
    [Tooltip("Moo volume.")]
    [Range(0f, 1f)]
    [SerializeField] private float mooVolume = 0.8f;
    [Tooltip("How far the moo pans toward the goal's side. 0 = centre, 1 = full left or right.")]
    [Range(0f, 1f)]
    [SerializeField] private float panAmount = 0.7f;

    Canvas canvas;
    AudioSource audioSource;
    int nextMoo;
    float lastT = float.MaxValue;   // large so the first frame counts as a wrap: the level opens on a moo

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();   // ponytail: auto-added so no scene setup is needed
        audioSource.playOnAwake = false;
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
        if (t < lastT) PlayMoo();   // t dropped: a new cycle began
        lastT = t;
        float a = Mathf.Clamp01(Mathf.Min(t, 2f * fadeTime + holdTime - t) / fadeTime);   // up, hold, down
        group.alpha = a * maxAlpha;
        if (a <= 0f) return;   // hidden: skip the placement maths

        bubble.localScale = Vector3.one * popCurve.Evaluate(Mathf.Clamp01(t / popTime));
        bubble.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * wiggleSpeed) * wiggleAngle * a);

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
            bubble.position = centre + ToEdge(dir, half);
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        tail.localEulerAngles = new Vector3(0f, 0f, angle + tailAngleOffset);
        tail.anchoredPosition = ToEdge(dir, bubble.rect.size * 0.5f);   // sits on the bubble's edge, half hidden behind the body
    }

    void PlayMoo()
    {
        if (mooClips == null || mooClips.Length == 0) return;
        float side = (cam.WorldToScreenPoint(target.position).x - Screen.width * 0.5f) / (Screen.width * 0.5f);
        audioSource.panStereo = Mathf.Clamp(side, -1f, 1f) * panAmount;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(mooClips[nextMoo++ % mooClips.Length], mooVolume);
    }

    // Stretches or shrinks dir until it touches a rect of this half-size: the smaller of the x and y factors.
    // Dividing by 0 gives Infinity, which Min ignores.
    Vector2 ToEdge(Vector2 dir, Vector2 half)
    {
        return dir * Mathf.Min(half.x / Mathf.Abs(dir.x), half.y / Mathf.Abs(dir.y));
    }
}
