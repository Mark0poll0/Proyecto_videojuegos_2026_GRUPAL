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
    private PlayerBuffs playerBuffs;   // stats de crítico/robo de vida
    private PlayerHealth playerHealth; // para curar por robo de vida

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        playerBuffs = GetComponentInParent<PlayerBuffs>();
        playerHealth = GetComponentInParent<PlayerHealth>();
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

            // Crítico: probabilidad de multiplicar el daño.
            int finalDamage = damage;
            bool isCrit = false;
            if (playerBuffs != null && playerBuffs.CritChance > 0f && Random.value < playerBuffs.CritChance)
            {
                finalDamage = Mathf.RoundToInt(damage * playerBuffs.CritMultiplier);
                isCrit = true;
            }

            damageable.TakeDamage(finalDamage);

            // Robo de vida: curar una fracción del daño infligido.
            if (playerBuffs != null && playerBuffs.LifeStealFraction > 0f && playerHealth != null)
            {
                int heal = Mathf.CeilToInt(finalDamage * playerBuffs.LifeStealFraction);
                if (heal > 0) playerHealth.Heal(heal);
            }

            // Feedback de impacto: número de daño flotante (crítico resaltado) + hit-stop.
            if (JuiceManager.Instance != null)
            {
                if (isCrit)
                    JuiceManager.Instance.ShowFloatingText(other.transform.position, finalDamage + "!", new Color(1f, 0.55f, 0.1f), 12f);
                else
                    JuiceManager.Instance.ShowFloatingText(other.transform.position, finalDamage.ToString(), Color.white);
                JuiceManager.Instance.HitStop(0.04f);
            }

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
