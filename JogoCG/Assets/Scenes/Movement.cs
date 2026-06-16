using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [Header("Movimento Horizontal")]
    public float speed = 5f;
    public float groundAcceleration = 0.8f;
    public float airAcceleration = 0.65f;

    [Header("Pulo - Modo Luz (Fixo e Alto)")]
    public float lightJumpForce = 22f;

    [Header("Pulo - Modo Sombra (Variável e Baixo)")]
    public float shadowJumpForce = 10f;

    [Header("Configurações de Gravidade")]
    public float baseGravity = -30f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Configurações de Tolerância")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Configurações Visuais (Sprites)")]
    public SpriteRenderer renderizadorSprite;
    public Sprite spriteParado;
    public Sprite spriteAndando;
    public Sprite spritePulando;

    [Header("Configurações do Wall Jump")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    public float wallSlidingSpeed = 2f;
    public float wallJumpingTime = 0.15f;
    public float wallJumpingDuration = 0.2f;
    public Vector3 wallJumpingPower = new Vector3(8f, 16f);

    [Header("Detecção de Chão")]
    public float sensibility = 0.7f;
    public bool isGrounded = false;

    // Input armazenado
    [HideInInspector] public float input;

    // Timers internos
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float wallJumpTimer;
    private float wallJumpingCounter;
    private float wallJumpingDirection;

    // Flags de input (capturadas no Update, consumidas no FixedUpdate)
    private bool jumpPressedThisFrame;
    private bool jumpHeld;
    private bool jumpReleasedThisFrame;

    // Estados
    private bool isWallSliding;
    private bool isWallJumping;

    // Referências
    private Rigidbody rb;
    private PlayerAttack playerAttack;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        playerAttack = GetComponent<PlayerAttack>();

        if (renderizadorSprite == null)
        {
            renderizadorSprite = GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        AtualizarSprite();
    }

    void FixedUpdate()
    {
        // ==========================================
        // 1. MOVIMENTO HORIZONTAL
        // ==========================================
        float targetX = input * speed;
        float accel = isGrounded ? groundAcceleration : airAcceleration;

        if (!isWallJumping)
        {
            float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, accel);
            rb.linearVelocity = new Vector3(newX, rb.linearVelocity.y);
        }

        // ==========================================
        // 2. GRAVIDADE COM MULTIPLIERS
        // ==========================================
        ApplyGravity();

        // ==========================================
        // 3. TIMERS
        // ==========================================

        // Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        // Jump Buffer
        if (jumpPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.fixedDeltaTime;
        }

        // Wall Jump Timer (substitui Invoke)
        if (isWallJumping)
        {
            wallJumpTimer -= Time.fixedDeltaTime;
            if (wallJumpTimer <= 0f)
            {
                isWallJumping = false;
            }
        }

        // ==========================================
        // 4. WALL SLIDE & WALL JUMP
        // ==========================================
        WallSlide();
        WallJump();

        // ==========================================
        // 5. FLIP (direção visual)
        // ==========================================
        if (!isWallJumping && !isWallSliding)
        {
            Flip();
        }

        // ==========================================
        // 6. PULO (GROUND JUMP)
        // ==========================================
        HandleJump();

        // ==========================================
        // 7. JUMP CUT (soltar botão corta a altura - só no modo Sombra)
        // ==========================================
        HandleJumpCut();

        // Consumir as flags de input no final do FixedUpdate
        jumpPressedThisFrame = false;
        jumpReleasedThisFrame = false;
    }

    // ==========================================
    // GRAVIDADE DINÂMICA
    // ==========================================
    private void ApplyGravity()
    {
        float gravityThisFrame = baseGravity;

        if (rb.linearVelocity.y < 0f)
        {
            // Caindo → gravidade aumentada para queda rápida e precisa
            gravityThisFrame = baseGravity * fallMultiplier;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && IsInShadowMode())
        {
            // Subindo com botão solto no modo Sombra → gravidade aumentada (pulo curto)
            gravityThisFrame = baseGravity * lowJumpMultiplier;
        }
        // else: subindo com botão pressionado OU no modo Luz → gravidade normal

        rb.linearVelocity += new Vector3(0, gravityThisFrame * Time.fixedDeltaTime);
    }

    // ==========================================
    // PULO
    // ==========================================
    private void HandleJump()
    {
        if (jumpBufferCounter <= 0f) return;
        if (coyoteTimeCounter <= 0f) return;

        // Determinar a força de pulo com base no modo
        float jumpForce = IsInShadowMode() ? shadowJumpForce : lightJumpForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce);
        isGrounded = false;
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }

    // ==========================================
    // JUMP CUT (só no modo Sombra)
    // ==========================================
    private void HandleJumpCut()
    {
        if (!jumpReleasedThisFrame) return;
        if (rb.linearVelocity.y <= 0f) return;
        if (!IsInShadowMode()) return;

        // Corta a velocidade vertical pela metade → pulo mais curto
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
    }

    // ==========================================
    // WALL SLIDE
    // ==========================================
    private void WallSlide()
    {
        // Só desliza se não estiver ativamente executando um Wall Jump
        if (!isWallJumping && IsWalled() && !isGrounded && input != 0f)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                Mathf.Clamp(rb.linearVelocity.y, -wallSlidingSpeed, float.MaxValue)
            );
        }
        else
        {
            isWallSliding = false;
        }
    }

    // ==========================================
    // WALL JUMP
    // ==========================================
    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -Mathf.Sign(transform.localScale.x);
            wallJumpingCounter = wallJumpingTime;
        }
        else
        {
            wallJumpingCounter -= Time.fixedDeltaTime;
        }
    }

    // ==========================================
    // SPRITES
    // ==========================================
    private void AtualizarSprite()
    {
        if (renderizadorSprite == null) return;

        if (!isGrounded && !isWallSliding && spritePulando != null)
        {
            renderizadorSprite.sprite = spritePulando;
        }
        else
        {
            if (Mathf.Abs(input) > 0.1f && spriteAndando != null)
            {
                renderizadorSprite.sprite = spriteAndando;
            }
            else if (spriteParado != null)
            {
                renderizadorSprite.sprite = spriteParado;
            }
        }
    }

    // ==========================================
    // FLIP
    // ==========================================
    public void Flip()
    {
        if (input > 0 && transform.localScale.x < 0)
        {
            Vector3 scaler = transform.localScale;
            scaler.x = Mathf.Abs(scaler.x);
            transform.localScale = scaler;
        }
        else if (input < 0 && transform.localScale.x > 0)
        {
            Vector3 scaler = transform.localScale;
            scaler.x = -Mathf.Abs(scaler.x);
            transform.localScale = scaler;
        }
    }

    // ==========================================
    // DETECÇÃO DE PAREDE
    // ==========================================
    private bool IsWalled()
    {
        if (wallCheck == null) return false;
        return Physics.CheckSphere(wallCheck.position, 0.2f, wallLayer);
    }

    // ==========================================
    // HELPER: VERIFICAR MODO ATUAL
    // ==========================================
    private bool IsInShadowMode()
    {
        if (playerAttack == null) return false;
        return playerAttack.currentMode == PlayerAttack.PlayerMode.Sombra;
    }

    // ==========================================
    // CALLBACKS DO INPUT SYSTEM
    // ==========================================
    public void OnMove(InputValue value)
    {
        input = value.Get<Vector2>().x;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpPressedThisFrame = true;
            jumpHeld = true;

            // Jump Buffer: armazenar o input
            jumpBufferCounter = jumpBufferTime;

            // Wall Jump: verificar se está na janela de coyote da parede
            if (wallJumpingCounter > 0f)
            {
                isWallJumping = true;
                wallJumpTimer = wallJumpingDuration;

                float jumpForce = IsInShadowMode() ? shadowJumpForce : lightJumpForce;
                rb.linearVelocity = new Vector3(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
                wallJumpingCounter = 0f;
                jumpBufferCounter = 0f;

                // Virar o personagem na direção do pulo
                if (Mathf.Sign(transform.localScale.x) != Mathf.Sign(wallJumpingDirection))
                {
                    Vector3 localScale = transform.localScale;
                    localScale.x *= -1f;
                    transform.localScale = localScale;
                }
            }
        }
        else
        {
            jumpHeld = false;
            jumpReleasedThisFrame = true;
        }
    }

    // ==========================================
    // DETECÇÃO DE CHÃO (COLISÕES)
    // ==========================================
    void OnCollisionEnter(Collision collision)
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