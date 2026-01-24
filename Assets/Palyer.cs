using System;
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
    public Transform groundCheck; // Center ground check
    public Transform wallCheckRight;
    public Transform wallCheckLeft;
    public float checkRadius = 0.1f;
    public float wallCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Visuals")]
    public float wallTiltAngle = 90f;

    private Rigidbody2D rb;
    private Animator animator;
    private int jumpsLeft;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool wallJumping;
    private float moveInput;
    private int wallSide;
    private bool centerHit;

    public bool wallRight;
    public bool wallLeft;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();

        // Ensure rotation is frozen on Z-axis to prevent physical tipping
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        animator = GetComponent<Animator>();

    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Check
        centerHit = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isGrounded = centerHit;

        // Wall Checks - Use horizontal raycasts instead of overlap circles
        RaycastHit2D wallRightHit = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckRadius, groundLayer);
        RaycastHit2D wallLeftHit = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckRadius, groundLayer);


        wallRight = wallRightHit.collider != null && !isGrounded;
        wallLeft = wallLeftHit.collider != null && !isGrounded;


        isTouchingWall = wallRight || wallLeft;

        if (wallRight) wallSide = -1;
        else if (wallLeft) wallSide = 1;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wallJumping) jumpsLeft = maxJumps;
        }

        if (Input.GetButtonDown("Jump")) lastJumpPressedTime = Time.time;

        HandleJump();
        HandleWallSlide();
        FlipSprite();
        HandleWallRotationAnimation();
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
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : deccel;
        }
        else
        {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel * airControl : deccel * airControl;
        }

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // EDGE SLIDING PREVENTION:
        // If center is grounded but sides are not, and there is no input, kill horizontal velocity
        //if (centerHit && Mathf.Abs(moveInput) < 0.01f)
        //{
        //    rb.velocity = new Vector2(0, rb.velocity.y);
        //}
    }

    void HandleJump()
    {
        bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

        if (buffered)
        {
            if (coyote || isWallSliding) Jump();
            else if (!isGrounded && jumpsLeft > 0) Jump();
        }
    }

    void Jump()
    {
        if (isWallSliding)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            wallJumping = true;
            rb.velocity = new Vector2(wallJumpAngle.x * wallJumpForce, wallJumpAngle.y * wallJumpForce);
            Invoke(nameof(StopWallJump), wallJumpDuration);
            jumpsLeft--;

        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            if (!isGrounded) jumpsLeft--;
        }
        lastJumpPressedTime = -999f;
    }

    void StopWallJump() => wallJumping = false;

    void HandleWallSlide()
    {
        if (!isGrounded && isTouchingWall && rb.velocity.y < 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    void FlipSprite()
    {
        // Horizontal Flip
        if (!wallJumping)
        {
            if (moveInput > 0) GetComponent<SpriteRenderer>().flipX = false;
            else if (moveInput < 0) GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    void HandleWallRotationAnimation()
    {
        if (animator == null) return;

        

        // Trigger wall rotation animations
        if (isTouchingWall && !isGrounded && !(rb.rotation > 0) || !(rb.rotation < 0))
        {
            if (wallSide == -1) // Right wall (rotate left)
            {
                animator.Play("playerrotatelef");
                wallRight = true;
            }
            else if (wallSide == 1) // Left wall (rotate right)
            {
                animator.Play("playerrotateright");
                wallLeft = true;
            }

            if (rb.rotation == 90 || rb.rotation == -90 && wallCheckLeft || wallCheckRight)
            {
                animator.Play("Idle");
            }
                



            //    if (rb.rotation == 90 || rb.rotation == -90)
            //{
            //    animator.SetBool("Left_Active", wallLeft);

            //    animator.SetBool("Right_Active", wallRight);
            //}
        }
        
        //else
        //{
        //    // Return to idle/normal state - reset rotation
        //    // You can create a "playerrotatenormal" animation or just reset transform
        //    transform.rotation = Quaternion.Euler(0, 0, 0);
        //}
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

        Gizmos.color = Color.blue;
        if (wallCheckRight != null)
        {
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
            Gizmos.DrawRay(wallCheckRight.position, Vector2.right * wallCheckRadius);
        }
        if (wallCheckLeft != null)
        {
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
            Gizmos.DrawRay(wallCheckLeft.position, Vector2.left * wallCheckRadius);
        }
    }
}