using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [Tooltip("Daño aplicado por cada golpe del combo que conecte.")]
    [SerializeField] private int damage = 1;

    [Header("Empuje (Knockback)")]
    [Tooltip("Fuerza del empuje al golpear al enemigo. Puedes cambiarlo para ver qué tan lejos lo empuja.")]
    public float knockbackForce = 10f;
    [Tooltip("Tiempo en segundos que el enemigo es empujado sin poder moverse.")]
    public float knockbackDuration = 0.15f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // Dirección desde el jugador (padre del hitbox) hacia el enemigo
                Vector2 knockbackDir = (other.transform.position - transform.parent.position).normalized;
                enemy.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);
            }
        }
    }
}
