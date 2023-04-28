using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryController : Enemy
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private bool defaultFacingLeft = false;
    [SerializeField] private float boundaryCheckDepth = 0.5f;
    [SerializeField] private float boundarySlopeAngle = 10f;

    [Header("Attack")]
    [SerializeField] private float damagePauseTimer = 1f;
    [SerializeField] private float attackPauseTimer = 1.75f;
    [SerializeField] private float attackFrequencyTimer = 1f;
    [SerializeField] private float attackTriggerDistance = .3f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = .5f;
    [SerializeField] private float attackDamage = 75;

    Rigidbody2D spriteRigidBody;
    Animator spriteAnimator;
    Collider2D spriteCollider;
    float velocity = 0f;
    float movePauseTimer = 0f;
    float attackTimer = 0f;
    bool triggerAttack = false;

    new void Start()
    {
        base.Start();
        spriteRigidBody = GetComponent<Rigidbody2D>();
        spriteAnimator = GetComponent<Animator>();
        spriteCollider = GetComponent<Collider2D>();
        velocity = moveSpeed;
    }

    void FixedUpdate()
    {
        if (!dead)
        {
            HandleDamage();
            Attack();
            Move();
        }
        HandleAnimation();
    }

    void Move()
    {
        movePauseTimer -= Time.deltaTime;
        if (movePauseTimer <= 0)
        {
            velocity = moveSpeed * Mathf.Sign(transform.localScale.x);
            // Just move the sprite in their normal pattern
            if (BoundaryCheck())
            {
                velocity = -velocity;
                FlipSprite();
            }
        }
        else
        {
            velocity = 0;
        }
        spriteRigidBody.velocity = new Vector2(velocity, spriteRigidBody.velocity.y);
    }

    void Attack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0) return;

        float direction = defaultFacingLeft ? -Mathf.Sign(transform.localScale.x) : Mathf.Sign(transform.localScale.x);
        Vector2 checkPosition = spriteCollider.bounds.center + new Vector3(direction * spriteCollider.bounds.extents.x, 0);
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, new Vector2(direction, 0), attackTriggerDistance, playerMask);
        Debug.DrawRay(checkPosition, new Vector2(direction, 0) * attackTriggerDistance, Color.blue);

        // Attack the player if they are in range (allow taking damage to disrupt attack)
        if (hit && !tookDamage)
        {
            triggerAttack = true;
            attackTimer = attackFrequencyTimer;
            movePauseTimer = attackPauseTimer;
        }
    }

    void HandleDamage()
    {
        if (tookDamage)
        {
            // If the damaging sprite is attacking in the same direction this sprite is facing, flip this sprite
            // Allow for sprites that face left by default
            float spriteDirection = defaultFacingLeft ? -Mathf.Sign(transform.localScale.x) : Mathf.Sign(transform.localScale.x);
            if (Mathf.Sign(damageDirection.x) == spriteDirection)
            {
                FlipSprite();
            }
            // Pause movement for a period when we are hit
            movePauseTimer = damagePauseTimer;

            //spriteRigidBody.velocity = Vector2.zero;
            if (dead)
            {
                spriteRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX;
            }
            else
            {
                spriteRigidBody.AddForce(new Vector2(200, 0) * damageDirection, ForceMode2D.Impulse);
            }
        }
    }

    void HandleAnimation()
    {
        if (tookDamage)
        {
            spriteAnimator.SetTrigger("damaged");
            tookDamage = false;
        }
        else if (dead)
        {
            spriteAnimator.SetBool("dead", true);
        }
        else if (triggerAttack)
        {
            spriteAnimator.SetTrigger("attack");
            triggerAttack = false;
        }
        else
        {
            spriteAnimator.SetBool("walking", true);
        }
    }

    private bool BoundaryCheck()
    {
        // Use the sign of the velocity to determine which direction to start the raycast.
        const float CHECK_COLLIDER_OFFSET = .02f;
        Vector2 checkPosition = spriteCollider.bounds.center + new Vector3(Mathf.Sign(spriteRigidBody.velocity.x) * spriteCollider.bounds.extents.x, -spriteCollider.bounds.extents.y + CHECK_COLLIDER_OFFSET);
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, spriteCollider.bounds.extents.y + boundaryCheckDepth, groundMask);
        Debug.DrawRay(checkPosition, Vector2.down * boundaryCheckDepth, Color.red);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(Vector2.up, hit.normal);
            // hit a steep enough slope, this is a boundary
            if (slopeAngle > boundarySlopeAngle)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    private void FlipSprite()
    {
        transform.localScale = new Vector2(-transform.localScale.x, 1f);
    }

    private void HitPlayer()
    {
        float direction = defaultFacingLeft ? -Mathf.Sign(transform.localScale.x) : Mathf.Sign(transform.localScale.x);
        Collider2D player = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerMask);
        if (player)
        {
            player.GetComponent<PlayerMovement>().TakeDamage(attackDamage, direction);
        }
    }

}
