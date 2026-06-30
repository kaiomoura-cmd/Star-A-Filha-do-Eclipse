using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Persistência do Jogador")]
    public string tagSpawnAlvo = "Spawn_Inicial";
    public int modoAtualDoPlayer = 0; // 0 = Luz, 1 = Sombra
    public int lightStarsSaved = 5;
    public int shadowStarsSaved = 1;
    public bool healthEverSaved = false; // true = já veio de outro ato

    void Awake()
    {
        // Padr�o Singleton para garantir que s� exista um GameManager no jogo todo
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Impede que este objeto suma ao mudar de cena
    }
}