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
    public Transform rightgroundCheck;
    public Transform leftgroundCheck;
    public Transform groundCheck; // Center ground check
    public Transform wallCheckRight;
    public Transform wallCheckLeft;
    public float checkRadius = 0.1f;
    public float wallCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Visuals")]
    public float wallTiltAngle = 90f;

    private Rigidbody2D rb;
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

    public float rotationSpeed = 20f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();
        
        // Ensure rotation is frozen on Z-axis to prevent physical tipping
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Checks
        bool leftHit = Physics2D.OverlapCircle(leftgroundCheck.position, checkRadius, groundLayer);
        bool rightHit = Physics2D.OverlapCircle(rightgroundCheck.position, checkRadius, groundLayer);
        centerHit = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // General grounded logic for jumping
        isGrounded = centerHit || leftHit || rightHit;

        // Wall Checks
        bool wallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, groundLayer);
        //print("wallRight" + wallRight);
        bool wallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, groundLayer);
        //print("wallLeft" + wallLeft);
        isTouchingWall = wallRight || wallLeft;

        if (wallRight) wallSide = 1;
        else if (wallLeft) wallSide = -1;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wallJumping) jumpsLeft = maxJumps;
        }

        if (Input.GetButtonDown("Jump")) lastJumpPressedTime = Time.time;

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
        if (centerHit && Mathf.Abs(moveInput) < 0.01f)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
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
            rb.velocity = new Vector2(wallJumpAngle.x * -wallSide * wallJumpForce, wallJumpAngle.y * wallJumpForce);
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
        
        //if (!isGrounded && isTouchingWall && rb.velocity.y < 0)
        if (!isGrounded && isTouchingWall)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else { isWallSliding = false; }
    }

    void FlipSprite()
    {
        // 1. Horizontal Flip
        if (!wallJumping)
        {
            if (moveInput > 0) GetComponent<SpriteRenderer>().flipX = false;
            else if (moveInput < 0) GetComponent<SpriteRenderer>().flipX = true;
        }
        
        // 2. Z-Axis Rotation (Wall Tilt)
        if (isWallSliding)
        {
            float targetZ = wallSide * wallTiltAngle;
            float newRotation = rb.rotation + rotationSpeed * Time.fixedDeltaTime;
            //// Apply the new rotation
            //rb.MoveRotation(newRotation*wallSide);
            float currentZ = rb.rotation;
            float newZ = Mathf.MoveTowardsAngle(currentZ, wallTiltAngle, rotationSpeed*Time.deltaTime);
            rb.rotation = (newZ);
            print("Roation");
        }
        //else
        //{
        //    //rb.MoveRotation = (0);
        //}
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.DrawWireSphere(rightgroundCheck.position, checkRadius);
        Gizmos.DrawWireSphere(leftgroundCheck.position, checkRadius);
        Gizmos.color = Color.blue;
        if (wallCheckRight != null) Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        if (wallCheckLeft != null) Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
    }
}
