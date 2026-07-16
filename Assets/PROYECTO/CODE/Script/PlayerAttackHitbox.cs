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

    private System.Collections.Generic.List<Collider2D> alreadyHit = new System.Collections.Generic.List<Collider2D>();

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public void ClearHits()
    {
        alreadyHit.Clear();
    }

    /// <summary>Aumenta el daño de cada golpe del combo (buff de daño).</summary>
    public void AddDamage(int amount)
    {
        if (amount == 0) return;
        damage = Mathf.Max(0, damage + amount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        // Si ya golpeamos a este colisionador durante esta ventana de golpe, lo ignoramos
        if (alreadyHit.Contains(other)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            alreadyHit.Add(other);
            Debug.Log($"[Hitbox] Golpeando a: {other.gameObject.name}. Daño: {damage}");
            damageable.TakeDamage(damage);

            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                // Dirección desde el jugador (centro o hitbox) hacia el enemigo
                Vector3 origin = transform.parent != null ? transform.parent.position : transform.position;
                Vector2 knockbackDir = (other.transform.position - origin).normalized;
                
                // Si la dirección es cero o muy pequeña (superposición total), usamos la dirección del ataque
                if (knockbackDir.sqrMagnitude < 0.01f)
                {
                    knockbackDir = transform.localPosition.normalized;
                }
                
                Debug.Log($"[Hitbox] EnemyController encontrado. Aplicando knockback: dir={knockbackDir}, force={knockbackForce}, duration={knockbackDuration}");
                enemy.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);
            }
        }
    }
}
