using UnityEngine;

// Lógica compartilhada da IA: encontrar alvos, perceber obstáculos e decidir quando atirar.
// SoldierEnemyController e BerserkerEnemyController definem as diferenças entre as classes.
public abstract class EnemyControllerBase : MonoBehaviour
{
    [Header("Visão")]
    public float sightRange = 40f;
    public float viewAngle = 200f;
    public float closeAwareness = 6f;
    public float eyeHeight = 0.5f;
    public LayerMask sightMask = ~0;
    public float scanInterval = 0.25f;
    public float loseTargetTime = 3f;

    [Header("Combate")]
    public float desiredRange = 14f;
    public float maxShootRange = 35f;
    public float rangeTolerance = 2f;
    public float aimTolerance = 8f;
    public float extraSpread = 2f;
    public float repathInterval = 0.2f;

    [Header("Perambulação")]
    public float wanderRadius = 25f;
    public Vector2 wanderWait = new Vector2(0.5f, 2f);

    protected HeroCombat combat;
    protected EnemyNavMesh navigation;

    private HeroCombat target;
    private bool targetVisible;
    private Vector3 lastSeenPosition;
    private float timeSinceSeen;
    private float scanTimer;
    private float repathTimer;
    private float waitTimer;
    private bool hasWanderDestination;
    private bool hasZoneDestination;
    private readonly RaycastHit[] sightHits = new RaycastHit[16];

    // Cada classe informa qual classe de atributos deve usar e configura suas distâncias.
    protected abstract HeroClassManager.HeroClass CharacterClass { get; }
    protected abstract void ConfigureClassBehavior();
    protected abstract bool ShouldUseSpecial();

    protected virtual void Awake()
    {
        combat = GetComponent<HeroCombat>();
        navigation = GetComponent<EnemyNavMesh>();

        if (combat == null)
            Debug.LogError("O inimigo precisa do componente HeroCombat: " + name);

        if (navigation == null)
            Debug.LogError("O inimigo precisa do componente EnemyNavMesh: " + name);
    }

    protected virtual void OnEnable()
    {
        if (combat != null)
            combat.OnDamaged += HandleDamaged;

        target = null;
        targetVisible = false;
        hasWanderDestination = false;
        hasZoneDestination = false;
        waitTimer = 0f;
    }

    protected virtual void Start()
    {
        if (combat == null || navigation == null)
        {
            enabled = false;
            return;
        }

        // Inimigos usam a mesma classe de atributos, mas não ficam sem munição.
        combat.ConfigureClass(CharacterClass);
        combat.infiniteReserve = true;

        // Cada controlador configura seu próprio alcance e comportamento do especial.
        ConfigureClassBehavior();
    }

    protected virtual void OnDisable()
    {
        if (combat != null)
            combat.OnDamaged -= HandleDamaged;
    }

    protected virtual void Update()
    {
        if (combat == null || combat.IsDead || navigation == null || !navigation.IsReady())
            return;

        // Recarrega quando estiver com pouca munição e não estiver em combate direto.
        if (combat.currentAmmo < combat.magazineSize / 2 &&
            !combat.IsReloading && target == null)
        {
            combat.Reload();
        }

        // A visão é atualizada em intervalos para não fazer Raycast todo frame.
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            ScanForTargets();
        }

        if (target != null && target.IsDead)
        {
            target = null;
            targetVisible = false;
        }

        if (target == null)
        {
            // Sem alvo: vai para a zona de captura (se existir) ou perambula.
            if (ZoneManager.Instance != null)
                GoToCaptureZone();
            else
                Wander();

            return;
        }

        // Com alvo ele luta; depois precisará escolher o destino da zona de novo.
        hasZoneDestination = false;

