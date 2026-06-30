using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    public enum DamageType { Light, Shadow }

    [Header("Sistema de Estrelas")]
    public int totalStars = 6;
    public int lightStars = 5;
    public int shadowStars = 1;

    [Header("Invencibilidade")]
    public float invincibilityDuration = 1.5f;
    public float blinkSpeed = 0.1f;
    private bool isInvincible = false;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.15f;

    [Header("Debug")]
    public bool enableDebugKeys = true;

    public int LightStars => lightStars;
    public int ShadowStars => shadowStars;

    public void SetInvincible(bool inv) { isInvincible = inv; }

    private void Start()
    {
        // Carregar vida salva do GameManager (persiste entre cenas)
        if (GameManager.Instance != null && GameManager.Instance.healthEverSaved)
        {
            lightStars = GameManager.Instance.lightStarsSaved;
            shadowStars = GameManager.Instance.shadowStarsSaved;
        }
        else
        {
            lightStars = 5;
            shadowStars = 1;
        }
    }

    private void Update()
    {
        if (enableDebugKeys && Keyboard.current != null)
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
                TakeDamage(DamageType.Light);
            if (Keyboard.current.lKey.wasPressedThisFrame)
                TakeDamage(DamageType.Shadow);
            // Teclas de cura: O = cura Luz, P = cura Sombra
            if (Keyboard.current.oKey.wasPressedThisFrame)
                Heal(DamageType.Light);
            if (Keyboard.current.pKey.wasPressedThisFrame)
                Heal(DamageType.Shadow);
        }
    }

    public void TakeDamage(DamageType damageType)
    {
        if (isInvincible) return; // Invencível após tomar dano
        if (damageType == DamageType.Light)
        {
            if (shadowStars <= 0) return; // Já tá tudo luz, mas não morre sem antes verificar
            shadowStars--;
            lightStars++;
        }
        else
        {
            if (lightStars <= 0) return;
            lightStars--;
            shadowStars++;
        }

        // Efeito visual de dano
        if (VignetteEffectManager.Instance != null)
        {
            if (damageType == DamageType.Light)
                VignetteEffectManager.Instance.TriggerLightDamage();
            else
                VignetteEffectManager.Instance.TriggerShadowDamage();
        }

        // Invencibilidade temporária
        StartCoroutine(InvincibilityFrames());

        Debug.Log($"Estrelas: {lightStars} Luz / {shadowStars} Sombra");

        // Verificar morte (tudo igual)
        if (lightStars == totalStars || shadowStars == totalStars)
        {
            Debug.Log("MORTE! Todas as estrelas iguais.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Heal(DamageType healType)
    {
        if (healType == DamageType.Light)
        {
            // Cura de Luz: converte Luz → Sombra (restaura equilíbrio)
            if (lightStars <= 0) return;
            lightStars--;
            shadowStars++;
        }
        else
        {
            // Cura de Sombra: converte Sombra → Luz
            if (shadowStars <= 0) return;
            shadowStars--;
            lightStars++;
        }

        Debug.Log($"Cura! Estrelas: {lightStars} Luz / {shadowStars} Sombra");
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckDamageSource(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckDamageSource(other.gameObject);
    }

    private void CheckDamageSource(GameObject obj)
    {
        DamageSource source = obj.GetComponent<DamageSource>();
        if (source != null)
        {
            TakeDamage(source.damageType);
            ApplyKnockback(obj.transform);
        }
    }

    private void ApplyKnockback(Transform fromWho)
    {
        Rigidbody playerRb = GetComponent<Rigidbody>();
        if (playerRb == null) return;

        Vector3 dir = (transform.position - fromWho.position).normalized;
        dir.y = 0.3f; // Um pouco pra cima (estilo Hollow Knight)
        dir.Normalize();

        StartCoroutine(KnockbackRoutine(playerRb, dir));
    }

    private System.Collections.IEnumerator KnockbackRoutine(Rigidbody rb, Vector3 dir)
    {
        // Guardar controle do movimento pra não interferir
        Movement mov = GetComponent<Movement>();
        bool wasEnabled = false;
        if (mov != null)
        {
            wasEnabled = mov.enabled;
            mov.enabled = false;
        }

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.fixedDeltaTime;
            rb.linearVelocity = dir * knockbackForce * (1f - elapsed / knockbackDuration);
            yield return new WaitForFixedUpdate();
        }

        // Devolver controle
        if (mov != null)
            mov.enabled = wasEnabled;
    }

    private System.Collections.IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Collider playerCol = GetComponent<Collider>();

        // Ignorar colisão com todos os inimigos próximos
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy e in enemies)
        {
            Collider enemyCol = e.GetComponent<Collider>();
            if (playerCol != null && enemyCol != null)
                Physics.IgnoreCollision(playerCol, enemyCol, true);
        }

        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            elapsed += blinkSpeed;
            if (sr != null)
                sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkSpeed);
        }

        // Reativar colisão
        foreach (Enemy e in enemies)
        {
            if (e == null) continue;
            Collider enemyCol = e.GetComponent<Collider>();
            if (playerCol != null && enemyCol != null)
                Physics.IgnoreCollision(playerCol, enemyCol, false);
        }

        if (sr != null)
            sr.enabled = true;
        isInvincible = false;
    }
}
