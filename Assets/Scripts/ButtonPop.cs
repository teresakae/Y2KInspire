using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Click squash for a menu button. The Button's On Click calls Pop(); when the squash ends, "After Pop"
/// fires, so the real action (load a scene, open a panel) happens after the animation, not before.
/// Uses unscaled time so it still plays if Time.timeScale is 0.
/// </summary>
public class ButtonPop : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    [Tooltip("Scale over the pop. Time 0 to 1 = start to end; value = scale of the button.")]
    [SerializeField] private AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.4f, 0.9f), new Keyframe(0.75f, 1.05f), new Keyframe(1f, 1f));
    [Tooltip("Length of the pop in seconds.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float duration = 0.2f;
    [Tooltip("Runs when the pop finishes. Put the button's real action here.")]
    [SerializeField] private UnityEvent afterPop;

    [Tooltip("Played when the button is clicked (UI_buttonClick). Empty = silent.")]
    [SerializeField] private AudioClip clickClip;

    [Tooltip("Played when the pointer enters the button (UI_hover). Empty = silent.")]
    [SerializeField] private AudioClip hoverClip;

    private RectTransform rect;
    private AudioSource audioSource;
    private bool popping;
    private float timer;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();   // ponytail: auto-added so no scene setup is needed
        audioSource.playOnAwake = false;
    }

    public void Pop()
    {
        if (popping) return;   // ignore repeat clicks
        popping = true;
        timer = 0f;
        if (clickClip != null) audioSource.PlayOneShot(clickClip);
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
    {
        if (hoverClip == null) return;
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null && !button.interactable) return;
        audioSource.PlayOneShot(hoverClip);
    }

    void Update()
    {
        if (!popping) return;
        timer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(timer / duration);
        rect.localScale = Vector3.one * curve.Evaluate(t);
        if (t >= 1f)
        {
            popping = false;
            rect.localScale = Vector3.one;
            afterPop.Invoke();
        }
    }
}
