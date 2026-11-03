using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class inGameManager : MonoBehaviour
{
    [Header("Referências")]
    public static inGameManager Instance;
    public GameObject PlayerSoldierPrefab;
    public GameObject PlayerBerserkPrefab;
    public Transform SpawnPoint;            // só o primeiro spawn do player
    public GameObject MainCameraObject;
    public CameraMov MainCamera;
    public GameObject PlayerObject;
    public HeroClassManager.HeroClass SelectedClass;

    [Header("Pontuação")]
    public int Score;
    public int HighScore;
    public int ScoreMultiplier = 1;
    public int ComboCount = 0;

    [Header("UI")]
    public TextMeshProUGUI ScoreUI;
    public TextMeshProUGUI HighScoreUI;
    public TextMeshProUGUI ComboUI;
    public TextMeshProUGUI MultiplierUI;

    [Header("UI Informações do jogador")]
    public TextMeshProUGUI HealthUI;
    public TextMeshProUGUI AmmoUI;
    public TextMeshProUGUI SpecialUI;

    [Header("Fim de partida")]
    public GameObject endPanel;               // painel com o resultado (começa desligado)
    public TextMeshProUGUI winnerText;        // "Vencedor da captura: ..."
    public TextMeshProUGUI topScorerText;     // "Maior pontuador: ..."
    public string menuSceneName = "Menu";     // cena do botão "Voltar ao menu"

    public bool IsMatchOver { get; private set; }

    void Awake()
    {
        Instance = this;

        // Garante o jogo rodando (a partida anterior pode ter deixado o tempo parado).
        Time.timeScale = 1f;
        IsMatchOver = false;

        if (endPanel != null)
            endPanel.SetActive(false);

        // Sem seleção de classe (ex.: testando direto a cena Game), usa o Soldier.
        if (HeroClassManager.Instance == null)
            SelectedClass = HeroClassManager.HeroClass.Soldier;
        else
            SelectedClass = HeroClassManager.Instance.GetHeroClass();

        // Se nenhuma classe foi selecionada, usa Soldier como padrão.
        if (SelectedClass == HeroClassManager.HeroClass.None)
            SelectedClass = HeroClassManager.HeroClass.Soldier;

        GameObject prefab =
            SelectedClass == HeroClassManager.HeroClass.Berserker
                ? PlayerBerserkPrefab
                : PlayerSoldierPrefab;

        PlayerObject = Instantiate(prefab, SpawnPoint.position, SpawnPoint.rotation);

        // Entrega a câmera de mira ao player.
        // Os dois controladores de jogador herdam de PlayerControllerBase.
        PlayerControllerBase input = PlayerObject.GetComponent<PlayerControllerBase>();

        if (input != null && MainCameraObject != null)
            input.aimCamera = MainCameraObject.GetComponent<Camera>();

        if (MainCamera != null)
            MainCamera.player = PlayerObject.transform;
        else
            Debug.LogWarning("Configure a referência MainCamera no inGameManager.");
    }

    public void UpdateLife(int life, int maxLife)
    {
        if (HealthUI != null)
            HealthUI.text = life + "/" + maxLife;
    }

    public void UpdateAmmo(int currentAmmo, int reserveAmmo)
    {
        if (AmmoUI != null)
            AmmoUI.text = currentAmmo + "/" + reserveAmmo;
    }

    public void UpdateSpecial(string text)
    {
        if (SpecialUI != null)
            SpecialUI.text = text;
    }

    // Mostra a pontuação do jogador. Quem chama: FragManager, quando a pontuação muda.
    public void UpdateScore(int score)
    {
        Score = score;

        if (ScoreUI != null)
            ScoreUI.text = score.ToString();
    }

    // Mostra a MAIOR pontuação da sala (de qualquer jogador). Quem chama: FragManager.
    public void UpdateHighScore(int highScore)
    {
        HighScore = highScore;

        if (HighScoreUI != null)
            HighScoreUI.text = highScore.ToString();
    }

    // Mostra o combo só enquanto ele existir (0 = esconde). Quem chama: FragManager.
    public void UpdateCombo(int combo)
    {
        ComboCount = combo;

        if (ComboUI == null)
            return;

        ComboUI.gameObject.SetActive(combo > 0);

        if (combo > 0)
            ComboUI.text = "COMBO x" + combo;
    }

    // Encerra a partida: mostra o resultado, congela o jogo e solta o mouse.
    // Quem chama: ZoneManager, quando alguém completa o tempo de captura.
    public void EndMatch(string winnerName)
    {
        if (IsMatchOver)
            return;

        IsMatchOver = true;

        string topName = "-";
        int topScore = 0;

        if (FragManager.Instance != null)
            FragManager.Instance.GetTopScorer(out topName, out topScore);

        if (winnerText != null)
            winnerText.text = "Vencedor da captura: " + winnerName;

        if (topScorerText != null)
            topScorerText.text = "MVP: " + topName + " (" + topScore + " pts)";

        // Desliga os controles do player e da IA, para cliques nos botões não atirarem.
        foreach (HeroCombat c in HeroCombat.All)
        {
            foreach (MonoBehaviour b in c.GetComponents<MonoBehaviour>())
            {
                if (b is PlayerControllerBase || b is PlayerMove || b is EnemyControllerBase)
                    b.enabled = false;
            }
        }

        // Desligar a CameraMov impede que ela prenda o mouse de novo ao clicar nos botões.
        if (MainCamera != null)
            MainCamera.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;

        if (endPanel != null)
            endPanel.SetActive(true);
    }

    // Botão "Reiniciar partida": recarrega a cena atual. Ligar no OnClick do botão.
    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Botão "Voltar ao menu": carrega a cena do menu. Ligar no OnClick do botão.
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}