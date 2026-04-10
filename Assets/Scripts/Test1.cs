//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Player : MonoBehaviour
//{
//    [Header("Movement")]
//    public float moveSpeed = 40f;
//    public float accel = 5f;
//    public float airaccel = 5f;
//    public float deccel = 15f;
//    public float airdeccel = 15f;
//    public float airControl = 0.5f;

//    [Header("Jump Settings")]
//    public float jumpForce = 6f;
//    public int maxJumps = 2;
//    public float coyoteTime = 0.15f;
//    public float jumpBufferTime = 0.15f;

//    [Header("Wall Jump")]
//    public float wallSlideSpeed = 0.5f;
//    public float wallJumpForce = 6f;
//    public Vector2 wallJumpAngle = new Vector2(1, 1);
//    public float wallJumpDuration = 0.15f;

//    [Header("Checks - Assign from LemonGuy's children")]
//    public Transform groundCheck;
//    public Transform wallCheckRight;
//    public Transform wallCheckLeft;
//    public Transform Top;
//    public float checkRadius = 0.1f;
//    public float groundCheckWidth = 0.5f; // New variable for box width
//    public float wallCheckRadius = 0.1f;
//    public LayerMask groundLayer;

//    [Header("References")]
//    public GameObject lemonGuyVisual;

//    private Rigidbody2D rb;
//    private SpriteRenderer spriteRenderer;
//    private Animator animator;
//    private int jumpsLeft;
//    private float lastGroundedTime;
//    private float lastJumpPressedTime;
//    private bool isGrounded;
//    private bool isTop;
//    private bool isTouchingWall;
//    private bool isWallSliding;
//    private bool wallJumping;
//    private float moveInput;
//    private int wallSide;
//    private bool wallRight;
//    private bool wallLeft;
//    private bool movingup = false;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody2D>();
//        spriteRenderer = GetComponent<SpriteRenderer>();

//        if (lemonGuyVisual == null)
//        {
//            lemonGuyVisual = transform.Find("LemonGuy")?.gameObject;
//            if (lemonGuyVisual == null)
//            {
//                Transform parent = transform.parent;
//                if (parent != null)
//                {
//                    lemonGuyVisual = parent.Find("LemonGuy")?.gameObject;
//                }
//            }
//        }

//        if (lemonGuyVisual != null)
//        {
//            animator = lemonGuyVisual.GetComponent<Animator>();
//        }

//        int groundLayerIndex = LayerMask.NameToLayer("Ground");
//        if (groundLayerIndex != -1)
//        {
//            groundLayer = 1 << groundLayerIndex;
//        }

//        jumpsLeft = maxJumps;
//        wallJumpAngle.Normalize();

//        if (rb != null)
//        {
//            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
//        }
//    }

//    void Update()
//    {
//        moveInput = Input.GetAxisRaw("Horizontal");

//        // Ground Check as a RECTANGLE
//        Vector2 groundBoxSize = new Vector2(groundCheckWidth, checkRadius);
//        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundLayer);

//        // Top Check remains a CIRCLE
//        isTop = Physics2D.OverlapCircle(Top.position, checkRadius, groundLayer);

//        RaycastHit2D wallRightHit = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckRadius, groundLayer);
//        RaycastHit2D wallLeftHit = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckRadius, groundLayer);

//        wallRight = wallRightHit.collider != null && !isGrounded;
//        wallLeft = wallLeftHit.collider != null && !isGrounded;
//        isTouchingWall = wallRight || wallLeft;

//        if (wallRight) wallSide = 1;
//        else if (wallLeft) wallSide = -1;
//        else wallSide = 0;

//        if (isGrounded)
//        {
//            lastGroundedTime = Time.time;
//            if (!wallJumping)
//            {
//                jumpsLeft = maxJumps;
//            }
//        }

//        if (Input.GetButtonDown("Jump"))
//        {
//            lastJumpPressedTime = Time.time;
//        }

//        movingup = Input.GetAxisRaw("Vertical") >= 0.2f;

//        if (isTop && movingup)
//        {
//            rb.gravityScale = 0;
//            rb.velocity = new Vector2(rb.velocity.x, 0);
//        }
//        else
//        {
//            rb.gravityScale = 1;
//        }

//        HandleJump();
//        HandleWallSlide();
//        FlipSprite();
//        // HandleWallRotationAnimation(); // Ensure this method exists in your full script
//    }

//    void FixedUpdate()
//    {
//        HandleMovement();
//    }

//    void HandleMovement()
//    {
//        if (wallJumping || rb == null) return;

//        float targetSpeed = moveInput * moveSpeed;
//        float speedDiff = targetSpeed - rb.velocity.x;
//        float accelRate;

//        if (isGrounded)
//            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accel : deccel;
//        else
//            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? airaccel * airControl : airdeccel * airControl;

//        rb.AddForce(speedDiff * Vector2.right * Time.fixedDeltaTime, ForceMode2D.Force);
//    }

//    void HandleJump()
//    {
//        if (rb == null) return;

//        bool jumpPressed = Input.GetButtonDown("Jump");
//        bool coyote = Time.time - lastGroundedTime <= coyoteTime;

//        if (jumpPressed)
//        {
//            if (isWallSliding) WallJump();
//            else if ((isGrounded || coyote || jumpsLeft > 0)) Jump();
//        }
//    }

//    void Jump()
//    {
//        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
//        jumpsLeft--;
//    }

//    void WallJump()
//    {
//        wallJumping = true;
//        float xDirection = -wallSide;
//        rb.velocity = new Vector2(xDirection * wallJumpAngle.x * wallJumpForce, wallJumpAngle.y * wallJumpForce);
//        Invoke("StopWallJump", wallJumpDuration);
//    }

//    void StopWallJump() { wallJumping = false; }

//    void HandleWallSlide()
//    {
//        isWallSliding = isTouchingWall && !isGrounded && rb.velocity.y < 0;
//        if (isWallSliding)
//        {
//            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
//        }
//    }

//    void FlipSprite()
//    {
//        if (moveInput > 0) spriteRenderer.flipX = false;
//        else if (moveInput < 0) spriteRenderer.flipX = true;
//    }

//    // VISUAL DEBUGGING
//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;

//        // Draw Ground Box
//        if (groundCheck != null)
//        {
//            Vector2 groundBoxSize = new Vector2(groundCheckWidth, checkRadius);
//            Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);
//        }

//        // Draw Top Circle
//        if (Top != null)
//        {
//            Gizmos.DrawWireSphere(Top.position, checkRadius);
//        }
//    }
//}
