using TMPro;
using UnityEngine;

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

    void Awake()
    {
        Instance = this;

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
}