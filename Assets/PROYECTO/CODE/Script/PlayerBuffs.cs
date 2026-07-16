using UnityEngine;

/// <summary>
/// Punto central para aplicar mejoras (buffs) al jugador. Aísla al
/// BuffSelectionManager de los detalles internos de cada componente.
/// Va colocado en el PROTAGONISTA.
/// </summary>
public class PlayerBuffs : MonoBehaviour
{
    private Player_Controller playerController;
    private PlayerHealth playerHealth;
    private PlayerAttackHitbox attackHitbox;

    private void Awake()
    {
        playerController = GetComponent<Player_Controller>();
        playerHealth = GetComponent<PlayerHealth>();
        attackHitbox = GetComponentInChildren<PlayerAttackHitbox>(true);
    }

    /// <summary>Aplica un buff según su tipo y magnitud (llamado desde BuffSelectionManager).</summary>
    public void ApplyBuff(BuffType type, float magnitude)
    {
        switch (type)
        {
            case BuffType.Speed:
                if (playerController != null) playerController.AddMoveSpeed(magnitude);
                break;

            case BuffType.Damage:
                if (attackHitbox != null) attackHitbox.AddDamage(Mathf.RoundToInt(magnitude));
                break;

            case BuffType.MaxHealth:
                if (playerHealth != null) playerHealth.IncreaseMaxHealth(Mathf.RoundToInt(magnitude));
                break;

            case BuffType.FullHeal:
                // Curación total: curamos una cantidad muy grande, PlayerHealth la limita a la vida máxima.
                if (playerHealth != null) playerHealth.Heal(9999);
                break;

            case BuffType.AttackSpeed:
                // magnitude = porcentaje extra (ej. 0.2 = +20% de velocidad de ataque).
                if (playerController != null) playerController.MultiplyAttackSpeed(1f + magnitude);
                break;
        }
    }
}
