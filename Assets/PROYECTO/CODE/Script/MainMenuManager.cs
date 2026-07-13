using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena a cargar al presionar Jugar.")]
    [SerializeField] private string gameplaySceneName = "mapa 1";

    [Header("Paneles de la UI")]
    [Tooltip("Panel principal del menú.")]
    [SerializeField] private GameObject mainPanel;
    [Tooltip("Panel de ajustes u opciones (opcional).")]
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        // Asegurarse de que el tiempo corra normalmente al entrar al menú
        Time.timeScale = 1f;

        // Mostrar menú principal y ocultar ajustes por defecto
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Inicia la partida cargando la escena de juego.
    /// </summary>
    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError("No se ha definido el nombre de la escena de juego en el MainMenuManager.");
        }
    }

    /// <summary>
    /// Abre el panel de configuración y oculta el principal.
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    /// <summary>
    /// Cierra el panel de configuración y muestra el principal.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    /// <summary>
    /// Cierra la aplicación (solo funciona en builds).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
