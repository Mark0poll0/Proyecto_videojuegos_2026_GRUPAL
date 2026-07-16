using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProceduralMapGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct DisenoMuroDosAlturas
    {
        [Tooltip("Parte superior (Techo).")]
        public TileBase parteSuperior;
        [Tooltip("Parte inferior (Cara).")]
        public TileBase parteInferior;
    }

    [System.Serializable]
    public struct EnemigoPonderado
    {
        [Tooltip("Prefab del enemigo (Green/Blue/Red...).")]
        public GameObject prefab;
        [Tooltip("Peso relativo de aparición (rareza). Más alto = más común.")]
        [Range(0f, 1f)] public float peso;
    }

    [Header("Tilemaps Destino")]
    [SerializeField] private Tilemap grassTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Esquina Inferior Izquierda (Lado Izquierdo de Muros - Muro Sur)")]
    [SerializeField] private TileBase esqInfIzq_Diseno1_Techo;
    [SerializeField] private TileBase esqInfIzq_Diseno1_Cara;
    [SerializeField] private TileBase esqInfIzq_Diseno2_Techo;
    [SerializeField] private TileBase esqInfIzq_Diseno2_Cara;

    [Header("Esquina Inferior Derecha (Lado Derecho de Muros - Muro Sur)")]
    [SerializeField] private TileBase esqInfDer_Diseno1_Techo;
    [SerializeField] private TileBase esqInfDer_Diseno1_Cara;
    [SerializeField] private TileBase esqInfDer_Diseno2_Techo;
    [SerializeField] private TileBase esqInfDer_Diseno2_Cara;

    [Header("Relleno Muros (Contenedor entre Esquinas - Muro Sur)")]
    [Tooltip("Los 17 diseños de relleno de doble altura (Techo + Cara agrupados).")]
    [SerializeField] private DisenoMuroDosAlturas[] rellenoMuros;

    [Header("Muros Superiores (Muro Norte de 1 Casilla de Alto)")]
    [Tooltip("Esquina superior izquierda (1 casilla).")]
    [SerializeField] private TileBase esqSupIzq;
    [Tooltip("Esquina superior derecha (1 casilla).")]
    [SerializeField] private TileBase esqSupDer;
    [Tooltip("Variaciones de relleno para el muro superior.")]
    [SerializeField] private TileBase[] rellenoSupDisenos;

    [Header("Muros Laterales (Muros Izquierdo y Derecho de 1 Casilla)")]
    [Tooltip("Variaciones de muro lateral izquierdo.")]
    [SerializeField] private TileBase[] muroIzqDisenos;
    [Tooltip("Variaciones de muro lateral derecho.")]
    [SerializeField] private TileBase[] muroDerDisenos;

    [Header("Dimensiones del Lienzo")]
    [Tooltip("Ancho recomendado para 5 zonas: 90.")]
    [SerializeField] private int anchoLienzo = 90;
    [Tooltip("Alto recomendado para 5 zonas: 90.")]
    [SerializeField] private int altoLienzo = 90;

    [Header("Ajustes del Algoritmo de Habitaciones (Por Zona)")]
    [Tooltip("Cantidad mínima de habitaciones superpuestas por zona.")]
    [SerializeField] private int minHabitaciones = 4;
    [Tooltip("Cantidad máxima de habitaciones superpuestas por zona.")]
    [SerializeField] private int maxHabitaciones = 5;

    [Header("Tamaño de Habitaciones")]
    [SerializeField] private int tamMinHabitacion = 7;
    [SerializeField] private int tamMaxHabitacion = 12;

    [Header("Ajustes de los Pasillos Conectores")]
    [Tooltip("Grosor en azulejos de los pasillos rectangulares que conectan las zonas.")]
    [Range(3, 10)] [SerializeField] private int grosorPasillo = 5;

    [Header("Decoración por Tiles")]
    [Range(0f, 0.1f)] [SerializeField] private float probabilidadFlores = 0.07f;

    [Header("Densidad de Prefabs (0 a 1)")]
    [Range(0f, 0.2f)] [SerializeField] private float densidadPlantas = 0.05f;
    [Range(0f, 0.2f)] [SerializeField] private float densidadProps = 0.02f;
    [Range(0f, 0.05f)] [SerializeField] private float densidadEnemigos = 0.008f;

    [Header("Colecciones de Prefabs (Opcional)")]
    [Tooltip("Si se deja vacío, se cargarán todos los prefabs de Assets/PROYECTO/PREFABS/Plant")]
    [SerializeField] private GameObject[] prefabsPlantasManual;
    [Tooltip("Si se deja vacío, se cargarán todos los prefabs de Assets/PROYECTO/PREFABS/Props (excepto escaleras)")]
    [SerializeField] private GameObject[] prefabsPropsManual;
    [Tooltip("Si se deja vacío, se cargarán todos los prefabs de Assets/PROYECTO/PREFABS/Enemy")]
    [SerializeField] private GameObject prefabEnemigoManual;
    [Tooltip("Enemigos con peso de aparición (rareza). Si tiene entradas, tiene prioridad sobre prefabEnemigoManual y la carpeta Enemy. Ej: Green 0.40, Blue 0.35, Red 0.25.")]
    [SerializeField] private EnemigoPonderado[] enemigosPonderados;

    /// <summary>
    /// Genera 5 zonas distribuidas en forma de cruz/quincuncio (Centro y 4 esquinas),
    /// conectadas de forma laberíntica por pasillos más cortos con codos y bucles aleatorios.
    /// </summary>
    [ContextMenu("Generar Mapa")]
    public void GenerateMap()
    {
        if (grassTilemap == null || wallTilemap == null)
        {
            Debug.LogError("Por favor, asigna los Tilemaps de Pasto y Pared en el Inspector.");
            return;
        }

        if (esqInfIzq_Diseno1_Techo == null || esqInfIzq_Diseno1_Cara == null ||
            esqInfIzq_Diseno2_Techo == null || esqInfIzq_Diseno2_Cara == null)
        {
            Debug.LogError("Por favor, asigna los 4 tiles de la Esquina Inferior Izquierda en el Inspector.");
            return;
        }

        if (esqInfDer_Diseno1_Techo == null || esqInfDer_Diseno1_Cara == null ||
            esqInfDer_Diseno2_Techo == null || esqInfDer_Diseno2_Cara == null)
        {
            Debug.LogError("Por favor, asigna los 4 tiles de la Esquina Inferior Derecha en el Inspector.");
            return;
        }

        if (rellenoMuros.Length == 0)
        {
            Debug.LogError("Por favor, configura al menos un diseño de Relleno Muros (Techo + Cara) en el Inspector.");
            return;
        }

#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(grassTilemap, "Generar Mapa");
        Undo.RegisterCompleteObjectUndo(wallTilemap, "Generar Mapa");
#endif

        // 1. Limpiar mapas y decoraciones previas
        grassTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        ClearDecorations();

        // 2. Cargar recursos de suelo
        TileBase[] grassTiles = LoadTilesFromPrefix("TX Tileset Grass ");
        TileBase[] grassFlowerTiles = LoadTilesFromPrefix("TX Tileset Grass Flower ");

        if (grassTiles.Length == 0)
        {
            Debug.LogError("No se encontraron tiles de pasto (TX Tileset Grass) en Assets/PROYECTO/ART/Tilemaps/");
            return;
        }

        // 3. Cargar Prefabs de Decoración
        GameObject[] prefabsPlantas = prefabsPlantasManual;
        if (prefabsPlantas == null || prefabsPlantas.Length == 0)
        {
            prefabsPlantas = LoadPrefabsFromPath("Plant");
        }

        GameObject[] prefabsProps = prefabsPropsManual;
        if (prefabsProps == null || prefabsProps.Length == 0)
        {
            // Excluimos las escaleras ("Stairs"), estructuras ("Struct") y puertas ("Gate")
            prefabsProps = LoadPrefabsFromPath("Props", new string[] { "Stairs", "Struct", "Gate" });
        }

        bool usarPonderado = enemigosPonderados != null && enemigosPonderados.Length > 0;
        GameObject[] prefabsEnemigos;
        if (usarPonderado)
        {
            prefabsEnemigos = new GameObject[0]; // no se usa en modo ponderado
        }
        else if (prefabEnemigoManual != null)
        {
            prefabsEnemigos = new GameObject[] { prefabEnemigoManual };
        }
        else
        {
            // Excluimos la barra de vida ("canvas") para no spawnearla como si fuera un enemigo.
            prefabsEnemigos = LoadPrefabsFromPath("Enemy", new string[] { "canvas", "Canvas" });
        }

        // 4. Crear contenedor de decoración en la escena
        GameObject containerObj = new GameObject("DecorationsContainer");
        containerObj.transform.parent = transform;
        containerObj.transform.localPosition = Vector3.zero;
        Transform decorContainer = containerObj.transform;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(containerObj, "Generar Mapa - Decoraciones");
#endif

        // Crear contenedor de enemigos en la escena
        GameObject enemiesContainerObj = new GameObject("EnemiesContainer");
        enemiesContainerObj.transform.parent = transform;
        enemiesContainerObj.transform.localPosition = Vector3.zero;
        Transform enemiesContainer = enemiesContainerObj.transform;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(enemiesContainerObj, "Generar Mapa - Enemigos");
#endif

        // 5. Matriz lógica de pasto
        bool[,] isGrass = new bool[anchoLienzo, altoLienzo];

        Vector2Int zCenter = new Vector2Int(anchoLienzo / 2, altoLienzo / 2);
        int offset = (int)(anchoLienzo * 0.27f); 

        Vector2Int zTopLeft = new Vector2Int(zCenter.x - offset, zCenter.y + offset);
        Vector2Int zTopRight = new Vector2Int(zCenter.x + offset, zCenter.y + offset);
        Vector2Int zBottomLeft = new Vector2Int(zCenter.x - offset, zCenter.y - offset);
        Vector2Int zBottomRight = new Vector2Int(zCenter.x + offset, zCenter.y - offset);

        GenerateZone(zCenter.x, zCenter.y, isGrass);
        GenerateZone(zTopLeft.x, zTopLeft.y, isGrass);
        GenerateZone(zTopRight.x, zTopRight.y, isGrass);
        GenerateZone(zBottomLeft.x, zBottomLeft.y, isGrass);
        GenerateZone(zBottomRight.x, zBottomRight.y, isGrass);

        ConnectZones(zCenter, zTopLeft, isGrass);
        ConnectZones(zCenter, zTopRight, isGrass);
        ConnectZones(zCenter, zBottomLeft, isGrass);
        ConnectZones(zCenter, zBottomRight, isGrass);

        if (Random.value < 0.60f) ConnectZones(zTopLeft, zTopRight, isGrass);
        if (Random.value < 0.60f) ConnectZones(zBottomLeft, zBottomRight, isGrass);
        if (Random.value < 0.60f) ConnectZones(zTopLeft, zBottomLeft, isGrass);
        if (Random.value < 0.60f) ConnectZones(zTopRight, zBottomRight, isGrass);

        // Limpieza de caminos o salientes muertos de 1 casilla de ancho
        PruneDeadEnds(isGrass);

        // 6. Pintar el suelo y los muros
        for (int x = 0; x < anchoLienzo; x++)
        {
            for (int y = 0; y < altoLienzo; y++)
            {
                int worldX = x - anchoLienzo / 2;
                int worldY = y - altoLienzo / 2;

                if (isGrass[x, y])
                {
                    // === ESTAMOS DENTRO DEL ÁREA DE PASTO ===

                    // 1. Pintar el pasto base
                    TileBase grassTile = grassTiles[Random.Range(0, Mathf.Min(grassTiles.Length, 4))];
                    grassTilemap.SetTile(new Vector3Int(worldX, worldY, 0), grassTile);

                    // 2. Pintar flores decorativas por tiles
                    if (grassFlowerTiles.Length > 0 && Random.value < probabilidadFlores)
                    {
                        TileBase flowerTile = grassFlowerTiles[Random.Range(0, grassFlowerTiles.Length)];
                        grassTilemap.SetTile(new Vector3Int(worldX, worldY, 0), flowerTile);
                    }

                    // 3. Evaluar bordes para Muros Superiores y Laterales (que van SOBRE el pasto)
                    bool noGrassUp = (y == altoLienzo - 1) || !isGrass[x, y + 1];
                    bool noGrassLeft = (x == 0) || !isGrass[x - 1, y];
                    bool noGrassRight = (x == anchoLienzo - 1) || !isGrass[x + 1, y];

                    bool tieneMuro = false;

                    if (noGrassUp && noGrassLeft)
                    {
                        tieneMuro = true;
                        if (esqSupIzq != null) wallTilemap.SetTile(new Vector3Int(worldX, worldY, 0), esqSupIzq);
                    }
                    else if (noGrassUp && noGrassRight)
                    {
                        tieneMuro = true;
                        if (esqSupDer != null) wallTilemap.SetTile(new Vector3Int(worldX, worldY, 0), esqSupDer);
                    }
                    else if (noGrassUp)
                    {
                        tieneMuro = true;
                        TileBase t = GetSafeTile(rellenoSupDisenos, Random.Range(0, rellenoSupDisenos.Length));
                        if (t != null) wallTilemap.SetTile(new Vector3Int(worldX, worldY, 0), t);
                    }
                    else if (noGrassLeft)
                    {
                        tieneMuro = true;
                        TileBase t = GetSafeTile(muroIzqDisenos, Random.Range(0, muroIzqDisenos.Length));
                        if (t != null) wallTilemap.SetTile(new Vector3Int(worldX, worldY, 0), t);
                    }
                    else if (noGrassRight)
                    {
                        tieneMuro = true;
                        TileBase t = GetSafeTile(muroDerDisenos, Random.Range(0, muroDerDisenos.Length));
                        if (t != null) wallTilemap.SetTile(new Vector3Int(worldX, worldY, 0), t);
                    }

                    // 4. Instanciación aleatoria de Prefabs Decorativos y Enemigos (solo si no se colocó un muro aquí)
                    if (!tieneMuro)
                    {
                        // Evitamos spawnear enemigos muy cerca del centro donde empieza el jugador
                        float distAlCentro = Vector2.Distance(new Vector2(x, y), new Vector2(anchoLienzo / 2, altoLienzo / 2));

                        bool hayEnemigos = usarPonderado || (prefabsEnemigos != null && prefabsEnemigos.Length > 0);
                        if (distAlCentro > 6f && hayEnemigos && Random.value < densidadEnemigos)
                        {
                            GameObject enemigoElegido = usarPonderado
                                ? ElegirEnemigoPonderado()
                                : prefabsEnemigos[Random.Range(0, prefabsEnemigos.Length)];
                            if (enemigoElegido != null)
                            {
                                Vector3 pos = grassTilemap.GetCellCenterWorld(new Vector3Int(worldX, worldY, 0));
                                SpawnPrefab(enemigoElegido, pos, enemiesContainer);
                            }
                        }
                        else if (prefabsPlantas.Length > 0 && Random.value < densidadPlantas)
                        {
                            GameObject plantaElegida = prefabsPlantas[Random.Range(0, prefabsPlantas.Length)];
                            Vector3 pos = grassTilemap.GetCellCenterWorld(new Vector3Int(worldX, worldY, 0));
                            SpawnPrefab(plantaElegida, pos, decorContainer);
                        }
                        else if (prefabsProps.Length > 0 && Random.value < densidadProps)
                        {
                            GameObject propElegido = prefabsProps[Random.Range(0, prefabsProps.Length)];
                            Vector3 pos = grassTilemap.GetCellCenterWorld(new Vector3Int(worldX, worldY, 0));
                            SpawnPrefab(propElegido, pos, decorContainer);
                        }
                    }
                }
                else
                {
                    // === ESTAMOS FUERA DEL ÁREA DE PASTO ===

                    // 5. Evaluar si es el borde justo debajo del pasto (Para el Muro Sur)
                    if (y < altoLienzo - 1 && isGrass[x, y + 1])
                    {
                        // Para saber si es esquina, revisamos si el PASTO de arriba continúa hacia los lados
                        bool hasGrassLeft = (x > 0 && isGrass[x - 1, y + 1]);
                        bool hasGrassRight = (x < anchoLienzo - 1 && isGrass[x + 1, y + 1]);

                        TileBase tileTecho = null;
                        TileBase tileCara = null;

                        if (!hasGrassLeft)
                        {
                            // Esquina Inferior Izquierda
                            if (Random.value < 0.5f)
                            {
                                tileTecho = esqInfIzq_Diseno1_Techo;
                                tileCara = esqInfIzq_Diseno1_Cara;
                            }
                            else
                            {
                                tileTecho = esqInfIzq_Diseno2_Techo;
                                tileCara = esqInfIzq_Diseno2_Cara;
                            }
                        }
                        else if (!hasGrassRight)
                        {
                            // Esquina Inferior Derecha
                            if (Random.value < 0.5f)
                            {
                                tileTecho = esqInfDer_Diseno1_Techo;
                                tileCara = esqInfDer_Diseno1_Cara;
                            }
                            else
                            {
                                tileTecho = esqInfDer_Diseno2_Techo;
                                tileCara = esqInfDer_Diseno2_Cara;
                            }
                        }
                        else
                        {
                            // Relleno Central
                            DisenoMuroDosAlturas muroElegido = rellenoMuros[Random.Range(0, rellenoMuros.Length)];
                            tileTecho = muroElegido.parteSuperior;
                            tileCara = muroElegido.parteInferior;
                        }

                        // Pintar Muro Sur (Ocupa esta casilla vacía y la de más abajo)
                        if (tileTecho != null)
                            wallTilemap.SetTile(new Vector3Int(worldX, worldY, 0), tileTecho);
                        
                        if (tileCara != null && y > 0)
                            wallTilemap.SetTile(new Vector3Int(worldX, worldY - 1, 0), tileCara);
                    }
                }
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(grassTilemap.gameObject);
        EditorUtility.SetDirty(wallTilemap.gameObject);
#endif

        Debug.Log("¡Mapa laberíntico de 5 zonas interconectadas y decorado generado con éxito! Deshaz con Ctrl+Z si es necesario.");
    }

    [ContextMenu("Limpiar Mapa")]
    public void ClearMap()
    {
        if (grassTilemap == null || wallTilemap == null) return;

#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(grassTilemap, "Limpiar Mapa");
        Undo.RegisterCompleteObjectUndo(wallTilemap, "Limpiar Mapa");
#endif

        grassTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        ClearDecorations();

#if UNITY_EDITOR
        EditorUtility.SetDirty(grassTilemap.gameObject);
        EditorUtility.SetDirty(wallTilemap.gameObject);
#endif

        Debug.Log("¡Mapa limpiado!");
    }

    private void ClearDecorations()
    {
        Transform container = transform.Find("DecorationsContainer");
        if (container != null)
        {
            DestroyImmediate(container.gameObject);
        }
        Transform enemiesContainer = transform.Find("EnemiesContainer");
        if (enemiesContainer != null)
        {
            DestroyImmediate(enemiesContainer.gameObject);
        }
    }

    private void SpawnPrefab(GameObject prefab, Vector3 position, Transform parent)
    {
        if (prefab == null) return;
#if UNITY_EDITOR
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.transform.position = position;
        Undo.RegisterCreatedObjectUndo(obj, "Generar Mapa - Decoración");
#else
        Instantiate(prefab, position, Quaternion.identity, parent);
#endif
    }

    private void PruneDeadEnds(bool[,] isGrass)
    {
        bool huboCambios = true;
        int maxIteraciones = 8;
        int iteracion = 0;

        while (huboCambios && iteracion < maxIteraciones)
        {
            huboCambios = false;
            iteracion++;

            bool[,] copiaGrass = (bool[,])isGrass.Clone();

            for (int x = 2; x < anchoLienzo - 2; x++)
            {
                for (int y = 2; y < altoLienzo - 2; y++)
                {
                    if (copiaGrass[x, y])
                    {
                        int vecinosGrass = 0;
                        if (copiaGrass[x + 1, y]) vecinosGrass++;
                        if (copiaGrass[x - 1, y]) vecinosGrass++;
                        if (copiaGrass[x, y + 1]) vecinosGrass++;
                        if (copiaGrass[x, y - 1]) vecinosGrass++;

                        if (vecinosGrass <= 1)
                        {
                            isGrass[x, y] = false;
                            huboCambios = true;
                        }
                    }
                }
            }
        }
    }

    private TileBase GetSafeTile(TileBase[] array, int index)
    {
        if (array == null || array.Length == 0) return null;
        return array[Mathf.Clamp(index, 0, array.Length - 1)];
    }

    // Elige un enemigo al azar según sus pesos (ruleta ponderada por rareza).
    private GameObject ElegirEnemigoPonderado()
    {
        float total = 0f;
        foreach (var e in enemigosPonderados)
            if (e.prefab != null) total += Mathf.Max(0f, e.peso);

        if (total <= 0f) return null;

        float r = Random.value * total;
        foreach (var e in enemigosPonderados)
        {
            if (e.prefab == null) continue;
            r -= Mathf.Max(0f, e.peso);
            if (r <= 0f) return e.prefab;
        }
        return null;
    }

    private void GenerateZone(int centerX, int centerY, bool[,] isGrass)
    {
        int minSize = Mathf.Min(tamMinHabitacion, tamMaxHabitacion);
        int maxSize = Mathf.Max(tamMinHabitacion, tamMaxHabitacion);
        int startW = Random.Range(minSize, maxSize + 1);
        int startH = Random.Range(minSize, maxSize + 1);
        AddRoom(centerX - startW / 2, centerY - startH / 2, startW, startH, isGrass);

        int targetRooms = Random.Range(minHabitaciones, maxHabitaciones + 1);
        int zoneRadius = 8; 

        for (int i = 0; i < targetRooms; i++)
        {
            Vector2Int connectionPoint = GetRandomGrassPointInZone(centerX - zoneRadius, centerX + zoneRadius, isGrass);

            int roomW = Random.Range(minSize, maxSize + 1);
            int roomH = Random.Range(minSize, maxSize + 1);

            int startRoomX = connectionPoint.x - Random.Range(1, roomW - 1);
            int startRoomY = connectionPoint.y - Random.Range(1, roomH - 1);

            AddRoom(startRoomX, startRoomY, roomW, roomH, isGrass);
        }
    }

    private void ConnectZones(Vector2Int p1, Vector2Int p2, bool[,] isGrass)
    {
        if (Random.value < 0.5f)
        {
            DrawHorizontalCorridor(p1.x, p2.x, p1.y, isGrass);
            DrawVerticalCorridor(p2.x, p1.y, p2.y, isGrass);
        }
        else
        {
            DrawVerticalCorridor(p1.x, p1.y, p2.y, isGrass);
            DrawHorizontalCorridor(p1.x, p2.x, p2.y, isGrass);
        }
    }

    private void AddRoom(int startX, int startY, int rWidth, int rHeight, bool[,] isGrass)
    {
        for (int x = startX; x < startX + rWidth; x++)
        {
            for (int y = startY; y < startY + rHeight; y++)
            {
                if (x >= 2 && x < anchoLienzo - 2 && y >= 2 && y < altoLienzo - 2)
                {
                    isGrass[x, y] = true;
                }
            }
        }
    }

    private void DrawHorizontalCorridor(int startX, int endX, int y, bool[,] isGrass)
    {
        int halfThickness = grosorPasillo / 2;
        int minX = Mathf.Min(startX, endX);
        int maxX = Mathf.Max(startX, endX);

        for (int x = minX; x <= maxX; x++)
        {
            for (int yOffset = y - halfThickness; yOffset < y - halfThickness + grosorPasillo; yOffset++)
            {
                if (x >= 2 && x < anchoLienzo - 2 && yOffset >= 2 && yOffset < altoLienzo - 2)
                {
                    isGrass[x, yOffset] = true;
                }
            }
        }
    }

    private void DrawVerticalCorridor(int x, int startY, int endY, bool[,] isGrass)
    {
        int halfThickness = grosorPasillo / 2;
        int minY = Mathf.Min(startY, endY);
        int maxY = Mathf.Max(startY, endY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int xOffset = x - halfThickness; xOffset < x - halfThickness + grosorPasillo; xOffset++)
            {
                if (xOffset >= 2 && xOffset < anchoLienzo - 2 && y >= 2 && y < altoLienzo - 2)
                {
                    isGrass[xOffset, y] = true;
                }
            }
        }
    }

    private Vector2Int GetRandomGrassPointInZone(int minX, int maxX, bool[,] isGrass)
    {
        System.Collections.Generic.List<Vector2Int> points = new System.Collections.Generic.List<Vector2Int>();
        int startX = Mathf.Max(2, minX);
        int endX = Mathf.Min(anchoLienzo - 2, maxX);

        for (int x = startX; x < endX; x++)
        {
            for (int y = 2; y < altoLienzo - 2; y++)
            {
                if (isGrass[x, y])
                {
                    points.Add(new Vector2Int(x, y));
                }
            }
        }

        if (points.Count > 0)
        {
            return points[Random.Range(0, points.Count)];
        }
        return new Vector2Int((minX + maxX) / 2, altoLienzo / 2);
    }

    private TileBase[] LoadTilesFromPrefix(string prefix)
    {
        System.Collections.Generic.List<TileBase> tiles = new System.Collections.Generic.List<TileBase>();

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/PROYECTO/ART/Tilemaps" });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(assetPath);
            if (name.StartsWith(prefix))
            {
                TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }
        }
#endif

        return tiles.ToArray();
    }

    private GameObject[] LoadPrefabsFromPath(string relativeFolder, string[] excludeKeywords = null)
    {
        System.Collections.Generic.List<GameObject> prefabs = new System.Collections.Generic.List<GameObject>();

#if UNITY_EDITOR
        string folderPath = "Assets/PROYECTO/PREFABS/" + relativeFolder;
        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string assetPath = file.Replace('\\', '/');
                
                if (excludeKeywords != null)
                {
                    bool skip = false;
                    foreach (string keyword in excludeKeywords)
                    {
                        if (assetPath.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }
        }
#endif
        return prefabs.ToArray();
    }
}
