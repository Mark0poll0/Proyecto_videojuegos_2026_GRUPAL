using UnityEngine;
using TMPro;

/// <summary>
/// Texto flotante de combate (números de daño, "+score", etc.). Sube y se
/// desvanece usando tiempo NO escalado (para completarse aunque un hit-stop o
/// una pausa congelen Time.timeScale). Se auto-devuelve al JuiceManager (pool).
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.7f;
    [SerializeField] private float riseSpeed = 1.6f;
    [SerializeField] private float horizontalJitter = 0.4f;

    private TextMeshPro tmp;
    private float timer;
    private Vector3 velocity;
    private Color baseColor;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    /// <summary>Configura y arranca la animación del texto flotante.</summary>
    public void Play(Vector3 worldPos, string text, Color color, float fontSize)
    {
        transform.position = worldPos;
        tmp.text = text;
        tmp.fontSize = fontSize;
        baseColor = color;
        tmp.color = color;

        // Pequeña desviación horizontal para que golpes seguidos no se solapen.
        float x = Random.Range(-horizontalJitter, horizontalJitter);
        velocity = new Vector3(x, riseSpeed, 0f);
        timer = 0f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        timer += dt;

        transform.position += velocity * dt;
        velocity.y = Mathf.Max(0f, velocity.y - 1.2f * dt); // se frena al subir

        float t = timer / lifetime;
        // Se mantiene opaco al inicio y se desvanece en la segunda mitad.
        float alpha = 1f - Mathf.Clamp01((t - 0.4f) / 0.6f);
        var c = baseColor; c.a = alpha;
        tmp.color = c;

        // Pequeño "pop" de escala al aparecer.
        float scale = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(timer / 0.12f));
        transform.localScale = Vector3.one * scale;

        if (timer >= lifetime)
        {
            gameObject.SetActive(false);
            if (JuiceManager.Instance != null) JuiceManager.Instance.ReturnToPool(this);
        }
    }
}
