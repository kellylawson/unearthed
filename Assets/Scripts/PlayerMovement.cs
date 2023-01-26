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
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] float groundDistance = .1f;
        // This margin is used to shrink the ground check cast box by a small amount on each side so that it doesn't detect walls as ground.
    [SerializeField] float groundboxMargin = 0.1f;
    [SerializeField] float maxAccelerateFrames = 10f;

    // [SerializeField] GameObject bullet;
    // [SerializeField] Transform gun;

    Vector2 moveInput;
    Rigidbody2D spriteRigidBody;
    Animator spriteAnimator;


    float spriteGravity;
    bool isAlive = true;
    bool jumpInput = false;
    bool grounded = true;
    float accelerateFrames = 0f;
    
    void Start()
    {
        spriteRigidBody = GetComponent<Rigidbody2D>();
        spriteAnimator = GetComponent<Animator>();
        spriteGravity = spriteRigidBody.gravityScale;
    }

    void FixedUpdate()
    {
        if (!isAlive) return;

        grounded = GroundCheck();
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

        // Checking if we were touching the ground layer seems unreliable here.
        if (value.isPressed && grounded) {
            jumpInput = true;
        }
    }

    void OnFire(InputValue value) 
    {
        if (!isAlive) return;

        if (value.isPressed) {
            // Instantiate(bullet, gun.position, transform.rotation);
        }
    }

    private bool GroundCheck() {

        // Check grounded by casting a box downward and checking for collisions.  
        Vector3 extents = groundCollider.bounds.extents;
        Vector2 size = groundCollider.bounds.size;
        Vector3 center = groundCollider.bounds.center;
        RaycastHit2D raycastHit = Physics2D.BoxCast(new Vector2(center.x, center.y - extents.y/2), new Vector2(size.x - (2 * groundboxMargin), extents.y), 0f, Vector2.down, groundDistance, LayerMask.GetMask("Ground"));

        // The rest of this just draws an informative box for the debugger
        Color debugColor = raycastHit.collider != null ? Color.cyan : Color.red;
        Debug.DrawRay(center + new Vector3(extents.x - groundboxMargin, 0), Vector2.down * (extents.y + groundDistance), debugColor);
        Debug.DrawRay(center - new Vector3(extents.x - groundboxMargin, 0), Vector2.down * (extents.y + groundDistance), debugColor);
        Debug.DrawRay(center - new Vector3(extents.x - groundboxMargin, extents.y + groundDistance), Vector2.right * (size.x - groundboxMargin * 2), debugColor);

        return raycastHit.collider != null;
    }

    private void HandleMove() 
    {
        if (Mathf.Abs(moveInput.x) <= Mathf.Epsilon && grounded) {
            spriteRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        } else {
            spriteRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            // When we switch direction we take a few frames to accelerate back to top speed to help with bouncing when change direction on a slope
            if (accelerateFrames > 0) --accelerateFrames;
            float adjustedRunSpeed = runSpeed * (1 - accelerateFrames/maxAccelerateFrames);
            spriteRigidBody.velocity = new Vector2(moveInput.x * adjustedRunSpeed, spriteRigidBody.velocity.y);
        }
    }

    private void HandleJump() {
        if (jumpInput) {
            // Need this line so y velocity from going uphill doesn't get amplified by a jump
            spriteRigidBody.velocity = new Vector2(spriteRigidBody.velocity.x, 0);
            spriteRigidBody.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
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
                accelerateFrames = maxAccelerateFrames;

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
