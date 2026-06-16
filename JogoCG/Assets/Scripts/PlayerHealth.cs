using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    public enum DamageType
    {
        Light,
        Shadow
    }

    [Header("Configurações de Vida")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Teclas de Teste de Dano")]
    public bool enableDebugKeys = true;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (enableDebugKeys && Keyboard.current != null)
        {
            if (Keyboard.current[Key.K].wasPressedThisFrame)
            {
                TakeDamage(DamageType.Light);
            }
            if (Keyboard.current[Key.L].wasPressedThisFrame)
            {
                TakeDamage(DamageType.Shadow);
            }
        }
    }

    public void TakeDamage(DamageType damageType)
    {
        Debug.Log($"Jogador recebeu dano do tipo: {damageType}");

        // Dispara o efeito visual correto com base no tipo de dano
        if (VignetteEffectManager.Instance != null)
        {
            if (damageType == DamageType.Light)
            {
                VignetteEffectManager.Instance.TriggerLightDamage();
            }
            else
            {
                VignetteEffectManager.Instance.TriggerShadowDamage();
            }
        }
        else
        {
            Debug.LogWarning("VignetteEffectManager.Instance não foi encontrado na cena!");
        }
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
        }
    }
}
