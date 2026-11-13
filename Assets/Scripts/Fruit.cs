using UnityEngine;

/// <summary>Fruit on the ground. The crow eats it by flying over; the cow ignores it.</summary>
public class Fruit : MonoBehaviour
{
    [Tooltip("Energy the crow gets.")]
    [SerializeField] private float energy = 30f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CrowEnergy crow)) return;
        crow.Eat(energy);
        Destroy(gameObject);
    }
}
