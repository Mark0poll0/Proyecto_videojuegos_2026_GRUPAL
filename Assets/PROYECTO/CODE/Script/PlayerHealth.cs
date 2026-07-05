using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Ajustes de Vida")]
    [Tooltip("Cada corazón equivale a 4 puntos de vida.")]
    [SerializeField] private int maxHearts = 3;
    
    private int currentHealth;
    private int maxHealth;

    private Player_Controller playerController;

    // Evento al que se suscribirá la UI para actualizarse automáticamente
    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        playerController = GetComponent<Player_Controller>();
        maxHealth = maxHearts * 4;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Notificamos la vida inicial al comenzar
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Reduce la vida del jugador.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        
        // Notificar a la UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            if (playerController != null) playerController.Die();
        }
        else
        {
            if (playerController != null) playerController.TakeDamage();
        }
    }

    /// <summary>
    /// Restaura vida al jugador.
    /// </summary>
    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return; // No se puede curar si está muerto

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Métodos de prueba rápida desde el Inspector de Unity
    [ContextMenu("Hacer Daño de Prueba (1 punto)")]
    private void TestDamage()
    {
        TakeDamage(1);
    }

    [ContextMenu("Curar de Prueba (1 punto)")]
    private void TestHeal()
    {
        Heal(1);
    }
}
