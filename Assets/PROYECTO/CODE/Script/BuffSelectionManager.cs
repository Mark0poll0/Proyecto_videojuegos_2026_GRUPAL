using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>Tipos de mejora que puede recibir el jugador.</summary>
public enum BuffType
{
    Speed,
    Damage,
    MaxHealth,
    FullHeal,
    AttackSpeed
}

/// <summary>
/// Controla la pantalla de elección de buffs (estilo "pastilla roja / azul").
///
/// Cuando el score alcanza el coste actual, congela el juego y muestra dos
/// mejoras al azar (izquierda = azul, derecha = roja). Al elegir, aplica el buff
/// vía PlayerBuffs, descuenta el coste del ScoreManager y sube el coste siguiente.
/// </summary>
public class BuffSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class BuffOption
    {
        [Tooltip("Tipo de mejora que otorga esta opción.")]
        public BuffType type;
        [Tooltip("Nombre visible. Ej: '+ Velocidad'")]
        public string displayName = "+ Mejora";
        [Tooltip("Descripción corta. Ej: 'Te mueves más rápido'")]
        [TextArea] public string description = "";
        [Tooltip("Magnitud del efecto. Velocidad = unidades; Daño = puntos; Vida = puntos (4 = 1 corazón); Cadencia = fracción (0.2 = +20%). Curación total ignora este valor.")]
        public float magnitude = 1f;
        [Tooltip("Icono opcional que se muestra en la pastilla.")]
        public Sprite icon;
    }

    [Header("Pool de Buffs (configurable)")]
    [Tooltip("Todas las mejoras posibles. En cada elección se sortean 2 distintas.")]
    [SerializeField] private List<BuffOption> buffPool = new List<BuffOption>();

    [Header("Coste (escalado)")]
    [Tooltip("Puntos necesarios para la primera elección.")]
    [SerializeField] private int baseCost = 100;
    [Tooltip("Multiplicador del coste tras cada compra. Ej: 1.75 → 100, 175, 306...")]
    [SerializeField] private float costMultiplier = 1.75f;

    [Header("Referencias UI")]
    [Tooltip("Panel raíz de la pantalla de elección (se activa/desactiva).")]
    [SerializeField] private GameObject panel;
    [Tooltip("Texto de la opción izquierda (pastilla azul).")]
    [SerializeField] private TMP_Text leftLabel;
    [Tooltip("Texto de la opción derecha (pastilla roja).")]
    [SerializeField] private TMP_Text rightLabel;
    [Tooltip("Icono opcional de la opción izquierda.")]
    [SerializeField] private Image leftIcon;
    [Tooltip("Icono opcional de la opción derecha.")]
    [SerializeField] private Image rightIcon;
    [Tooltip("Texto opcional que muestra el coste de esta elección.")]
    [SerializeField] private TMP_Text costLabel;

    [Header("Referencias Jugador")]
    [Tooltip("Componente PlayerBuffs del protagonista. Se auto-detecta si se deja vacío.")]
    [SerializeField] private PlayerBuffs playerBuffs;

    private int currentCost;
    private bool isOpen;
    private BuffOption leftOption;
    private BuffOption rightOption;

    private void Awake()
    {
        currentCost = baseCost;
        if (panel != null) panel.SetActive(false);
        if (playerBuffs == null) playerBuffs = FindFirstObjectByType<PlayerBuffs>();
    }

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            // Comprobación inicial por si ya se arranca con puntos suficientes.
            HandleScoreChanged(ScoreManager.Instance.CurrentScore);
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }
    }

    private void Update()
    {
        // Selección por teclado como extra (funciona con timeScale = 0).
        if (!isOpen || Keyboard.current == null) return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            ChooseBlue();
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            ChooseRed();
        }
    }

    private void HandleScoreChanged(int score)
    {
        if (!isOpen && score >= currentCost)
        {
            OpenSelection();
        }
    }

    private void OpenSelection()
    {
        if (buffPool == null || buffPool.Count == 0)
        {
            Debug.LogWarning("BuffSelectionManager: el pool de buffs está vacío, no se puede mostrar la elección.");
            return;
        }

        // Sorteamos 2 opciones distintas (o repetimos si solo hay una en el pool).
        leftOption = buffPool[Random.Range(0, buffPool.Count)];
        rightOption = leftOption;
        if (buffPool.Count > 1)
        {
            int guard = 0;
            while (rightOption == leftOption && guard++ < 20)
            {
                rightOption = buffPool[Random.Range(0, buffPool.Count)];
            }
        }

        ApplyOptionToUI(leftOption, leftLabel, leftIcon);
        ApplyOptionToUI(rightOption, rightLabel, rightIcon);

        if (costLabel != null) costLabel.text = "Coste: " + currentCost;

        isOpen = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void ApplyOptionToUI(BuffOption option, TMP_Text label, Image icon)
    {
        if (label != null)
        {
            label.text = string.IsNullOrEmpty(option.description)
                ? option.displayName
                : option.displayName + "\n<size=70%>" + option.description + "</size>";
        }

        if (icon != null)
        {
            icon.sprite = option.icon;
            icon.enabled = option.icon != null;
        }
    }

    /// <summary>Elige la opción izquierda (pastilla azul). Cablear en el onClick del botón azul.</summary>
    public void ChooseBlue()
    {
        Choose(leftOption);
    }

    /// <summary>Elige la opción derecha (pastilla roja). Cablear en el onClick del botón rojo.</summary>
    public void ChooseRed()
    {
        Choose(rightOption);
    }

    private void Choose(BuffOption option)
    {
        if (!isOpen) return;

        if (option != null && playerBuffs != null)
        {
            playerBuffs.ApplyBuff(option.type, option.magnitude);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.TrySpend(currentCost);
        }

        // Subimos el coste para la siguiente elección.
        currentCost = Mathf.RoundToInt(currentCost * costMultiplier);

        CloseSelection();

        // Si tras gastar todavía queda score suficiente para el nuevo coste, encadenamos otra elección.
        if (ScoreManager.Instance != null && ScoreManager.Instance.CurrentScore >= currentCost)
        {
            OpenSelection();
        }
    }

    private void CloseSelection()
    {
        isOpen = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }
}
