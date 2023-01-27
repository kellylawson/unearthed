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
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] string groundLayers;
    [SerializeField] float groundDistance = .1f;
    // This margin is used to shrink the ground check cast box by a small amount on each side so that it doesn't detect walls as ground.
    [SerializeField] float groundboxMargin = 0.1f;
    [SerializeField] float maxAccelerateFrames = 10f;
    [SerializeField] float slopeCheckDistance = 0.5f;

    // [SerializeField] GameObject bullet;
    // [SerializeField] Transform gun;

    Vector2 moveInput;
    Rigidbody2D spriteRigidBody;
    Animator spriteAnimator;


    float spriteGravity;
    bool isAlive = true;
    bool jumpInput = false;
    bool jumping = true;
    int framesToHandleJump = 0;
    bool grounded = true;
    int groundMask = 0;
    float accelerateFrames = 0f;
    Vector2 slopeNormalPerp;
    float slopeDownAngle;
    float slopeDownAngleOld = 0f;
    float slopeSideAngle;
    bool onSlope = false;
    Vector2 newVelocity = Vector2.zero;
    bool flippedSprite = false;
    
    void Start()
    {
        spriteRigidBody = GetComponent<Rigidbody2D>();
        spriteAnimator = GetComponent<Animator>();
        spriteGravity = spriteRigidBody.gravityScale;
        groundMask = LayerMask.GetMask(groundLayers);
    }

    void FixedUpdate()
    {
        if (!isAlive) return;

        GroundCheck();
        SlopeCheck();
        HandleJump();
        HandleMove();
        FlipSprite();
        Climb();
        Die();
        SetAnimation();
    }

    void OnMove(InputValue value) 
    {
        if (!isAlive) return;
 
        moveInput = value.Get<Vector2>();
        // Debug.Log(moveInput);
    }

    void OnJump(InputValue value)
    {
        if (!isAlive) return;

        if (value.isPressed) {
            jumpInput = true;
            framesToHandleJump = maxJumpForgiveness;
        }
    }

    void OnFire(InputValue value) 
    {
        if (!isAlive) return;

        if (value.isPressed) {
            // Instantiate(bullet, gun.position, transform.rotation);
        }
    }

    private void SlopeCheck() {
        Vector2 checkPosition = groundCollider.bounds.center - new Vector3(0, groundCollider.bounds.size.y / 2);

        SlopeCheckVertical(checkPosition);
        SlopeCheckHorizontal(checkPosition);
    }

    private void SlopeCheckVertical(Vector2 checkPosition) {
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, slopeCheckDistance, groundMask);
        Debug.DrawRay(checkPosition, Vector2.down * slopeCheckDistance, Color.yellow, 1f);

        if (hit) {
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != slopeDownAngleOld) {
                onSlope = true;
            }
            slopeDownAngleOld = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerp, Color.magenta);
            Debug.DrawRay(hit.point, hit.normal, Color.green);
        }
    }

    private void SlopeCheckHorizontal(Vector2 checkPosition) {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPosition, transform.right, slopeCheckDistance, groundMask);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPosition, -transform.right, slopeCheckDistance, groundMask);

        if (slopeHitFront) {
            onSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);
        } else if (slopeHitBack) {
            onSlope = true;
            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        } else {
            slopeSideAngle = 0.0f;
            onSlope = false;
        }
    }

    private void GroundCheck() {

        // // Check grounded by casting a box downward and checking for collisions.  
        // Vector3 extents = groundCollider.bounds.extents;
        // Vector2 size = groundCollider.bounds.size;
        // Vector3 center = groundCollider.bounds.center;
        // RaycastHit2D raycastHit = Physics2D.BoxCast(new Vector2(center.x, center.y - extents.y/2), new Vector2(size.x - (2 * groundboxMargin), extents.y), 0f, Vector2.down, groundDistance, groundMask);

        // // The rest of this just draws an informative box for the debugger
        // Color debugColor = raycastHit.collider != null ? Color.cyan : Color.red;
        // Debug.DrawRay(center + new Vector3(extents.x - groundboxMargin, 0), Vector2.down * (extents.y + groundDistance), debugColor);
        // Debug.DrawRay(center - new Vector3(extents.x - groundboxMargin, 0), Vector2.down * (extents.y + groundDistance), debugColor);
        // Debug.DrawRay(center - new Vector3(extents.x - groundboxMargin, extents.y + groundDistance), Vector2.right * (size.x - groundboxMargin * 2), debugColor);
        // // Debug.DrawRay(raycastHit.point, raycastHit.normal, Color.green);

        // grounded = raycastHit.collider != null;

        Vector3 extents = groundCollider.bounds.extents;
        Vector3 center = groundCollider.bounds.center;
        float radius = extents.x - groundboxMargin;
        Vector3 circleOrigin = center - new Vector3(0, extents.y - extents.x + groundDistance);
        grounded = Physics2D.OverlapCircle(circleOrigin, radius, groundMask);

        Color debugColor = grounded ? Color.cyan : Color.red;
        Debug.DrawRay(circleOrigin, Vector2.left *  radius, debugColor);
        Vector2 diagLeft = Vector2.left + Vector2.down;
        diagLeft.Normalize();
        Debug.DrawRay(circleOrigin, diagLeft * radius, debugColor);
        Debug.DrawRay(circleOrigin, Vector2.down * radius, debugColor);
        Vector2 diagRight = Vector2.down + Vector2.right;
        diagRight.Normalize();
        Debug.DrawRay(circleOrigin, diagRight * radius, debugColor);
        Debug.DrawRay(circleOrigin, Vector2.right * radius, debugColor);

        if (spriteRigidBody.velocity.y <= 0.0f) {
            jumping = false;
        }
    }

    private void HandleMove() 
    {
        if (Mathf.Abs(moveInput.x) <= Mathf.Epsilon && grounded) {
            spriteRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            if (Mathf.Abs(newVelocity.x) > Mathf.Epsilon && onSlope) {
                newVelocity = Vector2.zero;
                spriteRigidBody.constraints |= RigidbodyConstraints2D.FreezePositionY;
            }
        } else {
            spriteRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

            newVelocity = new Vector2(runSpeed * moveInput.x, spriteRigidBody.velocity.y);
            // if (grounded && !onSlope && !jumping) {
            //     newVelocity.Set(runSpeed * moveInput.x, 0.0f);
            // } else 
            if (grounded && onSlope && !jumping) {
                newVelocity.Set(runSpeed * slopeNormalPerp.x * -moveInput.x, runSpeed * slopeNormalPerp.y * -moveInput.x);
            }

            spriteRigidBody.velocity = newVelocity;

        }
        // Consider this handled
        flippedSprite = true;

    }

    private void HandleJump() {
        if (jumpInput && grounded) {
            // Need this line so y velocity from going uphill doesn't get amplified by a jump
            spriteRigidBody.velocity = new Vector2(spriteRigidBody.velocity.x, 0.0f);
            spriteRigidBody.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
            jumpInput = false;
            jumping = true;
        } else if (framesToHandleJump > 0) {
            framesToHandleJump--;
        } else if (framesToHandleJump <= 0) {
            // Drop the jump input if we haven't handled it within the allowed frames
            jumpInput = false;
        }
    }

    private void SetAnimation() {
        bool touchingLadder = hitCollider.IsTouchingLayers(LayerMask.GetMask("Ladder"));
        bool hasXSpeed = Mathf.Abs(spriteRigidBody.velocity.x) > Mathf.Epsilon;
        bool hasYSpeed = Mathf.Abs(spriteRigidBody.velocity.y) > Mathf.Epsilon;
        spriteAnimator.SetFloat("xVelocity", hasXSpeed ? spriteRigidBody.velocity.x : 0);
        spriteAnimator.SetFloat("yVelocity", hasYSpeed ? spriteRigidBody.velocity.y : 0);
        spriteAnimator.SetBool("isJumping", !grounded && !touchingLadder);
        spriteAnimator.SetBool("isRunning", grounded && Mathf.Abs(moveInput.x) > Mathf.Epsilon);
        spriteAnimator.SetBool("isClimbing", hasYSpeed && !grounded && touchingLadder);
    }

    private void FlipSprite() 
    {
        bool spriteHasHorizontalSpeed = Mathf.Abs(spriteRigidBody.velocity.x) > Mathf.Epsilon;
        if (spriteHasHorizontalSpeed)
        {
            // Check the transform section of the player game object, set the Scale field to the sign of the velocity
            if (Mathf.Sign(spriteRigidBody.velocity.x) != Mathf.Sign(transform.localScale.x)) {
                // We are switching direction.  Slow the sprite for a few frames to stop the bounce on slopes.
                flippedSprite = true;

                transform.localScale = new Vector2 (Mathf.Sign(spriteRigidBody.velocity.x), 1f);
            }
        }
    }

    private void Climb() {
        // Check if the move input up or down direction are pressed.  If so and we are touching a ladder, climb.
        if (!hitCollider.IsTouchingLayers(LayerMask.GetMask("Ladder"))) {
            spriteRigidBody.gravityScale = spriteGravity;
            return;
        }

        Vector2 playerClimbVelocity = new Vector2(spriteRigidBody.velocity.x, moveInput.y * climbSpeed);
        spriteRigidBody.velocity = playerClimbVelocity;
        spriteRigidBody.gravityScale = 0f;

    }
    
    private void Die() {
        // if (hitCollider.IsTouchingLayers(LayerMask.GetMask("Enemies", "Hazards"))) {
        //     isAlive = false;
        //     playerAnimator.SetTrigger("Dying");
        //     // FindObjectOfType<GameSession>().ProcessPlayerDeath();
        // }
    }

}
