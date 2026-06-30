using UnityEngine;
using System.Collections;

public class Brokk : MonoBehaviour
{
    [Header("Sistema de Estrelas")]
    public int totalStars = 20;
    public int lightStars = 10;
    public int shadowStars = 10;

    [Header("Movimento")]
    public float walkSpeed = 2f;
    public Sprite walkSprite1;
    public Sprite walkSprite2;

    [Header("Ataque")]
    public float attackCooldown = 3f;
    public float maxAttackRange = 10f;
    public GameObject wavePrefab;
    public Sprite attackSprite1;
    public Sprite attackSprite2;
    public Sprite attackSprite3;
    public Sprite attackSprite4;

    [Header("Onda")]
    public float waveSpeed = 5f;
    public float waveDamageRadius = 1.5f;
    public Color lightWaveColor = new Color(1f, 0.9f, 0.3f, 1f);
    public Color shadowWaveColor = new Color(0.5f, 0.1f, 1f, 1f);

    [Header("Knockback")]
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.2f;
    private bool isKnockedBack = false;

    [Header("Feedback Visual")]
    public float flashDuration = 0.15f;

    [Header("Invencibilidade")]
    public float invincibilityTime = 0.3f;

    [Header("Dano de Contato")]
    public PlayerHealth.DamageType contactDamageType = PlayerHealth.DamageType.Shadow;

    private Transform player;
    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;
    private bool isInvincible = false;
    private bool isAttacking = false;
    private float walkAnimTimer;
    private bool showingWalk1 = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        StartCoroutine(AttackRoutine());
    }

    void Update()
    {
        if (player == null) FindPlayer();
        if (isDead || isAttacking) return;
        AnimateWalk();
    }

    void FixedUpdate()
    {
        if (player == null) FindPlayer();
        if (isDead || isAttacking || isKnockedBack || player == null || rb == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > maxAttackRange * 0.5f)
        {
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            rb.linearVelocity = new Vector3(dirX * walkSpeed, 0f, 0f);
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Flip sprite
        if (spriteRenderer != null)
        {
            Vector3 scale = transform.localScale;
            scale.x = (player.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    IEnumerator AttackRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(attackCooldown);
            if (isDead || player == null) continue;
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > maxAttackRange) continue;
            yield return StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        if (attackSprite1 != null) SetSprite(attackSprite1);
        yield return new WaitForSeconds(0.15f);
        if (attackSprite2 != null) SetSprite(attackSprite2);
        yield return new WaitForSeconds(0.15f);
        if (attackSprite3 != null) SetSprite(attackSprite3);
        yield return new WaitForSeconds(0.15f);
        if (attackSprite4 != null) SetSprite(attackSprite4);
        SpawnWaves();
        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
    }

    void SpawnWaves()
    {
        if (player == null) return;
        bool useLight = lightStars >= 10;
        Color waveColor = useLight ? lightWaveColor : shadowWaveColor;
        PlayerHealth.DamageType dmgType = useLight ? PlayerHealth.DamageType.Light : PlayerHealth.DamageType.Shadow;

        Vector3 dir = (player.position - transform.position).normalized;
        StartCoroutine(SpawnWaveDelayed(dir, waveColor, dmgType, 0f));
        StartCoroutine(SpawnWaveDelayed(dir, waveColor, dmgType, 0.15f));
    }

    IEnumerator SpawnWaveDelayed(Vector3 dir, Color color, PlayerHealth.DamageType dmgType, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (wavePrefab == null)
        {
            GameObject wave = new GameObject("BrokkWave");
            wave.transform.position = transform.position + Vector3.up * 1f;

            BossWave bw = wave.AddComponent<BossWave>();
            bw.direction = dir;
            bw.speed = waveSpeed;
            bw.damageRadius = waveDamageRadius;
            bw.damageType = dmgType;

            SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs(x - size / 2f) / (size / 2f);
                    float alpha = (y > size * 0.3f && y < size * 0.7f) ? (1f - d) : 0f;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            tex.SetPixels(pixels); tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            sr.sortingOrder = 10;
            Destroy(wave, 3f);
        }
        else
        {
            GameObject wave = Instantiate(wavePrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            BossWave bw = wave.GetComponent<BossWave>();
            if (bw != null)
            {
                bw.direction = dir;
                bw.speed = waveSpeed;
                bw.damageRadius = waveDamageRadius;
                bw.damageType = dmgType;
            }
            Destroy(wave, 3f);
        }
    }

    void AnimateWalk()
    {
        if (walkSprite1 == null || walkSprite2 == null || spriteRenderer == null) return;
        walkAnimTimer += Time.deltaTime;
        if (walkAnimTimer >= 0.3f)
        {
            walkAnimTimer = 0f;
            showingWalk1 = !showingWalk1;
            spriteRenderer.sprite = showingWalk1 ? walkSprite1 : walkSprite2;
        }
    }

    void SetSprite(Sprite s) { if (spriteRenderer != null && s != null) spriteRenderer.sprite = s; }

    public void TakeDamage(PlayerHealth.DamageType dmgType)
    {
        if (isDead || isInvincible) return;

        if (dmgType == PlayerHealth.DamageType.Light) { shadowStars--; lightStars++; }
        else { lightStars--; shadowStars++; }

        if (lightStars < 0) lightStars = 0;
        if (shadowStars < 0) shadowStars = 0;

        StartCoroutine(InvincibilityCooldown());
        StartCoroutine(KnockbackRoutine());
        StartCoroutine(FlashRed());

        if (lightStars == totalStars || shadowStars == totalStars)
            StartCoroutine(DeathExplosion());
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
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            if (rb != null)
                rb.linearVelocity = dir * knockbackForce * (1f - elapsed / knockbackDuration);
            else
                transform.position += dir * knockbackForce * (1f - elapsed / knockbackDuration) * Time.deltaTime;
            yield return null;
        }
        isKnockedBack = false;
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(contactDamageType);
        }
    }

    IEnumerator DeathExplosion()
    {
        isDead = true;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        GameObject flash = new GameObject("BossDeathFlash");
        flash.transform.position = transform.position + Vector3.up * 2f;

        int texSize = 64;
        Texture2D tex = new Texture2D(texSize, texSize);
        Color[] pixels = new Color[texSize * texSize];
        float half = texSize / 2f;
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                pixels[y * texSize + x] = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, d));
            }
        tex.SetPixels(pixels); tex.Apply();

        SpriteRenderer fr = flash.AddComponent<SpriteRenderer>();
        fr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 1f);
        fr.sortingOrder = 999;
        fr.color = (lightStars >= totalStars) ? new Color(1f, 0.95f, 0.3f) : new Color(0.5f, 0.1f, 1f);
        flash.transform.localScale = new Vector3(0.13f, 0.13f, 1f);

        float elapsed = 0f;
        while (elapsed < 0.6f) { elapsed += Time.deltaTime; Color c = fr.color; c.a = Mathf.Lerp(1f, 0f, elapsed / 0.6f); fr.color = c; yield return null; }

        Destroy(flash);
        Destroy(gameObject);
    }
}
