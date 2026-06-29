using UnityEngine;

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
    public Color lightModeTint = new Color(1f, 0.97f, 0.85f, 1f);
    public Color shadowModeTint = new Color(0.75f, 0.55f, 1f, 1f);

    [Header("💥 Flash da Troca de Modo")]
    public float burstDuration = 0.4f;
    public float flashScale = 0.065f;
    public float flashYOffset = 1.5f;

    [Header("✨ Aura Permanente")]
    public float auraScale = 2f;
    public float auraAlpha = 0.2f;
    public int auraSortingOrder = 0;

    [Header("Transição")]
    public float transitionDuration = 0.3f;

    private PlayerAttack playerAttack;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer auraRenderer;

    private PlayerAttack.PlayerMode lastMode;
    private float transitionProgress = 1f;
    private Color tintFrom, tintTo;

    void Awake()
    {
        playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack == null) playerAttack = GetComponentInParent<PlayerAttack>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMovement = GetComponent<Movement>();

        if (playerAttack == null) { enabled = false; return; }

        lastMode = playerAttack.currentMode;
        ApplyModeInstant(lastMode);
    }

    void Start()
    {
        // Copia do burst: cria aura igual ao flash, mas permanente e atrás
        CriarAura(playerAttack.currentMode);
    }

    void CriarAura(PlayerAttack.PlayerMode modo)
    {
        // Destruir qualquer ModeAura antigo salvo no prefab
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (transform.GetChild(i).name == "ModeAura")
                Destroy(transform.GetChild(i).gameObject);
        }

        GameObject auraGO = new GameObject("ModeAura");
        auraGO.transform.SetParent(transform);
        auraGO.transform.localPosition = new Vector3(0f, flashYOffset, 0f);

        int texSize = 64;
        Texture2D tex = new Texture2D(texSize, texSize);
        Color[] pixels = new Color[texSize * texSize];
        float half = texSize / 2f;
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                pixels[y * texSize + x] = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.3f, 1f, dist));
            }
        tex.SetPixels(pixels);
        tex.Apply();

        auraRenderer = auraGO.AddComponent<SpriteRenderer>();
        auraRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 1f);

        // Mesma sorting layer do player, ordem atrás
        if (spriteRenderer != null)
            auraRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        auraRenderer.sortingOrder = auraSortingOrder;

        auraRenderer.color = (modo == PlayerAttack.PlayerMode.Luz)
            ? new Color(1f, 0.95f, 0.3f, auraAlpha)
            : new Color(0.5f, 0.1f, 1f, auraAlpha);

        auraGO.transform.localScale = new Vector3(auraScale, auraScale, 1f);
        Debug.Log("ModeAura criada com escala: " + auraScale);
    }

    void Update()
    {
        if (playerAttack == null) return;

        if (playerAttack.currentMode != lastMode)
        {
            StartTransition(playerAttack.currentMode);
            lastMode = playerAttack.currentMode;
        }

        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transitionProgress));
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(tintFrom, tintTo, t);
        }
    }

    void StartTransition(PlayerAttack.PlayerMode newMode)
    {
        transitionProgress = 0f;
        tintFrom = spriteRenderer != null ? spriteRenderer.color : Color.white;

        if (newMode == PlayerAttack.PlayerMode.Luz)
        {
            tintTo = lightModeTint;
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

        // Recriar aura com valores atualizados
        CriarAura(newMode);

        StartCoroutine(BurstFlash(newMode));
    }

    System.Collections.IEnumerator BurstFlash(PlayerAttack.PlayerMode modo)
    {
        yield return null;

        GameObject flashGO = new GameObject("ModeFlash");
        flashGO.transform.SetParent(transform);
        flashGO.transform.position = transform.position + new Vector3(0f, flashYOffset, 0f);

        int texSize = 64;
        Texture2D tex = new Texture2D(texSize, texSize);
        Color[] pixels = new Color[texSize * texSize];
        float half = texSize / 2f;
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                pixels[y * texSize + x] = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, dist));
            }
        tex.SetPixels(pixels);
        tex.Apply();

        SpriteRenderer fr = flashGO.AddComponent<SpriteRenderer>();
        fr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 1f);
        fr.sortingOrder = 999;
        fr.color = (modo == PlayerAttack.PlayerMode.Luz)
            ? new Color(1f, 0.95f, 0.3f, 1f)
            : new Color(0.5f, 0.1f, 1f, 1f);
        flashGO.transform.localScale = new Vector3(flashScale, flashScale, 1f);

        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            Color c = fr.color;
            c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / burstDuration));
            fr.color = c;
            yield return null;
        }
        Destroy(flashGO);
    }

    void ApplyModeInstant(PlayerAttack.PlayerMode mode)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = (mode == PlayerAttack.PlayerMode.Luz) ? lightModeTint : shadowModeTint;

        if (mode == PlayerAttack.PlayerMode.Luz)
        {
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
