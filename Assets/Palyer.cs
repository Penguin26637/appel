using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Palyer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float accel = 15f;
    public float deccel = 15f;
    public float airControl = 0.5f;

    [Header("Jump Settings")]
    public float jumpForce = 14f;
    public int maxJumps = 2;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Wall Jump")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 16f;
    public Vector2 wallJumpAngle = new Vector2(1, 2);
    public float wallJumpDuration = 0.15f;

    [Header("Checks")]
    public Transform groundCheck;
    public Transform leftWallCheck;
    public Transform rightWallCheck;
    public float checkRadius = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private int jumpsLeft;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    private bool isGrounded;
    private bool wasGrounded;

    private bool touchingLeftWall;
    private bool touchingRightWall;
    private bool isWallSliding;
    private bool wallJumping;

    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground check
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (isGrounded && !wasGrounded)
        {
            lastGroundedTime = Time.time;
            jumpsLeft = maxJumps;
            transform.rotation = Quaternion.identity;
        }
        wasGrounded = isGrounded;

        // Wall checks
        touchingLeftWall = Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, groundLayer);
        touchingRightWall = Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, groundLayer);

        // Jump input (Space + Controller A)
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
        if (jumpPressed)
            lastJumpPressedTime = Time.time;

        HandleJump();
        HandleWallSlide();
        HandleWallRotation();
        FlipSprite();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (wallJumping) return;

        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;

        float accelRate = isGrounded
            ? (Mathf.Abs(targetSpeed) > 0.01f ? accel : deccel)
            : (Mathf.Abs(targetSpeed) > 0.01f ? accel * airControl : deccel * airControl);

        rb.AddForce(speedDiff * accelRate * Time.fixedDeltaTime * Vector2.right);
    }

    void HandleJump()
    {
        bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool canJump = (Time.time - lastGroundedTime <= coyoteTime) || jumpsLeft > 0;

        if (!buffered || !canJump) return;

        Jump();
        lastJumpPressedTime = -999f; // consume buffer
    }

    void Jump()
    {
        if (isWallSliding)
        {
            wallJumping = true;
            int wallDir = touchingLeftWall ? 1 : -1;

            rb.velocity = new Vector2(
                wallJumpAngle.x * wallDir * wallJumpForce,
                wallJumpAngle.y * wallJumpForce
            );

            jumpsLeft--;
            Invoke(nameof(StopWallJump), wallJumpDuration);
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        if (!isGrounded)
            jumpsLeft--;
    }

    void StopWallJump()
    {
        wallJumping = false;
    }

    void HandleWallSlide()
    {
        isWallSliding = false;

        if (!isGrounded && rb.velocity.y < 0 && (touchingLeftWall || touchingRightWall))
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }
    }

    void HandleWallRotation()
    {
        if (!isWallSliding) return;

        if (touchingLeftWall)
            transform.rotation = Quaternion.Euler(0, 0, 90);
        else if (touchingRightWall)
            transform.rotation = Quaternion.Euler(0, 0, -90);
    }

    void FlipSprite()
    {
        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftWallCheck.position, checkRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(rightWallCheck.position, checkRadius);
    }
}
