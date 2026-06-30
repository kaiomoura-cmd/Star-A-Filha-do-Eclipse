using UnityEngine;
using System.Collections;

public class FlyingEnemy : MonoBehaviour
{
    [Header("Sistema de Estrelas")]
    public int maxStars = 4;
    public int lightStars = 2;

    [Header("Voo — Flutuação própria")]
    public float floatSpeed = 1f;
    public float floatAmplitudeX = 2f;
    public float floatAmplitudeY = 0.5f;
    public float floatHeight = 3f;

    [Header("Voo — Perseguição")]
    public float followRange = 8f;
    public float chaseSpeed = 3f;
    public float orbitRadius = 5f;
    public float diveInterval = 4f;
    public float diveSpeed = 8f;

    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite sprite1;
    public Sprite sprite2;
    public Sprite sprite3;
    public Sprite spriteDisabled;
    public Sprite spriteDisabled2;
    public float animSpeed = 0.3f;

    [Header("Dano")]
    public PlayerHealth.DamageType damageType = PlayerHealth.DamageType.Light;
    public float damageRadius = 1.5f;

    [Header("Knockback")]
    public float knockbackForce = 4f;
    public float knockbackDuration = 0.15f;

    [Header("Feedback Visual")]
    public float flashDuration = 0.15f;

    [Header("Invencibilidade")]
    public float invincibilityTime = 0.3f;

    [Header("Morte")]
    public Color explosionColor = new Color(1f, 0.95f, 0.3f, 1f);

    [Header("Desligamento")]
    public float fallSpeed = 8f;
    public float fadeDuration = 5f;

    private Transform player;
    private Rigidbody rb;
    private Vector3 homePosition;
    private float floatTimer;
    private float animTimer;
    private int animFrame = 0;
    private int animDir = 1;
    private bool isInvincible = false;
    private bool isDisabled = false;
    private bool isDead = false;
    private bool isKnockedBack = false;
    private bool isDiving = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        homePosition = transform.position;
        floatTimer = Random.Range(0f, 100f);

        // Garantir Rigidbody Kinematic (precisa pra colidir com paredes)
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (spriteRenderer != null && sprite1 != null)
            spriteRenderer.sprite = sprite1;

