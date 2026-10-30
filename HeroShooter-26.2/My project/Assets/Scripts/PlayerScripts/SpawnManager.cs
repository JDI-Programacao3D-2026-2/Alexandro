using System.Collections;
using UnityEngine;

// Escolhe onde player E inimigos nascem/renascem, e cria os inimigos da partida.
// Coloque em um objeto vazio da cena Game.
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Pontos de spawn (em cima do NavMesh!)")]
    public Transform[] spawnPoints;

    [Header("Escolha do ponto")]
    [Tooltip("Combatentes dentro deste raio deixam o ponto 'perigoso'.")]
    public float dangerRadius = 15f;

    [Header("Inimigos")]
    [Tooltip("Um prefab por classe (ex.: EnemySoldier, EnemyBerserker). Cada inimigo sorteia um.")]
    public GameObject[] enemyPrefabs;
    public int enemyCount = 5;

    private int lastUsedIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        for (int i = 0; i < enemyCount; i++)
        {
            Transform sp = GetBestSpawn();
            if (sp == null) return;

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Instantiate(prefab, sp.position, sp.rotation);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Regra: ignora o último ponto usado; prefere o ponto com MENOS combatentes
    // perto (dentro do dangerRadius); em empate, o mais longe do combatente mais próximo.
    public Transform GetBestSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager sem pontos de spawn.");
            return null;
        }

        int best = -1;
        int bestNearby = int.MaxValue;
        float bestMinDist = -1f;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (i == lastUsedIndex && spawnPoints.Length > 1)
                continue;

            Vector3 pos = spawnPoints[i].position;
            int nearby = 0;
            float minDist = float.MaxValue;

            foreach (HeroCombat c in HeroCombat.All)
            {
                if (c.IsDead) continue;

                float d = Vector3.Distance(pos, c.transform.position);

                if (d < dangerRadius) nearby++;
                if (d < minDist) minDist = d;
            }

            // Pequeno sorteio para desempatar quando ninguém está por perto.
            minDist += Random.value * 0.01f;

            if (nearby < bestNearby || (nearby == bestNearby && minDist > bestMinDist))
            {
                best = i;
                bestNearby = nearby;
                bestMinDist = minDist;
            }
        }

        lastUsedIndex = best;
        return spawnPoints[best];
    }

    public void RequestRespawn(HeroCombat combatant, float delay)
    {
        StartCoroutine(RespawnRoutine(combatant, delay));
    }

    IEnumerator RespawnRoutine(HeroCombat combatant, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (combatant == null)
            yield break;

        Transform sp = GetBestSpawn();
        if (sp == null)
            yield break;

        combatant.Respawn(sp.position, sp.rotation);
    }
}
