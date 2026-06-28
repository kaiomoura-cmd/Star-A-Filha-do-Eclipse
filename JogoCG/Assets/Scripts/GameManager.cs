using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Persistência do Jogador")]
    public string tagSpawnAlvo = "Spawn_Inicial"; // Guarda o ID do spawn onde o jogador deve nascer
    public int modoAtualDoPlayer = 0; // 0 = Luz, 1 = Sombra (ou use o seu Enum original)

    void Awake()
    {
        // Padrão Singleton para garantir que só exista um GameManager no jogo todo
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Impede que este objeto suma ao mudar de cena
    }
}