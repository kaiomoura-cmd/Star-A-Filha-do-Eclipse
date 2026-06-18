using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem; // Biblioteca necessária para ler o teclado e mouse no novo sistema

public class AtivarCutscene : MonoBehaviour
{
    [Header("Arraste a CinemachineCamera aqui")]
    public CinemachineCamera cameraDaCutscene;

    [Header("Velocidade do movimento no trilho")]
    public float velocidadeDaCamera = 0.5f;

    private bool jaMostrou = false;
    private CinemachineSplineDolly splineDolly;

    void Start()
    {
        if (cameraDaCutscene != null)
        {
            splineDolly = cameraDaCutscene.GetComponent<CinemachineSplineDolly>();
            cameraDaCutscene.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag("Player") && !jaMostrou)
        {
            jaMostrou = true;
            StartCoroutine(TocarCutscene());
        }
    }

    IEnumerator TocarCutscene()
    {
        if (splineDolly == null) yield break;

        // Zera a posição e liga a câmera cinematográfica
        splineDolly.CameraPosition = 0;
        cameraDaCutscene.gameObject.SetActive(true);

        // 1. Faz a câmera andar suavemente pelo trilho até o final (posição 1.0)
        while (splineDolly.CameraPosition < 1f)
        {
            splineDolly.CameraPosition += velocidadeDaCamera * Time.deltaTime;
            yield return null;
        }

        // 2. A câmera para e fica aguardando o input do jogador
        bool apertouBotao = false;
        while (!apertouBotao)
        {
            // Checa se apertou qualquer tecla do teclado
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                apertouBotao = true;
            }
            // Checa se clicou com o botão esquerdo do mouse
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                apertouBotao = true;
            }

            // Pausa a rotina até o próximo frame para não travar o jogo
            yield return null;
        }

        // 3. O jogador apertou o botão! Desliga a câmera e volta para o gameplay normal
        cameraDaCutscene.gameObject.SetActive(false);
    }
}