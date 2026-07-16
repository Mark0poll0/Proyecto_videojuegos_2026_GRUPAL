using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// HUD de la puntuación. Se suscribe a ScoreManager.OnScoreChanged y pinta el
/// texto "Score: N". Mismo patrón dirigido por eventos que HeartUIManager.
/// Al subir la puntuación hace un "pop" (brinco de escala + flash de color).
/// </summary>
public class ScoreUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Texto (TextMeshPro) donde se muestra la puntuación. Si se deja vacío, se busca en este mismo objeto.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Formato")]
    [Tooltip("Texto que aparece antes del número. Ej: 'Score: '")]
    [SerializeField] private string prefix = "Score: ";

    [Header("Pop al subir")]
    [Tooltip("Escala máxima del brinco al sumar puntos.")]
    [SerializeField] private float popScale = 1.35f;
    [Tooltip("Duración del brinco en segundos (tiempo real).")]
    [SerializeField] private float popDuration = 0.22f;
    [Tooltip("Color del flash al sumar puntos.")]
    [SerializeField] private Color popColor = new Color(1f, 0.9f, 0.25f);

    private int lastValue;
    private bool hasValue;
    private Color baseColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private Coroutine popRoutine;

    private void Awake()
    {
        if (scoreText == null) scoreText = GetComponent<TMP_Text>();
        if (scoreText != null)
        {
            baseColor = scoreText.color;
            baseScale = scoreText.rectTransform.localScale;
        }
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
        if (scoreText == null) return;

        scoreText.text = prefix + score.ToString();

        // Pop solo cuando la puntuación SUBE (no en el pintado inicial).
        if (hasValue && score > lastValue && isActiveAndEnabled)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popRoutine = StartCoroutine(PopRoutine());
        }

        lastValue = score;
        hasValue = true;
    }

    private IEnumerator PopRoutine()
    {
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popDuration);
            // 0 -> 1 -> 0 (brinco)
            float punch = Mathf.Sin(k * Mathf.PI);
            scoreText.rectTransform.localScale = baseScale * Mathf.Lerp(1f, popScale, punch);
            scoreText.color = Color.Lerp(baseColor, popColor, punch);
            yield return null;
        }
        scoreText.rectTransform.localScale = baseScale;
        scoreText.color = baseColor;
        popRoutine = null;
    }
}
