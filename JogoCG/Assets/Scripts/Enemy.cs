using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Configurações do Inimigo")]
    public float maxHealth = 30f;
    public float currentHealth;

    [Header("Visual Feedback (Dano)")]
    [SerializeField] private Renderer enemyRenderer;
    public Color damageColor = Color.red;
    public float flashDuration = 0.15f;

    private Color originalColor;
    private Coroutine flashCoroutine;

    void Start()
    {
        currentHealth = maxHealth;

        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponent<Renderer>();
        }

        if (enemyRenderer != null && enemyRenderer.material.HasProperty("_Color"))
        {
            originalColor = enemyRenderer.material.color;
        }
        else if (enemyRenderer != null)
        {
            // Fallback para URP/HDRP que podem usar _BaseColor
            if (enemyRenderer.material.HasProperty("_BaseColor"))
            {
                originalColor = enemyRenderer.material.GetColor("_BaseColor");
            }
        }
    }

    // Método chamado pelo SendMessage do Dash de Luz (e outros ataques)
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"[Inimigo {gameObject.name}] Recebeu {damage} de dano! Vida restante: {currentHealth}/{maxHealth}");

        // Piscar vermelho como feedback visual
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashDamageEffect());

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private IEnumerator FlashDamageEffect()
    {
        if (enemyRenderer == null) yield break;

        string colorProp = enemyRenderer.material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        
        enemyRenderer.material.SetColor(colorProp, damageColor);
        yield return new WaitForSeconds(flashDuration);
        enemyRenderer.material.SetColor(colorProp, originalColor);
    }

    private void Die()
    {
        Debug.Log($"[Inimigo {gameObject.name}] Morreu!");
        
        // Efeito de desaparecer simples
        Destroy(gameObject);
    }
}
