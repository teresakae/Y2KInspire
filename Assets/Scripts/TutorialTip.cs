using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One tutorial sign. Waits until a target is on camera with no fog over it, pops a world-space sign above it,
/// then closes (time, mover walked away, target gone, or Done called). Each tip shows once per play session.
/// </summary>
public class TutorialTip : MonoBehaviour
{
    [Tooltip("Name of this tip. A tip whose id was already shown does not show again, so use one id per tip.")]
    [SerializeField] private string tipId;
    [Tooltip("Camera that sees the level. Used to tell if a target is on screen.")]
    [SerializeField] private Camera cam;
    [Tooltip("Fog grid. A target under fog does not count as seen. Empty = ignore fog.")]
    [SerializeField] private FogGrid fog;
    [Tooltip("Things the sign can point at. The first one seen wins. Destroyed or inactive ones are skipped.")]
    [SerializeField] private Transform[] targets;

    [Header("Sign")]
    [Tooltip("The sign (child of a world-space Canvas). Inactive in the prefab. Pivot should be the centre.")]
    [SerializeField] private RectTransform sign;
    [Tooltip("The key drawing inside the sign (optional). Squashes like a pressed key.")]
    [SerializeField] private RectTransform key;
    [Tooltip("Where the sign floats, relative to the target (world units).")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 2f);

    [Header("When to show")]
    [Tooltip("Seconds to wait after the level starts before the sign may open.")]
    [SerializeField] private float showDelay = 0.4f;
    [Tooltip("How far inside the screen edge a target must be to count as seen, as a fraction of the screen.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float viewMargin = 0.05f;
    [Tooltip("World units kept between the sign and the camera edge.")]
    [SerializeField] private float edgePadding = 0.3f;

    [Header("When to close")]
    [Tooltip("Seconds the sign stays open before it closes. 0 = never.")]
    [SerializeField] private float maxTime = 0f;
    [Tooltip("Closes when this moves far enough away from where it was when the sign opened (optional, e.g. the cow).")]
    [SerializeField] private Transform mover;
    [Tooltip("How far the mover must walk to close the sign (world units).")]
    [SerializeField] private float moveDistance = 3f;

    [Header("Repeats")]
    [Tooltip("How many different targets get a sign before this tip is done for the session.")]
    [SerializeField] private int showCount = 1;

    [Header("Hit feedback")]
    [Tooltip("The sign's words (optional).")]
    [SerializeField] private TMP_Text label;
    [Tooltip("Words the sign swaps to on a hit. Empty = keep.")]
    [SerializeField] private string hitText = "";
    [Tooltip("Sign scale over the hit close.")]
    [SerializeField] private AnimationCurve hitPop = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.12f, 1.5f), new Keyframe(0.3f, 0.9f), new Keyframe(0.45f, 1.1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f));
    [Tooltip("Seconds the hit close takes.")]
    [SerializeField] private float hitTime = 0.9f;
    [Tooltip("Degrees the sign shakes each way on a hit.")]
    [SerializeField] private float hitShake = 10f;
    [Tooltip("Speed of the hit shake (radians per second).")]
    [SerializeField] private float hitShakeSpeed = 45f;
    [Tooltip("Key height at the moment of the hit. Springs back to 1 over the first half of the hit time.")]
    [SerializeField] private float keySlam = 0.45f;

