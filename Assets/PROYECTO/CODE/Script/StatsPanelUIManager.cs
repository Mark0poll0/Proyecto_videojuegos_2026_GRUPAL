using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Pergamino de personaje. Con la tecla E se abre/cierra (pausando el juego) y
/// muestra los buffs conseguidos (con su contador) y las stats actuales.
/// Va en un objeto SIEMPRE activo (HUD Canvas); él togglea el panel.
/// </summary>
public class StatsPanelUIManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Panel raíz del pergamino (se activa/desactiva).")]
    [SerializeField] private GameObject panel;
    [Tooltip("Texto donde se listan los buffs.")]
    [SerializeField] private TMP_Text buffsText;
    [Tooltip("Texto donde se listan las stats.")]
    [SerializeField] private TMP_Text statsText;

    [Header("Referencias del jugador (auto-detectadas por tag 'Player')")]
    [SerializeField] private PlayerBuffs playerBuffs;
    [SerializeField] private Player_Controller playerController;
    [SerializeField] private PlayerAttackHitbox attackHitbox;
    [SerializeField] private PlayerHealth playerHealth;

    private bool isOpen;

    private void Start()
    {
        if (playerBuffs == null || playerController == null || attackHitbox == null || playerHealth == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                if (playerBuffs == null) playerBuffs = go.GetComponent<PlayerBuffs>();
                if (playerController == null) playerController = go.GetComponent<Player_Controller>();
                if (attackHitbox == null) attackHitbox = go.GetComponentInChildren<PlayerAttackHitbox>(true);
                if (playerHealth == null) playerHealth = go.GetComponent<PlayerHealth>();
            }
        }
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    /// <summary>Abre/cierra el pergamino (público para poder probarlo por código).</summary>
    public void Toggle()
    {
        if (isOpen)
        {
            if (panel != null) panel.SetActive(false);
            Time.timeScale = 1f;
            isOpen = false;
        }
        else
        {
            // Solo abrir en juego normal (no encima de la pausa de buffs / Game Over).
            if (!Mathf.Approximately(Time.timeScale, 1f)) return;
            Rebuild();
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
            isOpen = true;
        }
    }

    private void Rebuild()
    {
        // ----- Buffs conseguidos -----
        if (buffsText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>MEJORAS</b>");
            bool any = false;
            if (playerBuffs != null)
            {
                foreach (var kv in playerBuffs.GetPickCounts())
                {
                    if (kv.Value <= 0) continue;
                    sb.AppendLine("• " + NombreBuff(kv.Key) + "  x" + kv.Value);
                    any = true;
                }
            }
            if (!any) sb.AppendLine("<i>Sin mejoras aún</i>");
            buffsText.text = sb.ToString();
        }

        // ----- Stats actuales -----
        if (statsText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>STATS</b>");
            if (playerHealth != null)
            {
                int hearts = Mathf.CeilToInt(playerHealth.MaxHealth / 4f);
                sb.AppendLine("Vida: " + playerHealth.CurrentHealth + "/" + playerHealth.MaxHealth + " (" + hearts + " ♥)");
            }
            if (playerController != null)
            {
                sb.AppendLine("Velocidad: " + playerController.MoveSpeed.ToString("0.0"));
                sb.AppendLine("Cadencia: x" + playerController.AttackSpeedMultiplier.ToString("0.00"));
            }
            if (attackHitbox != null)
                sb.AppendLine("Daño/golpe: " + attackHitbox.Damage);
            if (playerBuffs != null)
            {
                sb.AppendLine("Crítico: " + Mathf.RoundToInt(playerBuffs.CritChance * 100f) + "% (x" + playerBuffs.CritMultiplier.ToString("0.#") + ")");
                sb.AppendLine("Robo de vida: " + Mathf.RoundToInt(playerBuffs.LifeStealFraction * 100f) + "%");
                sb.AppendLine("Espinas: " + playerBuffs.ThornsDamage);
                if (playerBuffs.TurboUnlocked)
                    sb.AppendLine("Turbo: " + playerBuffs.TurboDuration.ToString("0.#") + "s / cd " + playerBuffs.TurboCooldown.ToString("0.#") + "s");
                else
                    sb.AppendLine("Turbo: bloqueado");
            }
            if (ScoreManager.Instance != null)
                sb.AppendLine("Score: " + ScoreManager.Instance.CurrentScore + "   Kills: " + ScoreManager.Instance.Kills);
            statsText.text = sb.ToString();
        }
    }

    private string NombreBuff(BuffType t)
    {
        switch (t)
        {
            case BuffType.Speed: return "Velocidad";
            case BuffType.Damage: return "Daño";
            case BuffType.MaxHealth: return "Vida máxima";
            case BuffType.FullHeal: return "Curación total";
            case BuffType.AttackSpeed: return "Cadencia";
            case BuffType.CritChance: return "Crítico";
            case BuffType.LifeSteal: return "Robo de vida";
            case BuffType.Thorns: return "Espinas";
            case BuffType.Turbo: return "Turbo";
            default: return t.ToString();
        }
    }
}
