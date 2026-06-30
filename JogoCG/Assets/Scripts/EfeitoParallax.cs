using UnityEngine;

public class EfeitoParallax : MonoBehaviour
{
    [Header("Configurações")]
    public Transform cameraDoJogo;

    [Tooltip("0 = Acompanha a câmera (muito longe). 1 = Fica parado (muito perto).")]
    [Range(0f, 1f)]
    public float multiplicadorParallax = 0.5f;

    private Vector3 ultimaPosicaoCamera;

    void Start()
    {
        // Se não tiver arrastado a câmera no Inspector, ele acha a principal
        if (cameraDoJogo == null) cameraDoJogo = Camera.main.transform;

        ultimaPosicaoCamera = cameraDoJogo.position;
    }

    void LateUpdate()
    {
        // Calcula o quanto a câmera andou desde o último frame
        Vector3 movimentoCamera = cameraDoJogo.position - ultimaPosicaoCamera;

        // Move o fundo criando o efeito, MAS APENAS NO EIXO X. O Y fica em 0.
        transform.position += new Vector3(movimentoCamera.x * multiplicadorParallax, 0, 0);

        // Atualiza a posição para o próximo frame
        ultimaPosicaoCamera = cameraDoJogo.position;
    }
}