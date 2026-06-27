using UnityEngine;

/// <summary>
/// Gerencia o feedback visual do jogador baseado no modo atual (Luz ou Sombra).
/// Altera a cor do SpriteRenderer e cria/controla um Point Light para brilho sutil.
/// Requer um componente PlayerAttack no mesmo GameObject.
/// </summary>
public class PlayerModeVisual : MonoBehaviour
{
    [Header("Sprites de Luz")]
    public Sprite luzParado;
    public Sprite luzAndando;
    public Sprite luzPulando;
    public Sprite luzDash1;
    public Sprite luzDash2;
    public Sprite luzAtaque1;
    public Sprite luzAtaque2;

    [Header("Sprites de Sombra")]
    public Sprite sombraParado;
    public Sprite sombraAndando;
    public Sprite sombraPulando;
    public Sprite sombraPulando2;
    public Sprite sombraAtaque1;
    public Sprite sombraAtaque2;

    private Movement playerMovement;

    [Header("Cores do Sprite")]
    [Tooltip("Tinta aplicada ao sprite no modo Luz")]
    public Color lightModeTint = new Color(1f, 0.97f, 0.85f, 1f);
    [Tooltip("Tinta aplicada ao sprite no modo Sombra")]
    public Color shadowModeTint = new Color(0.75f, 0.55f, 1f, 1f);

    [Header("Configurações da Luz de Aura")]
    [Tooltip("Cor da aura no modo Luz")]
    public Color lightAuraColor = new Color(1f, 0.92f, 0.6f, 1f);
    [Tooltip("Cor da aura no modo Sombra")]
    public Color shadowAuraColor = new Color(0.4f, 0.1f, 0.8f, 1f);
    public float lightAuraIntensity = 3f;
    public float shadowAuraIntensity = 2f;
    public float auraRange = 4f;

    [Header("Transição")]
    [Tooltip("Duração da transição suave entre modos (em segundos)")]
    public float transitionDuration = 0.3f;

    // Referências internas
    private PlayerAttack playerAttack;
    private SpriteRenderer spriteRenderer;
    private Light auraLight;

    // Estado de transição
    private PlayerAttack.PlayerMode lastMode;
    private float transitionProgress = 1f;

    // Cores/valores de origem e destino da transição
    private Color tintFrom;
    private Color tintTo;
    private Color auraColorFrom;
    private Color auraColorTo;
    private float auraIntensityFrom;
    private float auraIntensityTo;

    void Start()
    {
        playerAttack = GetComponent<PlayerAttack>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMovement = GetComponent<Movement>();

        if (playerAttack == null)
        {
            Debug.LogWarning("PlayerModeVisual: PlayerAttack não encontrado no GameObject!");
            enabled = false;
            return;
        }

        // Criar ou encontrar a luz de aura
        SetupAuraLight();

        // Inicializar com o modo atual
        lastMode = playerAttack.currentMode;
        ApplyModeInstant(lastMode);
    }

    void Update()
    {
        if (playerAttack == null) return;

        // Detectar troca de modo
        if (playerAttack.currentMode != lastMode)
        {
            StartTransition(playerAttack.currentMode);
            lastMode = playerAttack.currentMode;
        }

        // Atualizar transição suave
        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            float t = Mathf.SmoothStep(0f, 1f, transitionProgress);

            // Interpolar cor do sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(tintFrom, tintTo, t);
            }

