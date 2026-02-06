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
    public Vector2 wallJumpAngle = new Vector2(1, 1);
    public float wallJumpDuration = 0.15f;

    [Header("Checks - Assign from LemonGuy's children")]
    public Transform groundCheck; // Assign "Ground" from LemonGuy children
    public Transform wallCheckRight; // Assign "Right" or "RightGround" 
    public Transform wallCheckLeft; // Assign "Left" or "LeftGround"
    public Transform Top;
    public float checkRadius = 0.1f;
    public float wallCheckRadius = 0.1f;
    public LayerMask groundLayer; // Set to the layer your platforms/walls use

    [Header("References")]
    public GameObject lemonGuyVisual; // Assign LemonGuy object for animations

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private int jumpsLeft;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private bool isGrounded;
    private bool isTop;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool wallJumping;
    private float moveInput;
    private int wallSide;
    private bool wallRight;
    private bool wallLeft;
    private bool movingup = false;

    void Start()
    {
        // Get Rigidbody2D from this object (LemonGut)
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-find LemonGuy if not assigned
        if (lemonGuyVisual == null)
        {
            lemonGuyVisual = transform.Find("LemonGuy")?.gameObject;
            if (lemonGuyVisual == null)
            {
                // Try finding it as a sibling instead of child
                Transform parent = transform.parent;
                if (parent != null)
                {
                    lemonGuyVisual = parent.Find("LemonGuy")?.gameObject;
                }
            }
        }

        // Get Animator from LemonGuy visual object
        if (lemonGuyVisual != null)
        {
            animator = lemonGuyVisual.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator not found on LemonGuy!");
            }
        }
        else
        {
            Debug.LogError("LemonGuy visual object not found! Please assign it in the Inspector.");
        }

        // Set ground layer to "Ground" layer by name
        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex != -1)
        {
            groundLayer = 1 << groundLayerIndex;
        }
        else
        {
            Debug.LogWarning("Layer 'Ground' not found! Please create a layer named 'Ground' or manually set groundLayer in Inspector.");
        }

        // Validation
        if (rb == null)
            Debug.LogError("Rigidbody2D not found on " + gameObject.name);
        if (groundCheck == null)
            Debug.LogError("Ground check not assigned! Assign Ground child from LemonGuy");
        if (wallCheckRight == null)
            Debug.LogError("Right wall check not assigned!");
        if (wallCheckLeft == null)
            Debug.LogError("Left wall check not assigned!");
        if (Top == null)
            Debug.LogError("Top Wall Check not assigned!");

        jumpsLeft = maxJumps;
        wallJumpAngle.Normalize();

        // Freeze rotation
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Ground Check using Ground child position from LemonGuy
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        isTop = Physics2D.OverlapCircle(Top.position, checkRadius, groundLayer);

        // Wall Checks using Left and Right child positions from LemonGuy
        RaycastHit2D wallRightHit = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckRadius, groundLayer);
        RaycastHit2D wallLeftHit = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckRadius, groundLayer);

        wallRight = wallRightHit.collider != null && !isGrounded;
        wallLeft = wallLeftHit.collider != null && !isGrounded;

        isTouchingWall = wallRight || wallLeft;

        // Determine which side of wall we're on
        if (wallRight) wallSide = 1; // Wall is on right side
        else if (wallLeft) wallSide = -1; // Wall is on left side
        else wallSide = 0;

        // Reset jumps when grounded
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wallJumping)
            {
                jumpsLeft = maxJumps;
            }
        }



        if (Input.GetAxisRaw("Vertical") >= 0.2) {
            movingup = true;
        } else {
            movingup = false;
        }

        print("Moving up" + movingup);
        print("Axis Raw" + Input.GetAxisRaw("Vertical"));
                
        if (isTop == true && movingup)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            // keep player in place

            // Detect jump input
            if (Input.GetButtonDown("Jump"))
            {
                lastJumpPressedTime = Time.time;
            }
        }

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
        if (wallJumping || rb == null) return;

        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate;

        // Use different acceleration rates for ground vs air
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
    }

    void HandleJump()
    {
        if (rb == null) return;

        bool jumpPressed = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

        if (jumpPressed)
        {
            // Priority 1: Wall jump
            if (isWallSliding)
            {
                WallJump();
            }
            // Priority 2: Ground jump (including coyote time)
            else if (isGrounded || coyote)
            {
                if (jumpsLeft == maxJumps) // Only jump if we haven't used jumps yet
                {
                    Jump();
                }
            }
            // Priority 3: Air jump (double jump, triple jump, etc.)
            else if (!isGrounded && jumpsLeft > 0)
            {
                Jump();
            }
        }
    }

    void Jump()
    {
        // Set Y velocity to jump force, keep X velocity
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        // Only decrease jumps if we're in the air
        if (!isGrounded)
        {
            jumpsLeft--;
        }

        lastJumpPressedTime = -999f; // Reset jump buffer
    }

    void WallJump()
    {
        wallJumping = true;

        // Jump away from the wall
        // If wall is on left (wallSide = -1), jump right (+1)
        // If wall is on right (wallSide = 1), jump left (-1)
        float xDirection = -wallSide;

        rb.velocity = new Vector2(
            xDirection * wallJumpAngle.x * wallJumpForce,
            wallJumpAngle.y * wallJumpForce
        );

        // Reset jumps after wall jump
        jumpsLeft = maxJumps - 1;

        lastJumpPressedTime = -999f; // Reset jump buffer

        Invoke(nameof(StopWallJump), wallJumpDuration);
    }

    void StopWallJump()
    {
        wallJumping = false;
    }

    void HandleWallSlide()
    {
        if (rb == null) return;

        // Wall slide conditions:
        // 1. Not grounded
        // 2. Touching a wall
        // 3. Moving downward
        if (!isGrounded && isTouchingWall && rb.velocity.y < 0)
        {
            isWallSliding = true;
            // Limit fall speed when wall sliding
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    void FlipSprite()
    {
        if (wallJumping) return;

        // Flip the visual sprite (on LemonGuy if assigned, otherwise on this object)
        SpriteRenderer targetSprite = spriteRenderer;
        if (lemonGuyVisual != null)
        {
            SpriteRenderer visualSprite = lemonGuyVisual.GetComponent<SpriteRenderer>();
            if (visualSprite != null) targetSprite = visualSprite;
        }

        if (targetSprite == null) return;

        // Flip sprite based on movement direction
        if (moveInput > 0)
        {
            targetSprite.flipX = false;
        }
        else if (moveInput < 0)
        {
            targetSprite.flipX = true;
        }
    }

    void HandleWallRotationAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is null!");
            return;
        }

        // Debug logging - check wall detection
        if (isTouchingWall)
        {
            Debug.Log($"Wall detected! wallLeft: {wallLeft}, wallRight: {wallRight}, wallSide: {wallSide}, isGrounded: {isGrounded}");
        }

        // Set animator parameters based on wall touching state
        if (isTouchingWall && !isGrounded)
        {
            if (wallSide == -1) // Wall on left
            {
                Debug.Log("Setting Left_Active = true, Right_Active = false");
                animator.SetBool("Left_Active", true);
                animator.SetBool("Right_Active", false);
            }
            else if (wallSide == 1) // Wall on right
            {
                Debug.Log("Setting Left_Active = false, Right_Active = true");
                animator.SetBool("Left_Active", false);
                animator.SetBool("Right_Active", true);
            }
        }
        else
        {
            // Not touching wall - reset parameters
            animator.SetBool("Left_Active", false);
            animator.SetBool("Right_Active", false);
        }

        if (animator.GetBool("Left_Active") || animator.GetBool("Right_Active"))
        {
            jumpsLeft = 2;
        }

        // Debug: Show current parameter values
        bool leftActive = animator.GetBool("Left_Active");
        bool rightActive = animator.GetBool("Right_Active");
        Debug.Log($"Current animator state: Left_Active = {leftActive}, Right_Active = {rightActive}");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
            Gizmos.DrawWireSphere(Top.position, checkRadius);
        }

        // Draw right wall check
        if (wallCheckRight != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
            Gizmos.DrawRay(wallCheckRight.position, Vector2.right * wallCheckRadius);
        }   

        // Draw left wall check
        if (wallCheckLeft != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
            Gizmos.DrawRay(wallCheckLeft.position, Vector2.left * wallCheckRadius);
        }
    }
}