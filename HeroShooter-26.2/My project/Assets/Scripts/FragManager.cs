using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Pontuação da sala: guarda pontos, finalizações, mortes e combo de CADA combatente
// (player e inimigos), atualiza o HUD do player e o painel de frags (segure Tab).
//
// IMPORTANTE: coloque este script em um objeto vazio (ou no Canvas), NUNCA no
// próprio painel de frags, porque o painel é ligado/desligado por este script.
public class FragManager : MonoBehaviour
{
    public static FragManager Instance;

    // Uma linha do painel. Para caber mais jogadores, aumente a lista "rows" no Inspector.
    [System.Serializable]
    public class FragRow
    {
        public GameObject root;       // o objeto da linha (liga/desliga conforme o nº de jogadores)
        public TMP_Text nameText;
        public TMP_Text scoreText;
        public TMP_Text killsText;
        public TMP_Text deathsText;
    }

    [Header("Painel de frags (segurar para mostrar)")]
    public GameObject fragPanel;
    public Key showKey = Key.Tab;
    public List<FragRow> rows = new List<FragRow>(); // 1 linha por jogador da sala

    [Header("Pontos")]
    public int killPoints = 300;          // por finalização
    public int streakBonusPerKill = 100;  // a cada finalização seguida SEM morrer, soma mais isto
    public float comboWindow = 20f;       // segundos para a próxima finalização manter o combo
    public int comboBonusEvery = 3;       // a cada X de combo...
    public int comboBonusPoints = 400;    // ...ganha este bônus

    // Dados de um combatente na tabela.
    class Entry
    {
        public HeroCombat combat;
        public string name;
        public int score;
        public int kills;
        public int deaths;
        public int killStreak;   // finalizações seguidas sem morrer
        public int combo;        // finalizações seguidas dentro da janela de tempo
        public float lastKillTime;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private readonly Dictionary<HeroCombat, Entry> byCombat = new Dictionary<HeroCombat, Entry>();
    private int enemyCounter;
    private bool panelDirty;
    private bool warnedRows;

    // Guarda a referência global. Quem chama: a Unity.
    void Awake()
    {
        Instance = this;

        if (fragPanel != null)
            fragPanel.SetActive(false);
    }

    // Limpa a referência ao sair da cena. Quem chama: a Unity.
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Cada frame: encerra combos que passaram do tempo e mostra/esconde o painel.
    // Quem chama: a Unity.
    void Update()
    {
        ExpireCombos();
        HandlePanel();
    }

    // ======================= REGISTRO =======================

    // Coloca um combatente na tabela e passa a ouvir as mortes dele.
    // Quem chama: HeroCombat.Start() (player e inimigos).
    public void Register(HeroCombat combat)
    {
        if (combat == null || byCombat.ContainsKey(combat))
            return;

        // Nome: o do Inspector; senão "Jogador" ou "Inimigo N".
        if (string.IsNullOrEmpty(combat.displayName))
            combat.displayName = combat.isPlayer ? "Player" : "Inimigo " + (++enemyCounter);

        Entry entry = new Entry { combat = combat, name = combat.displayName };
        entries.Add(entry);
        byCombat[combat] = entry;

        combat.OnDied += killer => HandleDeath(combat, killer);

        panelDirty = true;
        RefreshHud();
    }

    // ======================= PONTUAÇÃO =======================

    // Conta a morte da vítima e dá os pontos a quem matou.
    // Quem chama: o evento OnDied do HeroCombat (registrado em Register).
    void HandleDeath(HeroCombat victim, HeroCombat killer)
    {
        if (byCombat.TryGetValue(victim, out Entry victimEntry))
        {
            victimEntry.deaths++;
            victimEntry.killStreak = 0; // morreu: perde a sequência
            victimEntry.combo = 0;      // e o combo
        }

        if (killer != null && killer != victim && byCombat.TryGetValue(killer, out Entry killerEntry))
            AwardKill(killerEntry);

        panelDirty = true;
        RefreshHud();
    }

