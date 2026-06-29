using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class AtivarCutscene : MonoBehaviour
{
    [Header("Configura��es da C�mera")]
    public CinemachineCamera cameraDaCutscene;
    public float velocidadeDaCamera = 0.5f;

    [Header("Refer�ncias da Personagem (Preenchidas Automaticamente)")]
    public SpriteRenderer renderizadorPlayer;
    public Movement scriptMovimento;

    [Header("Configura��o dos Sprites")]
    public Sprite spriteCostas;
    public Sprite spriteOriginalLuz;

    [Range(0f, 1f)]
    [Tooltip("Em qual ponto do trilho (0 a 1) a personagem deve virar de costas? Ex: 0.5 � metade do caminho.")]
    public float pontoDaVirada = 0.5f;

    private bool jaMostrou = false;
    private CinemachineSplineDolly splineDolly;
    private bool mudouParaCostas = false;

    // Vari�vel para lembrar a rota��o original do jogador
    private Quaternion rotacaoOriginal;

    void Start()
    {
        if (cameraDaCutscene != null)
        {
            splineDolly = cameraDaCutscene.GetComponent<CinemachineSplineDolly>();
            cameraDaCutscene.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Tecla T = resetar cutscene pra testar de novo sem reload
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            jaMostrou = false;
            Debug.Log("Cutscene resetada! Passe pelo trigger de novo.");
        }
    }

    void OnTriggerEnter(Collider outro)
    {
        // Se quem encostou no cubo tem a tag "Player" e a cutscene ainda n�o rodou
        if (outro.CompareTag("Player") && !jaMostrou)
        {
            jaMostrou = true;

            // Captura os componentes da personagem que colidiu
            renderizadorPlayer = outro.GetComponent<SpriteRenderer>();
            scriptMovimento = outro.GetComponent<Movement>();

            if (cameraDaCutscene != null)
            {
                cameraDaCutscene.LookAt = outro.transform;
            }

            StartCoroutine(TocarCutscene());
        }
    }

    IEnumerator TocarCutscene()
    {
        if (splineDolly == null) yield break;

        // Guarda a rota��o que o jogador estava antes da cutscene come�ar
        if (renderizadorPlayer != null)
        {
            rotacaoOriginal = renderizadorPlayer.transform.rotation;
        }

        // 1. Trava o movimento e as anima��es padr�o da personagem antes da cena come�ar
        if (scriptMovimento != null)
        {
            scriptMovimento.isAttacking = true;
            scriptMovimento.enabled = false; // Desliga o script de movimento para evitar o bug do Dash
        }

        // Zera a posi��o e liga a c�mera cinematogr�fica
        splineDolly.CameraPosition = 0;
        mudouParaCostas = false;
        cameraDaCutscene.gameObject.SetActive(true);

        // 2. Faz a c�mera andar suavemente pelo trilho at� o final (posi��o 1.0)
        while (splineDolly.CameraPosition < 1f)
        {
            splineDolly.CameraPosition += velocidadeDaCamera * Time.deltaTime;

            // CHECAGEM DA VIRADA: Se a c�mera passou do ponto estipulado, troca o sprite
            if (!mudouParaCostas && splineDolly.CameraPosition >= pontoDaVirada)
            {
                mudouParaCostas = true;
                if (renderizadorPlayer != null && spriteCostas != null)
                {
                    renderizadorPlayer.sprite = spriteCostas;
                }
            }

            // ROTA��O (Anti-Invisibilidade):
            // For�a o plano do sprite a acompanhar o �ngulo Y da c�mera
            if (mudouParaCostas && renderizadorPlayer != null && cameraDaCutscene != null)
            {
                float anguloYDaCamera = cameraDaCutscene.transform.eulerAngles.y;

                // Aplica a rota��o apenas no eixo Y para o personagem n�o inclinar para frente ou para os lados
                renderizadorPlayer.transform.rotation = Quaternion.Euler(0, anguloYDaCamera, 0);
            }

            yield return null;
        }

        // 3. A c�mera para e fica aguardando o input do jogador
        bool apertouBotao = false;
        while (!apertouBotao)
        {
            // Mant�m o sprite alinhado mesmo enquanto espera o input
            if (mudouParaCostas && renderizadorPlayer != null && cameraDaCutscene != null)
            {
                float anguloYDaCamera = cameraDaCutscene.transform.eulerAngles.y;
                renderizadorPlayer.transform.rotation = Quaternion.Euler(0, anguloYDaCamera, 0);
            }

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                apertouBotao = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                apertouBotao = true;
            }

            yield return null;
        }

        // 4. O jogador apertou o bot�o! Restaura os sprites, devolve a rota��o original e o controle
        if (renderizadorPlayer != null)
        {
            if (spriteOriginalLuz != null)
                renderizadorPlayer.sprite = spriteOriginalLuz;

            // Devolve a rota��o que ele tinha antes da cutscene come�ar
            renderizadorPlayer.transform.rotation = rotacaoOriginal;
        }

        if (scriptMovimento != null)
        {
            scriptMovimento.isAttacking = false;
            scriptMovimento.enabled = true; // Religa o script de movimento, devolvendo o controle
        }

        cameraDaCutscene.gameObject.SetActive(false);
    }
}