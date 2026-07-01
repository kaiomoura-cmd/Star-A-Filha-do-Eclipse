using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeMenu : MonoBehaviour
{
    public string nomeDaPrimeiraFase = "SampleScene";

    public void BotaoJogar()
    {
        SceneManager.LoadScene(nomeDaPrimeiraFase);
    }

    public void BotaoSair()
    {
        Debug.Log("O jogo foi fechado!");
        Application.Quit();
    }
}