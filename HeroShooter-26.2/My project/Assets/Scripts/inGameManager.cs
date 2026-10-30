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

        GameObject prefab =
            SelectedClass == HeroClassManager.HeroClass.Berserker
                ? PlayerBerserkPrefab
                : PlayerSoldierPrefab;

        PlayerObject = Instantiate(prefab, SpawnPoint.position, SpawnPoint.rotation);

        // Entrega a câmera de mira ao player.
        PlayerCombatInput input = PlayerObject.GetComponent<PlayerCombatInput>();

        if (input != null && MainCameraObject != null)
            input.aimCamera = MainCameraObject.GetComponent<Camera>();

        MainCamera.player = PlayerObject.transform;
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
}
