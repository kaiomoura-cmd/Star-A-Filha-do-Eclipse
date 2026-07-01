using UnityEngine;
using System.Collections;

public class Seraphin : MonoBehaviour
{
    [Header("Sistema de Vida")]
    public int maxHits = 20;
    private int shadowHits = 0;

    [Header("Sprites de Animação")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] sprites = new Sprite[16];
    public float animSpeed = 0.3f;

    [Header("Invocação")]
    public float summonCooldown = 4f;
    public int spikesPerSummon = 3;

    [Header("Luz de Aviso")]
    public float warningDuration = 1f;

    [Header("Feedback")]
    public float invincibilityTime = 0.3f;

    private Transform player;
    private int animFrame = 0;
    private float animTimer;
    private bool isDead = false;
    private bool isInvincible = false;
    public bool IsDead => isDead;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && sprites.Length > 0 && sprites[0] != null)
            spriteRenderer.sprite = sprites[0];
        StartCoroutine(SummonRoutine());
    }

    void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (isDead) return;
        Animate();
    }

    void Animate()
    {
        if (spriteRenderer == null || sprites.Length == 0) return;
        animTimer += Time.deltaTime;
        if (animTimer >= animSpeed)
        {
            animTimer = 0f;
            animFrame = (animFrame + 1) % 16;
            if (sprites[animFrame] != null)
                spriteRenderer.sprite = sprites[animFrame];
        }
    }

    IEnumerator SummonRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(summonCooldown);
            if (isDead || player == null) continue;
            yield return StartCoroutine(SummonSpikes());
        }
    }

    IEnumerator SummonSpikes()
    {
        // Salvar posição onde a luz apareceu
        Vector3 spawnPos = player.position;

        // Aviso: luz brilhante na cabeça do player
        GameObject warning = new GameObject("SeraphinWarning");
        warning.transform.position = spawnPos + Vector3.up * 3f;

        SpriteRenderer wr = warning.AddComponent<SpriteRenderer>();
        wr.sortingOrder = 50;
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                pixels[y * size + x] = new Color(1f, 0.95f, 0.3f, (1f - dist) * 0.7f);
            }
        tex.SetPixels(pixels); tex.Apply();
        wr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        warning.transform.localScale = Vector3.one * 2f;

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 4f, 1f) + 0.5f;
            wr.color = new Color(1f, 0.95f, 0.3f, 0.7f * pulse);
            yield return null;
        }
        Destroy(warning);

        // Invocar 3 espinhos na posição salva
        for (int i = 0; i < spikesPerSummon; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, 0f);
            CreateSpike(spawnPos + offset);
            yield return new WaitForSeconds(0.15f);
        }
    }

    void CreateSpike(Vector3 pos)
    {
        GameObject spike = new GameObject("SeraphinSpike");
        spike.transform.position = pos;
        spike.transform.localScale = Vector3.one * 3f;

        SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        Rigidbody rb = spike.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        BoxCollider bc = spike.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(0.4f, 0.8f, 1f);
        bc.center = new Vector3(0f, 0.4f, 0f);

        int s = 32;
        Texture2D t = new Texture2D(s, s);
        Color[] p = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float tip = 1f - (float)y / s;
                float width = Mathf.Abs(x - s / 2f) / (s / 2f);
                float alpha = (width < 0.3f * tip) ? (1f - width / (0.3f * tip)) * 0.8f : 0f;
                p[y * s + x] = new Color(1f, 0.95f, 0.3f, alpha);
            }
        t.SetPixels(p); t.Apply();
        sr.sprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0f), 32f);

        SeraphinSpike ss = spike.AddComponent<SeraphinSpike>();
        ss.damageType = PlayerHealth.DamageType.Light;
        Destroy(spike, 5f);
    }

    public void TakeDamage(PlayerHealth.DamageType dmgType)
    {
        if (isDead || isInvincible) return;

        if (dmgType == PlayerHealth.DamageType.Light)
        {
            StartCoroutine(FlashWhite());
            return;
        }

        shadowHits++;
        StartCoroutine(InvincibilityCooldown());
        StartCoroutine(FlashRed());

        if (shadowHits >= maxHits)
            StartCoroutine(DeathExplosion());
    }

    IEnumerator FlashWhite()
    {
        if (spriteRenderer == null) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = orig;
    }

    IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = orig;
    }

    IEnumerator InvincibilityCooldown() { isInvincible = true; yield return new WaitForSeconds(invincibilityTime); isInvincible = false; }

    IEnumerator DeathExplosion()
    {
        isDead = true;
        GameObject flash = new GameObject("SeraphinDeath");
        flash.transform.position = transform.position + Vector3.up * 2f;

        int ts = 128;
        Texture2D t2 = new Texture2D(ts, ts);
        Color[] px = new Color[ts * ts];
        float hf = ts / 2f;
        for (int y = 0; y < ts; y++)
            for (int x = 0; x < ts; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(hf, hf)) / hf;
                px[y * ts + x] = new Color(0.5f, 0.1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, d));
            }
        t2.SetPixels(px); t2.Apply();

        SpriteRenderer fr = flash.AddComponent<SpriteRenderer>();
        fr.sprite = Sprite.Create(t2, new Rect(0, 0, ts, ts), new Vector2(0.5f, 0.5f), 1f);
        fr.sortingOrder = 999;
        flash.transform.localScale = Vector3.one * 0.2f;

        float elapsed = 0f;
        while (elapsed < 0.8f) { elapsed += Time.deltaTime; Color c = fr.color; c.a = Mathf.Lerp(1f, 0f, elapsed / 0.8f); fr.color = c; yield return null; }

        Destroy(flash);
        Destroy(gameObject);
    }
}
