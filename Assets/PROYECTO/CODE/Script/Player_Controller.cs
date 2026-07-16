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

    [Header("Combos - Dinámicas")]
    [Tooltip("Fuerza de empuje para los golpes 1 y 2")]
    [SerializeField] private float lightKnockbackForce = 12f;
    
    [Tooltip("Fuerza de empuje para el golpe final (3)")]
    [SerializeField] private float heavyKnockbackForce = 25f;

    [Tooltip("Fuerza de impulso hacia adelante (lunge) al dar cada golpe")]
    [SerializeField] private float lungeForce = 0f;

    [Tooltip("Tamaño del hitbox de área (todas las direcciones) para el tercer golpe")]
    [SerializeField] private Vector2 hitboxSizeAOE = new Vector2(2.5f, 2.5f);

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

    // Multiplicador de velocidad de ataque (1 = normal). Lo modifica el buff de cadencia.
    private float attackSpeedMultiplier = 1f;

    // Turbo (habilidad activa - tecla Shift)
    private PlayerBuffs playerBuffs;
    private bool turboActive;
    private float turboTimer;
    private float cooldownTimer;

    // Estado del Turbo para el HUD
    public bool TurboUnlocked => playerBuffs != null && playerBuffs.TurboUnlocked;
    public bool TurboActive => turboActive;
    public bool TurboReady => TurboUnlocked && !turboActive && cooldownTimer <= 0f;
    public float TurboRemaining01 => (playerBuffs != null && playerBuffs.TurboDuration > 0f) ? Mathf.Clamp01(turboTimer / playerBuffs.TurboDuration) : 0f;
    public float CooldownProgress01 => (playerBuffs != null && playerBuffs.TurboCooldown > 0f) ? Mathf.Clamp01(1f - cooldownTimer / playerBuffs.TurboCooldown) : 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerBuffs = GetComponent<PlayerBuffs>();

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
        if (isDead || Time.timeScale == 0f)
            return;

        // El Turbo se gestiona incluso mientras atacas (timers + tecla Shift).
        HandleTurbo();

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

        // Si está atacando y el bloqueo está activo, no aplicamos input de movimiento manual
        if (isAttacking && bloquearMovimientoAlAtacar)
        {
            // Quitamos el rb.linearVelocity = Vector2.zero constante para permitir el impulso (lunge)
            return;
        }

        // Movemos con Física en FixedUpdate (ritmo fijo del motor de física).
        // Durante el Turbo, la velocidad se multiplica.
        float speedMult = (turboActive && playerBuffs != null) ? playerBuffs.TurboSpeedMult : 1f;
        rb.linearVelocity = moveInput * moveSpeed * speedMult;
    }

    // Gestiona los temporizadores del Turbo y su activación con la tecla Shift.
    private void HandleTurbo()
    {
        if (playerBuffs == null) return;

        if (turboActive)
        {
            turboTimer -= Time.deltaTime;
            if (turboTimer <= 0f)
            {
                turboActive = false;
                cooldownTimer = playerBuffs.TurboCooldown;
            }
        }
        else if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (playerBuffs.TurboUnlocked && !turboActive && cooldownTimer <= 0f
            && Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            turboActive = true;
            turboTimer = playerBuffs.TurboDuration;
            if (JuiceManager.Instance != null)
                JuiceManager.Instance.ShowFloatingText(transform.position, "TURBO!", new Color(0.3f, 0.9f, 1f), 8f);
        }
    }

    // Se dispara cuando se presiona la acción Attack (tecla Z)
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (this == null || isDead || Time.timeScale == 0f) return; // Salvaguarda si el objeto de Unity ha sido destruido, está muerto o pausado

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

        for (int step = 0; step < 3; step++)
        {
            // Reproducimos la animación específica del paso (e.g. "attack_down_stay 1")
            string state = $"attack_{comboDir}_stay {step + 1}";

            // Reproducimos el sonido de esfuerzo según el paso del combo
            if (step == 0) PlayRandomSFX(attack1Sounds);
            else if (step == 1) PlayRandomSFX(attack2Sounds);
            else if (step == 2) PlayRandomSFX(attack3Sounds);

            // Reproducimos un sonido de espada aleatorio
            PlayRandomSFX(swordSounds);

            // Aplicar impulso hacia adelante (lunge)
            Vector2 lungeDir = Vector2.zero;
            switch (comboDir)
            {
                case "up": lungeDir = Vector2.up; break;
                case "down": lungeDir = Vector2.down; break;
                case "left": lungeDir = Vector2.right; break; // Invertido físicamente
                case "right": lungeDir = Vector2.left; break; // Invertido físicamente
            }
            rb.linearVelocity = lungeDir * lungeForce;

            // Obtener la duración del clip de animación
            AnimationClip clip = GetAnimationClip(state);
            float duration = clip != null ? clip.length : 0.3f;

            float atkMult = attackSpeedMultiplier * (turboActive && playerBuffs != null ? playerBuffs.TurboAtkSpeedMult : 1f);
            animator.speed = atkMult;
            animator.Play(state, 0, 0f);
            yield return null; // Dejamos pasar un frame para iniciar la transición

            // Activamos el hitbox (el tercer golpe, step == 2, es AOE en todas las direcciones)
            PositionAttackHitbox(comboDir, step == 2);
            if (attackHitboxCollider != null)
            {
                attackHitboxCollider.enabled = true;
                
                PlayerAttackHitbox hitbox = attackHitboxCollider.GetComponent<PlayerAttackHitbox>();
                if (hitbox == null) 
                    hitbox = attackHitboxCollider.GetComponentInParent<PlayerAttackHitbox>();
                    
                if (hitbox != null) 
                {
                    hitbox.ClearHits();
                    // Configurar empuje dinámico (leve para 1 y 2, fuerte para el 3)
                    hitbox.knockbackForce = (step == 2) ? heavyKnockbackForce : lightKnockbackForce;
                    hitbox.knockbackDuration = (step == 2) ? 0.2f : 0.1f;
                }
            }

            // Avanzamos hasta el final de la animación usando el tiempo del clip
            float elapsed = 0f;
            // Restamos un frame estimado del total para compensar el yield return null inicial.
            // Dividimos por el multiplicador porque a mayor cadencia la animación dura menos.
            float waitDuration = Mathf.Max(0.05f, (duration / Mathf.Max(0.01f, atkMult)) - Time.deltaTime);
            while (elapsed < waitDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Desactivamos el hitbox al terminar
            if (attackHitboxCollider != null) attackHitboxCollider.enabled = false;

            // Frenar al personaje al terminar el golpe
            rb.linearVelocity = Vector2.zero;
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

        // Volvemos al Blend Tree de Idle
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
    // Si isAOE es true (tercer golpe), el hitbox se centra en el jugador y cubre todas las direcciones.
    private void PositionAttackHitbox(string dir, bool isAOE = false)
    {
        if (attackHitboxCollider == null) return;
        Vector2 offset = Vector2.zero;
        Vector2 size = Vector2.one;

        if (isAOE)
        {
            offset = Vector2.zero;
            size = hitboxSizeAOE;
        }
        else
        {
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
        }

        attackHitboxCollider.transform.localPosition = offset * (isAOE ? 0f : attackHitboxOffset);
        
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

    /// <summary>Aumenta la velocidad de movimiento del jugador (buff de velocidad).</summary>
    public void AddMoveSpeed(float amount)
    {
        moveSpeed = Mathf.Max(0f, moveSpeed + amount);
    }

    /// <summary>
    /// Multiplica la velocidad de ataque del jugador (buff de cadencia).
    /// Ej: factor 1.2 → los combos van un 20% más rápidos. Se acumula multiplicativamente.
    /// </summary>
    public void MultiplyAttackSpeed(float factor)
    {
        if (factor <= 0f) return;
        attackSpeedMultiplier *= factor;
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