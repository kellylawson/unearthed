using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Collider2D groundCollider;
    [SerializeField] Collider2D hitCollider;

    [Header("Movement")]
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] int maxJumpForgiveness = 5;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = .1f;
    // This margin is used to shrink the ground check cast box by a small amount on each side so that it doesn't detect walls as ground.
    [SerializeField] float groundcheckMargin = 0.1f;
    [SerializeField] float slopeCheckDistance = 0.5f;

    [Header("Attack")]
    [SerializeField] float lightAttackTimer = .5f;
    [SerializeField] float heavyAttackTimer = .7f;
    [SerializeField] float attackRange = .5f;
    [SerializeField] Transform attackPoint;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] int lightAttackDamage = 25;
    [SerializeField] int heavyAttackDamage = 40;

    [Header("Health")]
    [SerializeField] float maxHealth = 500f;

    [Header("Effects")]
    [SerializeField] ParticleSystem lightAttackEffect;
    [SerializeField] ParticleSystem heavyAttackEffect;
    [SerializeField] GameObject leftWeaponSprite;
    [SerializeField] GameObject rightWeaponSprite;
    [SerializeField] Material defaultMaterial;
    [SerializeField] Material glowMaterial;
    [SerializeField] ParticleSystem rightWeaponEffect;
    [SerializeField] ParticleSystem leftWeaponEffect;

    Vector2 moveInput;
    Rigidbody2D spriteRigidBody;
    Animator spriteAnimator;


    float spriteGravity;
    bool isAlive = true;
    bool jumpInput = false;
    bool jumping = true;
    int framesToHandleJump = 0;
    bool grounded = true;
    Vector2 slopeDirection;
    float slopeDownAngle;
    bool onSlope = false;
    Vector2 newVelocity = Vector2.zero;
    bool triggerLightAttack = false;
    bool triggerHeavyAttack = false;
    SpriteRenderer leftWeaponSpriteRenderer;
    SpriteRenderer rightWeaponSpriteRenderer;
    float attackTimer = -1;
    float health;
    Vector2 damagedFrom;
    bool knockBack = false;
    bool damageTaken = false;


    void Start()
    {
        spriteRigidBody = GetComponent<Rigidbody2D>();
        spriteAnimator = GetComponent<Animator>();
        spriteGravity = spriteRigidBody.gravityScale;
        leftWeaponSpriteRenderer = leftWeaponSprite.GetComponent<SpriteRenderer>();
        rightWeaponSpriteRenderer = rightWeaponSprite.GetComponent<SpriteRenderer>();
        health = maxHealth;
        damagedFrom = Vector2.zero;

        DeactivateRightWeaponTrail();
        DeactivateLeftWeaponTrail();
    }

    void FixedUpdate()
    {
        if (!isAlive) return;

        GroundCheck();
        SlopeCheck();
        HandleJump();
        HandleMove();
        HandleAttack();
        HandleKnockBack();
        Climb();
        HandleAnimation();
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (!isAlive || knockBack) return;

        if (value.isPressed)
        {
            jumpInput = true;
            framesToHandleJump = maxJumpForgiveness;
        }
    }

    void OnFire(InputValue value)
    {
        if (!isAlive) return;

        if (value.isPressed)
        {
            // Instantiate(bullet, gun.position, transform.rotation);
        }
    }

    void OnLightAttack()
    {
        if (!isAlive || knockBack) return;

        if (attackTimer <= 0)
        {
            triggerLightAttack = true;
            attackTimer = lightAttackTimer;
        }
    }

    void OnHeavyAttack()
    {
        if (!isAlive || knockBack) return;

        if (attackTimer <= 0)
        {
            triggerHeavyAttack = true;
            attackTimer = heavyAttackTimer;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint)
        {
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

    }

    private void SlopeCheck()
    {
        Vector2 checkPosition = groundCollider.bounds.center - new Vector3(0, groundCollider.bounds.extents.y);

        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, slopeCheckDistance, groundMask);
        Debug.DrawRay(checkPosition, Vector2.down * slopeCheckDistance, Color.yellow, 1f);

        if (hit)
        {
            slopeDirection = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != 0.0f)
            {
                onSlope = true;
            }
            else
            {
                onSlope = false;
            }

            Debug.DrawRay(hit.point, slopeDirection, Color.magenta);
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
    }

    private void GroundCheck()
    {

        Vector3 extents = groundCollider.bounds.extents;
        Vector3 center = groundCollider.bounds.center;
        float radius = extents.x - groundcheckMargin;
        Vector3 circleOrigin = center - new Vector3(0, extents.y - extents.x + groundDistance);
        grounded = Physics2D.OverlapCircle(circleOrigin, radius, groundMask);

        // Draw some debug lines to show the ground check result
        Color debugColor = grounded ? Color.cyan : Color.red;
        Debug.DrawRay(circleOrigin, Vector2.left * radius, debugColor);
        Vector2 diagLeft = Vector2.left + Vector2.down;
        diagLeft.Normalize();
        Debug.DrawRay(circleOrigin, diagLeft * radius, debugColor);
        Debug.DrawRay(circleOrigin, Vector2.down * radius, debugColor);
        Vector2 diagRight = Vector2.down + Vector2.right;
        diagRight.Normalize();
        Debug.DrawRay(circleOrigin, diagRight * radius, debugColor);
        Debug.DrawRay(circleOrigin, Vector2.right * radius, debugColor);

        // If we are moving downward or neutral, cancel the jump
        if (spriteRigidBody.velocity.y <= 0.0f)
        {
            jumping = false;
        }
    }

    private void HandleMove()
    {
        if (!isAlive || knockBack) return;

        if (Mathf.Abs(moveInput.x) <= Mathf.Epsilon && grounded)
        {
            spriteRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            // Check to see if we have just stopped.  This contains a bounce when stopping on a slope
            if (Mathf.Abs(newVelocity.x) > Mathf.Epsilon && onSlope)
            {
                newVelocity = Vector2.zero;
                spriteRigidBody.constraints |= RigidbodyConstraints2D.FreezePositionY;
            }
        }
        else if (Mathf.Abs(moveInput.x) > Mathf.Epsilon)
        {
            spriteRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

            newVelocity = new Vector2(runSpeed * moveInput.x, spriteRigidBody.velocity.y);
            if (grounded && onSlope && !jumping)
            {
                newVelocity.Set(runSpeed * slopeDirection.x * -moveInput.x, runSpeed * slopeDirection.y * -moveInput.x);
            }

            spriteRigidBody.velocity = newVelocity;
            if (Mathf.Sign(moveInput.x) != Mathf.Sign(transform.localScale.x))
            {
                FlipSprite();
            }
        }

    }

    private void HandleJump()
    {
        if (jumpInput && grounded)
        {
            // Need this line so y velocity from going uphill doesn't get amplified by a jump
            spriteRigidBody.velocity = new Vector2(spriteRigidBody.velocity.x, 0.0f);
            spriteRigidBody.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
            jumpInput = false;
            jumping = true;
        }
        else if (framesToHandleJump > 0)
        {
            framesToHandleJump--;
        }
        else if (framesToHandleJump <= 0)
        {
            // Drop the jump input if we haven't handled it within the allowed frames
            jumpInput = false;
        }
    }

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
    }

    private void HandleKnockBack()
    {
        if (knockBack && !grounded)
        {
            damageTaken = true;
        }
        if (damageTaken && grounded)
        {
            knockBack = false;
            damageTaken = false;
        }
        if (damagedFrom == Vector2.zero) return;

        // Allow non-input based movement, we disable x movement not related to player input in HandleMove
        spriteRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        spriteRigidBody.velocity = Vector2.zero;
        damagedFrom.y = 1f;
        spriteRigidBody.AddForce(new Vector2(4, 10) * damagedFrom, ForceMode2D.Impulse);
        damagedFrom = Vector2.zero;
        knockBack = true;
    }

    private void HandleAnimation()
    {
        bool touchingLadder = hitCollider.IsTouchingLayers(LayerMask.GetMask("Ladder"));
        bool hasXSpeed = Mathf.Abs(spriteRigidBody.velocity.x) > Mathf.Epsilon;
        bool hasYSpeed = Mathf.Abs(spriteRigidBody.velocity.y) > Mathf.Epsilon;
        spriteAnimator.SetFloat("xVelocity", hasXSpeed ? spriteRigidBody.velocity.x : 0);
        spriteAnimator.SetFloat("yVelocity", hasYSpeed ? spriteRigidBody.velocity.y : 0);
        spriteAnimator.SetBool("isJumping", !grounded && !touchingLadder);
        spriteAnimator.SetBool("isRunning", grounded && Mathf.Abs(moveInput.x) > Mathf.Epsilon);
        spriteAnimator.SetBool("isClimbing", hasYSpeed && !grounded && touchingLadder);
        if (triggerLightAttack)
        {
            spriteAnimator.SetTrigger("closeLightAttack");
            triggerLightAttack = false;
        }
        if (triggerHeavyAttack)
        {
            spriteAnimator.SetTrigger("closeHeavyAttack");
            triggerHeavyAttack = false;
        }
    }

    private void Climb()
    {
        // Check if the move input up or down direction are pressed.  If so and we are touching a ladder, climb.
        if (!hitCollider.IsTouchingLayers(LayerMask.GetMask("Ladder")))
        {
            spriteRigidBody.gravityScale = spriteGravity;
            return;
        }
    }

    private void Die()
    {
        spriteAnimator.SetTrigger("dead");
        isAlive = false;
        spriteRigidBody.velocity = Vector3.zero;
    }

    void LightAttackEffect()
    {
        if (lightAttackEffect != null)
        {
            lightAttackEffect.Play();
            HitEnemies(lightAttackDamage);
        }
    }

    void HeavyAttackEffect()
    {
        if (heavyAttackEffect != null)
        {
            heavyAttackEffect.Play();
            HitEnemies(heavyAttackDamage);
        }
    }

    void ActivateRightWeaponTrail()
    {
        rightWeaponEffect.Play();
        rightWeaponSpriteRenderer.material = glowMaterial;
    }

    void DeactivateRightWeaponTrail()
    {
        rightWeaponSpriteRenderer.material = defaultMaterial;
    }

    void ActivateLeftWeaponTrail()
    {
        leftWeaponEffect.Play();
        leftWeaponSpriteRenderer.material = glowMaterial;
    }

    void DeactivateLeftWeaponTrail()
    {
        leftWeaponSpriteRenderer.material = defaultMaterial;
    }

    private void HitEnemies(int attackDamage)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyMask);

        foreach (Collider2D enemy in hitEnemies)
        {
            // here we call each enemy and damage them.
            enemy.GetComponent<Enemy>().TakeDamage(attackDamage, Mathf.Sign(transform.localScale.x));
        }

    }

    public void TakeDamage(float damage, float direction)
    {
        damagedFrom = direction < 0 ? Vector2.left : Vector2.right;
        health -= damage;
        if (health <= 0 && isAlive)
        {
            Die();
        }
    }

    private void FlipSprite()
    {
        transform.localScale = new Vector2(Mathf.Sign(spriteRigidBody.velocity.x), 1f);
        ParticleSystemRenderer renderer = lightAttackEffect.GetComponent<ParticleSystemRenderer>();
        renderer.flip = new Vector3(spriteRigidBody.velocity.x < 0.0f ? 1 : 0, renderer.flip.y, renderer.flip.z);
        FlipWeaponEffect(leftWeaponEffect);
        FlipWeaponEffect(rightWeaponEffect);
    }

    private void FlipWeaponEffect(ParticleSystem effect)
    {
        var shape = effect.shape;
        shape.scale = new Vector3(Mathf.Sign(spriteRigidBody.velocity.x), 1f, 0f);
    }

}
