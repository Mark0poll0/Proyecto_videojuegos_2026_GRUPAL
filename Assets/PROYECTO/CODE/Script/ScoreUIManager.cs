using UnityEngine;
using TMPro;

/// <summary>
/// HUD de la puntuación. Se suscribe a ScoreManager.OnScoreChanged y pinta el
/// texto "Score: N". Mismo patrón dirigido por eventos que HeartUIManager.
/// </summary>
public class ScoreUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Texto (TextMeshPro) donde se muestra la puntuación. Si se deja vacío, se busca en este mismo objeto.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Formato")]
    [Tooltip("Texto que aparece antes del número. Ej: 'Score: '")]
    [SerializeField] private string prefix = "Score: ";

    private void Awake()
    {
        if (scoreText == null) scoreText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // Salvaguarda: si el ScoreManager se creó después de nuestro OnEnable,
        // nos aseguramos de estar suscritos y con el valor inicial pintado.
        Subscribe();
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
        }
    }

    private void Subscribe()
    {
        if (ScoreManager.Instance == null) return;

        // Evitamos suscripción doble antes de volver a suscribirnos.
        ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
        ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
        UpdateScoreText(ScoreManager.Instance.CurrentScore);
    }

    private void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = prefix + score.ToString();
        }
    }
}
