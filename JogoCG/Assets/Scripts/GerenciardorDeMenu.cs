using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeMenu : MonoBehaviour
{
    public string nomeDaPrimeiraFase = "SampleScene";

    public void BotaoJogar()
    {
        Debug.Log("Botao Jogar clicado! Carregando: " + nomeDaPrimeiraFase);
        SceneManager.LoadScene(nomeDaPrimeiraFase);
    }

    public void BotaoSair()
    {
        Debug.Log("O jogo foi fechado!");
        Application.Quit();
    }
}