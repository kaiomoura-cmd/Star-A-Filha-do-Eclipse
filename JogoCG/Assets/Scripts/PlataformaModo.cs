using UnityEngine;

public class PlataformaModo : MonoBehaviour
{
    [Header("Configuração da Plataforma")]
    [Tooltip("Essa plataforma deve aparecer em qual modo?")]
    public PlayerAttack.PlayerMode modoDestaPlataforma;

    private PlayerAttack player;
    private SpriteRenderer meuSprite;
    private Collider meuColisor;

    void Start()
    {
        meuSprite = GetComponent<SpriteRenderer>();
        meuColisor = GetComponent<Collider>();
    }

    void Update()
    {
        // Buscar player se não encontrou (spawn atrasa)
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.GetComponent<PlayerAttack>();
        }

        if (player == null) return;

        bool deveEstarAtiva = (player.currentMode == modoDestaPlataforma);

        if (meuSprite != null && meuSprite.enabled != deveEstarAtiva)
            meuSprite.enabled = deveEstarAtiva;

        if (meuColisor != null && meuColisor.enabled != deveEstarAtiva)
            meuColisor.enabled = deveEstarAtiva;
    }
}
