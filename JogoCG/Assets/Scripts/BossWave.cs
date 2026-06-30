using UnityEngine;

public class BossWave : MonoBehaviour
{
    [Header("Configuração (setado pelo Brokk)")]
    public Vector3 direction = Vector3.right;
    public float speed = 5f;
    public float damageRadius = 1.5f;
    public PlayerHealth.DamageType damageType = PlayerHealth.DamageType.Light;

    void Update()
    {
        // Viajar na direção
        transform.position += direction * speed * Time.deltaTime;

        // Verificar dano no player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < damageRadius)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(damageType);
                    Destroy(gameObject);
                }
            }
        }
    }
}
