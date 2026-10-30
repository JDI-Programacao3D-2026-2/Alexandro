using System.Collections;
using TMPro;
using UnityEngine;

public class inGameManager : MonoBehaviour
{
    [Header("Referências")]
    public static inGameManager Instance;
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

    [Header("UI de Game Over")]
    public GameObject[] spawnpoints;

    void Awake()
    {
        Instance = this;

        if (HeroClassManager.Instance == null)
            SelectedClass = HeroClassManager.HeroClass.Soldier;
        else
            SelectedClass = HeroClassManager.Instance.GetHeroClass();

        if (SelectedClass == HeroClassManager.HeroClass.Soldier)
        {
            GameObject spawnedSoldier = Instantiate(
                PlayerSoldierPrefab, SpawnPoint.position, SpawnPoint.rotation);

            SoldierShootPool soldierShootPool =
                spawnedSoldier.GetComponent<SoldierShootPool>();

            if (soldierShootPool != null && MainCameraObject != null)
                soldierShootPool.aimCamera = MainCameraObject.GetComponent<Camera>();

            PlayerObject = spawnedSoldier;
        }
        else if (SelectedClass == HeroClassManager.HeroClass.Berserker)
        {
            PlayerObject = Instantiate(
                PlayerBerserkPrefab, SpawnPoint.position, SpawnPoint.rotation);
        }

        if (PlayerObject != null)
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

    // Chamado pelo soldado quando morre. A espera acontece aqui, num
    // objeto que continua ativo.
    public void RespawnPlayer(float delay, SoldierShootPool soldier)
    {
        StartCoroutine(RespawnRoutine(delay, soldier));
    }

    IEnumerator RespawnRoutine(float delay, SoldierShootPool soldier)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log("Soldier revivido!");

        if (soldier != null)
            soldier.ResetHealth();

        RevivePlayer();
    }

    public void RevivePlayer()
    {
        if (PlayerObject == null)
            return;

        // Joga o player para um spawnpoint aleatório.
        Transform target = SpawnPoint;

        if (spawnpoints != null && spawnpoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnpoints.Length);
            target = spawnpoints[randomIndex].transform;
        }

        PlayerObject.transform.SetPositionAndRotation(
            target.position, target.rotation);

        // Zera a velocidade para não "herdar" a queda/movimento anterior.
        if (PlayerObject.TryGetComponent(out Rigidbody playerRb))
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        PlayerObject.SetActive(true);
    }
}