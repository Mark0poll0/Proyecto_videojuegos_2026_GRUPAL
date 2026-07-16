using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Dificultad creciente por REFUERZOS. Cuando los enemigos vivos bajan de un
/// umbral, spawnea lotes alrededor del jugador (fuera de cámara, sobre pasto
/// válido). La intensidad 'd' (0..1) sube con el TIEMPO y el SCORE: más
/// frecuencia, lotes mayores y más proporción de Red.
/// </summary>
public class DifficultyDirector : MonoBehaviour
{
    [Header("Mapa (para posiciones válidas)")]
    [SerializeField] private Tilemap grassTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Enemigos [0]=Green [1]=Blue [2]=Red")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Escalado de dificultad")]
    [Tooltip("Segundos para alcanzar la dificultad máxima por tiempo.")]
    [SerializeField] private float timeToMax = 180f;
    [Tooltip("Score para alcanzar la dificultad máxima por puntuación.")]
    [SerializeField] private float scoreToMax = 1500f;

    [Header("Refuerzos")]
    [Tooltip("Si hay menos vivos que esto, entran refuerzos.")]
    [SerializeField] private int reinforceThreshold = 40;
    [SerializeField] private int maxAliveStart = 60;
    [SerializeField] private int maxAliveEnd = 120;
    [SerializeField] private float spawnIntervalStart = 3.5f;
    [SerializeField] private float spawnIntervalEnd = 0.9f;
    [SerializeField] private int batchStart = 1;
    [SerializeField] private int batchEnd = 4;

    [Header("Posición de spawn (anillo alrededor del jugador)")]
    [Tooltip("Radio mínimo (debe superar el borde de cámara ~9).")]
    [SerializeField] private float spawnRadiusMin = 10f;
    [SerializeField] private float spawnRadiusMax = 14f;
    [SerializeField] private int maxIntentosPorSpawn = 25;

    [Header("Pesos por color (base -> máximo con la dificultad)")]
    [SerializeField] private float greenBase = 0.5f, greenMax = 0.05f;
    [SerializeField] private float blueBase = 0.4f, blueMax = 0.35f;
    [SerializeField] private float redBase = 0.1f, redMax = 0.6f;

    private float elapsed;
    private float spawnTimer;
    private Transform player;
    private Transform container;

    /// <summary>Dificultad actual 0..1 (para depurar / HUD futuro).</summary>
    public float Difficulty { get; private set; }

    private void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.transform;
        var c = GameObject.Find("EnemiesContainer");
        if (c != null) container = c.transform;
    }

    private void Update()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform; else return;
        }

        elapsed += Time.deltaTime;

        float timeFactor = timeToMax > 0f ? elapsed / timeToMax : 0f;
        float scoreFactor = 0f;
        if (scoreToMax > 0f && ScoreManager.Instance != null)
            scoreFactor = ScoreManager.Instance.CurrentScore / scoreToMax;
        Difficulty = Mathf.Clamp01(Mathf.Max(timeFactor, scoreFactor));

        float interval = Mathf.Lerp(spawnIntervalStart, spawnIntervalEnd, Difficulty);
        spawnTimer += Time.deltaTime;
        if (spawnTimer < interval) return;
        spawnTimer = 0f;

        int alive = CountAlive();
        int maxAlive = Mathf.RoundToInt(Mathf.Lerp(maxAliveStart, maxAliveEnd, Difficulty));
        if (alive >= reinforceThreshold || alive >= maxAlive) return;

        int batch = Mathf.RoundToInt(Mathf.Lerp(batchStart, batchEnd, Difficulty));
        for (int i = 0; i < batch && alive + i < maxAlive; i++)
        {
            SpawnOne();
        }
    }

    private int CountAlive()
    {
        return Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Length;
    }

    private void SpawnOne()
    {
        GameObject prefab = ElegirPrefab();
        if (prefab == null || grassTilemap == null) return;

        for (int intento = 0; intento < maxIntentosPorSpawn; intento++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(spawnRadiusMin, spawnRadiusMax);
            Vector3 world = player.position + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * r;
            Vector3Int cell = grassTilemap.WorldToCell(world);

            bool grass = grassTilemap.HasTile(cell);
            bool wall = wallTilemap != null && wallTilemap.HasTile(cell);
            if (grass && !wall)
            {
                Vector3 pos = grassTilemap.GetCellCenterWorld(cell);
                Instantiate(prefab, pos, Quaternion.identity, container);
                return;
            }
        }
        // Si no encontró celda válida en los intentos, no spawnea este ciclo.
    }

    private GameObject ElegirPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        if (enemyPrefabs.Length < 3)
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        float gW = Mathf.Lerp(greenBase, greenMax, Difficulty);
        float bW = Mathf.Lerp(blueBase, blueMax, Difficulty);
        float rW = Mathf.Lerp(redBase, redMax, Difficulty);
        float total = gW + bW + rW;
        if (total <= 0f) return enemyPrefabs[0];

        float pick = Random.value * total;
        if (pick < gW) return enemyPrefabs[0];
        if (pick < gW + bW) return enemyPrefabs[1];
        return enemyPrefabs[2];
    }
}