            // Interpolar cor e intensidade da luz
            if (auraLight != null)
            {
                auraLight.color = Color.Lerp(auraColorFrom, auraColorTo, t);
                auraLight.intensity = Mathf.Lerp(auraIntensityFrom, auraIntensityTo, t);
            }
        }
    }

    private void SetupAuraLight()
    {
        // Procurar uma luz existente chamada "ModeAuraLight"
        Transform existingLight = transform.Find("ModeAuraLight");
        if (existingLight != null)
        {
            auraLight = existingLight.GetComponent<Light>();
        }
        else
        {
            // Criar a luz dinamicamente
            GameObject lightObj = new GameObject("ModeAuraLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            auraLight = lightObj.AddComponent<Light>();
            auraLight.type = LightType.Point;
            auraLight.range = auraRange;
            auraLight.shadows = LightShadows.None;
        }
    }

    private void StartTransition(PlayerAttack.PlayerMode newMode)
    {
        transitionProgress = 0f;

        // Capturar valores atuais como origem
        tintFrom = spriteRenderer != null ? spriteRenderer.color : Color.white;
        auraColorFrom = auraLight != null ? auraLight.color : Color.white;
        auraIntensityFrom = auraLight != null ? auraLight.intensity : 0f;

        // Definir destino
        if (newMode == PlayerAttack.PlayerMode.Luz)
        {
            tintTo = lightModeTint;
            auraColorTo = lightAuraColor;
            auraIntensityTo = lightAuraIntensity;

            // INJETA OS SPRITES DE LUZ DURANTE A TRANSIÇÃO
            if (playerMovement != null)
            {
                playerMovement.spriteParado = luzParado;
                playerMovement.spriteAndando = luzAndando;
                playerMovement.spritePulando = luzPulando;
                playerMovement.spriteDash1 = luzDash1;
                playerMovement.spriteDash2 = luzDash2;
            }
            if (playerAttack != null)
            {
                playerAttack.spriteAtaque1 = luzAtaque1;
                playerAttack.spriteAtaque2 = luzAtaque2;
            }
        }
        else
        {
            tintTo = shadowModeTint;
            auraColorTo = shadowAuraColor;
            auraIntensityTo = shadowAuraIntensity;

            // INJETA OS SPRITES DE SOMBRA DURANTE A TRANSIÇÃO
            if (playerMovement != null)
            {
                playerMovement.spriteParado = sombraParado;
                playerMovement.spriteAndando = sombraAndando;
                playerMovement.spritePulando = sombraPulando;
                playerMovement.spritePulando = sombraPulando2;
            }
            if (playerAttack != null)
            {
                playerAttack.spriteAtaque1 = sombraAtaque1;
                playerAttack.spriteAtaque2 = sombraAtaque2;
            }
        }
    }

    private void ApplyModeInstant(PlayerAttack.PlayerMode mode)
    {
        if (mode == PlayerAttack.PlayerMode.Luz)
        {
            if (spriteRenderer != null) spriteRenderer.color = lightModeTint;
            if (auraLight != null)
            {
                auraLight.color = lightAuraColor;
                auraLight.intensity = lightAuraIntensity;
            }

            // TROCA PARA OS SPRITES DE LUZ
            if (playerMovement != null)
            {
                playerMovement.spriteParado = luzParado;
                playerMovement.spriteAndando = luzAndando;
                playerMovement.spritePulando = luzPulando;
                playerMovement.spriteDash1 = luzDash1;
                playerMovement.spriteDash2 = luzDash2;
            }
            if (playerAttack != null)
            {
                playerAttack.spriteAtaque1 = luzAtaque1;
                playerAttack.spriteAtaque2 = luzAtaque2;
            }
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.color = shadowModeTint;
            if (auraLight != null)
            {
                auraLight.color = shadowAuraColor;
                auraLight.intensity = shadowAuraIntensity;
            }

            // TROCA PARA OS SPRITES DE SOMBRA
            if (playerMovement != null)
            {
                playerMovement.spriteParado = sombraParado;
                playerMovement.spriteAndando = sombraAndando;
                playerMovement.spritePulando = sombraPulando;
                playerMovement.spritePulando = sombraPulando2;
            }
            if (playerAttack != null)
            {
                playerAttack.spriteAtaque1 = sombraAtaque1;
                playerAttack.spriteAtaque2 = sombraAtaque2;
            }
        }
    }
}
