using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class AtivarCutscene : MonoBehaviour
{
    [Header("Configurações da Câmera")]
    public CinemachineCamera cameraDaCutscene;
    public float velocidadeDaCamera = 0.5f;
    [Tooltip("Alvo que a câmera deve olhar durante a cutscene. Se vazio, olha pro player.")]
    public Transform lookTarget;
    [Range(0f, 1f)]
    [Tooltip("Ponto inicial da câmera no trilho (0 = começo, 1 = fim)")]
    public float cameraStartPosition = 0f;
    [Tooltip("Velocidade da personagem andando durante a cutscene")]
    public float walkSpeed = 2f;
    [Tooltip("Distância mínima do alvo antes de parar de andar")]
    public float stopDistance = 4f;

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
                // Olhar pro alvo configurado ou pro player como fallback
                cameraDaCutscene.LookAt = (lookTarget != null) ? lookTarget : outro.transform;
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

        // Zera a posição e liga a câmera cinematográfica
        splineDolly.CameraPosition = cameraStartPosition;
        mudouParaCostas = false;
        cameraDaCutscene.gameObject.SetActive(true);

        // 2. Faz a câmera andar suavemente pelo trilho até o final (posição 1.0)
        Transform playerTransform = null;
        Rigidbody playerRb = null;
        if (renderizadorPlayer != null)
        {
            playerTransform = renderizadorPlayer.transform;
            playerRb = playerTransform.GetComponent<Rigidbody>();
        }

        while (splineDolly.CameraPosition < 1f)
        {
            splineDolly.CameraPosition += velocidadeDaCamera * Time.deltaTime;

            // Auto-walk da personagem em direção ao alvo (templo)
            if (playerTransform != null && lookTarget != null)
            {
                float dist = Vector3.Distance(new Vector3(playerTransform.position.x, 0, 0),
                                              new Vector3(lookTarget.position.x, 0, 0));
                if (dist > stopDistance)
                {
                    float dirX = Mathf.Sign(lookTarget.position.x - playerTransform.position.x);
                    if (playerRb != null)
                        playerRb.linearVelocity = new Vector3(dirX * walkSpeed, 0f, 0f);
                    else
                        playerTransform.position += new Vector3(dirX * walkSpeed * Time.deltaTime, 0f, 0f);
                }
                else
                {
                    if (playerRb != null) playerRb.linearVelocity = Vector3.zero;
                }
            }

            // CHECAGEM DA VIRADA: Se a câmera passou do ponto estipulado, troca o sprite
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