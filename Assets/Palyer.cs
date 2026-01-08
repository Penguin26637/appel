using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
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
    // Keep this check for general ground logic, but use a separate variable for sliding
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
    // Change wallSide from string "-1" to int -1
    private int wallSide; // 1 for right, -1 for left 
    
    // New variable to specifically track if we are fully off the ground
    private bool isFullyAirborne;
    
    // Track center ground check for edge sliding prevention
    private bool centerHit; 

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();
    }

    void Update() {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Checks using OverlapCircle
        bool leftHit = Physics2D.OverlapCircle(leftgroundCheck.position, checkRadius, groundLayer);
        bool rightHit = Physics2D.OverlapCircle(rightgroundCheck.position, checkRadius, groundLayer);
        centerHit = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer); // Removed redundant assignment

        // 'isGrounded' for general game logic (e.g., jump reset, movement accel/decel)
        isGrounded = centerHit || leftHit || rightHit;
        
        // 'isFullyAirborne' is true ONLY if all ground checks are false. 
        // Use this specific boolean to decide when to allow wall slides.
        isFullyAirborne = !centerHit && !leftHit && !rightHit;


        // Wall Check using OverlapCircle on both specific transforms
        bool wallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, groundLayer);
        bool wallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, groundLayer);
        isTouchingWall = wallRight || wallLeft;

        // Determine which side the wall is on for jumping mechanics
        if (wallRight) 
            wallSide = 1;
        else if (wallLeft) 
            // Fix string to int
            wallSide = -1;

        if (isGrounded) {
            lastGroundedTime = Time.time;
            if (!wallJumping) {
                jumpsLeft = maxJumps;
            }
        }

        if (Input.GetButtonDown("Jump")) {
            lastJumpPressedTime = Time.time;
        }

        HandleJump();
        HandleWallSlide();
        FlipSprite();
        HandleWallRotation();

        // Reset rotation if not touching wall and rotation is not 0
        if (!isTouchingWall && transform.localEulerAngles.z != 0f) {
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }

    void FixedUpdate() {
        HandleMovement();
    }

    void HandleWallRotation() {
        // Only apply rotation if fully airborne
        if (isFullyAirborne && isTouchingWall) { 
            // Fix string to float
            float targetZ = (wallSide == 1) ? -90f : 90f; 
            transform.localEulerAngles = new Vector3(0, 0, targetZ);
        } else if (transform.localEulerAngles.z != 0f) {
            transform.localEulerAngles = Vector3.zero;
        }
    }

    void HandleMovement() {
        if (wallJumping) return;

        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate;

        if (isGrounded) {
            // Fix capitalization
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : deccel; 
        } else {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel * airControl : deccel * airControl;
        }

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        
            // Stop horizontal momentum when losing ground at edges
            if (!isGrounded && centerHit) {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
    }

    void HandleJump() {
        bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

        if (buffered) {
            if (coyote || isWallSliding) { 
                Jump();
            } else if (!isGrounded && jumpsLeft > 0) {
                Jump();
            }
        }
    }

    void Jump() {
        if (isWallSliding) {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            wallJumping = true;
            // Fix string to int/float conversion
            rb.velocity = new Vector2(wallJumpAngle.x * -wallSide * wallJumpForce, wallJumpAngle.y * wallJumpForce); 
            Invoke(nameof(StopWallJump), wallJumpDuration);
            jumpsLeft--;
        } else {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            if (!isGrounded) {
                jumpsLeft--;
            }
        }
        // Fix string to float assignment
        lastJumpPressedTime = -999f; 
    }
// player is sliding off of the ledge when the ground check is false but is grounded is still true 
    void StopWallJump() => wallJumping = false;

    void HandleWallSlide() {
        isWallSliding = false;
        // Use isFullyAirborne to only wall slide when completely off the ground
        if (isFullyAirborne && isTouchingWall) { 
            isWallSliding = true;
            // Fix string to float
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue)); 
        }
    }

    void FlipSprite() {
        if (wallJumping) return;
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.DrawWireSphere(rightgroundCheck.position, checkRadius);
        Gizmos.DrawWireSphere(leftgroundCheck.position, checkRadius);
        Gizmos.color = Color.blue;
        if (wallCheckRight != null) Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        if (wallCheckLeft != null) Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
    }
}
