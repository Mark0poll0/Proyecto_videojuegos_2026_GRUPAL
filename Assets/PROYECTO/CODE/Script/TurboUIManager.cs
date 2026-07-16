using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Barra de HUD del Turbo. Mientras está activo, la barra se vacía indicando el
/// tiempo restante; en cooldown se rellena (gris); al estar lista, llena y viva.
/// Se oculta hasta que el Turbo se desbloquea. Polla el Player_Controller.
/// </summary>
public class TurboUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Player_Controller del protagonista. Se auto-busca por tag 'Player' si se deja vacío.")]
    [SerializeField] private Player_Controller player;
    [Tooltip("Contenedor de la barra (se oculta si el Turbo no está desbloqueado).")]
    [SerializeField] private GameObject root;
    [Tooltip("Imagen de relleno (Image Type = Filled).")]
    [SerializeField] private Image fillImage;
    [Tooltip("Etiqueta de estado.")]
    [SerializeField] private TMP_Text label;

    [Header("Colores")]
    [SerializeField] private Color activeColor = new Color(0.3f, 0.9f, 1f);
    [SerializeField] private Color readyColor = new Color(0.4f, 1f, 0.5f);
    [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f);

    private void Start()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.GetComponent<Player_Controller>();
        }
    }

    private void Update()
    {
        bool unlocked = player != null && player.TurboUnlocked;
        if (root != null && root.activeSelf != unlocked) root.SetActive(unlocked);
        if (!unlocked || fillImage == null) return;

        if (player.TurboActive)
        {
            fillImage.fillAmount = player.TurboRemaining01;
            fillImage.color = activeColor;
            if (label != null) label.text = "TURBO";
        }
        else if (!player.TurboReady)
        {
            fillImage.fillAmount = player.CooldownProgress01;
            fillImage.color = cooldownColor;
            if (label != null) label.text = "Recargando";
        }
        else
        {
            fillImage.fillAmount = 1f;
            fillImage.color = readyColor;
            if (label != null) label.text = "TURBO [Shift]";
        }
    }
}
