using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Centraliza los efectos de "juice" de combate: textos flotantes (números de
/// daño, "+score"), hit-stop (micro-congelación) y screen shake.
/// Singleton simple sin DontDestroyOnLoad (se recrea al recargar escena).
/// </summary>
public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    [Header("Texto flotante")]
    [Tooltip("Tamaño de fuente por defecto de los textos flotantes (unidades de mundo).")]
    [SerializeField] private float defaultFontSize = 8f;
    [Tooltip("Orden de dibujo del texto flotante (alto para que se vea sobre los enemigos).")]
    [SerializeField] private int sortingOrder = 1000;

    private readonly Queue<FloatingText> pool = new Queue<FloatingText>();
    private TMP_FontAsset font;
    private Cainos.PixelArtTopDown_Basic.CameraFollow cam;
    private bool isHitStopping;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        font = TMP_Settings.defaultFontAsset;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- Texto flotante ----------------

    public void ShowFloatingText(Vector3 worldPos, string text, Color color)
    {
        ShowFloatingText(worldPos, text, color, defaultFontSize);
    }

    public void ShowFloatingText(Vector3 worldPos, string text, Color color, float fontSize)
    {
        FloatingText ft = GetFromPool();
        ft.Play(worldPos + Vector3.up * 0.6f, text, color, fontSize);
    }

    private FloatingText GetFromPool()
    {
        if (pool.Count > 0) return pool.Dequeue();

        var go = new GameObject("FloatingText");
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Colocar en la sorting layer FRONTAL (la última de la lista) para dibujar
            // sobre enemigos/escenario; el orden solo cuenta dentro de la misma layer.
            var layers = SortingLayer.layers;
            if (layers.Length > 0) mr.sortingLayerID = layers[layers.Length - 1].id;
            mr.sortingOrder = sortingOrder;
        }
        // RectTransform pequeño para que el texto no se recorte.
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(4f, 2f);
        return go.AddComponent<FloatingText>();
    }

    public void ReturnToPool(FloatingText ft)
    {
        if (ft != null) pool.Enqueue(ft);
    }

    // ---------------- Hit-stop ----------------

    /// <summary>Micro-congelación del tiempo. Solo actúa durante juego normal (timeScale==1),
    /// así nunca "descongela" el panel de buffs ni el Game Over.</summary>
    public void HitStop(float seconds)
    {
        if (isHitStopping) return;
        if (!Mathf.Approximately(Time.timeScale, 1f)) return; // hay una pausa "dura" activa
        StartCoroutine(HitStopRoutine(seconds));
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        isHitStopping = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        // Solo restauramos si nadie más tomó el control del tiempo entretanto.
        if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = 1f;
        isHitStopping = false;
    }

    // ---------------- Screen shake ----------------

    public void Shake(float duration, float magnitude)
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.GetComponent<Cainos.PixelArtTopDown_Basic.CameraFollow>();
        if (cam != null) cam.ShakeCamera(duration, magnitude);
    }
}
