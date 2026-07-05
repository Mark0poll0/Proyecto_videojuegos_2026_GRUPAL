using System.Collections;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    // Cámara seguidora mejorada con amortiguación física y temblor de pantalla (Camera Shake)
    public class CameraFollow : MonoBehaviour
    {
        [Header("Objetivo")]
        public Transform target;

        [Header("Ajustes de Movimiento")]
        [Tooltip("Velocidad de amortiguación (menor tiempo = seguimiento más elástico y reactivo).")]
        [SerializeField] private float smoothTime = 0.2f;

        [Tooltip("Desplazamiento vertical para ajustar la altura de la cámara respecto al jugador.")]
        [SerializeField] private float offsetY = 0f;

        [Tooltip("Distancia de la cámara en el eje Z (para 2D normalmente es -10).")]
        [SerializeField] private float offsetZ = -10f;

        // Variables internas para el movimiento suave (SmoothDamp)
        private Vector3 currentVelocity = Vector3.zero;

        // Variables internas para el efecto de temblor (Shake)
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0f;

        private void LateUpdate()
        {
            if (target == null) return;

            // 1. Calculamos la posición destino ideal
            Vector3 targetPos = new Vector3(target.position.x, target.position.y + offsetY, target.position.z + offsetZ);

            // 2. Movimiento suave amortiguado (SmoothDamp crea una inercia elástica muy natural)
            Vector3 newPos = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, smoothTime);

            // 3. Si hay un temblor activo, añadimos el offset aleatorio
            if (shakeDuration > 0)
            {
                Vector2 shakeOffset = Random.insideUnitCircle * shakeMagnitude;
                newPos += new Vector3(shakeOffset.x, shakeOffset.y, 0);
                shakeDuration -= Time.deltaTime;
            }

            transform.position = newPos;
        }

        /// <summary>
        /// Hace temblar la pantalla con una duración y fuerza determinadas.
        /// </summary>
        /// <param name="duration">Segundos que durará el temblor.</param>
        /// <param name="magnitude">Intensidad del temblor.</param>
        public void ShakeCamera(float duration, float magnitude)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
        }
    }
}
