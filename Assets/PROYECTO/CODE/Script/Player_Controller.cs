using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player_Controller : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    private PlayerController controls; // <- Clase generada por el Input System
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        controls = new PlayerController(); // Creamos el objeto del input
    }

    // Activar/desactivar el mapa de acciones cuando el objeto se esciende/apaga
    private void OnEnable() => controls.Gameplay.Enable();
    private void OnDisable() => controls.Gameplay.Disable();

    private void Update()
    {
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
        }
    }

    private void FixedUpdate()
    {
        // Movemos con Física en FixedUpdate (ritmo fijo del motor de física)
        rb.linearVelocity = moveInput * moveSpeed;
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