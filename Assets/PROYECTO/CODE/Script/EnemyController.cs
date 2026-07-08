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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyHealth = GetComponent<EnemyHealth>();
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

        // El arte del Heavy Knight no tiene sprites por dirección: se voltea por código.
        if (spriteRenderer != null && Mathf.Abs(rb.linearVelocity.x) > 0.01f)
        {
            spriteRenderer.flipX = rb.linearVelocity.x < 0f;
        }
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        animator.Play("Attack", 0, 0f);
        PlayRandomSound(attackSounds);

        yield return new WaitForSeconds(attackDelay);

        if (isDead || playerHealth == null || playerTransform == null) yield break;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= attackRange)
        {
            playerHealth.TakeDamage(attackDamage);
        }
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
        animator.SetTrigger("Dead");
        rb.linearVelocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 2f);
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (sfxSource == null || clips == null || clips.Length == 0) return;
        sfxSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
