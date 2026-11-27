using UnityEngine;

/// <summary>Slides this object left and right forever, for drifting mist.</summary>
public class MistDrift : MonoBehaviour
{
    [Tooltip("How fast it drifts. Higher = faster back and forth.")]
    [SerializeField] private float speed = 0.2f;
    [Tooltip("How far it travels each way, in units. Negative starts the other direction.")]
    [SerializeField] private float distance = 3f;

    private Vector3 start;

    private void Start()
    {
        start = transform.position;
    }

    private void Update()
    {
        // ponytail: plain sine, no easing or randomness
        // Motion setting (SettingsPanel) shrinks the drift; the global is 0 until set, so full drift by default
        float motion = 1f - Shader.GetGlobalFloat("_ReduceMotion");
        transform.position = start + Vector3.right * Mathf.Sin(Time.time * speed) * distance * motion;
    }
}
