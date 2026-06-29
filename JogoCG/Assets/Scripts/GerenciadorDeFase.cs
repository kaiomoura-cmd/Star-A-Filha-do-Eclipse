using UnityEngine;
using Unity.Cinemachine; // Biblioteca do Cinemachine novo

public class GerenciadorDeFase : MonoBehaviour
{
    [Header("Refer�ncias da Fase")]
    public GameObject prefabDoPlayer; // Arraste o Prefab do seu PLAYER aqui
    public CinemachineCamera cameraDoCinemachine; // Arraste a c�mera da fase aqui

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

        // Se n�o achar o espec�fico, tenta usar o primeiro que encontrar para o jogo n�o travar
        if (spawnEscolhido == null && pontosDeSpawn.Length > 0)
        {
            spawnEscolhido = pontosDeSpawn[0].transform;
        }

        if (spawnEscolhido != null)
        {
            // 2. Instancia o Player na posi��o do spawn correto
            GameObject playerInstanciado = Instantiate(prefabDoPlayer, spawnEscolhido.position, spawnEscolhido.rotation);
            playerInstanciado.name = "PLAYER"; // Mant�m o nome limpo

            // 3. Restaura o modo Luz ou Sombra que estava salvo
            PlayerAttack attackScript = playerInstanciado.GetComponent<PlayerAttack>();
            PlayerModeVisual visualScript = playerInstanciado.GetComponent<PlayerModeVisual>();

            if (attackScript != null)
            {
                // Converte de volta o inteiro para o formato do seu Enum
                attackScript.currentMode = (PlayerAttack.PlayerMode)GameManager.Instance.modoAtualDoPlayer;

                // For�a o script visual a atualizar a skin/luz instantaneamente no in�cio da fase
                if (visualScript != null)
                {
                    // Usa a fun��o de inicializa��o imediata que ajustamos ontem
                    System.Reflection.MethodInfo method = visualScript.GetType().GetMethod("ApplyModeInstant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null) method.Invoke(visualScript, new object[] { attackScript.currentMode });
                }
            }

            // 4. Configura a câmera do Cinemachine
            if (cameraDoCinemachine != null)
            {
                // Criar alvo separado pra câmera (CameraLook pode mover ele)
                GameObject camTarget = new GameObject("CameraTarget");
                camTarget.transform.SetParent(playerInstanciado.transform);
                camTarget.transform.localPosition = Vector3.zero;

                cameraDoCinemachine.Follow = camTarget.transform;
                cameraDoCinemachine.LookAt = playerInstanciado.transform;
            }
        }
        else
        {
            Debug.LogError("Nenhum Ponto de Spawn encontrado nesta cena! Verifique as tags.");
        }
    }
}