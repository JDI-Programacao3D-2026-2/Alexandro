using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Zona de captura: quem ficar dentro dela acumula tempo. O primeiro a completar
// o tempo total vence a partida. O tempo NÃO precisa ser seguido e NÃO zera ao morrer.
//
// Coloque este script no objeto da zona, junto de UM collider convexo (Box, Sphere
// ou Capsule), alto o bastante para pegar o personagem. Ele vira Trigger sozinho.
public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance;

    [Header("Captura")]
    public float captureTime = 60f;            // segundos acumulados para vencer
    public bool contestedStopsCapture = false; // true = com 2+ jogadores dentro, ninguém acumula

    [Header("UI")]
    public TMP_Text leaderText;                // "Jogador liderando o controle: ..."
    public TMP_Text progressText;              // (opcional) "25s / 60s" do líder

    private Collider zoneCollider;
    private bool matchEnded;

    private readonly Dictionary<HeroCombat, float> progress = new Dictionary<HeroCombat, float>();
    private readonly List<HeroCombat> inside = new List<HeroCombat>();

    private string lastLeaderName;
    private int lastLeaderSecond = -1;

    // Guarda a referência global e prepara o collider. Quem chama: a Unity.
    void Awake()
    {
        Instance = this;

        zoneCollider = GetComponent<Collider>();

        if (zoneCollider == null)
            Debug.LogError("A zona precisa de um Collider (Box, Sphere ou Capsule): " + name);
        else
            zoneCollider.isTrigger = true;
    }

    // Mostra o texto inicial. Quem chama: a Unity.
    void Start()
    {
        RefreshUI(true);
    }

    // Limpa a referência ao sair da cena. Quem chama: a Unity.
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Soma tempo a todos que estão dentro da zona e confere se alguém venceu.
    // Quem chama: a Unity, a cada frame.
    void Update()
    {
        if (matchEnded || zoneCollider == null)
            return;

        // Só combatentes vivos (os mortos ficam desativados e fora da lista All).
        inside.Clear();

        foreach (HeroCombat c in HeroCombat.All)
        {
            if (!c.IsDead && IsInside(c.transform.position))
                inside.Add(c);
        }

        bool canCapture = inside.Count > 0 && !(contestedStopsCapture && inside.Count > 1);

        if (canCapture)
        {
            foreach (HeroCombat c in inside)
            {
                progress.TryGetValue(c, out float time);
                time += Time.deltaTime;
                progress[c] = time;

                if (time >= captureTime)
                {
                    Win(c);
                    return;
                }
            }
        }

        RefreshUI(false);
    }

    // ======================= CONSULTAS (usadas pela IA) =======================

    // Diz se uma posição está dentro da zona. Quem chama: este script e o EnemyControllerBase.
    public bool IsInside(Vector3 position)
    {
        if (zoneCollider == null)
            return false;

        Vector3 closest = zoneCollider.ClosestPoint(position);
        return (closest - position).sqrMagnitude < 0.0001f;
    }

    // Sorteia um ponto dentro da zona para o inimigo ir até lá.
    // Quem chama: EnemyControllerBase.GoToCaptureZone().
    public Vector3 GetRandomPointInZone()
    {
        Bounds b = zoneCollider.bounds;

        for (int i = 0; i < 10; i++)
        {
            Vector3 point = new Vector3(
                Random.Range(b.min.x, b.max.x),
                b.center.y,
                Random.Range(b.min.z, b.max.z));

            if (IsInside(point))
                return point;
        }

        return b.center;
    }

    // ======================= VITÓRIA =======================

    // Encerra a partida avisando o inGameManager. Quem chama: Update.
    void Win(HeroCombat winner)
    {
        matchEnded = true;

        RefreshUI(true);

        if (inGameManager.Instance != null)
            inGameManager.Instance.EndMatch(NameOf(winner));
        else
            Debug.Log("Vencedor da captura: " + NameOf(winner));
    }

    // ======================= UI =======================

    // Mostra quem lidera a captura. Só mexe no texto quando algo muda.
    // Quem chama: Start, Update e Win.
    void RefreshUI(bool force)
    {
        HeroCombat leader = null;
        float best = 0f;

        foreach (KeyValuePair<HeroCombat, float> pair in progress)
        {
            if (pair.Value > best)
            {
                best = pair.Value;
                leader = pair.Key;
            }
        }

        string leaderName = leader != null ? NameOf(leader) : "Ninguém";
        int second = Mathf.FloorToInt(best);

        if (leaderText != null && (force || leaderName != lastLeaderName))
            leaderText.text = "Jogador liderando o controle: " + leaderName;

        if (progressText != null && (force || second != lastLeaderSecond))
            progressText.text = second + "s / " + captureTime.ToString("0") + "s";

        lastLeaderName = leaderName;
        lastLeaderSecond = second;
    }

    // Nome mostrado na tela (o do FragManager; se vazio, o nome do objeto).
    string NameOf(HeroCombat c)
    {
        return string.IsNullOrEmpty(c.displayName) ? c.name : c.displayName;
    }
}
