using UnityEngine;
using Unity.Cinemachine; // Biblioteca do Cinemachine novo

public class GerenciadorDeFase : MonoBehaviour
{
    [Header("Referências da Fase")]
    public GameObject prefabDoPlayer; // Arraste o Prefab do seu PLAYER aqui
    public CinemachineCamera cameraDoCinemachine; // Arraste a câmera da fase aqui

    void Start()
    {
        // 1. Encontra todos os pontos de spawn na cena atual
        GameObject[] pontosDeSpawn = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Transform spawnEscolhido = null;

        // Procura o spawn que possui o mesmo nome/ID salvo no GameManager
        foreach (GameObject spawn in pontosDeSpawn)
        {
            if (spawn.name == GameManager.Instance.tagSpawnAlvo)
            {
                spawnEscolhido = spawn.transform;
                break;
            }
        }

        // Se não achar o específico, tenta usar o primeiro que encontrar para o jogo não travar
        if (spawnEscolhido == null && pontosDeSpawn.Length > 0)
        {
            spawnEscolhido = pontosDeSpawn[0].transform;
        }

        if (spawnEscolhido != null)
        {
            // 2. Instancia o Player na posição do spawn correto
            GameObject playerInstanciado = Instantiate(prefabDoPlayer, spawnEscolhido.position, spawnEscolhido.rotation);
            playerInstanciado.name = "PLAYER"; // Mantém o nome limpo

            // 3. Restaura o modo Luz ou Sombra que estava salvo
            PlayerAttack attackScript = playerInstanciado.GetComponent<PlayerAttack>();
            PlayerModeVisual visualScript = playerInstanciado.GetComponent<PlayerModeVisual>();

            if (attackScript != null)
            {
                // Converte de volta o inteiro para o formato do seu Enum
                attackScript.currentMode = (PlayerAttack.PlayerMode)GameManager.Instance.modoAtualDoPlayer;

                // Força o script visual a atualizar a skin/luz instantaneamente no início da fase
                if (visualScript != null)
                {
                    // Usa a função de inicialização imediata que ajustamos ontem
                    System.Reflection.MethodInfo method = visualScript.GetType().GetMethod("ApplyModeInstant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null) method.Invoke(visualScript, new object[] { attackScript.currentMode });
                }
            }

            // 4. Configura a câmera do Cinemachine para seguir e olhar o novo Player criado
            if (cameraDoCinemachine != null)
            {
                cameraDoCinemachine.Follow = playerInstanciado.transform;
                cameraDoCinemachine.LookAt = playerInstanciado.transform;
            }
        }
        else
        {
            Debug.LogError("Nenhum Ponto de Spawn encontrado nesta cena! Verifique as tags.");
        }
    }
}