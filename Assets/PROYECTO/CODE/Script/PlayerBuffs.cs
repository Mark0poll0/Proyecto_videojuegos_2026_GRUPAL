using UnityEngine;

/// <summary>
/// Punto central para aplicar y ALMACENAR las mejoras (buffs) del jugador.
/// Aísla al BuffSelectionManager de los detalles internos, y expone los stats
/// de combate que leen el hitbox (crítico/robo de vida), los enemigos (espinas)
/// y el Player_Controller (turbo). Va colocado en el PROTAGONISTA.
/// </summary>
public class PlayerBuffs : MonoBehaviour
{
    [Header("Crítico")]
    [Tooltip("Probabilidad de golpe crítico (0..1).")]
    [SerializeField] private float critChance = 0f;
    [Tooltip("Multiplicador de daño en golpe crítico.")]
    [SerializeField] private float critMultiplier = 2f;

    [Header("Robo de vida")]
    [Tooltip("Fracción del daño infligido que cura al jugador (0..1).")]
    [SerializeField] private float lifeStealFraction = 0f;

    [Header("Espinas")]
    [Tooltip("Daño reflejado al enemigo cada vez que te golpea.")]
    [SerializeField] private int thornsDamage = 0;

    [Header("Turbo (habilidad activa - tecla Shift)")]
    [SerializeField] private bool turboUnlocked = false;
    [Tooltip("Duración del turbo en segundos.")]
    [SerializeField] private float turboDuration = 4f;
    [Tooltip("Cooldown del turbo en segundos.")]
    [SerializeField] private float turboCooldown = 10f;
    [Tooltip("Cooldown mínimo al que puede bajar tras mejoras.")]
    [SerializeField] private float turboCooldownMin = 3f;
    [Tooltip("Multiplicador de velocidad de movimiento durante el turbo.")]
    [SerializeField] private float turboSpeedMult = 1.7f;
    [Tooltip("Multiplicador de velocidad de ataque durante el turbo.")]
    [SerializeField] private float turboAtkSpeedMult = 1.7f;

    // Lectura pública para los otros sistemas.
    public float CritChance => critChance;
    public float CritMultiplier => critMultiplier;
    public float LifeStealFraction => lifeStealFraction;
    public int ThornsDamage => thornsDamage;
    public bool TurboUnlocked => turboUnlocked;
    public float TurboDuration => turboDuration;
    public float TurboCooldown => turboCooldown;
    public float TurboSpeedMult => turboSpeedMult;
    public float TurboAtkSpeedMult => turboAtkSpeedMult;

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

            case BuffType.CritChance:
                critChance = Mathf.Clamp01(critChance + magnitude);
                break;

            case BuffType.LifeSteal:
                lifeStealFraction = Mathf.Clamp01(lifeStealFraction + magnitude);
                break;

            case BuffType.Thorns:
                thornsDamage += Mathf.Max(0, Mathf.RoundToInt(magnitude));
                break;

            case BuffType.Turbo:
                if (!turboUnlocked)
                    turboUnlocked = true; // primer pick: desbloquea
                else
                    turboCooldown = Mathf.Max(turboCooldownMin, turboCooldown - magnitude); // picks siguientes: baja cooldown
                break;
        }
    }
}
