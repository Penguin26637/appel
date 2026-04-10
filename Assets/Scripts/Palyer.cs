using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    // public float accel = 5f;
    public float airaccel = 5f;
    // public float deccel = 15f;
    public float airdeccel = 15f;
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
    public float groundCheckWidth = 0.5f;
    public float topCheckWidth = 0.5f;
    public float wallCheckRadius = 0.1f;
    public float wallCheckHeight = 0.5f;
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

        // Ground Check (bottom) - RECTANGLE
        Vector2 groundBoxSize = new Vector2(groundCheckWidth, checkRadius);
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundLayer);

        // Top Check - RECTANGLE
        Vector2 topBoxSize = new Vector2(topCheckWidth, checkRadius);
        isTop = Physics2D.OverlapBox(Top.position, topBoxSize, 0f, groundLayer);

        // Wall Checks - RECTANGLES
        Vector2 wallBoxSize = new Vector2(wallCheckRadius * 2f, wallCheckHeight);
        Vector2 wallRightCenter = (Vector2)wallCheckRight.position + Vector2.right * wallCheckRadius;
        Vector2 wallLeftCenter = (Vector2)wallCheckLeft.position + Vector2.left * wallCheckRadius;
        wallRight = Physics2D.OverlapBox(wallRightCenter, wallBoxSize, 0f, groundLayer) != null && !isGrounded;
        wallLeft = Physics2D.OverlapBox(wallLeftCenter, wallBoxSize, 0f, groundLayer) != null && !isGrounded;

        isTouchingWall = wallRight || wallLeft;
        // print("left" + wallRight);
        // print("right" + wallLeft);
        // print("ground" + isGrounded);

        // print("jumpsLeft: " + jumpsLeft);
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

        // Detect jump input
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpPressedTime = Time.time;
        }

        if (Input.GetAxisRaw("Vertical") >= 0.2)
        {
            movingup = true;
        }
        else
        {
            movingup = false;
        }

        if (isTop == true && movingup)
        {
            // Stick to roof by disabling gravity and zeroing velocity
            jumpsLeft = maxJumps;
            rb.gravityScale = 0;
            rb.velocity = new Vector2(rb.velocity.x, 0);
        }
        else
        {
            // Re-enable gravity when not sticking to roof
            rb.gravityScale = 1;
        }

        // print(wallLeft || wallRight);

        HandleWallSlide();
        HandleJump();
        FlipSprite();
        // HandleWallRotationAnimation(); // Note: This method was referenced but not defined in your snippet
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (wallJumping || rb == null) return;
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // float targetSpeed = moveInput * moveSpeed;
        // float speedDiff = targetSpeed - rb.velocity.x;
        // float accelRate;

        // Use different acceleration rates for ground vs air
        // if (isGrounded)
        // {
        //     accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : deccel;
        // }
        // else
        // {
        //     accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? airaccel * airControl : airdeccel * airControl;
        // }

        // float movement = speedDiff * Time.fixedDeltaTime;
        // rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    void HandleJump()
    {
        if (rb == null) return;

        bool jumpPressed = Input.GetButtonDown("Jump");
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

        if (jumpPressed)
        {
            if (isWallSliding)
            {
                WallJump();
            }
            else if ((isGrounded || coyote) && jumpsLeft > 0)
            {
                Jump();
            }
            else if (!isGrounded && jumpsLeft > 0)
            {
                Jump();
            }
        }
    }

    void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpsLeft--;
    }

    void WallJump()
    {
        wallJumping = true;
        float xDirection = -wallSide;
        rb.velocity = new Vector2(
            xDirection * wallJumpAngle.x * wallJumpForce,
            wallJumpAngle.y * wallJumpForce
        );
        Invoke("StopWallJump", wallJumpDuration);
    }

    void StopWallJump()
    {
        wallJumping = false;
    }

    void HandleWallSlide()
    {
        if (isTouchingWall && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }

        if (wallSide == -1) // Wall on left
        {
            animator.SetBool("Left_Active", true);
            animator.SetBool("Right_Active", false);
            jumpsLeft = maxJumps;
        }
        else if (wallSide == 1) // Wall on right
        {
            animator.SetBool("Left_Active", false);
            animator.SetBool("Right_Active", true);
            jumpsLeft = maxJumps;
        }
        else
        {
            // Not touching wall - reset parameters
            animator.SetBool("Left_Active", false);
            animator.SetBool("Right_Active", false);
        }
    }
    
    void FlipSprite()
    {
        if (moveInput > 0) spriteRenderer.flipX = false;
        else if (moveInput < 0) spriteRenderer.flipX = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (groundCheck != null)
        {
            Vector2 groundBoxSize = new Vector2(groundCheckWidth, checkRadius);
            Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);
        }
        if (Top != null)
        {
            Gizmos.color = Color.blue;
            Vector2 topBoxSize = new Vector2(topCheckWidth, checkRadius);
            Gizmos.DrawWireCube(Top.position, topBoxSize);
        }
        if (wallCheckRight != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 wallBoxSize = new Vector2(wallCheckRadius * 2f, wallCheckHeight);
            Vector2 wallRightCenter = (Vector2)wallCheckRight.position + Vector2.right * wallCheckRadius;
            Gizmos.DrawWireCube(wallRightCenter, wallBoxSize);
        }
        if (wallCheckLeft != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 wallBoxSize = new Vector2(wallCheckRadius * 2f, wallCheckHeight);
            Vector2 wallLeftCenter = (Vector2)wallCheckLeft.position + Vector2.left * wallCheckRadius;
            Gizmos.DrawWireCube(wallLeftCenter, wallBoxSize);
        }
    }
}
