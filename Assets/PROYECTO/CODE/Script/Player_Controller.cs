using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Player_Controller : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Ataque")]
    [Tooltip("Si está activo, el jugador se queda quieto mientras ataca.")]
    [SerializeField] private bool bloquearMovimientoAlAtacar = true;

    [Tooltip("Cortes del combo en normalizedTime del clip: 3 golpes = 4 valores (0..1). " +
             "Cada golpe reproduce el tramo [corte i, corte i+1].")]
    [SerializeField] private float[] comboCuts = { 0f, 0.34f, 0.67f, 1f };

    [Tooltip("Segundos que espera tras un golpe para encadenar el siguiente antes de resetear el combo.")]
    [SerializeField] private float comboWindow = 0.35f;

    private PlayerController controls; // <- Clase generada por el Input System
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastFacing = Vector2.down; // dirección a la que mira el personaje (por defecto: abajo)
    private bool isAttacking;
    private bool comboQueued;   // se marca al presionar Z durante un golpe para encadenar el siguiente
    private string comboDir;    // dirección fijada al iniciar el combo ("up"/"down"/"left"/"right")

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Activar/desactivar el mapa de acciones cuando el objeto se enciende/apaga
    private void OnEnable()
    {
        // Inicialización perezosa: OnEnable puede correr antes de Awake o tras un domain reload
        if (controls == null)
            controls = new PlayerController(); // Creamos el objeto del input

        controls.Gameplay.Enable();
        controls.Gameplay.Attack.performed += OnAttack; // suscribimos el ataque (tecla Z)
    }

    private void OnDisable()
    {
        if (controls == null)
            return;

        controls.Gameplay.Attack.performed -= OnAttack;
        controls.Gameplay.Disable();
    }

    private void Update()
    {
        // Mientras ataca, el ataque tiene prioridad: no leemos movimiento
        if (isAttacking)
            return;

        // Leemos el Input en cada Frame (En Update, que va al ritmo de los fotogramas)
        moveInput = controls.Gameplay.Move.ReadValue<Vector2>();

        // Alimentamos el animator
        // Speed = 0 cuando está quieto idle, >0 cuando se mueve
        animator.SetFloat("Speed", moveInput.sqrMagnitude);

        // guardamos la última dirección
        if (moveInput.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
            lastFacing = moveInput; // recordamos hacia dónde mira para el ataque
        }
    }

    private void FixedUpdate()
    {
        // Si está atacando y el bloqueo está activo, se queda quieto
        if (isAttacking && bloquearMovimientoAlAtacar)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Movemos con Física en FixedUpdate (ritmo fijo del motor de física)
        rb.linearVelocity = moveInput * moveSpeed;
    }

    // Se dispara cuando se presiona la acción Attack (tecla Z)
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!isAttacking)
        {
            // Inicio del combo: fijamos la dirección y arrancamos la corrutina
            comboDir = GetAttackDirection();
            StartCoroutine(ComboRoutine());
        }
        else
        {
            // Ya estamos atacando: bufferizamos el siguiente golpe del combo
            comboQueued = true;
        }
    }

    private IEnumerator ComboRoutine()
    {
        isAttacking = true;
        comboQueued = false;

        // Cortamos el movimiento al iniciar el combo
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        animator.SetFloat("Speed", 0f);

        // Usamos el clip "en su lugar" (_stay) de la dirección fijada
        string state = "attack_" + comboDir + "_stay";

        int golpes = Mathf.Max(0, comboCuts.Length - 1); // nº de tramos = cortes - 1 (3 golpes con 4 cortes)
        for (int step = 0; step < golpes; step++)
        {
            float inicio = comboCuts[step];
            float fin = comboCuts[step + 1];

            // Reproducimos el tramo [inicio, fin] del clip
            animator.speed = 1f;
            animator.Play(state, 0, inicio);
            yield return null; // dejamos que el Animator entre al estado

            // Avanzamos hasta el final del tramo
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < fin)
                yield return null;

            // Congelamos en el último frame del golpe
            animator.speed = 0f;

            // Ventana de encadenado: esperamos un Z para el siguiente golpe
            float t = 0f;
            while (t < comboWindow && !comboQueued)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (!comboQueued)
                break; // no encadenó a tiempo: terminamos el combo

            comboQueued = false; // consumimos el golpe encadenado y seguimos
        }

        // Volvemos al Blend Tree de Idle (retomará Walk/Idle según el input)
        animator.speed = 1f;
        animator.Play("Idle", 0, 0f);
        isAttacking = false;
    }

    // Devuelve la dirección del ataque según el eje dominante de la última dirección mirada.
    // NOTA: los sprites de ataque horizontales (attack_left/right y sus _stay) están dibujados
    // en espejo respecto a su nombre (el clip "attack_right_*" se ve hacia la izquierda y viceversa),
    // por eso invertimos left/right aquí para que el golpe salga hacia donde mira el jugador.
    // El eje vertical (up/down) NO está afectado.
    private string GetAttackDirection()
    {
        if (Mathf.Abs(lastFacing.x) >= Mathf.Abs(lastFacing.y))
            return lastFacing.x >= 0f ? "left" : "right";
        return lastFacing.y >= 0f ? "up" : "down";
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Si el trigger de la escalera te cambió la capa física a Layer 2, 
        // despertamos el Rigidbody para consolidar el paso al segundo piso
        if (gameObject.layer == LayerMask.NameToLayer("Layer 2") || gameObject.layer == LayerMask.NameToLayer("Layer 1"))
        {
            rb.WakeUp();
        }
    }
}