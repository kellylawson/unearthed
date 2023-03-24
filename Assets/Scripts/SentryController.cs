using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SentryController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private string groundLayers = "Ground";
    [SerializeField] private float boundaryCheckDepth = 0.5f;
    [SerializeField] private float boundarySlopeAngle = 10f;

    Rigidbody2D spriteRigidBody;
    Animator spriteAnimator;
    Collider2D spriteCollider;
    int groundMask;
    float velocity = 0f;

    void Start()
    {
        spriteRigidBody = GetComponent<Rigidbody2D>();
        spriteAnimator = GetComponent<Animator>();
        spriteCollider = GetComponent<Collider2D>();
        groundMask = LayerMask.GetMask(groundLayers);
        velocity = moveSpeed;
    }

    void FixedUpdate()
    {
        spriteAnimator.SetBool("walking", true);
        if (BoundaryCheck())
        {
            Debug.Log("Hit a boundary, flipping sprite");
            velocity = -velocity;
            FlipSprite();
        }
        spriteRigidBody.velocity = new Vector2(velocity, spriteRigidBody.velocity.y);
    }

    private bool BoundaryCheck()
    {
        // Use the sign of the velocity to determine which direction to start the raycast.
        Vector2 checkPosition = spriteCollider.bounds.center + new Vector3(Mathf.Sign(spriteRigidBody.velocity.x) * spriteCollider.bounds.extents.x, -spriteCollider.bounds.extents.y / 2);
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, spriteCollider.bounds.extents.y + spriteCollider.bounds.extents.y, groundMask);
        Debug.DrawRay(checkPosition, Vector2.down * spriteCollider.bounds.extents.y, Color.red);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(Vector2.up, hit.normal);
            Debug.Log($"Angle of the slope in radians is {slopeAngle}");
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
        bool spriteHasHorizontalSpeed = Mathf.Abs(spriteRigidBody.velocity.x) > Mathf.Epsilon;
        if (spriteHasHorizontalSpeed)
        {
            // Check the transform section of the player game object, set the Scale field to the sign of the velocity
            if (Mathf.Sign(spriteRigidBody.velocity.x) != Mathf.Sign(transform.localScale.x))
            {
                transform.localScale = new Vector2(Mathf.Sign(spriteRigidBody.velocity.x), 1f);
            }
        }
    }

}
