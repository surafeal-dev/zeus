using System;
using System.Collections;
using UnityEngine;

public class Entity : MonoBehaviour
{
    protected Rigidbody2D rb;
    protected Animator anim;
    protected Collider2D col;
    protected SpriteRenderer sr;

    [Header("Health details")]
    [SerializeField] protected int maxHealth = 4;
    [SerializeField] protected float knockbackPower = 6;
    [SerializeField] protected float knockbackDuration = .2f;
    [SerializeField] protected float deathDelay = 1.5f;
    [Tooltip("Upward pop applied when dying, before falling off the level.")]
    [SerializeField] protected float deathImpulse = 13;
    [Tooltip("Gravity scale while falling to death. Normal play uses 3.5.")]
    [SerializeField] protected float deathGravityScale = 5;
    [SerializeField] private Material damageMaterial;
    [SerializeField] private float damageFeedbackDuration = .2f;
    private Coroutine damageFeedbackCoroutine;
    private Material defaultMaterial;


    protected int currentHealth;
    protected bool isDead;
    protected bool isKnocked;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public event Action<int, int> OnHealthChanged;

    [Header("Attack details")]
    [SerializeField] protected float attackRadius;
    [SerializeField] protected Transform attackPoint;
    [SerializeField] protected LayerMask whatIsTarget;
    [SerializeField] protected int damage = 1;

    [Header("Movement details")]
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] private float jumpForce = 8;
    protected int facingDir = 1;
    public float xinput;
    private bool facingRight = true;
    protected bool canMove = true;

    [Header("Collision details")]
    [SerializeField] protected float groundCheckDistance;
    [SerializeField] protected LayerMask whatIsGround;
    protected bool isGrounded;


    protected virtual void Awake()
    {
        rb= GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        
        if (sr != null)
            defaultMaterial = sr.sharedMaterial;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        HandleCollision();
        HandleInput();

        HandleMovement();
        HandleAnimations();
        HandleFlip();
    }

    public void DamageTargets()
    {
        if (attackPoint == null)
            return;

        Collider2D[] targetColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, whatIsTarget);

        foreach (Collider2D target in targetColliders)
        {
            Entity entityTarget = target.GetComponentInParent<Entity>();

            if (entityTarget == null || entityTarget == this)
                continue;

            entityTarget.TakeDamage(damage, transform);
        }
    }

    public virtual void TakeDamage(int damageAmount, Transform damageDealer)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(currentHealth - damageAmount, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        PlayDamageFeedback();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(KnockbackCo(damageDealer));
    }

    private void PlayDamageFeedback()
    {
        if (sr == null || damageMaterial == null)
            return;

        if (damageFeedbackCoroutine != null)
        {
            StopCoroutine(damageFeedbackCoroutine);
            sr.material = defaultMaterial;
        }

        damageFeedbackCoroutine = StartCoroutine(DamageFeedbackCo());
    }

    private IEnumerator DamageFeedbackCo()
    {
        sr.material = damageMaterial;

        yield return new WaitForSeconds(damageFeedbackDuration);

        sr.material = defaultMaterial;
        damageFeedbackCoroutine = null;
    }

    protected virtual void Die()
    {
        isDead = true;
        canMove = false;
        isKnocked = false;

        if (anim != null)
            anim.enabled = false;

        if (col != null)
            col.enabled = false;

        rb.simulated = true;
        rb.gravityScale = deathGravityScale;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, deathImpulse);

        Destroy(gameObject, deathDelay);
    }

    private IEnumerator KnockbackCo(Transform damageDealer)
    {
        isKnocked = true;

        int knockbackDir = 1;

        if (damageDealer != null && damageDealer.position.x > transform.position.x)
            knockbackDir = -1;

        rb.linearVelocity = new Vector2(knockbackPower * knockbackDir, rb.linearVelocity.y);

        yield return new WaitForSeconds(knockbackDuration);

        isKnocked = false;
    }

    protected virtual void HandleAnimations()
    {
        anim.SetFloat("xVelocity", rb.linearVelocity.x);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    protected virtual void HandleInput()
    {
        xinput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
            TryToJump();
        if (Input.GetKeyDown(KeyCode.Mouse0))
            TryToAttack();
    }
    protected virtual bool TryToAttack()
    {
        if (isGrounded == false)
            return false;

        anim.SetTrigger("Attack");
        return true;
    }
    protected virtual void HandleMovement()
    {
        if (isKnocked)
            return;

        if(canMove)
        rb.linearVelocity = new Vector2(xinput * moveSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void TryToJump()
    {
        if(isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

    }
    protected virtual void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
    }
    protected virtual void HandleFlip()
    {
        if (isKnocked)
            return;

        if (rb.linearVelocity.x > 0 && facingRight == false)
            flip();
        else if (rb.linearVelocity.x < 0 && facingRight == true)
            flip();
    }
    protected void flip()
    {
        transform.Rotate(0,180,0);
        facingRight = !facingRight;
        facingDir *= -1;
    }
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3 (0, -groundCheckDistance));

        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
