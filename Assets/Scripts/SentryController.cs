using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryController : Enemy
{
    [Header("Movement")]
    [SerializeField] private float defaultMoveSpeed = 1.0f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private bool defaultFacingLeft = false;
    [SerializeField] private float boundaryCheckDepth = 0.5f;
    [SerializeField] private float boundarySlopeAngle = 10f;

    [Header("Attack")]
    [SerializeField] private float attackFrequencyTimer = 1f;
    [SerializeField] private float attackTriggerDistance = .3f;
    [SerializeField] private float rushTriggerDistance = 2f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = .5f;
    [SerializeField] private float attackDamage = 75;

    [Header("Damage")]
    [SerializeField] ParticleSystem damageEffect;


    Rigidbody2D spriteRigidBody;
    Animator spriteAnimator;
    Collider2D spriteCollider;
    float velocity = 0f;
    float attackCooldownTimer = 0f;
    bool triggerAttack = false;
    bool rushing = false;
    const float effectRotationDefault = 315f;
    const float effectRotationFlipped = 135f;

    new void Start()
    {
        base.Start();
        spriteRigidBody = GetComponent<Rigidbody2D>();
        spriteAnimator = GetComponent<Animator>();
        spriteCollider = GetComponent<Collider2D>();
        velocity = defaultMoveSpeed;
    }

    void FixedUpdate()
    {
        if (!dead)
        {
            HandleDamage();
            HandleAttack();
            HandleMove();
        }
        HandleAnimation();
    }

    void HandleMove()
    {
        if (BoundaryCheck())
        {
            TurnAround();
        }
        spriteRigidBody.velocity = new Vector2(velocity, spriteRigidBody.velocity.y);
    }

    void HandleAttack()
    {
        attackCooldownTimer -= Time.deltaTime;
        if (attackCooldownTimer > 0) return;

        float direction = defaultFacingLeft ? -Mathf.Sign(transform.localScale.x) : Mathf.Sign(transform.localScale.x);
        Vector2 checkPosition = spriteCollider.bounds.center + new Vector3(direction * spriteCollider.bounds.extents.x, 0);

        RaycastHit2D hitAttack = Physics2D.Raycast(checkPosition, new Vector2(direction, 0), attackTriggerDistance, playerMask);
        Debug.DrawRay(checkPosition, new Vector2(direction, 0) * attackTriggerDistance, Color.blue);

        RaycastHit2D hitRush = Physics2D.Raycast(checkPosition, new Vector2(direction, 0), rushTriggerDistance, playerMask);
        Debug.DrawRay(checkPosition, new Vector2(direction, 0) * rushTriggerDistance, Color.green);

        // Attack the player if they are in range (allow taking damage to disrupt attack)
        if (hitAttack)
        {
            Attack();
        }
        else if (hitRush)
        {
            rushing = true;
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

            PlayDamageEffects(damageDirection);

            if (dead)
            {
                spriteRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX;
            }
            else
            {
                velocity = -defaultMoveSpeed * Mathf.Sign(transform.localScale.x);
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
            spriteAnimator.SetBool("rushing", rushing);
            spriteAnimator.SetBool("walking", !rushing);
        }
    }

    void Attack()
    {
        triggerAttack = true;
        rushing = false;
        attackCooldownTimer = attackFrequencyTimer;
    }

    private bool BoundaryCheck()
    {
        // Use the sign of the velocity to determine which direction to start the raycast.
        const float CHECK_COLLIDER_OFFSET = .1f;
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

    void TurnAround()
    {
        rushing = false;
        velocity = -velocity;
        FlipSprite();
    }

    private void FlipSprite()
    {
        Vector3 localScale = transform.localScale;
        localScale.x = -transform.localScale.x;
        transform.localScale = localScale;
    }

    void PauseMovement()
    {
        velocity = 0;
    }

    void ResumeMovement(int speedMultiplier)
    {
        velocity = defaultMoveSpeed * speedMultiplier * Mathf.Sign(transform.localScale.x);
    }

    void Hop(float force)
    {
        // Cause the character to move upward by the 'force' amount
        spriteRigidBody.AddForce(new Vector2(0, force), ForceMode2D.Impulse);
    }

    private void DamagePlayer()
    {
        float direction = defaultFacingLeft ? -Mathf.Sign(transform.localScale.x) : Mathf.Sign(transform.localScale.x);
        Collider2D player = Physics2D.OverlapCircle(attackPoint.position, attackRange, playerMask);
        if (player)
        {
            player.GetComponent<PlayerMovement>().TakeDamage(attackDamage, direction);
        }
    }

    private void PlayDamageEffects(Vector2 damageDirection)
    {
        Vector3 transformRotation = damageEffect.transform.eulerAngles;
        if (damageDirection == Vector2.left)
        {
            Debug.Log($"Flipping effect transform {effectRotationFlipped}");
            damageEffect.transform.eulerAngles = new Vector3(transformRotation.x, transformRotation.y, effectRotationFlipped);
        }
        else
        {
            Debug.Log($"Default effect transform {effectRotationDefault}");
            damageEffect.transform.eulerAngles = new Vector3(transformRotation.x, transformRotation.y, effectRotationDefault);
        }
        damageEffect.Play();
    }
}
