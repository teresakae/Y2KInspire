using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Click squash for a menu button. The Button's On Click calls Pop(); when the squash ends, "After Pop"
/// fires, so the real action (load a scene, open a panel) happens after the animation, not before.
/// Uses unscaled time so it still plays if Time.timeScale is 0.
/// </summary>
public class ButtonPop : MonoBehaviour
{
    [Tooltip("Scale over the pop. Time 0 to 1 = start to end; value = scale of the button.")]
    [SerializeField] private AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.4f, 0.9f), new Keyframe(0.75f, 1.05f), new Keyframe(1f, 1f));
    [Tooltip("Length of the pop in seconds.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float duration = 0.2f;
    [Tooltip("Runs when the pop finishes. Put the button's real action here.")]
    [SerializeField] private UnityEvent afterPop;

    private RectTransform rect;
    private bool popping;
    private float timer;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Pop()
    {
        if (popping) return;   // ignore repeat clicks
        popping = true;
        timer = 0f;
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
