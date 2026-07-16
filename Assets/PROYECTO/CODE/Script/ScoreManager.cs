using UnityEngine;
using System;

/// <summary>
/// Gestiona la puntuación de la partida. El score sube al matar enemigos y se
/// gasta al comprar buffs (ver BuffSelectionManager).
///
/// Es un singleton SIN DontDestroyOnLoad: al recargar la escena (tras Game Over)
/// se recrea desde cero, por lo que el score vuelve a 0 automáticamente (run única).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>Puntuación actual del jugador en esta partida.</summary>
    public int CurrentScore { get; private set; }

    /// <summary>Se dispara cada vez que cambia la puntuación. Parámetro: nueva puntuación total.</summary>
    public event Action<int> OnScoreChanged;

    private void Awake()
    {
        // Patrón Singleton: solo puede existir un ScoreManager.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EnemyHealth.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Notificamos el valor inicial para que la UI se pinte al arrancar.
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void HandleEnemyKilled(int scoreValue)
    {
        AddScore(scoreValue);
    }

    /// <summary>Suma puntos a la puntuación (al matar enemigos, recoger objetos, etc.).</summary>
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>
    /// Intenta gastar puntos (para comprar un buff).
    /// Devuelve true si había suficiente puntuación y se descontó.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || CurrentScore < amount) return false;
        CurrentScore -= amount;
        OnScoreChanged?.Invoke(CurrentScore);
        return true;
    }

    [ContextMenu("Debug: Sumar 100 puntos")]
    private void DebugAdd100() => AddScore(100);

    [ContextMenu("Debug: Reiniciar puntuación a 0")]
    private void DebugReset()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
