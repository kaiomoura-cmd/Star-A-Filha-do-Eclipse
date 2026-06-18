using UnityEngine;

public class InimigoPatrulha : MonoBehaviour
{
    [Header("Configurações")]
    public float velocidade = 3f;
    public float danoNoPlayer = 10f;

    [Header("Sensores")]
    public Transform sensorChao;
    public Transform sensorParede;
    public LayerMask oQueEChao; // Coloque a layer "Ground" ou "Wall" aqui

    private bool indoParaDireita = true;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 1. Move o inimigo constantemente
        float direcao = indoParaDireita ? 1f : -1f;
        rb.linearVelocity = new Vector3(direcao * velocidade, rb.linearVelocity.y, 0f);

        // 2. Joga um raio invisível para baixo para ver se ainda tem chão
        bool temChao = Physics.Raycast(sensorChao.position, Vector3.down, 1.5f, oQueEChao);

        // 3. Joga um raio invisível para frente para ver se bateu no muro
        bool bateuParede = Physics.Raycast(sensorParede.position, Vector3.right * direcao, 0.2f, oQueEChao);

        // Se o chão acabou OU bateu na parede, ele vira
        if (!temChao || bateuParede)
        {
            Virar();
        }
    }

    void Virar()
    {
        indoParaDireita = !indoParaDireita;

        // Vira a imagem do inimigo espelhando a escala
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    void OnCollisionEnter(Collision colisao)
    {
        // Se bater no jogador, causa dano
        if (colisao.gameObject.CompareTag("Player"))
        {
            // O SendMessage tenta ativar uma função chamada "TakeDamage" no PlayerHealth
            colisao.gameObject.SendMessage("TakeDamage", danoNoPlayer, SendMessageOptions.DontRequireReceiver);
        }
    }
}