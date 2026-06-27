using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [Header("Estados do Personagem")]
    public bool isAttacking = false;

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
    public float fallMultiplier = 1.5f;
    public float lowJumpMultiplier = 2.0f;
    [Range(0.1f, 0.9f)] public float jumpCutMultiplier = 0.45f;

    [Header("Configurações de Tolerância")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Configurações Visuais (Sprites)")]
    public SpriteRenderer renderizadorSprite;
    public Sprite spriteParado;
    public Sprite spriteAndando;
    public Sprite spritePulando;
    public Sprite spriteDash1;
    public Sprite spriteDash2;
    public float tempoEntreFramesDash = 0.1f;

    [Header("Configurações do Wall Jump")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    public float wallSlidingSpeed = 2f;
    public float wallJumpingTime = 0.15f;
    public float wallJumpingDuration = 0.2f;
    public Vector3 wallJumpingPower = new Vector3(8f, 16f);
    [Range(0f, 1f)] public float sameWallJumpPenalty = 0.5f;

    [Header("Configurações de Dash")]
    public float lightDashSpeed = 22f;
    public float lightDashDuration = 0.35f;
    public float lightDashCooldown = 1.0f;
    public float shadowDashSpeed = 16f;
    public float shadowDashDuration = 0.25f;
    public float shadowDashCooldown = 0.8f;
    public float lightDashRepelForce = 22f; // Aumentado para repulsão mais forte
    public float lightDashRepelUpwardForce = 8f; // Aumentado para empurrar mais para cima
    public Vector3 trailOffset = new Vector3(0f, 1f, 0f); // Deslocamento para centralizar no corpo
    public float trailStartWidth = 1.2f; // Largura inicial do rastro para cobrir o corpo

    [Header("Detecção de Chão")]
    public float sensibility = 0.7f;
    public bool isGrounded = false;

    // Input armazenado
    [HideInInspector] public float input;
    private Vector2 moveInput;

    // Timers internos
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float wallJumpTimer;
    private float wallJumpingCounter;
    private float wallJumpingDirection;
    private float dashTimeCounter;
    private float dashCooldownCounter;

    // Flags de input (capturadas no Update, consumidas no FixedUpdate)
    private bool jumpPressedThisFrame;
    private bool jumpHeld;
    private bool jumpReleasedThisFrame;

    // Estados
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isDashing;
    private bool isShadowDashing;
    private Vector3 dashDirection;

    // Controle de Wall Jump consecutivos na mesma parede
    private float lastWallJumpSide;
    private int consecutiveSameWallJumps;

    // Referências
    private Rigidbody rb;
    private PlayerAttack playerAttack;
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private TrailRenderer trailRenderer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        playerAttack = GetComponent<PlayerAttack>();
        
        // Busca robusta pelo PlayerInput no próprio objeto, pais ou filhos
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null) playerInput = GetComponentInChildren<PlayerInput>();

        if (playerInput != null)
        {
            jumpAction = playerInput.actions["Jump"];
            if (jumpAction == null)
            {
                Debug.LogWarning("Jump action not found in PlayerInput actions map.");
            }
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found in player hierarchy. Using SendMessages fallback.");
        }

        if (renderizadorSprite == null)
        {
            renderizadorSprite = GetComponent<SpriteRenderer>();
        }

        // Criar um ponto de origem filho para centralizar o TrailRenderer no corpo da protagonista
        Transform trailSource = transform.Find("DashTrailSource");
        if (trailSource == null)
        {
            GameObject go = new GameObject("DashTrailSource");
            go.transform.SetParent(transform);
            go.transform.localPosition = trailOffset;
            trailSource = go.transform;
        }
        else
        {
            trailSource.localPosition = trailOffset;
        }

        // Inicializar e configurar o TrailRenderer no objeto filho
        trailRenderer = trailSource.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = trailSource.gameObject.AddComponent<TrailRenderer>();
        }
        ConfigureTrailRenderer();
    }

    void Update()
    {
        AtualizarSprite();

        bool previouslyHeld = jumpHeld;
        jumpHeld = CheckJumpInputHeld();

        if (previouslyHeld && !jumpHeld)
        {
            jumpReleasedThisFrame = true;
        }
    }

    private bool CheckJumpInputHeld()
    {
        // 1. Tentar ler pela InputAction do PlayerInput
        if (jumpAction != null)
        {
            if (jumpAction.IsPressed() || jumpAction.ReadValue<float>() > 0.3f)
            {
                return true;
            }
        }

        // 2. Fallback direto para os dispositivos de hardware (teclado e gamepad)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed)
            {
                return true;
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // MÉTODOS DE DASH
    // ==========================================
    private void StartDash()
    {
        StartCoroutine(AnimarDash());

        isDashing = true;

        if (IsInShadowMode())
        {
            isShadowDashing = true;
            dashTimeCounter = shadowDashDuration;
            dashCooldownCounter = shadowDashCooldown;

            // Direção do dash de Sombra: qualquer direção baseada no input 2D
            if (moveInput.magnitude > 0.1f)
            {
                dashDirection = new Vector3(moveInput.x, moveInput.y, 0f).normalized;
            }
            else
            {
                // Se não houver direcional pressionado, vai para frente
                dashDirection = new Vector3(Mathf.Sign(transform.localScale.x), 0f, 0f);
            }

            // Tornar intangível a inimigos
            SetIntangible(true);
        }
        else
        {
            isShadowDashing = false;
            dashTimeCounter = lightDashDuration;
            dashCooldownCounter = lightDashCooldown;

            // Direção do dash de Luz: apenas para frente
            dashDirection = new Vector3(Mathf.Sign(transform.localScale.x), 0f, 0f);
        }

        // Zera velocidade antes de aplicar o impulso do dash
        rb.linearVelocity = dashDirection * (isShadowDashing ? shadowDashSpeed : lightDashSpeed);

        // Ativa o rastro visual
        EnableDashTrail(true);
    }

    private void StopDash()
    {
        isDashing = false;

        if (isShadowDashing)
        {
            isShadowDashing = false;
            SetIntangible(false);
        }

        // Restaura velocidade de forma suave
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.5f, 0f, 0f);

        // Desativa a emissão do rastro visual
        EnableDashTrail(false);
    }

    private void ConfigureTrailRenderer()
    {
        if (trailRenderer == null) return;

        trailRenderer.time = 0.25f;
        trailRenderer.startWidth = trailStartWidth;
        trailRenderer.endWidth = 0f;
        trailRenderer.numCornerVertices = 5;
        trailRenderer.numCapVertices = 5;

        // Tentativa de carregar material unlit para cores vibrantes
        Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
        if (trailMaterial != null)
        {
            trailRenderer.material = trailMaterial;
        }

        // Inicia sem emitir
        trailRenderer.emitting = false;
    }

    private void EnableDashTrail(bool enable)
    {
        if (trailRenderer == null) return;

        if (enable)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            if (IsInShadowMode())
            {
                // Sombra: Rastro Roxo (Purple)
                colorKeys[0] = new GradientColorKey(new Color(0.6f, 0f, 1f), 0f); // Roxo vibrante
                colorKeys[1] = new GradientColorKey(new Color(0.2f, 0f, 0.4f), 1f); // Roxo escuro
            }
            else
            {
                // Luz: Rastro Amarelo (Yellow)
                colorKeys[0] = new GradientColorKey(new Color(1f, 0.9f, 0f), 0f); // Amarelo brilhante
                colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 1f); // Laranja/Dourado
            }

            alphaKeys[0] = new GradientAlphaKey(0.8f, 0f); // Opaco no início
            alphaKeys[1] = new GradientAlphaKey(0f, 1f);   // Transparente no fim

            gradient.SetKeys(colorKeys, alphaKeys);
            trailRenderer.colorGradient = gradient;

            trailRenderer.emitting = true;
        }
        else
        {
            trailRenderer.emitting = false;
        }
    }

    private void SetIntangible(bool intangible)
    {
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider == null) return;

        // Procura todos os GameObjects com a tag "Enemy" e ignora/reabilita as colisões
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
            foreach (Collider enemyCollider in enemyColliders)
            {
                Physics.IgnoreCollision(playerCollider, enemyCollider, intangible);
            }
        }
    }

    void FixedUpdate()
    {
        // Decrementar cooldown do dash
        if (dashCooldownCounter > 0f)
        {
            dashCooldownCounter -= Time.fixedDeltaTime;
        }

        // ==========================================
        // 0. COMPORTAMENTO DO DASH
        // ==========================================
        if (isDashing)
        {
            dashTimeCounter -= Time.fixedDeltaTime;

            // Manter velocidade do dash constante
            rb.linearVelocity = dashDirection * (isShadowDashing ? shadowDashSpeed : lightDashSpeed);

            if (dashTimeCounter <= 0f)
            {
                StopDash();
            }

            // Pular o resto da física normal enquanto está no dash
            return;
        }

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
            consecutiveSameWallJumps = 0;
            lastWallJumpSide = 0f;
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

        // Corta a velocidade vertical conforme o multiplicador configurado
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
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
        if (isGrounded)
        {
            wallJumpingCounter = 0f;
            return;
        }

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
        if (isAttacking || isDashing) return;

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

    private IEnumerator AnimarDash()
    {
        // 1. Coloca a pose inicial do Dash (impulso)
        if (renderizadorSprite != null && spriteDash1 != null)
            renderizadorSprite.sprite = spriteDash1;

        // 2. Espera a fração de segundo
        yield return new WaitForSeconds(tempoEntreFramesDash);

        // 3. Coloca a pose 2 (deslizando)
        if (renderizadorSprite != null && spriteDash2 != null)
            renderizadorSprite.sprite = spriteDash2;

        // Não precisamos destravar manualmente aqui, 
        // pois a função StopDash() do seu código já faz o isDashing virar false!
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
        moveInput = value.Get<Vector2>();
        input = moveInput.x;
    }

    public void OnDash()
    {
        if (dashCooldownCounter <= 0f && !isDashing)
        {
            StartDash();
        }
    }

    public void OnJump(InputValue value)
    {
        // O OnJump (mensagem do PlayerInput) sempre lidará com o evento de pressionar o botão (Press),
        // pois é 100% confiável independente da estrutura de GameObjects e versão da Input System.
        if (value.isPressed)
        {
            jumpPressedThisFrame = true;
            jumpHeld = true;

            // Jump Buffer: armazenar o input
            jumpBufferCounter = jumpBufferTime;

            // Wall Jump: verificar se está na janela de coyote da parede e no ar
            if (wallJumpingCounter > 0f && !isGrounded)
            {
                isWallJumping = true;
                wallJumpTimer = wallJumpingDuration;

                // Verificar pulos consecutivos na mesma parede
                float wallSide = -wallJumpingDirection;
                if (wallSide == lastWallJumpSide)
                {
                    consecutiveSameWallJumps++;
                }
                else
                {
                    consecutiveSameWallJumps = 1;
                    lastWallJumpSide = wallSide;
                }

                float verticalForce = wallJumpingPower.y;
                if (consecutiveSameWallJumps > 1)
                {
                    verticalForce *= sameWallJumpPenalty;
                }

                rb.linearVelocity = new Vector3(wallJumpingDirection * wallJumpingPower.x, verticalForce);
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
            // Fallback caso a mensagem de soltar (Release) também seja enviada pelo PlayerInput
            jumpHeld = false;
            jumpReleasedThisFrame = true;
        }
    }

    // ==========================================
    // DETECÇÃO DE CHÃO E INIMIGOS (COLISÕES)
    // ==========================================
    void OnCollisionEnter(Collision collision)
    {
        // Colisão com Inimigos durante o Dash de Luz
        if (isDashing && !isShadowDashing && collision.gameObject.CompareTag("Enemy"))
        {
            // Causar dano no inimigo via SendMessage (compatível com qualquer script de vida do inimigo)
            collision.gameObject.SendMessage("TakeDamage", 10f, SendMessageOptions.DontRequireReceiver);
            Debug.Log("Dash de Luz causou dano no inimigo!");

            // Player é repelido (knockback)
            isDashing = false;
            EnableDashTrail(false); // Desativa emissão do rastro imediatamente!

            Vector3 repelDir = -dashDirection;
            repelDir.y = 1f; // Empurra levemente para cima
            repelDir = repelDir.normalized;

            rb.linearVelocity = new Vector3(repelDir.x * lightDashRepelForce, repelDir.y * lightDashRepelUpwardForce, 0f);
            return;
        }

        // Colisão com Inimigos durante o Dash de Sombra (intangibilidade de emergência caso passem pelo ignore inicial)
        if (isShadowDashing && collision.gameObject.CompareTag("Enemy"))
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider, true);
            return;
        }

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
        if (isShadowDashing && collision.gameObject.CompareTag("Enemy"))
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider, true);
            return;
        }

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