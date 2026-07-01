using UnityEngine;

public class SeraphinSpike : MonoBehaviour
{
    public PlayerHealth.DamageType damageType = PlayerHealth.DamageType.Shadow;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damageType);
            Destroy(gameObject);
        }
    }
}
