using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Panel que contiene la interfaz de la pantalla de derrota (GameOver).")]
    [SerializeField] private GameObject gameOverPanel;
    
    [Tooltip("Referencia al PlayerHealth del jugador. Se auto-detectará si se deja vacío.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Ajustes")]
    [Tooltip("Tiempo en segundos que se esperará antes de mostrar la pantalla de derrota (permite ver la animación de muerte).")]
    [SerializeField] private float delayBeforeShow = 1.5f;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
        }
        else
        {
            Debug.LogWarning("No se encontró referencia a PlayerHealth en GameOverManager. Se reintentará buscar en ejecución.");
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0)
        {
            // Desuscribirse para evitar múltiples ejecuciones
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
            }
            StartCoroutine(ShowGameOverRoutine());
        }
    }

    private IEnumerator ShowGameOverRoutine()
    {
        // Esperamos el tiempo configurado para que se complete la animación de muerte
        yield return new WaitForSeconds(delayBeforeShow);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Congelamos el tiempo del juego al mostrar la derrota
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Reinicia la escena actual de juego.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Regresa a la escena del menú principal.
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("No se ha definido el nombre de la escena del menú principal en el GameOverManager.");
        }
    }
}
