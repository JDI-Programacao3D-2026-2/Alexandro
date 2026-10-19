using TMPro;
using UnityEngine;


public class inGameManager : MonoBehaviour
{
    [Header("Referências")]
    public static inGameManager instance;
    public GameObject PlayerSoldierPrefab;
    public GameObject PlayerBerserkPrefab;
    public Transform SpawnPoint;
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

        //Instanciamento do Player e da câmera principal
        instance = this;

        if (HeroClassManager.Instance == null)
        {
            SelectedClass = HeroClassManager.HeroClass.Soldier;
        }else
        {
            SelectedClass = HeroClassManager.Instance.GetHeroClass();
        }

        if (SelectedClass == HeroClassManager.HeroClass.Soldier)
        {
            GameObject spawnedSoldier = Instantiate(PlayerSoldierPrefab, SpawnPoint.position, SpawnPoint.rotation);
            
            SoldierShootPool soldierShootPool = spawnedSoldier.GetComponent<SoldierShootPool>();
            
            MainCamera.player = spawnedSoldier.transform;
            
            if (soldierShootPool != null && MainCameraObject != null)
            {
                soldierShootPool.aimCamera = MainCameraObject.GetComponent<Camera>();
            }
        }
        else if (SelectedClass == HeroClassManager.HeroClass.Berserker)
        {
            Instantiate(PlayerBerserkPrefab, SpawnPoint.position, SpawnPoint.rotation);
        }

        PlayerObject = GameObject.FindGameObjectWithTag("Player");
        MainCamera.player = PlayerObject.transform;


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
