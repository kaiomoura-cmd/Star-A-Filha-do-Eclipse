using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Dano")]
    public PlayerHealth.DamageType damageType = PlayerHealth.DamageType.Shadow;
    public bool teleportToSafePosition = true;

    [Header("Brilho")]
    public Color glowColor = new Color(0.5f, 0.1f, 1f, 0.4f);
    public float glowScale = 1.5f;
    public int glowSortingOrder = -1;

    void Start()
    {
        CreateGlow();
    }

    void CreateGlow()
    {
        GameObject glow = new GameObject("SpikeGlow");
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * glowScale;

        SpriteRenderer sr = glow.AddComponent<SpriteRenderer>();
        sr.sortingOrder = glowSortingOrder;
        sr.color = glowColor;

        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float half = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                float alpha = (1f - Mathf.SmoothStep(0f, 1f, dist)) * glowColor.a;
                pixels[y * size + x] = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
            }
        tex.SetPixels(pixels); tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Causar dano
        PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damageType);

        // Teleportar pra última posição segura
        if (teleportToSafePosition)
        {
            Movement mov = collision.gameObject.GetComponent<Movement>();
            if (mov != null)
            {
                collision.transform.position = mov.lastSafePosition;

                Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
