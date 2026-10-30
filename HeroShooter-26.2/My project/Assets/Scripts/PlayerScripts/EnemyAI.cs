using UnityEngine;
using UnityEngine.AI;

// IA do inimigo com NavMesh. Todos contra todos: o inimigo foca em qualquer
// HeroCombat que ele enxergar (player ou outros inimigos).
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HeroCombat))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 4f;
    public float turnSpeed = 360f;
    public float wanderRadius = 25f;
    public Vector2 wanderWait = new Vector2(0.5f, 2f);

    [Header("Visão")]
    public float sightRange = 40f;
    [Range(30f, 360f)] public float viewAngle = 200f;
    public float closeAwareness = 6f;        // perto assim ele percebe mesmo sem olhar
    public float eyeHeight = 0.5f;           // altura dos olhos em relação ao pivot
    public LayerMask sightMask = ~0;         // o que bloqueia a visão
    public float scanInterval = 0.25f;
    public float loseTargetTime = 3f;

    [Header("Combate")]
    public bool useClassDefaults = true;     // distâncias padrão por classe
    public float desiredRange = 12f;         // distância ideal para atirar
    public float maxShootRange = 30f;
    public float rangeTolerance = 2f;
    public float aimTolerance = 8f;          // graus de erro aceitos antes de atirar
    public float extraSpread = 2f;           // imprecisão extra do inimigo
    public float repathInterval = 0.2f;

    private NavMeshAgent agent;
    private HeroCombat combat;

    private HeroCombat target;
    private bool targetVisible;
    private Vector3 lastSeenPos;
    private float timeSinceSeen;

    private float scanTimer;
    private float repathTimer;
    private float waitTimer;
    private bool hasWanderDest;

    private readonly RaycastHit[] sightHits = new RaycastHit[16];

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        combat = GetComponent<HeroCombat>();

        agent.speed = moveSpeed;
        agent.updateRotation = false;   // a rotação é feita por aqui
        agent.stoppingDistance = 0f;

        combat.infiniteReserve = true;

        if (useClassDefaults)
        {
            if (combat.heroClass == HeroClassManager.HeroClass.Berserker)
            {
                desiredRange = 5f;
                maxShootRange = 11f;
            }
            else
            {
                desiredRange = 14f;
                maxShootRange = 35f;
            }
        }
    }

    void OnEnable()
    {
        combat.OnDamaged += HandleDamaged;

        target = null;
        targetVisible = false;
        hasWanderDest = false;
        waitTimer = 0f;
    }

    void OnDisable()
    {
        combat.OnDamaged -= HandleDamaged;
    }

    void Update()
    {
        if (combat.IsDead || !agent.enabled || !agent.isOnNavMesh)
            return;

        // Recarrega sozinho quando não dá mais para atirar.
        if (combat.currentAmmo < combat.magazineSize / 2 && !combat.IsReloading && target == null)
            combat.Reload();

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            Scan();
        }

        if (target != null && target.IsDead)
        {
            target = null;
            targetVisible = false;
        }

        if (target == null)
        {
            Wander();
            return;
        }

        if (targetVisible)
            EngageTarget();
        else
            GoToLastSeen();
    }

    // ---------- percepção ----------

    void Scan()
    {
        HeroCombat best = null;
        float bestDist = float.MaxValue;

        foreach (HeroCombat c in HeroCombat.All)
        {
            if (c == combat || c.IsDead)
                continue;

            float dist = Vector3.Distance(transform.position, c.transform.position);

            if (dist > sightRange || dist >= bestDist)
                continue;

            if (dist > closeAwareness && !InFieldOfView(c))
                continue;

            if (!HasLineOfSight(c))
                continue;

            best = c;
            bestDist = dist;
        }

        if (best != null)
        {
            target = best;
            targetVisible = true;
            lastSeenPos = best.AimPosition;
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

    bool InFieldOfView(HeroCombat c)
    {
        Vector3 to = c.transform.position - transform.position;
        to.y = 0f;

        return Vector3.Angle(transform.forward, to) <= viewAngle * 0.5f;
    }

    bool HasLineOfSight(HeroCombat t)
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 to = t.AimPosition - eye;
        float dist = to.magnitude;

        if (dist < 0.01f)
            return true;

        int count = Physics.RaycastNonAlloc(
            eye, to / dist, sightHits, dist + 0.5f, sightMask,
            QueryTriggerInteraction.Ignore);

        float closest = float.MaxValue;
        Collider closestCollider = null;

        for (int i = 0; i < count; i++)
        {
            Collider c = sightHits[i].collider;

            if (System.Array.IndexOf(combat.OwnColliders, c) >= 0)
                continue;

            if (c.GetComponentInParent<Projectile>() != null)
                continue;

            if (sightHits[i].distance < closest)
            {
                closest = sightHits[i].distance;
                closestCollider = c;
            }
        }

        return closestCollider != null &&
               closestCollider.GetComponentInParent<HeroCombat>() == t;
    }

    // Levou tiro de alguém que ele não vê: passa a ir atrás dessa pessoa.
    void HandleDamaged(HeroCombat attacker)
    {
        if (attacker == null || attacker == combat || attacker.IsDead)
            return;

        if (target == null || !targetVisible)
        {
            target = attacker;
            targetVisible = false;
            lastSeenPos = attacker.AimPosition;
            timeSinceSeen = 0f;
        }
    }

    // ---------- comportamentos ----------

    void EngageTarget()
    {
        Vector3 toTarget = target.AimPosition - transform.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = toTarget.magnitude;

        FaceDirection(flat);

        repathTimer -= Time.deltaTime;

        if (dist > desiredRange + rangeTolerance)
        {
            // Longe demais: aproxima.
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                agent.isStopped = false;
                agent.SetDestination(target.transform.position);
            }
        }
        else if (dist < desiredRange * 0.5f)
        {
            // Perto demais: recua um pouco.
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                Vector3 away = transform.position - flat.normalized * 4f;

                if (NavMesh.SamplePosition(away, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
        }
        else
        {
            // Distância boa: fica parado atirando.
            agent.isStopped = true;
        }

        // Atira quando está de frente para o alvo e dentro do alcance.
        float angle = Vector3.Angle(transform.forward, flat);

        if (dist <= maxShootRange && angle <= aimTolerance)
        {
            if (!combat.TryShoot(target.AimPosition, extraSpread) &&
                combat.currentAmmo < 1)
            {
                combat.Reload();
            }

            // Usa o especial durante o combate.
            if (UseSpecialNow())
                combat.TryActivateSpecial();
        }
    }

    bool UseSpecialNow()
    {
        if (combat.heroClass == HeroClassManager.HeroClass.Berserker)
            return combat.currentHealth < combat.maxHealth * 0.6f;

        return true;
    }

    void GoToLastSeen()
    {
        FaceVelocity();

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            agent.isStopped = false;
            agent.SetDestination(lastSeenPos);
        }
    }

    void Wander()
    {
        FaceVelocity();

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        if (agent.pathPending)
            return;

        agent.isStopped = false;

        if (!hasWanderDest)
        {
            PickWanderPoint();
        }
        else if (agent.remainingDistance <= 0.5f)
        {
            hasWanderDest = false;
            waitTimer = Random.Range(wanderWait.x, wanderWait.y);
        }
    }

    void PickWanderPoint()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 random = transform.position + Random.insideUnitSphere * wanderRadius;

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                hasWanderDest = true;
                return;
            }
        }
    }

    // ---------- rotação ----------

    void FaceVelocity()
    {
        FaceDirection(agent.velocity);
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(dir),
            turnSpeed * Time.deltaTime);
    }
}
