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
    public int maxJumps = 2; // double jump
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Wall Jump")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 16f;
    public Vector2 wallJumpAngle = new Vector2(1, 2);
    public float wallJumpDuration = 0.15f;

    [Header("Checks")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.1f;
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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Check ground & wall
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.Raycast(wallCheck.position, Vector2.right * transform.localScale.x, 0.1f, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            jumpsLeft = maxJumps;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressedTime = Time.time;
        }

        HandleJump();

        HandleWallSlide();

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

        float accelRate;

        if (isGrounded)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : deccel;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel * airControl : deccel * airControl;

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    void HandleJump()
    {
        // Coyote + Jump Buffer
        bool canJump = (Time.time - lastGroundedTime <= coyoteTime) || jumpsLeft > 0;
        bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;

        if (buffered && canJump)
        {
            Jump();
        }
    }

    void Jump()
    {
        // wall jump
        if (isWallSliding)
        {
            wallJumping = true;
            rb.velocity = new Vector2(wallJumpAngle.x * -transform.localScale.x * wallJumpForce,
                                      wallJumpAngle.y * wallJumpForce);

            Invoke(nameof(StopWallJump), wallJumpDuration);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        jumpsLeft--;
        lastJumpPressedTime = -999f;
    }

    void StopWallJump()
    {
        wallJumping = false;
    }

    void HandleWallSlide()
    {
        isWallSliding = false;

        if (!isGrounded && isTouchingWall && moveInput != 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
    }

    void FlipSprite()
    {
        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * transform.localScale.x * 0.1f);
    }
}
