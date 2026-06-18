using UnityEngine;

public class PlataformaModo : MonoBehaviour
{
    [Header("Configuração da Plataforma")]
    [Tooltip("Essa plataforma deve aparecer em qual modo?")]
    public PlayerAttack.PlayerMode modoDestaPlataforma;

    // Referências
    private PlayerAttack player;
    private Renderer meuRenderizador;
    private Collider meuColisor;

    void Start()
    {
        // Pega os componentes do próprio bloquinho (visual e física)
        meuRenderizador = GetComponent<Renderer>();
        meuColisor = GetComponent<Collider>();

        // Procura a personagem no mapa automaticamente
        player = Object.FindFirstObjectByType<PlayerAttack>();

        if (player == null)
        {
            Debug.LogWarning("Plataforma não encontrou o Player no mapa!");
        }
    }

    void Update()
    {
        if (player == null) return;

        // A mágica acontece aqui: A plataforma deve estar ativa se o modo dela for IGUAL ao modo atual do player
        bool deveEstarAtiva = (player.currentMode == modoDestaPlataforma);

        // Liga ou desliga o visual (pra sumir da tela)
        if (meuRenderizador != null && meuRenderizador.enabled != deveEstarAtiva)
        {
            meuRenderizador.enabled = deveEstarAtiva;
        }

        // Liga ou desliga a física (pra personagem cair através dela)
        if (meuColisor != null && meuColisor.enabled != deveEstarAtiva)
        {
            meuColisor.enabled = deveEstarAtiva;
        }
    }
}