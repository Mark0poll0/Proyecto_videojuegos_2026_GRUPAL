using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeartUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Asigna aquí el script Player Health si esto es para el protagonista.")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Asigna aquí el script Enemy Health si esto es para un enemigo.")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private GameObject heartPrefab; // Un GameObject que contendrá un componente Image

    [Header("Sprites de Corazones")]
    [Tooltip("Deben estar ordenados exactamente de vacío a lleno:\n" +
             "0: Vacío (Gris)\n" +
             "1: 1/4 Corazón\n" +
             "2: Medio Corazón\n" +
             "3: 3/4 Corazón\n" +
             "4: Lleno (Rojo)")]
    [SerializeField] private Sprite[] heartSprites;

    private List<Image> spawnedHearts = new List<Image>();

    private void Awake()
    {
        // Auto-detectar scripts si no se asignaron
        if (playerHealth == null && enemyHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        // ¡A PRUEBA DE BALAS! Si el usuario olvidó poner el Horizontal Layout Group al recrear el objeto, se lo ponemos por código
        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 4;
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHearts;
        }
        else if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += UpdateHearts;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHearts;
        }
        
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= UpdateHearts;
        }
    }

    private void UpdateHearts(int currentHealth, int maxHealth)
    {
        // Limpiar cualquier referencia nula de corazones destruidos previamente
        spawnedHearts.RemoveAll(heart => heart == null);

        int totalHearts = Mathf.CeilToInt(maxHealth / 4f);

        while (spawnedHearts.Count < totalHearts)
        {
            // Instanciar como elemento UI falso para que no intente mantener posiciones de mundo gigantes
            GameObject newHeart = Instantiate(heartPrefab, transform, false);
            
            if (newHeart == null) continue;

            RectTransform rt = newHeart.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition3D = Vector3.zero;
            }
            
            Image heartImage = newHeart.GetComponent<Image>();
            
            // Forzamos a que el sprite mantenga su proporción original y no se estire
            if (heartImage != null)
            {
                heartImage.preserveAspect = true;
            }
            
            spawnedHearts.Add(heartImage);
        }

        while (spawnedHearts.Count > totalHearts)
        {
            int lastIndex = spawnedHearts.Count - 1;
            if (spawnedHearts[lastIndex] != null && spawnedHearts[lastIndex].gameObject != null)
            {
                Destroy(spawnedHearts[lastIndex].gameObject);
            }
            spawnedHearts.RemoveAt(lastIndex);
        }

        // 2. Pintar cada corazón según la salud actual
        for (int i = 0; i < spawnedHearts.Count; i++)
        {
            if (spawnedHearts[i] == null) continue; // Salvaguarda por si el objeto fue destruido

            // Puntos de vida que corresponden a este corazón en particular (ej: corazón 0 maneja vida 1-4)
            int minHealthValue = i * 4;
            int healthDifference = currentHealth - minHealthValue;

            if (healthDifference >= 4)
            {
                // Este corazón está completamente lleno
                spawnedHearts[i].sprite = heartSprites[4];
            }
            else if (healthDifference <= 0)
            {
                // Este corazón está vacío
                spawnedHearts[i].sprite = heartSprites[0];
            }
            else
            {
                // Este corazón está parcialmente lleno (1, 2 o 3 de salud)
                spawnedHearts[i].sprite = heartSprites[healthDifference];
            }
        }
    }
}
