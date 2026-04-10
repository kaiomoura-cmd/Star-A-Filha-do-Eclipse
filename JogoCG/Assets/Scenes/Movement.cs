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
    private bool isJumping;

    public float input;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float moveHorizontal = input;
        rb.linearVelocity = new Vector3(moveHorizontal * speed, rb.linearVelocity.y);
        rb.linearVelocity += new Vector3(0, gravity * Time.fixedDeltaTime);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }

    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>().x;
    }

    public void OnJump()
    {
        if (coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
            coyoteTimeCounter = 0f;
            isJumping = true;
            jumpTimeCounter = jumpTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
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