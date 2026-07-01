using UnityEngine;
using Unity.Cinemachine; // Biblioteca do Cinemachine novo

public class GerenciadorDeFase : MonoBehaviour
{
    [Header("Referências da Fase")]
    public GameObject prefabDoPlayer;
    public CinemachineCamera cameraDoCinemachine;

    void Start()
    {
        GameObject[] pontosDeSpawn = GameObject.FindGameObjectsWithTag("SpawnPoint");
        Transform spawnEscolhido = null;

        foreach (GameObject spawn in pontosDeSpawn)
        {
            if (spawn.name == GameManager.Instance.tagSpawnAlvo)
            {
                spawnEscolhido = spawn.transform;
                break;
            }
        }

        if (spawnEscolhido == null && pontosDeSpawn.Length > 0)
            spawnEscolhido = pontosDeSpawn[0].transform;

        if (spawnEscolhido != null)
        {
            GameObject playerInstanciado = Instantiate(prefabDoPlayer, spawnEscolhido.position, spawnEscolhido.rotation);
            playerInstanciado.name = "PLAYER";

            PlayerAttack attackScript = playerInstanciado.GetComponent<PlayerAttack>();
            PlayerModeVisual visualScript = playerInstanciado.GetComponent<PlayerModeVisual>();

            if (attackScript != null)
            {
                attackScript.currentMode = (PlayerAttack.PlayerMode)GameManager.Instance.modoAtualDoPlayer;
                if (visualScript != null)
                {
                    System.Reflection.MethodInfo method = visualScript.GetType().GetMethod("ApplyModeInstant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null) method.Invoke(visualScript, new object[] { attackScript.currentMode });
                }
            }

            // Criar NOVA câmera se não tiver
            CinemachineCamera cam = cameraDoCinemachine;
            if (cam == null)
            {
                GameObject camGo = new GameObject("PlayerCam");
                cam = camGo.AddComponent<CinemachineCamera>();
                cam.Priority = 10;
            }
            cam.gameObject.SetActive(true);

            GameObject camTarget = new GameObject("CameraTarget");
            camTarget.transform.SetParent(playerInstanciado.transform);
            camTarget.transform.localPosition = Vector3.zero;

            cam.Follow = camTarget.transform;
        }
        else
        {
            Debug.LogError("Nenhum Ponto de Spawn encontrado!");
        }
    }
}