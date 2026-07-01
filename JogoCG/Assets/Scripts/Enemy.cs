using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public enum EnemyState { MalignoLight, LightHurt, Neutro, ShadowHurt, MalignoShadow }

    [Header("Sistema de Estrelas")]
    public int totalStars = 6;
    public int lightStars = 5;
    public int shadowStars = 1;

    [Header("Movimento")]
    public float speed = 2f;
    public bool moveRight = true;

    [Header("Sprites — Luz Maligno")]
    public Sprite luzMaligno1;
    public Sprite luzMaligno2;
    [Header("Sprites — Luz Machucado")]
    public Sprite luzMachucado1;
    public Sprite luzMachucado2;
    [Header("Sprites — Sombra Machucado")]
    public Sprite sombraMachucado1;
    public Sprite sombraMachucado2;
    [Header("Sprites — Sombra Maligno")]
    public Sprite sombraMaligno1;
    public Sprite sombraMaligno2;
    [Header("Sprite — Neutro")]
    public Sprite neutro;

    public SpriteRenderer spriteRenderer;
    public float animSpeed = 0.3f;

    [Header("Morte")]
    public float deathFlashDuration = 0.5f;
    public Color lightExplosionColor = new Color(1f, 0.95f, 0.3f, 1f);
    public Color shadowExplosionColor = new Color(0.5f, 0.1f, 1f, 1f);

    [Header("Knockback")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.15f;

    [Header("Feedback Visual")]
    public float flashDuration = 0.15f;

    [Header("Invencibilidade")]
    public float invincibilityTime = 0.3f;
    private bool isInvincible = false;

    private Rigidbody rb;
    private float animTimer;
    private bool showingFrame1 = true;
    private EnemyState currentState = EnemyState.MalignoLight;
    private bool isDead = false;
    private bool isKnockedBack = false;
    private bool canDamage = true;
    private DamageSource damageSource;
    public bool IsDead => isDead;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        damageSource = GetComponent<DamageSource>();
        if (damageSource == null)
            damageSource = gameObject.AddComponent<DamageSource>();

        UpdateState();
    }

    void Update()
    {
        if (isDead) return;
        Animate();
    }

    void FixedUpdate()
    {
        if (isDead || rb == null || isKnockedBack) return;
        if (currentState == EnemyState.Neutro)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        float dir = moveRight ? 1f : -1f;
        rb.linearVelocity = new Vector3(dir * speed, 0f, 0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        moveRight = !moveRight;
        if (spriteRenderer != null)
        {
            Vector3 scale = transform.localScale;
            scale.x = moveRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    public void TakeDamage(PlayerHealth.DamageType damageType)
    {
        if (isDead || isInvincible) return;

        if (damageType == PlayerHealth.DamageType.Light)
        {
            if (shadowStars > 0) { shadowStars--; lightStars++; }
        }
        else
        {
            if (lightStars > 0) { lightStars--; shadowStars++; }
        }

        Debug.Log($"Inimigo: {lightStars} Luz / {shadowStars} Sombra");

        // Repulsão (knockback)
        StartCoroutine(KnockbackRoutine());
        StartCoroutine(FlashRed());

        // Invencibilidade temporária
        StartCoroutine(InvincibilityCooldown());

        UpdateState();

        if (lightStars == totalStars)
            StartCoroutine(DeathExplosion(true));
        else if (shadowStars == totalStars)
            StartCoroutine(DeathExplosion(false));
    }

    void UpdateState()
    {
        EnemyState prev = currentState;

        if (lightStars == 5 && shadowStars == 1)
            currentState = EnemyState.MalignoLight;
        else if (lightStars == 4 && shadowStars == 2)
            currentState = EnemyState.LightHurt;
        else if (lightStars == 3 && shadowStars == 3)
            currentState = EnemyState.Neutro;
        else if (lightStars == 2 && shadowStars == 4)
            currentState = EnemyState.ShadowHurt;
        else if (lightStars == 1 && shadowStars == 5)
            currentState = EnemyState.MalignoShadow;
        else if (lightStars >= 4)
            currentState = EnemyState.MalignoLight;
        else if (shadowStars >= 4)
            currentState = EnemyState.MalignoShadow;

        // Só atualiza sprite se o estado mudou
        if (currentState != prev)
            ApplyStateSprites();

        // Dano e velocidade
        switch (currentState)
        {
            case EnemyState.MalignoLight:
            case EnemyState.LightHurt:
                speed = 2f;
                DamageOn(PlayerHealth.DamageType.Light);
                break;
            case EnemyState.Neutro:
                speed = 0f;
                DamageOff();
                rb.linearVelocity = Vector3.zero;
                break;
            case EnemyState.MalignoShadow:
            case EnemyState.ShadowHurt:
                speed = 2f;
                DamageOn(PlayerHealth.DamageType.Shadow);
                break;
        }
    }

    void DamageOn(PlayerHealth.DamageType dmgType)
    {
        if (damageSource == null)
            damageSource = gameObject.AddComponent<DamageSource>();
        damageSource.damageType = dmgType;
        canDamage = true;
    }

    void DamageOff()
    {
        if (damageSource != null)
            Destroy(damageSource);
        damageSource = null;
        canDamage = false;
    }

    IEnumerator InvincibilityCooldown()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = original;
    }

    IEnumerator KnockbackRoutine()
    {
        if (rb == null || isDead) yield break;
        isKnockedBack = true;

        // Direção: afastar do player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 dir = Vector3.right;
        if (player != null)
            dir = (transform.position - player.transform.position).normalized;
        dir.y = 0f;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.fixedDeltaTime;
            rb.linearVelocity = dir * knockbackForce * (1f - elapsed / knockbackDuration);
            yield return new WaitForFixedUpdate();
        }
        isKnockedBack = false;
    }

    void ApplyStateSprites()
    {
        if (spriteRenderer == null) return;
        showingFrame1 = true;
        animTimer = 0f;

        switch (currentState)
        {
            case EnemyState.MalignoLight:
                if (luzMaligno1 != null) spriteRenderer.sprite = luzMaligno1;
                break;
            case EnemyState.LightHurt:
                if (luzMachucado1 != null) spriteRenderer.sprite = luzMachucado1;
                break;
            case EnemyState.Neutro:
                if (neutro != null) spriteRenderer.sprite = neutro;
                break;
            case EnemyState.ShadowHurt:
                if (sombraMachucado1 != null) spriteRenderer.sprite = sombraMachucado1;
                break;
            case EnemyState.MalignoShadow:
                if (sombraMaligno1 != null) spriteRenderer.sprite = sombraMaligno1;
                break;
        }
    }
    IEnumerator DeathExplosion(bool isLight)
    {
        isDead = true;
        damageSource.enabled = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        // Flash IDÊNTICO ao burst da protagonista
        GameObject flash = new GameObject("EnemyDeathFlash");
        flash.transform.position = transform.position + new Vector3(0f, 1.5f, 0f); // Centro do sprite

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

        SpriteRenderer fr = flash.AddComponent<SpriteRenderer>();
        fr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 1f);
        fr.sortingOrder = 999;
        fr.color = isLight ? lightExplosionColor : shadowExplosionColor;
        flash.transform.localScale = new Vector3(0.065f, 0.065f, 1f); // Mesmo flashScale do burst

        float elapsed = 0f;
        float duration = 0.4f; // Mesma duração do burst
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = fr.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            fr.color = c;
            yield return null;
        }

        Destroy(flash);
        Destroy(gameObject);
    }

    void Animate()
    {
        if (spriteRenderer == null || currentState == EnemyState.Neutro) return;

        animTimer += Time.deltaTime;
        if (animTimer >= animSpeed)
        {
            animTimer = 0f;
            showingFrame1 = !showingFrame1;

            Sprite frame1 = null, frame2 = null;
            switch (currentState)
            {
                case EnemyState.MalignoLight:
                    frame1 = luzMaligno1; frame2 = luzMaligno2; break;
                case EnemyState.LightHurt:
                    frame1 = luzMachucado1; frame2 = luzMachucado2; break;
                case EnemyState.ShadowHurt:
                    frame1 = sombraMachucado1; frame2 = sombraMachucado2; break;
                case EnemyState.MalignoShadow:
                    frame1 = sombraMaligno1; frame2 = sombraMaligno2; break;
            }
            spriteRenderer.sprite = showingFrame1 ? frame1 : frame2;
        }
    }
}
