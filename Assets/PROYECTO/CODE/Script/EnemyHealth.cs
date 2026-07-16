using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [Tooltip("Vida máxima del enemigo. Ajustar por dificultad (Green/Blue/Red).")]
    [SerializeField] private int maxHealth = 6;

    [Header("Puntuación")]
    [Tooltip("Puntos que otorga este enemigo al morir. Ajustar por dificultad (Green/Blue/Red).")]
    [SerializeField] private int scoreValue = 10;

    /// <summary>
    /// Evento estático que se dispara al morir CUALQUIER enemigo, con los puntos que otorga.
    /// El ScoreManager se suscribe aquí (funciona con los enemigos que instancia el generador
    /// procedural, sin necesidad de cablear referencias a mano).
    /// </summary>
    public static event Action<int> OnEnemyKilled;

    [Header("Sonidos (SFX)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] deathSounds;

    private int currentHealth;
    private bool isDead;

    // La UI o cualquier listener futuro se suscribe a estos eventos.
    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlayRandomSound(hurtSounds);
        }
    }

    private void Die()
    {
        isDead = true;
        PlayRandomSound(deathSounds);
        OnDeath?.Invoke();
        OnEnemyKilled?.Invoke(scoreValue);
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;
        sfxSource.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)]);
    }

    [ContextMenu("Debug: Recibir Daño (1 punto)")]
    private void DebugTakeDamage()
    {
        TakeDamage(1);
    }
}
