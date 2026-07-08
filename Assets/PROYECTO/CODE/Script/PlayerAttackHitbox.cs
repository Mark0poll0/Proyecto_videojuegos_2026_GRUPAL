using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [Tooltip("Daño aplicado por cada golpe del combo que conecte.")]
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage);
    }
}
