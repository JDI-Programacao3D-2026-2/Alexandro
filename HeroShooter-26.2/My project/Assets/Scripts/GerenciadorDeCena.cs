using UnityEngine;
using UnityEngine.SceneManagement;

// Mudamos o nome da classe para não conflitar com o Unity
public class GerenciadorDeCenas : MonoBehaviour 
{
    public enum Scene
    {
        Menu,
        CharacterSelection,
        Game,
        EasterEgg,
        Exit
    }

    // A variável pública fica aqui, para você escolher no Inspector do botão
    [Header("Escolha a cena deste botao:")]
    public Scene SceneToLoad;

    // Removemos o parâmetro da função! Agora o botão consegue enxergar o método.
    public void LoadScene()
    {
        // Usamos a variável que você escolheu lá no Inspector do botão
        switch (SceneToLoad)
        {
            case Scene.Menu:
                SceneManager.LoadScene("Menu");
                break;
            case Scene.CharacterSelection:
                SceneManager.LoadScene("CharacterSelection");
                break;
            case Scene.Game:
                SceneManager.LoadScene("Game");
                break;
            case Scene.EasterEgg:
                SceneManager.LoadScene("EasterEgg");
                break;
            case Scene.Exit:
                CloseGame();
                break;
        }
    }

    public void CloseGame() // Deixei pública para você conseguir usar em um botão de sair
    {
        Application.Quit();
    }
}