        // Ignorar colisão física com o player (dano é por código)
        StartCoroutine(IgnorePlayerCollision());
        StartCoroutine(DiveCycle());
    }

    IEnumerator IgnorePlayerCollision()
    {
        // Esperar o player existir
        while (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            yield return null;
        }
        Collider playerCol = player.GetComponent<Collider>();
        Collider myCol = GetComponent<Collider>();
        if (playerCol != null && myCol != null)
            Physics.IgnoreCollision(myCol, playerCol, true);
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (isDead || isDisabled) return;
        Animate();
    }

    void FixedUpdate()
    {
        if (isDead || isDisabled || isKnockedBack || isDiving) return;

        floatTimer += Time.fixedDeltaTime * floatSpeed;

        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= followRange)
        {
            // PERSEGUIR: mover suavemente na direção do player
            Vector3 target = player.position + new Vector3(0f, floatHeight, 0f);
            Vector3 pos = Vector3.MoveTowards(
                transform.position,
                target,
                chaseSpeed * Time.fixedDeltaTime
            );
            rb.MovePosition(pos);
        }
        else
        {
            // FLUTUAR no lugar (ao redor da home)
            float fx = Mathf.Sin(floatTimer * 0.7f) * floatAmplitudeX;
            float fy = Mathf.Sin(floatTimer * 1.3f) * floatAmplitudeY;
            Vector3 floatPos = homePosition + new Vector3(fx, floatHeight + fy, 0f);
            Vector3 lerped = Vector3.Lerp(
                transform.position,
                floatPos,
                2f * Time.fixedDeltaTime
            );
            rb.MovePosition(lerped);
        }

        FlipSprite();
    }

    IEnumerator DiveCycle()
    {
        while (!isDead && !isDisabled)
        {
            yield return new WaitForSeconds(diveInterval);
            if (isDead || isDisabled || player == null) continue;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > followRange * 1.5f) continue; // Não mergulha se muito longe

            yield return StartCoroutine(PerformDive());
        }
    }

    IEnumerator PerformDive()
    {
        if (player == null) yield break;
        isDiving = true;

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position + new Vector3(0f, 1f, 0f);

        float t = 0f;
        float dist = Vector3.Distance(startPos, targetPos);
        float duration = dist / diveSpeed;

        while (t < 1f)
        {
            if (isDead || isDisabled) { isDiving = false; yield break; }
            t += Time.deltaTime / duration;
            rb.MovePosition(Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(t)));
            FlipSprite();

            if (t > 0.2f && Vector3.Distance(transform.position, player.position) < damageRadius)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(damageType);
                break;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        // Recuar suavemente pro lado oposto do player
        float retreatX = homePosition.x;
        if (player != null)
        {
            bool playerOnRight = player.position.x > transform.position.x;
            retreatX = player.position.x + (playerOnRight ? -orbitRadius : orbitRadius);
        }
        Vector3 retreatPos = new Vector3(retreatX, homePosition.y + floatHeight, 0f);

        float retreatSpeed = 3f;
        while (Vector3.Distance(transform.position, retreatPos) > 0.2f)
        {
            if (isDead || isDisabled) { isDiving = false; yield break; }
            Vector3 pos = Vector3.MoveTowards(transform.position, retreatPos, retreatSpeed * Time.deltaTime);
            rb.MovePosition(pos);
            FlipSprite();
            yield return null;
        }

        isDiving = false;
    }

    void FlipSprite()
    {
        if (spriteRenderer == null || player == null || isDead || isDisabled) return;
        Vector3 scale = transform.localScale;
        bool isRight = transform.position.x > player.position.x;
        scale.x = isRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public void TakeDamage(PlayerHealth.DamageType dmgType)
    {
        if (isDead || isDisabled || isInvincible) return;

        if (dmgType == PlayerHealth.DamageType.Light) lightStars++;
        else lightStars--;
        if (lightStars < 0) lightStars = 0;

        StartCoroutine(InvincibilityCooldown());
        StartCoroutine(KnockbackRoutine());
        StartCoroutine(FlashRed());

        if (lightStars >= maxStars)
            StartCoroutine(DeathExplosion());
        else if (lightStars <= 0)
            Disable();
    }

    void Disable() { isDisabled = true; StartCoroutine(FallAndFade()); }

    IEnumerator FallAndFade()
    {
        Debug.Log("FlyingEnemy: Iniciando queda!");

        // Mostrar sprite de desligado
        if (spriteRenderer != null && spriteDisabled != null)
            spriteRenderer.sprite = spriteDisabled;
        if (spriteDisabled2 != null)
        {
            yield return new WaitForSeconds(animSpeed);
            if (spriteRenderer != null) spriteRenderer.sprite = spriteDisabled2;
        }

        // Cair + contagem de 5s começa já no ar
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }

        // Fade out lento
        float fadeTime = 1.5f;
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator InvincibilityCooldown() { isInvincible = true; yield return new WaitForSeconds(invincibilityTime); isInvincible = false; }

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
        isKnockedBack = true;
        float elapsed = 0f;
        Vector3 dir = (player != null) ? (transform.position - player.position).normalized : Vector3.right;
        dir.y = 0f;
        while (elapsed < knockbackDuration) { elapsed += Time.deltaTime; transform.position += dir * knockbackForce * (1f - elapsed / knockbackDuration) * Time.deltaTime; yield return null; }
        isKnockedBack = false;
    }

    IEnumerator DeathExplosion()
    {
        isDead = true;
        GameObject flash = new GameObject("FlyingDeathFlash");
        flash.transform.position = transform.position;
        int texSize = 64;
        Texture2D tex = new Texture2D(texSize, texSize);
        Color[] pixels = new Color[texSize * texSize];
        float half = texSize / 2f;
        for (int y = 0; y < texSize; y++) for (int x = 0; x < texSize; x++) { float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half; pixels[y * texSize + x] = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, dist)); }
        tex.SetPixels(pixels); tex.Apply();
        SpriteRenderer fr = flash.AddComponent<SpriteRenderer>();
        fr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 1f);
        fr.sortingOrder = 999; fr.color = explosionColor;
        flash.transform.localScale = new Vector3(0.065f, 0.065f, 1f);
        float elapsed = 0f;
        while (elapsed < 0.4f) { elapsed += Time.deltaTime; Color c = fr.color; c.a = Mathf.Lerp(1f, 0f, elapsed / 0.4f); fr.color = c; yield return null; }
        Destroy(flash); Destroy(gameObject);
    }

    void Animate()
    {
        if (spriteRenderer == null) return;
        animTimer += Time.deltaTime;
        if (animTimer >= animSpeed) { animTimer = 0f; animFrame += animDir; if (animFrame >= 3) { animFrame = 1; animDir = -1; } if (animFrame <= 0) { animFrame = 1; animDir = 1; } switch (animFrame) { case 0: if (sprite1 != null) spriteRenderer.sprite = sprite1; break; case 1: if (sprite2 != null) spriteRenderer.sprite = sprite2; break; case 2: if (sprite3 != null) spriteRenderer.sprite = sprite3; break; } }
    }
}