    // Soma os pontos de uma finalização: base + bônus de sequência + bônus de combo.
    // Quem chama: HandleDeath.
    void AwardKill(Entry e)
    {
        e.kills++;
        e.killStreak++;

        // Sequência: 1ª = só a base, 2ª = +1x bônus, 3ª = +2x bônus...
        int points = killPoints + streakBonusPerKill * (e.killStreak - 1);

        // Combo: mantém se a finalização veio dentro da janela de tempo.
        bool insideWindow = e.combo > 0 && Time.time - e.lastKillTime <= comboWindow;
        e.combo = insideWindow ? e.combo + 1 : 1;
        e.lastKillTime = Time.time;

        if (comboBonusEvery > 0 && e.combo % comboBonusEvery == 0)
            points += comboBonusPoints;

        e.score += points;
    }

    // Zera o combo de quem ficou tempo demais sem finalizar.
    // Quem chama: Update.
    void ExpireCombos()
    {
        foreach (Entry e in entries)
        {
            if (e.combo > 0 && Time.time - e.lastKillTime > comboWindow)
            {
                e.combo = 0;
                RefreshHud();
            }
        }
    }

    // ======================= HUD DO PLAYER =======================

    // Atualiza pontuação, maior pontuação da sala e combo na tela do player.
    // Quem chama: Register, HandleDeath e ExpireCombos.
    void RefreshHud()
    {
        if (inGameManager.Instance == null)
            return;

        int highScore = 0;
        Entry human = null;

        foreach (Entry e in entries)
        {
            if (e.score > highScore)
                highScore = e.score;

            if (e.combat != null && e.combat.isPlayer)
                human = e;
        }

        inGameManager.Instance.UpdateHighScore(highScore);

        if (human != null)
        {
            inGameManager.Instance.UpdateScore(human.score);
            inGameManager.Instance.UpdateCombo(human.combo);
        }
    }

    // Devolve quem tem a maior pontuação (desempate: mais finalizações, depois menos mortes).
    // Quem chama: inGameManager.EndMatch(), para o painel de fim de partida.
    public void GetTopScorer(out string playerName, out int score)
    {
        Entry best = null;

        foreach (Entry e in entries)
        {
            if (best == null || e.score > best.score ||
                (e.score == best.score && (e.kills > best.kills ||
                (e.kills == best.kills && e.deaths < best.deaths))))
            {
                best = e;
            }
        }

        playerName = best != null ? best.name : "-";
        score = best != null ? best.score : 0;
    }

    // ======================= PAINEL (TAB) =======================

    // Mostra o painel enquanto a tecla estiver segurada e o redesenha se houver mudança.
    // Quem chama: Update.
    void HandlePanel()
    {
        if (fragPanel == null || Keyboard.current == null)
            return;

        bool show = Keyboard.current[showKey].isPressed;

        if (fragPanel.activeSelf != show)
        {
            fragPanel.SetActive(show);

            if (show)
                panelDirty = true;
        }

        if (show && panelDirty)
        {
            panelDirty = false;
            RefreshPanel();
        }
    }

    // Preenche as linhas do painel, ordenadas do maior para o menor placar.
    // Quem chama: HandlePanel.
    void RefreshPanel()
    {
        List<Entry> sorted = new List<Entry>(entries);

        sorted.Sort((a, b) =>
        {
            int result = b.score.CompareTo(a.score);
            if (result != 0) return result;

            result = b.kills.CompareTo(a.kills);
            if (result != 0) return result;

            return a.deaths.CompareTo(b.deaths);
        });

        if (sorted.Count > rows.Count && !warnedRows)
        {
            warnedRows = true;
            Debug.LogWarning("FragManager: faltam linhas no painel (" + sorted.Count +
                             " jogadores e " + rows.Count + " linhas). Aumente a lista Rows.");
        }

        for (int i = 0; i < rows.Count; i++)
        {
            FragRow row = rows[i];

            if (row.root != null)
                row.root.SetActive(i < sorted.Count);

            if (i >= sorted.Count)
                continue;

            Entry e = sorted[i];
            bool isLocal = e.combat != null && e.combat.isPlayer;

            if (row.nameText != null) row.nameText.text = isLocal ? e.name + " (Você)" : e.name;
            if (row.scoreText != null) row.scoreText.text = e.score.ToString();
            if (row.killsText != null) row.killsText.text = e.kills.ToString();
            if (row.deathsText != null) row.deathsText.text = e.deaths.ToString();
        }
    }
}