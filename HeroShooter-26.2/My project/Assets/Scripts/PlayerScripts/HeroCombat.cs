using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Vida, munição, tiro e especial. Serve para o PLAYER e para os INIMIGOS:
// quem decide quando atirar é o PlayerCombatInput ou o EnemyAI.
public class HeroCombat : MonoBehaviour, IDamageable
{
    // Todos os combatentes ativos (player + inimigos). Usado pela IA e pelo spawn.
    public static readonly List<HeroCombat> All = new List<HeroCombat>();

    // ====== DADOS DE CADA CLASSE ======
    struct ClassStats
    {
        public int maxHealth;
        public int magazineSize;
        public int reserveAmmo;
        public float fireDelay;
        public float reloadTime;
        public int pellets;          // projéteis por tiro
        public float spreadAngle;    // espalhamento em graus
        public int ammoPerShot;      // munição gasta por tiro
        public float specialDuration;
        public float specialCooldown;
        public int specialHeal;      // vida recuperada ao ativar o especial
        public float specialDamageTaken; // multiplicador de dano recebido no especial
    }

    static ClassStats GetStats(HeroClassManager.HeroClass heroClass)
    {
        switch (heroClass)
        {
            case HeroClassManager.HeroClass.Berserker:
                return new ClassStats
                {
                    maxHealth = 150,
                    magazineSize = 2,
                    reserveAmmo = 20,
                    fireDelay = 0.8f,
                    reloadTime = 1.7f,
                    pellets = 5,
                    spreadAngle = 7f,
                    ammoPerShot = 1,
                    specialDuration = 8f,
                    specialCooldown = 25f,
                    specialHeal = 50,
                    specialDamageTaken = 0.5f
                };

            default: // Soldier
                return new ClassStats
                {
                    maxHealth = 100,
                    magazineSize = 30,
                    reserveAmmo = 90,
                    fireDelay = 0.15f,
                    reloadTime = 1.3f,
                    pellets = 1,
                    spreadAngle = 0.5f,
                    ammoPerShot = 1,
                    specialDuration = 15f,
                    specialCooldown = 30f,
                    specialHeal = 0,
                    specialDamageTaken = 1f
                };
        }
    }

    [Header("Classe")]
    public HeroClassManager.HeroClass heroClass = HeroClassManager.HeroClass.Soldier;

    [Header("Armas")]
    public Transform firePoint;
    public GameObject secondWeapon;     // só Soldier (especial)
    public Transform secondFirePoint;   // só Soldier (especial)

    [Header("Respawn")]
    public float respawnDelay = 3f;

    [Header("Munição")]
    public bool infiniteReserve = false; // o EnemyAI liga isto

    [Header("Estado (somente leitura)")]
    public int maxHealth;
    public int currentHealth;
    public int magazineSize;
    public int currentAmmo;
    public int reserveAmmo;
    public bool isPlayer; // o PlayerCombatInput liga isto

    public bool IsDead { get; private set; }
    public bool IsReloading => reloading;
    public bool IsSpecialActive => specialActive;
    public Collider[] OwnColliders { get; private set; }

    public event System.Action<HeroCombat> OnDamaged; // atacante
    public event System.Action<HeroCombat> OnDied;    // atacante (quem matou)
    public event System.Action OnShot;                // disparado a cada tiro (usado pelo áudio)

    private ClassStats stats;
    private bool reloading;
    private bool specialActive;
    private float specialCooldownTimer;
    private float nextFireTime;
    private string lastSpecialText;

    // Centro do corpo, usado como ponto de mira por quem atira nesse alvo.
    public Vector3 AimPosition
    {
        get
        {
            if (OwnColliders != null)
            {
                foreach (Collider c in OwnColliders)
                {
                    if (c != null && c.enabled && !c.isTrigger &&
                        c.gameObject.activeInHierarchy)
                        return c.bounds.center;
                }
            }

            return transform.position + Vector3.up;
        }
    }

    void Awake()
    {
        stats = GetStats(heroClass);
        OwnColliders = GetComponentsInChildren<Collider>(true);

        if (secondWeapon != null)
            secondWeapon.SetActive(false);

        ApplyStatsFull();
    }

    void Start()
    {
        UpdateAmmoUI();
        UpdateLifeUI();
    }

