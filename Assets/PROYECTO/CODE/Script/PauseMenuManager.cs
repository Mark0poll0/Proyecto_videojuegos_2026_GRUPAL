using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("Panel que contiene la interfaz del menú de pausa.")]
    [SerializeField] private GameObject pausePanel;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    private void Start()
    {
        Debug.Log("PauseMenuManager: Iniciado en la escena.");

        // Asegurarse de que el panel de pausa empiece desactivado
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: El panel 'Pause Panel' NO ha sido asignado en el Inspector.");
        }
        isPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        bool pausePressed = false;

        // 1. Intentar detectar con el Nuevo Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
            {
                Debug.Log("PauseMenuManager: Pulsación detectada por New Input System.");
                pausePressed = true;
            }
        }

        // 2. Intentar detectar con el Sistema de Input Antiguo (como fallback seguro)
        try
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("PauseMenuManager: Pulsación detectada por Old Input System.");
                pausePressed = true;
            }
        }
        catch (System.InvalidOperationException)
        {
            // Ignorar el error si el sistema antiguo está totalmente desactivado en los Player Settings de Unity
        }

        // Si alguno de los sistemas detectó la pulsación, alternamos la pausa
        if (pausePressed)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Reanuda el juego y oculta el menú de pausa.
    /// </summary>
    public void Resume()
    {
        Debug.Log("PauseMenuManager: Reanudando juego.");
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
    }

    /// <summary>
    /// Pausa el juego y muestra el menú de pausa.
    /// </summary>
    public void Pause()
    {
        Debug.Log("PauseMenuManager: Pausando juego.");
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: Intentando pausar pero 'Pause Panel' es nulo.");
        }
        Time.timeScale = 0f;
        isPaused = true;
    }

    /// <summary>
    /// Reinicia la escena actual de juego.
    /// </summary>
    public void Restart()
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
            Debug.LogError("No se ha definido el nombre de la escena del menú principal en el PauseMenuManager.");
        }
    }
}
