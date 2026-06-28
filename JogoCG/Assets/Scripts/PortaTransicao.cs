using UnityEngine;
using UnityEngine.SceneManagement;

public class PortaTransicao : MonoBehaviour
{
    [Header("Configurações da Transição")]
    [Tooltip("Nome exato do arquivo da cena para onde o jogador vai")]
    public string nomeDaProximaCena;

    [Tooltip("A tag/ID do ponto de spawn onde o jogador vai nascer na PRÓXIMA cena")]
    public string tagDoSpawnDestino;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Salva o estado atual do player no GameManager antes de mudar
            PlayerAttack attackScript = other.GetComponent<PlayerAttack>();
            if (attackScript != null)
            {
                // Converte o modo atual para inteiro para salvar de forma simples
                GameManager.Instance.modoAtualDoPlayer = (int)attackScript.currentMode;
            }

            // 2. Avisa o GameManager qual é o spawn correto da próxima fase
            GameManager.Instance.tagSpawnAlvo = tagDoSpawnDestino;

            // 3. Carrega a nova cena
            SceneManager.LoadScene(nomeDaProximaCena);
        }
    }
}