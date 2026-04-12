using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 20f;
    public bool isGrounded = false;
    public float gravity = -10f;
    
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    public float jumpTime = 0.35f;
    private float jumpTimeCounter;
    public float input;

    public float sensibility = 0.7f;
    
    public float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;
    
    private bool isWallSliding;
    private float wallSlidingSpeed = 2f;

    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.3f;
    private Vector3 wallJumpingPower = new Vector3(8f, 8f);
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody rb;
    

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        float moveHorizontal = input;

        float targetX = moveHorizontal * speed;

        float airControl = isGrounded ? 0.8f : 0.05f;

        float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, airControl);

        if (!isWallJumping)
        {
            rb.linearVelocity = new Vector3(newX, rb.linearVelocity.y);
        }
        rb.linearVelocity += new Vector3(0, gravity * Time.fixedDeltaTime);

        jumpBufferCounter -= Time.fixedDeltaTime;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        WallSlide();
        WallJump();

        if (!isWallJumping)
        {
            Flip();
        }

        JumpHandler();
    }

    public void Flip()
    {
        if (input > 0) transform.localScale = new Vector3(1, 2, 1);
        else if (input < 0) transform.localScale = new Vector3(-1, 2, 1);
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>().x;
    }

    public void OnJump()
    {
        jumpBufferCounter = jumpBufferTime;
        if(wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.linearVelocity = new Vector3(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if(transform.localScale.x != wallJumpingDirection)
            {
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    public void JumpHandler()
    {
        if (jumpBufferCounter <= 0f) return;

        if (coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            coyoteTimeCounter = 0f;
            jumpTimeCounter = jumpTime;
        }

    }

    private bool IsWalled()
    {
        return Physics.CheckSphere(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if(IsWalled() && !isGrounded && input != 0f)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach(ContactPoint contact in collision.contacts)
            {
                if(contact.normal.y > sensibility)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > sensibility)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}