using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 40f;
    public float accel = 5f;
    public float deccel = 15f;
    public float airControl = 0.5f;

    [Header("Jump Settings")]
    public float jumpForce = 6f;
    public int maxJumps = 2;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Wall Jump")]
    public float wallSlideSpeed = 0.5f;
    public float wallJumpForce = 6f;
    public Vector2 wallJumpAngle = new Vector2(0, 0);
    public float wallJumpDuration = 0.15f;

    [Header("Checks")]
    public Transform groundCheck;
    public Transform wallCheckRight;
    public Transform wallCheckLeft;
    public float checkRadius = 0.1f;
    public float wallCheckRadius = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private int jumpsLeft;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool wallJumping;
    private float moveInput;
    private int wallSide; // 1 for right, -1 for left

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();
    }

    void Update()
    {
        // Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        // if (hit != null) {
        //     print("Hitting: " + hit.name);
        // }

        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // Wall Check using OverlapCircle on both specific transforms
        bool wallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, groundLayer);
        bool wallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, groundLayer);
        isTouchingWall = wallRight || wallLeft;

        // Determine which side the wall is on for jumping mechanics
        if (wallRight) wallSide = 1;
        else if (wallLeft) wallSide = -1;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            // Reset jumps only if we aren't currently wall jumping (prevents instant double jump after wall jump)
            if (!wallJumping)
            {
                jumpsLeft = maxJumps;
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            lastJumpPressedTime = Time.time;
        }

        HandleJump();
        HandleWallSlide();
        FlipSprite();
        HandleWallRotation();
        // Reset rotation if not touching wall and rotation is not 0
        if (!isTouchingWall && transform.localEulerAngles.z != 0f)
        {
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }
void HandleWallRotation()
{
    if (isTouchingWall && !isGrounded)
    {
        // Rotate -90 degrees if touching right wall, 90 if touching left wall
        float targetZ = (wallSide == 1) ? -90f : 90f;
        transform.localEulerAngles = new Vector3(0, 0, targetZ);
    }
    else if (transform.localEulerAngles.z != 0f)
    {
        // Reset rotation when not on a wall
        transform.localEulerAngles = Vector3.zero;
    }
}
void HandleMovement() {
    if (wallJumping) return;

    float targetSpeed = moveInput * moveSpeed;
    float speedDiff = targetSpeed - rb.velocity.x;
    float accelRate;

    if (isGrounded) {
        accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : deccel;
        // print("Grounded"); // Capital D and inside braces
    } else {
        accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel * airControl : deccel * airControl;
    }

    float movement = speedDiff * accelRate * Time.fixedDeltaTime;
    rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
}

    void HandleJump()
    {
        bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

        if (buffered)
        {
            // Ground/Coyote Jump
            if (coyote || isWallSliding) // Allow wall jump via buffer
            {
                Jump();
            }
            // Air Jump
            else if (!isGrounded && jumpsLeft > 0)
            {
                Jump();
            }
        }
    }

    void Jump()
    {
        // If wall sliding, perform a wall jump
        if (isWallSliding)
        {
            // Reset vertical velocity for consistent jump feel
            rb.velocity = new Vector2(rb.velocity.x, 0); 
            wallJumping = true;
            // Jump away from the wall side detected
            rb.velocity = new Vector2(wallJumpAngle.x * -wallSide * wallJumpForce, wallJumpAngle.y * wallJumpForce);
            Invoke(nameof(StopWallJump), wallJumpDuration);
            // After wall jump, count it as an air jump
            jumpsLeft--; 
        }
        else
        {
            // Regular ground or air jump logic
            // Reset current vertical velocity to ensure consistent jump height
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            if (!isGrounded) // Only decrement for air jumps (ground jumps reset jumpsLeft in Update)
            {
                 jumpsLeft--;
                //  print("Air jump performed");
            }
        }
        
        lastJumpPressedTime = -999f; // Reset buffer after successful jump
    }

    void StopWallJump() => wallJumping = false;

    void HandleWallSlide()
    {
        isWallSliding = false;
        // Slide if hitting a wall in mid-air (no need to check moveInput to simply *stick* to the wall)
        if (!isGrounded && isTouchingWall)
        {
            isWallSliding = true;
            // Clamping the downward speed
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
    }

    void FlipSprite()
    {
        // Don't flip while wall jumping to prevent jitter
        if (wallJumping) return;

        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    // Draw Gizmos for the new check points
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.color = Color.blue;
        if (wallCheckRight != null)
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        if (wallCheckLeft != null)
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
    }
}
