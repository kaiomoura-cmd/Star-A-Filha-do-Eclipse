using UnityEngine;
using UnityEngine.SceneManagement;

public class PortaTransicao : MonoBehaviour
{
    [Header("Configura��es da Transi��o")]
    [Tooltip("Nome exato do arquivo da cena para onde o jogador vai")]
    public string nomeDaProximaCena;

    [Tooltip("A tag/ID do ponto de spawn onde o jogador vai nascer na PR�XIMA cena")]
    public string tagDoSpawnDestino;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Salva o modo atual
            PlayerAttack attackScript = other.GetComponent<PlayerAttack>();
            if (attackScript != null)
            {
                GameManager.Instance.modoAtualDoPlayer = (int)attackScript.currentMode;
            }

            // 2. Salva a vida atual do player
            PlayerHealth healthScript = other.GetComponent<PlayerHealth>();
            if (healthScript != null)
            {
                GameManager.Instance.lightStarsSaved = healthScript.LightStars;
                GameManager.Instance.shadowStarsSaved = healthScript.ShadowStars;
                GameManager.Instance.healthEverSaved = true;
            }

            // 3. Spawn da próxima fase
            GameManager.Instance.tagSpawnAlvo = tagDoSpawnDestino;

            // 4. Carrega a nova cena
            SceneManager.LoadScene(nomeDaProximaCena);
        }
    }
}