    void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);

        // Corrotinas morrem quando o objeto é desativado.
        reloading = false;

        if (specialActive)
            EndSpecial();
    }

    void Update()
    {
        if (specialCooldownTimer > 0f)
            specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - Time.deltaTime);

        UpdateSpecialUI();
    }

    void ApplyStatsFull()
    {
        maxHealth = stats.maxHealth;
        currentHealth = maxHealth;
        magazineSize = stats.magazineSize;
        currentAmmo = magazineSize;
        reserveAmmo = stats.reserveAmmo;
        specialCooldownTimer = 0f;
        nextFireTime = 0f;
        reloading = false;
        specialActive = false;
        IsDead = false;
    }

    // ================= TIRO =================

    // aimPoint = ponto no mundo para onde a bala deve ir (mira da câmera ou do inimigo).
    public bool TryShoot(Vector3 aimPoint, float extraSpread = 0f)
    {
        if (IsDead || reloading || Time.time < nextFireTime)
            return false;

        if (firePoint == null)
        {
            Debug.LogError("Configure o Fire Point em " + name);
            return false;
        }

        if (ProjectilePool.Instance == null)
        {
            Debug.LogError("Falta o ProjectilePool na cena.");
            return false;
        }

        if (currentAmmo < stats.ammoPerShot)
            return false;

        nextFireTime = Time.time + stats.fireDelay;

        Fire(firePoint, aimPoint, extraSpread);

        // Soldier no especial: a segunda arma dispara junto.
        if (specialActive && secondWeapon != null && secondFirePoint != null)
            Fire(secondFirePoint, aimPoint, extraSpread);

        currentAmmo -= stats.ammoPerShot;
        UpdateAmmoUI();

        OnShot?.Invoke();
        return true;
    }

    void Fire(Transform fp, Vector3 aimPoint, float extraSpread)
    {
        // Direção: do firePoint até o ponto mirado.
        Vector3 dir = aimPoint - fp.position;

        // Ponto mirado atrás do cano (parede colada): atira para a frente.
        if (dir.sqrMagnitude < 0.0001f || Vector3.Dot(dir, transform.forward) <= 0f)
            dir = transform.forward;

        dir.Normalize();

        Quaternion baseRotation = Quaternion.LookRotation(dir);
        float spread = stats.spreadAngle + extraSpread;

        for (int i = 0; i < stats.pellets; i++)
        {
            Quaternion rotation = baseRotation;

            if (spread > 0f)
            {
                Vector2 r = Random.insideUnitCircle * spread;
                rotation = baseRotation * Quaternion.Euler(r.y, r.x, 0f);
            }

            Projectile p = ProjectilePool.Instance.Get();
            p.transform.SetPositionAndRotation(fp.position, rotation);
            p.StartProjectile(rotation * Vector3.forward, this);
        }
    }

    public void Reload()
    {
        if (IsDead || reloading) return;
        if (currentAmmo >= magazineSize) return;
        if (!infiniteReserve && reserveAmmo <= 0) return;

        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        reloading = true;

        yield return new WaitForSeconds(stats.reloadTime);

        int missing = magazineSize - currentAmmo;
        int toLoad = infiniteReserve ? missing : Mathf.Min(missing, reserveAmmo);

        currentAmmo += toLoad;

        if (!infiniteReserve)
            reserveAmmo -= toLoad;

        reloading = false;
        UpdateAmmoUI();
    }

    // ================= ESPECIAL =================

    public bool TryActivateSpecial()
    {
        if (IsDead || specialActive || specialCooldownTimer > 0f)
            return false;

        bool needsSecondWeapon = heroClass != HeroClassManager.HeroClass.Berserker;

        if (needsSecondWeapon && (secondWeapon == null || secondFirePoint == null))
        {
            Debug.LogWarning("Configure a segunda arma e o segundo Fire Point.");
            return false;
        }

        StartCoroutine(SpecialRoutine());
        return true;
    }

    IEnumerator SpecialRoutine()
    {
        specialActive = true;
        specialCooldownTimer = stats.specialCooldown;

        if (secondWeapon != null)
            secondWeapon.SetActive(true);

        if (stats.specialHeal > 0)
            Heal(stats.specialHeal);

        yield return new WaitForSeconds(stats.specialDuration);

        EndSpecial();
    }

    void EndSpecial()
    {
        specialActive = false;

        if (secondWeapon != null)
            secondWeapon.SetActive(false);
    }

    public void RecoverSpecialCooldown(float amount)
    {
        specialCooldownTimer = Mathf.Max(0f, specialCooldownTimer - amount);
    }

    // ================= VIDA =================

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateLifeUI();
    }

    public void TakeDamage(float amount, HeroCombat attacker)
    {
        if (IsDead) return;

        // Berserker no especial leva metade do dano.
        if (specialActive)
            amount *= stats.specialDamageTaken;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, Mathf.RoundToInt(amount)));
        UpdateLifeUI();

        OnDamaged?.Invoke(attacker);

        if (currentHealth <= 0)
            Die(attacker);
    }

    void Die(HeroCombat killer)
    {
        IsDead = true;
        OnDied?.Invoke(killer);

        Debug.Log(name + " morreu.");

        gameObject.SetActive(false);

        // A espera do respawn acontece no SpawnManager (que continua ativo).
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.RequestRespawn(this, respawnDelay);
        else
            Debug.LogError("Falta o SpawnManager na cena.");
    }

    // Chamado pelo SpawnManager.
    public void Respawn(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

        if (TryGetComponent(out Rigidbody body) && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        ApplyStatsFull();
        gameObject.SetActive(true);

        // Inimigos: coloca o agente de volta no NavMesh.
        if (TryGetComponent(out NavMeshAgent agent) && agent.enabled)
            agent.Warp(position);

        UpdateAmmoUI();
        UpdateLifeUI();
    }

    // ================= UI (só para o player) =================

    void UpdateAmmoUI()
    {
        if (isPlayer && inGameManager.Instance != null)
            inGameManager.Instance.UpdateAmmo(currentAmmo, reserveAmmo);
    }

    void UpdateLifeUI()
    {
        if (isPlayer && inGameManager.Instance != null)
            inGameManager.Instance.UpdateLife(currentHealth, maxHealth);
    }

    void UpdateSpecialUI()
    {
        if (!isPlayer || inGameManager.Instance == null)
            return;

        string text = specialActive ? "ATIVO"
            : specialCooldownTimer > 0f ? Mathf.CeilToInt(specialCooldownTimer) + "s"
            : "PRONTO";

        if (text != lastSpecialText)
        {
            lastSpecialText = text;
            inGameManager.Instance.UpdateSpecial(text);
        }
    }
}