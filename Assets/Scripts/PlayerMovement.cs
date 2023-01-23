using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] float climbSpeed = 5f;
    // [SerializeField] GameObject bullet;
    // [SerializeField] Transform gun;

    Vector2 moveInput;
    Rigidbody2D playerRigidBody;
    Animator playerAnimator;
    CapsuleCollider2D playerCollider;
    // BoxCollider2D playerFeetCollider;
    float playerGravity;
    bool isAlive = true;
    bool jumped = false;
    bool playJump = false;
    bool canRun = true;
    
    void Start()
    {
        playerRigidBody = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider2D>();
        // playerFeetCollider = GetComponent<BoxCollider2D>();
        playerGravity = playerRigidBody.gravityScale;
    }

    void FixedUpdate()
    {
        if (!isAlive) return;

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
        if (value.isPressed) {
            // playerRigidBody.velocity += new Vector2(0f, jumpSpeed);
            jumped = true;
            canRun = false;
        }
    }

    void OnFire(InputValue value) 
    {
        if (!isAlive) return;

        if (value.isPressed) {
            // Instantiate(bullet, gun.position, transform.rotation);
        }
    }

    void HandleMove() 
    {
        // Can also use AddForce to move a rigidbody in a way that will work with the physics module better
        Vector2 playerVelocity = new Vector2(moveInput.x * runSpeed, playerRigidBody.velocity.y);
        playerRigidBody.velocity = playerVelocity;
        // if (Mathf.Abs(playerRigidBody.velocity.x) <= Mathf.Epsilon) {
        //     Debug.Log("Zero Velocity");
        // }
    }

    void HandleJump() {
        if (jumped && playerCollider.IsTouchingLayers(LayerMask.GetMask("Ground"))) {
            playerRigidBody.velocity = new Vector2(playerRigidBody.velocity.x, 0);
            playerRigidBody.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Impulse);
            jumped = false;
            playJump = true;
        }
    }

    void SetAnimation() {
        bool touchingGround = playerCollider.IsTouchingLayers(LayerMask.GetMask("Ground"));
        bool touchingLadder = playerCollider.IsTouchingLayers(LayerMask.GetMask("Ladder"));
        bool hasXSpeed = Mathf.Abs(playerRigidBody.velocity.x) > Mathf.Epsilon;
        bool hasYSpeed = Mathf.Abs(playerRigidBody.velocity.y) > Mathf.Epsilon;
        // if (hasYSpeed && !touchingGround && !touchingLadder) {
        //     Debug.Log($"Jumping ySpeed: {playerRigidBody.velocity.y} touchingGround: {touchingGround} touchingLadder: {touchingLadder}");
        // }
        playerAnimator.SetFloat("xVelocity", hasXSpeed ? playerRigidBody.velocity.x : 0);
        playerAnimator.SetFloat("yVelocity", hasYSpeed ? playerRigidBody.velocity.y : 0);
        // Wait until we are off the ground to play the animation so we can check for touching ground to leave the jump state
        if (playJump && !touchingGround) {
            playerAnimator.SetBool("isJumping", true);
            playJump = false;
        }
        if (touchingGround && !playJump) {
            playerAnimator.SetBool("isJumping", false);
            canRun = true;
        }
        bool isRunning = canRun && Mathf.Abs(moveInput.x) > Mathf.Epsilon;
        // Player walking on uneven surface says he's not touching ground
        // if (!touchingGround && hasXSpeed) {
        //     Debug.Log($"Not Running touchingGround: {touchingGround} xspeed: {moveInput.x}");
        // }
        playerAnimator.SetBool("isRunning", isRunning);
        playerAnimator.SetBool("isClimbing", hasYSpeed && !touchingGround && touchingLadder);
    }

    void FlipSprite() 
    {
        bool playerHasHorizontalSpeed = Mathf.Abs(playerRigidBody.velocity.x) > Mathf.Epsilon;
        if (playerHasHorizontalSpeed)
        {
            // Check the transform section of the player game object, set the Scale field to the sign of the velocity
            transform.localScale = new Vector2 (Mathf.Sign(playerRigidBody.velocity.x), 1f);
        }
    }

    void Climb() {
        // Check if the move input up or down direction are pressed.  If so and we are touching a ladder, climb.
        if (!playerCollider.IsTouchingLayers(LayerMask.GetMask("Ladder"))) {
            playerRigidBody.gravityScale = playerGravity;
            return;
        }

        Vector2 playerClimbVelocity = new Vector2(playerRigidBody.velocity.x, moveInput.y * climbSpeed);
        playerRigidBody.velocity = playerClimbVelocity;
        playerRigidBody.gravityScale = 0f;

    }
    
    void Die() {
        // if (playerCollider.IsTouchingLayers(LayerMask.GetMask("Enemies", "Hazards"))) {
        //     isAlive = false;
        //     playerAnimator.SetTrigger("Dying");
        //     // FindObjectOfType<GameSession>().ProcessPlayerDeath();
        // }
    }

}

/*
    Figuring out the state of the character.  
    - Jumping has y velocity up and not touching the ground with the feet collider.  
    - Falling has y velocity down and not touching the ground with the feet collider.  
    - Running has x velocity right or left.  
    - Flipped sprite has x velocity left for running and jumping.
    - Climbing is touching a climb surface and not touching the ground with the feet collider and not jumping/falling.
    - Climbing and looking left/right has left/right input.
    - Climbing and not falling requires touching a climb surface.

    Requirements
    - Knowing vertical and horizontal speed on each update.
    - Know if touching a climbable surface and/or if feet touching ground. 
*/
