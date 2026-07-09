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

    [Tooltip("Cantidad de frames que dura cada golpe del combo.")]
    [SerializeField] private int framesPerHit = 5;

    [Tooltip("Segundos que espera tras un golpe para encadenar el siguiente antes de resetear el combo.")]
    [SerializeField] private float comboWindow = 0.35f;

    [Header("Configuración del Hitbox")]
    [Tooltip("Collider (trigger) del hitbox de daño. Se activa solo durante la ventana de cada golpe.")]
    [SerializeField] private Collider2D attackHitboxCollider;
    [Tooltip("Distancia desde el jugador a la que se posiciona el hitbox, según la dirección del golpe.")]
    [SerializeField] private float attackHitboxOffset = 0.55f;
    [Tooltip("Tamaño del hitbox al atacar a los lados (Left/Right). X=alcance frontal, Y=margen de altura (para no fallar si no estás al mismo nivel).")]
    [SerializeField] private Vector2 hitboxSizeHorizontal = new Vector2(0.8f, 1.5f);
    [Tooltip("Tamaño del hitbox al atacar arriba/abajo (Up/Down). X=margen de anchura, Y=alcance frontal.")]
    [SerializeField] private Vector2 hitboxSizeVertical = new Vector2(1.5f, 0.8f);

    [Header("Sonidos (SFX)")]
    [Tooltip("Fuente de audio local del personaje para efectos de sonido.")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Gemidos o sonidos de esfuerzo al dar el primer golpe (se elegirá uno al azar).")]
    [SerializeField] private AudioClip[] attack1Sounds;

    [Tooltip("Gemidos o sonidos de esfuerzo al dar el segundo golpe (se elegirá uno al azar).")]
    [SerializeField] private AudioClip[] attack2Sounds;

    [Tooltip("Gemidos o sonidos de esfuerzo al dar el tercer golpe (se elegirá uno al azar).")]
    [SerializeField] private AudioClip[] attack3Sounds;

    [Tooltip("Sonidos de espadazo (se elegirá uno al azar en cada golpe del combo).")]
    [SerializeField] private AudioClip[] swordSounds;

    [Tooltip("Gemidos o sonidos al recibir daño (se elegirá uno al azar).")]
    [SerializeField] private AudioClip[] hitSounds;

    [Tooltip("Gemidos o sonidos cuando el personaje muere (se elegirá uno al azar).")]
    [SerializeField] private AudioClip[] deathSounds;

    private PlayerController controls; // <- Clase generada por el Input System
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastFacing = Vector2.down; // dirección a la que mira el personaje (por defecto: abajo)
    private bool isAttacking;
    private bool comboQueued;   // se marca al presionar Z durante un golpe para encadenar el siguiente
    private string comboDir;    // dirección fijada al iniciar el combo ("up"/"down"/"left"/"right")
    private bool isDead;
    private Coroutine comboCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Inicializamos los controles del Input System
        controls = new PlayerController();

        // Buscamos un AudioSource local si no fue asignado en el Inspector
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (attackHitboxCollider != null)
            attackHitboxCollider.enabled = false;
    }

    // Activar/desactivar el mapa de acciones cuando el objeto se enciende/apaga
    private void OnEnable()
    {
        if (controls != null)
        {
            controls.Gameplay.Enable();
            controls.Gameplay.Attack.performed += OnAttack; // suscribimos el ataque (tecla Z)
        }
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
        if (isDead)
            return;

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
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

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
        if (this == null || isDead) return; // Salvaguarda si el objeto de Unity ha sido destruido o está muerto

        if (!isAttacking)
        {
            // Inicio del combo: fijamos la dirección y arrancamos la corrutina
            comboDir = GetAttackDirection();
            comboCoroutine = StartCoroutine(ComboRoutine());
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

        // Intentamos obtener la información del clip de forma directa por su nombre
        float totalFrames = 20f; // Valor de respaldo por defecto
        AnimationClip clip = GetAnimationClip(state);
        if (clip != null)
        {
            totalFrames = clip.length * clip.frameRate;
        }

        // Calculamos los cortes en tiempo normalizado (0 a 1) para que dure 'framesPerHit' por golpe
        float normalizedStep = (float)framesPerHit / totalFrames;
        float[] cuts = new float[] { 
            0f, 
            normalizedStep, 
            normalizedStep * 2f, 
            1f // El tercer golpe va hasta el final de la animación para completarla
        };

        int golpes = cuts.Length - 1; // 3 golpes
        for (int step = 0; step < golpes; step++)
        {
            float inicio = cuts[step];
            float fin = cuts[step + 1];

            // Reproducimos el sonido de esfuerzo según el paso del combo
            if (step == 0)
                PlayRandomSFX(attack1Sounds);
            else if (step == 1)
                PlayRandomSFX(attack2Sounds);
            else if (step == 2)
                PlayRandomSFX(attack3Sounds);

            // Reproducimos un sonido de espada aleatorio en cada golpe del combo
            PlayRandomSFX(swordSounds);

            // Reproducimos el tramo [inicio, fin] del clip
            animator.speed = 1f;
            animator.Play(state, 0, inicio);
            yield return null; // Dejamos pasar un frame para iniciar la transición

            // Esperamos un frame y verificamos si ya entró al estado (con un límite de seguridad de 5 frames)
            int safetyCounter = 0;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName(state) && safetyCounter < 5)
            {
                safetyCounter++;
                yield return null;
            }

            // Activamos el hitbox de daño durante la ventana de este golpe
            PositionAttackHitbox(comboDir);
            if (attackHitboxCollider != null)
            {
                attackHitboxCollider.enabled = true;
                
                // Buscamos el script tanto en el propio collider como en el padre (el jugador)
                PlayerAttackHitbox hitbox = attackHitboxCollider.GetComponent<PlayerAttackHitbox>();
                if (hitbox == null) 
                    hitbox = attackHitboxCollider.GetComponentInParent<PlayerAttackHitbox>();
                    
                if (hitbox != null) 
                    hitbox.ClearHits();
            }

            // Avanzamos hasta el final del tramo
            while (animator.GetCurrentAnimatorStateInfo(0).IsName(state) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < fin)
            {
                yield return null;
            }

            // Desactivamos el hitbox al terminar la ventana de golpe
            if (attackHitboxCollider != null) attackHitboxCollider.enabled = false;

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
        comboCoroutine = null;
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

    // Posiciona el hitbox de ataque frente al jugador según la dirección del golpe.
    // "dir" (comboDir) viene con left/right invertidos respecto al mundo real, igual que en
    // GetAttackDirection (por el espejado de los sprites) — aquí los volvemos a invertir para
    // colocar el hitbox del lado físico correcto (donde realmente se ve el golpe).
    private void PositionAttackHitbox(string dir)
    {
        if (attackHitboxCollider == null) return;
        Vector2 offset = Vector2.zero;
        Vector2 size = Vector2.one;

        switch (dir)
        {
            case "up":
                offset = Vector2.up;
                size = hitboxSizeVertical;
                break;
            case "down":
                offset = Vector2.down;
                size = hitboxSizeVertical;
                break;
            case "left":
                offset = Vector2.right; // Mantenemos la inversión original del proyecto
                size = hitboxSizeHorizontal;
                break;
            case "right":
                offset = Vector2.left; // Mantenemos la inversión original del proyecto
                size = hitboxSizeHorizontal;
                break;
        }

        attackHitboxCollider.transform.localPosition = offset * attackHitboxOffset;
        
        BoxCollider2D boxCol = attackHitboxCollider as BoxCollider2D;
        if (boxCol != null)
        {
            boxCol.size = size;
        }
    }

    private AnimationClip GetAnimationClip(string name)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
                return clip;
        }
        return null;
    }

    /// <summary>
    /// Activa la animación de daño ("Hit") y reproduce un sonido aleatorio de quejido por daño.
    /// </summary>
    [ContextMenu("Probar Daño (Hit)")]
    public void TakeDamage()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        PlayRandomSFX(hitSounds);

        // Hacemos temblar la cámara al recibir daño
        Cainos.PixelArtTopDown_Basic.CameraFollow cam = Camera.main?.GetComponent<Cainos.PixelArtTopDown_Basic.CameraFollow>();
        if (cam != null)
        {
            cam.ShakeCamera(0.15f, 0.15f); // 0.15 segundos de duración, 0.15 de fuerza
        }
    }

    /// <summary>
    /// Activa la animación de muerte ("Dead") y reproduce el sonido de fallecimiento.
    /// </summary>
    [ContextMenu("Probar Muerte (Die)")]
    public void Die()
    {
        isDead = true;

        // Cancelar el combo activo para evitar que vuelva a Idle
        if (comboCoroutine != null)
        {
            StopCoroutine(comboCoroutine);
            comboCoroutine = null;
        }
        isAttacking = false;

        // Deshabilitar controles del jugador
        if (controls != null)
        {
            controls.Gameplay.Disable();
        }

        // Frenar por completo el Rigidbody
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play("die", 0, 0f); // Forzamos el estado de muerte directamente para evitar que quede atrapado en un ataque
            animator.SetTrigger("Dead"); // Mantenemos el trigger para compatibilidad
        }
        PlayRandomSFX(deathSounds);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void PlayRandomSFX(AudioClip[] clips)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;

        int randomIndex = Random.Range(0, clips.Length);
        AudioClip clip = clips[randomIndex];
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
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