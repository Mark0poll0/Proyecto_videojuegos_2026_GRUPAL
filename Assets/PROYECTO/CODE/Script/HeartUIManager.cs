using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeartUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
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

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHearts;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHearts;
        }
    }

    private void UpdateHearts(int currentHealth, int maxHealth)
    {
        int totalHearts = maxHealth / 4;

        // 1. Ajustar cantidad de GameObjects de corazones en la UI
        while (spawnedHearts.Count < totalHearts)
        {
            GameObject newHeart = Instantiate(heartPrefab, transform);
            Image heartImage = newHeart.GetComponent<Image>();
            spawnedHearts.Add(heartImage);
        }

        while (spawnedHearts.Count > totalHearts)
        {
            Destroy(spawnedHearts[spawnedHearts.Count - 1].gameObject);
            spawnedHearts.RemoveAt(spawnedHearts.Count - 1);
        }

        // 2. Pintar cada corazón según la salud actual
        for (int i = 0; i < spawnedHearts.Count; i++)
        {
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
