# unearthed
2D platformer

# For reference, ChatGPT controller
```
using UnityEngine;

public class CharacterController2D : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float jumpForce = 500f;
    public float climbSpeed = 5f;

    private Rigidbody2D rb;
    private bool isJumping = false;
    private bool isClimbing = false;
    private bool isAttacking = false;
    private float attackTimer = 0;
    private float attackCoolDown = 0.3f;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Jump") && !isJumping)
        {
            isJumping = true;
            rb.AddForce(new Vector2(0f, jumpForce));
            animator.SetBool("isJumping", true);
        }

        if (Input.GetButtonDown("Attack") && !isAttacking)
        {
            isAttacking = true;
            attackTimer = attackCoolDown;
            animator.SetBool("isAttacking", true);
        }

        if (Input.GetButton("Climb"))
        {
            isClimbing = true;
            animator.SetBool("isClimbing", true);
            rb.velocity = new Vector2(rb.velocity.x, Input.GetAxis("Vertical") * climbSpeed);
        }
        else
        {
            isClimbing = false;
            animator.SetBool("isClimbing", false);
        }

        if (isAttacking)
        {
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }
            else
            {
                isAttacking = false;
                animator.SetBool("isAttacking", false);
            }
        }

        rb.velocity = new Vector2(moveX * moveSpeed, rb.velocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
            animator.SetBool("isJumping", false);
        }
    }
}
```