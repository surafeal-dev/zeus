using UnityEngine;

public class Enemy : Entity
{
    [Header("Patrol details")]
    [SerializeField] private float wallCheckDistance = .6f;
    [SerializeField] private float ledgeCheckDistance = .8f;
    [SerializeField] private float idleTimeOnTurn = 1;

    [Header("Player detection")]
    [SerializeField] private LayerMask whatIsPlayer;
    [SerializeField] private float playerCheckDistance = 5;
    [SerializeField] private float attackDistance = 1.2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDuration = .5f;

    private bool wallDetected;
    private bool groundInFrontDetected;
    private float distanceToPlayer;
    private bool playerDetected;

    private float idleTimer;
    private float attackTimer;
    private float busyTimer;

    
    protected override void HandleInput()
    {
        attackTimer -= Time.deltaTime;

        if (busyTimer > 0)
        {
            busyTimer -= Time.deltaTime;
            xinput = 0;
            return;
        }

        if (playerDetected)
        {
            HandleCombat();
            return;
        }

        HandlePatrol();
    }

    private void HandleCombat()
    {
        if (distanceToPlayer > attackDistance)
        {
            xinput = facingDir;
            return;
        }

        xinput = 0;

        if (attackTimer <= 0)
        {
            attackTimer = attackCooldown;
            busyTimer = attackDuration;
            TryToAttack();
        }
    }

    private void HandlePatrol()
    {
        if (idleTimer > 0)
        {
            idleTimer -= Time.deltaTime;
            xinput = 0;
            return;
        }

        if (isGrounded && (wallDetected || groundInFrontDetected == false))
        {
            flip();
            idleTimer = idleTimeOnTurn;
            xinput = 0;
            return;
        }

        xinput = facingDir;
    }

    protected override void HandleCollision()
    {
        base.HandleCollision();

        Vector2 forward = Vector2.right * facingDir;

        wallDetected = Physics2D.Raycast(transform.position, forward, wallCheckDistance, whatIsGround);

        Vector2 ledgeCheckPoint = (Vector2)transform.position + forward * ledgeCheckDistance;
        groundInFrontDetected = Physics2D.Raycast(ledgeCheckPoint, Vector2.down, groundCheckDistance, whatIsGround);

        RaycastHit2D playerHit = Physics2D.Raycast(transform.position, forward, playerCheckDistance, whatIsPlayer);
        playerDetected = playerHit.collider != null;
        distanceToPlayer = playerDetected ? playerHit.distance : Mathf.Infinity;
    }

   
    protected override void HandleAnimations()
    {
        anim.SetFloat("xVelocity", rb.linearVelocity.x);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Vector3 forward = Vector3.right * facingDir;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + forward * wallCheckDistance);

        Vector3 ledgeCheckPoint = transform.position + forward * ledgeCheckDistance;
        Gizmos.DrawLine(ledgeCheckPoint, ledgeCheckPoint + Vector3.down * groundCheckDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + forward * playerCheckDistance);
    }
}
