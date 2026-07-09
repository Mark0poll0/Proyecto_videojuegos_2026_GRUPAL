using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    private enum EnemyState { Idle, Chase, Attack, Dead }

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Persecución")]
    [Tooltip("Distancia a la que el enemigo detecta al jugador y empieza a perseguir.")]
    [SerializeField] private float detectionRange = 4f;
    [Tooltip("Distancia a la que el enemigo abandona la persecución.")]
    [SerializeField] private float loseRange = 6f;

    [Header("Ataque")]
    [Tooltip("Distancia a la que el enemigo deja de perseguir y ataca. Debe ser mayor que el radio físico de contacto (collider del enemigo + collider del jugador), o el enemigo se quedará empujando sin atacar nunca.")]
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 1;
    [Tooltip("Tiempo desde que empieza la animación de ataque hasta que se aplica el daño.")]
    [SerializeField] private float attackDelay = 0.3f;

    [Header("Sonidos (SFX)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] attackSounds;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EnemyHealth enemyHealth;
    private Transform playerTransform;
    private PlayerHealth playerHealth;

    private EnemyState state = EnemyState.Idle;
    private float lastAttackTime = -999f;
    private bool isDead;
    private bool isAttacking;
    private Vector2 lastFacing = Vector2.down;
    private float knockbackTimer = 0f;
    private Coroutine attackCoroutine;

    private string moveXParam;
    private string moveYParam;
    private string attackTriggerParam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var param in animator.parameters)
            {
                string pName = param.name;
                if (pName == "MoveX" || pName == "move x" || pName == "moveX") moveXParam = pName;
                if (pName == "MoveY" || pName == "move y" || pName == "moveY") moveYParam = pName;
                if (pName == "atacar" || pName == "Atacar" || pName == "attack" || pName == "Attack") attackTriggerParam = pName;
            }
        }
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += HandleHealthChanged;
            enemyHealth.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= HandleHealthChanged;
            enemyHealth.OnDeath -= HandleDeath;
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogWarning("EnemyController: no se encontró ningún GameObject con tag 'Player' en la escena.");
        }
    }

    private void Update()
    {
        if (isDead || playerTransform == null) return;

        if (isAttacking)
        {
            UpdateAnimator();
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (distance <= detectionRange) state = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (distance <= attackRange) state = EnemyState.Attack;
                else if (distance > loseRange) state = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                if (distance > attackRange) state = EnemyState.Chase;
                else TryAttack();
                break;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isDead || playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return; // Permite que la física de Unity mueva al enemigo con la velocidad del knockback y bloquea la IA
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (state == EnemyState.Chase)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 dir = rb.linearVelocity.normalized;
            
            if (moveXParam != null) animator.SetFloat(moveXParam, dir.x);
            if (moveYParam != null) animator.SetFloat(moveYParam, dir.y);

            // Si no tiene parámetro en el animator, usamos flipX como fallback temporal
            if (moveXParam == null && spriteRenderer != null)
            {
                spriteRenderer.flipX = dir.x < 0f;
            }

            lastFacing = dir;
        }
    }

    private string GetDirectionString()
    {
        if (Mathf.Abs(lastFacing.x) >= Mathf.Abs(lastFacing.y))
            return lastFacing.x >= 0f ? "rigth" : "left";
        return lastFacing.y >= 0f ? "up" : "down";
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        PlayRandomSound(attackSounds);

        string comboDir = GetDirectionString();
        string stateName = "attack_" + comboDir;

        // Fallback al estado básico "Attack" si no existen las animaciones por dirección
        AnimationClip clip = GetAnimationClip(stateName);
        if (clip == null)
        {
            stateName = "Attack";
        }

        animator.speed = 1f;
        animator.Play(stateName, 0, 0f);

        if (attackTriggerParam != null)
        {
            animator.SetTrigger(attackTriggerParam);
        }

        yield return new WaitForSeconds(attackDelay);

        if (isDead || playerHealth == null || playerTransform == null)
        {
            isAttacking = false;
            attackCoroutine = null;
            yield break;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= attackRange)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        if (!isDead)
        {
            // Intentar reproducir Idle o idle según lo que tenga el controller
            if (GetAnimationClip("Idle") != null) animator.Play("Idle", 0, 0f);
            else if (GetAnimationClip("idle") != null) animator.Play("idle", 0, 0f);
            else animator.Play("Idle", 0, 0f);
        }
        
        isAttacking = false;
        attackCoroutine = null;
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

    private void HandleHealthChanged(int current, int max)
    {
        // Se descarta el aviso inicial (current == max) y la muerte (current == 0, ver HandleDeath).
        if (current > 0 && current < max)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void HandleDeath()
    {
        isDead = true;
        state = EnemyState.Dead;

        // Desactivar la física por completo para que no sea empujado ni registre más colisiones
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // Limpiar triggers del Animator para que transiciones pendientes de "Hit" no interrumpan la muerte
        animator.ResetTrigger("Hit");
        if (attackTriggerParam != null) animator.ResetTrigger(attackTriggerParam);

        // Resetear parámetros de movimiento
        animator.SetFloat("Speed", 0f);
        if (moveXParam != null) animator.SetFloat(moveXParam, 0f);
        if (moveYParam != null) animator.SetFloat(moveYParam, 0f);

        string comboDir = GetDirectionString();
        string stateName = "death_" + comboDir;
        if (GetAnimationClip(stateName) != null)
        {
            animator.Play(stateName, 0, 0f);
        }
        else
        {
            animator.SetTrigger("Dead");
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2f);
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (isDead) return;
        Debug.Log($"[Enemy] ApplyKnockback recibido en {gameObject.name}. Dir={direction}, Force={force}, Duration={duration}");

        // Cancelar el ataque si el enemigo estaba atacando
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
        animator.speed = 1f;

        // Forzar animación de impacto/aturdimiento usando el trigger del Blend Tree
        animator.SetTrigger("Hit");

        // Activar el temporizador de aturdimiento y aplicar velocidad inicial del empuje
        knockbackTimer = duration;
        rb.linearVelocity = direction.normalized * force;
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;
        sfxSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