    [Header("Juice")]
    [Tooltip("Sign scale while it opens.")]
    [SerializeField] private AnimationCurve popIn = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.55f, 1.15f), new Keyframe(0.8f, 0.95f), new Keyframe(1f, 1f));
    [Tooltip("Seconds the pop-in takes.")]
    [SerializeField] private float popInTime = 0.4f;
    [Tooltip("Sign scale while it closes.")]
    [SerializeField] private AnimationCurve popOut = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 1.12f), new Keyframe(1f, 0f));
    [Tooltip("Seconds the pop-out takes.")]
    [SerializeField] private float popOutTime = 0.25f;
    [Tooltip("Key height over one loop: press, hold, spring up. Teaches hold and release.")]
    [SerializeField] private AnimationCurve keyPress = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.15f, 0.86f), new Keyframe(0.65f, 0.82f), new Keyframe(0.72f, 1.12f), new Keyframe(0.82f, 1f), new Keyframe(1f, 1f));
    [Tooltip("Seconds one press loop takes.")]
    [SerializeField] private float keyLoopTime = 1.4f;
    [Tooltip("How much the key widens as it is pressed down.")]
    [SerializeField] private float keySquash = 0.5f;

    // Static so a death reload doesn't replay tips; a browser refresh does.
    static List<string> seen = new List<string>();

    // The editor may skip the domain reload on Play, which would keep the list from last time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSeen()
    {
        seen = new List<string>();
    }

    // Main menu Start begins a fresh game, so every tip shows again.
    public static void ForgetAll()
    {
        seen.Clear();
    }

    [Tooltip("Played when a sign opens (UI_guide). Empty = silent.")]
    [SerializeField] private AudioClip guideClip;

    private AudioSource audioSource;
    bool showing, closing, hit;
    int shown;             // signs opened and closed so far
    float timer;
    Transform target;
    Vector3 lastPos, moverStart;
    Vector3 baseScale;     // sign scale as authored, before juice
    string labelText;      // label as authored, restored when the sign goes back to waiting

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();   // ponytail: auto-added so no scene setup is needed
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (cam == null) Debug.LogError($"{name}: missing cam");
        if (sign == null) Debug.LogError($"{name}: missing sign");
        if (string.IsNullOrEmpty(tipId)) Debug.LogError($"{name}: empty tipId");
        if (cam == null || sign == null)
        {
            enabled = false;
            return;
        }
        baseScale = sign.localScale;
        if (label != null) labelText = label.text;
        if (seen.Contains(tipId)) gameObject.SetActive(false);
    }

    void Update()
    {
        if (closing)
        {
            timer += Time.deltaTime;
            if (hit) AnimateHit();
            if (timer >= (hit ? hitTime : popOutTime))
            {
                sign.gameObject.SetActive(false);
                if (shown >= showCount) gameObject.SetActive(false);
                else ResetToWaiting();
            }
            return;
        }

        if (showing)
        {
            timer += Time.deltaTime;
            if (target == null || !target.gameObject.activeInHierarchy) Close();   // fruit eaten, snake dead
            else if (maxTime > 0f && timer >= maxTime) Close();
            else if (mover != null && Vector3.Distance(mover.position, moverStart) >= moveDistance) Close();
            AnimateInside();
            return;
        }

        // waiting
        showDelay -= Time.deltaTime;
        if (showDelay > 0f) return;
        for (int i = 0; i < targets.Length; i++)
        {
            Transform t = targets[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (IsSeen(t))
            {
                targets[i] = null;   // so the same target never gets a second sign
                Open(t);
                break;
            }
        }
    }

    // LateUpdate: the camera moves in its own LateUpdate, so placing earlier would lag a frame.
    void LateUpdate()
    {
        if (!showing && !closing) return;
        if (target != null) lastPos = target.position;   // keep the last position, so a destroyed target closes in place

        float s;
        if (closing && hit) s = hitPop.Evaluate(Mathf.Clamp01(timer / hitTime));
        else if (closing) s = popOut.Evaluate(Mathf.Clamp01(timer / popOutTime));
        else s = popIn.Evaluate(Mathf.Clamp01(timer / popInTime));
        sign.localScale = baseScale * s;

        Vector2 worldSize = sign.rect.size * baseScale;   // read here: the layout sizes the sign only once it is open
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;
        Vector2 pos = (Vector2)lastPos + offset;
        float marginX = Mathf.Max(0f, halfW - edgePadding - worldSize.x * 0.5f);
        float marginY = Mathf.Max(0f, halfH - edgePadding - worldSize.y * 0.5f);
        // How far the camera edge may push the sign. Past that it slides off screen with its target
        // instead of sticking to the edge when the players walk away.
        float pushX = worldSize.x + Mathf.Abs(offset.x);
        float pushY = worldSize.y + Mathf.Abs(offset.y);
        float inX = Mathf.Clamp(pos.x, c.x - marginX, c.x + marginX);
        float inY = Mathf.Clamp(pos.y, c.y - marginY, c.y + marginY);
        pos.x += Mathf.Clamp(inX - pos.x, -pushX, pushX);
        pos.y += Mathf.Clamp(inY - pos.y, -pushY, pushY);
        sign.position = new Vector3(pos.x, pos.y, 0f);
    }

    bool IsSeen(Transform t)
    {
        Vector3 vp = cam.WorldToViewportPoint(t.position);
        if (vp.z <= 0f) return false;
        if (vp.x < viewMargin || vp.x > 1f - viewMargin || vp.y < viewMargin || vp.y > 1f - viewMargin) return false;
        return fog == null || fog.IsClear(t.position);
    }

    void Open(Transform t)
    {
        target = t;
        lastPos = t.position;
        if (mover != null) moverStart = mover.position;
        showing = true;
        timer = 0f;
        if (guideClip != null) audioSource.PlayOneShot(guideClip);
        sign.localScale = baseScale * popIn.Evaluate(0f);   // no full-size flash before the first LateUpdate
        sign.gameObject.SetActive(true);
    }

    void AnimateInside()
    {
        if (key != null)
        {
            float y = keyPress.Evaluate((timer % keyLoopTime) / keyLoopTime);
            key.localScale = new Vector3(1f + (1f - y) * keySquash, y, 1f);
        }
    }

    // Shake and key slam while a hit closes the sign.
    void AnimateHit()
    {
        float t01 = Mathf.Clamp01(timer / hitTime);
        sign.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(timer * hitShakeSpeed) * hitShake * (1f - t01));
        AnimateInside();
        if (key != null)
        {
            float y = Mathf.Lerp(keySlam, 1f, Mathf.Clamp01(t01 * 2f));   // springs back in the first half
            key.localScale = new Vector3(1f + (1f - y) * keySquash, y, 1f);
        }
    }

    // More targets to go: undo everything the close changed and wait for the next one.
    void ResetToWaiting()
    {
        closing = false;
        hit = false;
        target = null;
        sign.localRotation = Quaternion.identity;
        if (label != null) label.text = labelText;
        if (key != null)
        {
            key.localScale = Vector3.one;
            key.localRotation = Quaternion.identity;
        }
    }

    void Close()
    {
        shown++;
        // Dying mid-tip reloads the scene and shows it again; intended.
        if (shown >= showCount && !seen.Contains(tipId)) seen.Add(tipId);
        showing = false;
        closing = true;
        timer = 0f;
    }

    /// <summary>The player did the thing. Closes an open sign; a waiting one is skipped for good.</summary>
    public void Done()
    {
        if (!seen.Contains(tipId)) seen.Add(tipId);   // done counts even if more targets were left
        if (closing)
        {
            shown = showCount;   // the running close ends deactivated, not back to waiting
            return;
        }
        if (showing)
        {
            shown = showCount - 1;   // Close adds the last one
            Close();
            return;
        }
        shown = showCount;
        gameObject.SetActive(false);
    }

    /// <summary>Hides the sign at once without counting it as seen, so it shows again after a restart. Used when the crow dies, so the sign doesn't cover the death.</summary>
    public void Hide()
    {
        if (sign != null) sign.gameObject.SetActive(false);
        enabled = false;
    }

    /// <summary>The target was hit (e.g. the snake bonked). Swaps the words and closes with a shake. Other targets are ignored.</summary>
    public void Hit(Transform t)
    {
        if (!showing || t != target) return;
        if (label != null && !string.IsNullOrEmpty(hitText)) label.text = hitText;
        hit = true;
        Close();
    }
}
