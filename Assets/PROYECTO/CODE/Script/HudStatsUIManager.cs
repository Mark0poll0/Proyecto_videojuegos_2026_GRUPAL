using UnityEngine;
using TMPro;

/// <summary>
/// HUD de estadísticas de la partida: enemigos eliminados (Kills, dirigido por
/// evento) y cronómetro (mm:ss, acumula tiempo escalado → se congela en pausa).
/// </summary>
public class HudStatsUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text timerText;

    [Header("Formato")]
    [SerializeField] private string killsPrefix = "Kills: ";

    private float elapsed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnKillsChanged -= UpdateKills;
    }

    private void Subscribe()
    {
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.OnKillsChanged -= UpdateKills;
        ScoreManager.Instance.OnKillsChanged += UpdateKills;
        UpdateKills(ScoreManager.Instance.Kills);
    }

    private void Update()
    {
        // Tiempo escalado: se congela durante la pausa de buffs / game over (es lo justo).
        elapsed += Time.deltaTime;
        if (timerText != null)
        {
            int total = Mathf.FloorToInt(elapsed);
            timerText.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }

    private void UpdateKills(int kills)
    {
        if (killsText != null) killsText.text = killsPrefix + kills;
    }
}