        if (targetVisible)
            EngageTarget();
        else
            GoToLastSeenPosition();
    }

    // Procura o combatente mais próximo que esteja dentro do alcance e visível.
    void ScanForTargets()
    {
        HeroCombat bestTarget = null;
        float bestDistance = float.MaxValue;

        foreach (HeroCombat possibleTarget in HeroCombat.All)
        {
            if (possibleTarget == null || possibleTarget == combat || possibleTarget.IsDead)
                continue;

            float distance = Vector3.Distance(transform.position, possibleTarget.transform.position);

            if (distance > sightRange || distance >= bestDistance)
                continue;

            if (distance > closeAwareness && !InFieldOfView(possibleTarget))
                continue;

            if (!HasLineOfSight(possibleTarget))
                continue;

            bestTarget = possibleTarget;
            bestDistance = distance;
        }

        if (bestTarget != null)
        {
            target = bestTarget;
            targetVisible = true;
            lastSeenPosition = target.AimPosition;
            timeSinceSeen = 0f;
        }
        else if (target != null)
        {
            targetVisible = false;
            timeSinceSeen += scanInterval;

            if (timeSinceSeen > loseTargetTime)
                target = null;
        }
    }

    // Verifica se o alvo está dentro do ângulo de visão do inimigo.
    bool InFieldOfView(HeroCombat possibleTarget)
    {
        Vector3 direction = possibleTarget.transform.position - transform.position;
        direction.y = 0f;
        return Vector3.Angle(transform.forward, direction) <= viewAngle * 0.5f;
    }

    // Verifica se há uma parede ou outro obstáculo entre o inimigo e o alvo.
    bool HasLineOfSight(HeroCombat possibleTarget)
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 direction = possibleTarget.AimPosition - eyePosition;
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return true;

        int count = Physics.RaycastNonAlloc(
            eyePosition,
            direction / distance,
            sightHits,
            distance + 0.5f,
            sightMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        Collider closestCollider = null;

        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = sightHits[i].collider;

            if (combat.OwnColliders != null &&
                System.Array.IndexOf(combat.OwnColliders, hitCollider) >= 0)
                continue;

            if (hitCollider.GetComponentInParent<Projectile>() != null)
                continue;

            if (sightHits[i].distance < closestDistance)
            {
                closestDistance = sightHits[i].distance;
                closestCollider = hitCollider;
            }
        }

        return closestCollider != null &&
               closestCollider.GetComponentInParent<HeroCombat>() == possibleTarget;
    }

    // Se levar dano de alguém que não está mirando, passa a procurar esse atacante.
    void HandleDamaged(HeroCombat attacker)
    {
        if (attacker == null || attacker == combat || attacker.IsDead)
            return;

        if (target == null || !targetVisible)
        {
            target = attacker;
            targetVisible = false;
            lastSeenPosition = attacker.AimPosition;
            timeSinceSeen = 0f;
        }
    }

    // Decide se deve aproximar, recuar ou ficar parado para atirar.
    void EngageTarget()
    {
        Vector3 toTarget = target.AimPosition - transform.position;
        Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = toTarget.magnitude;

        navigation.FaceDirection(flatDirection);
        repathTimer -= Time.deltaTime;

        if (distance > desiredRange + rangeTolerance)
        {
            // O alvo está longe: aproxima.
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                navigation.MoveTo(target.transform.position);
            }
        }
        else if (distance < desiredRange * 0.5f)
        {
            // O alvo está perto demais: tenta recuar alguns metros.
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                Vector3 awayPosition = transform.position - flatDirection.normalized * 4f;

                if (navigation.TryGetNavMeshPoint(awayPosition, 4f, out Vector3 validPosition))
                    navigation.MoveTo(validPosition);
            }
        }
        else
        {
            // Está na distância desejada: para para mirar e atirar.
            navigation.Stop();
        }

        float angle = Vector3.Angle(transform.forward, flatDirection);

        if (distance <= maxShootRange && angle <= aimTolerance)
        {
            if (!combat.TryShoot(target.AimPosition, extraSpread) && combat.currentAmmo < 1)
                combat.Reload();

            if (ShouldUseSpecial())
                combat.TryActivateSpecial();
        }
    }

    // Vai até o último local em que o alvo foi visto.
    void GoToLastSeenPosition()
    {
        navigation.FaceVelocity();
        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            navigation.MoveTo(lastSeenPosition);
        }
    }

    // Vai até a zona de captura e fica parado dentro dela, capturando.
    // Quem chama: Update, quando o inimigo não tem alvo e existe um ZoneManager.
    void GoToCaptureZone()
    {
        navigation.FaceVelocity();

        // Já está dentro: para e deixa o tempo de captura correr.
        if (ZoneManager.Instance.IsInside(transform.position))
        {
            navigation.Stop();
            hasZoneDestination = false;
            return;
        }

        // Cada inimigo sorteia um ponto da zona, para não ficarem todos empilhados.
        if (!hasZoneDestination || navigation.ReachedDestination())
        {
            Vector3 point = ZoneManager.Instance.GetRandomPointInZone();

            if (navigation.TryGetNavMeshPoint(point, 6f, out Vector3 validPoint))
            {
                navigation.MoveTo(validPoint);
                hasZoneDestination = true;
            }
        }
    }

    // Anda pelo cenário quando não existe um alvo.
    void Wander()
    {
        navigation.FaceVelocity();

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        if (navigation.agent.pathPending)
            return;

        if (!hasWanderDestination)
        {
            PickWanderPoint();
        }
        else if (navigation.ReachedDestination())
        {
            hasWanderDestination = false;
            waitTimer = Random.Range(wanderWait.x, wanderWait.y);
        }
    }

    // Sorteia um destino de patrulha válido no NavMesh.
    void PickWanderPoint()
    {
        if (navigation.TryGetRandomPoint(transform.position, wanderRadius, out Vector3 point))
        {
            navigation.MoveTo(point);
            hasWanderDestination = true;
        }
    }
}