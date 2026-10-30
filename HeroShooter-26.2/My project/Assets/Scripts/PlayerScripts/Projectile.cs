using System.Collections.Generic;
using UnityEngine;

// Bala movida pelo Rigidbody. Os dados de dano, velocidade e alcance de cada
// classe ficam AQUI, neste script (ajuste no Inspector do prefab da bala).
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Soldier")]
    public float soldierDamage = 10f;
    public float soldierSpeed = 40f;
    public float soldierMaxDistance = 100f;

    [Header("Berserker (por projétil: mais dano, menos alcance)")]
    public float berserkerDamage = 6f;
    public float berserkerSpeed = 35f;
    public float berserkerMaxDistance = 5f;

    [Header("Geral")]
    public float lifeTime = 5f;

    private Rigidbody rb;
    private Collider[] myColliders;
    private readonly List<Collider> ignored = new List<Collider>();

    private HeroCombat shooter;
    private float damage;
    private float maxDistance;
    private Vector3 startPosition;
    private float spawnTime;
    private bool hasReturned = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myColliders = GetComponentsInChildren<Collider>(true);

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Balas nunca colidem entre si (importante para a shotgun).
        Physics.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
    }

    // Chamado pelo HeroCombat depois de posicionar a bala no firePoint.
    public void StartProjectile(Vector3 direction, HeroCombat shooter)
    {
        this.shooter = shooter;

        bool berserker = shooter.heroClass == HeroClassManager.HeroClass.Berserker;
        damage = berserker ? berserkerDamage : soldierDamage;
        maxDistance = berserker ? berserkerMaxDistance : soldierMaxDistance;
        float speed = berserker ? berserkerSpeed : soldierSpeed;

        hasReturned = false;
        startPosition = transform.position;
        spawnTime = Time.time;

        // A bala ignora o próprio atirador (só colliders ativos; a Unity
        // reclama de IgnoreCollision com collider desativado).
        ignored.Clear();
        foreach (Collider own in shooter.OwnColliders)
        {
            if (own == null || !own.enabled || !own.gameObject.activeInHierarchy)
                continue;

            foreach (Collider mine in myColliders)
            {
                Physics.IgnoreCollision(mine, own, true);
            }

            ignored.Add(own);
        }

        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (hasReturned)
            return;

        if ((rb.position - startPosition).sqrMagnitude >= maxDistance * maxDistance ||
            Time.time - spawnTime >= lifeTime)
        {
            ReturnToPool();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasReturned)
            return;

        if (Time.time - spawnTime < 0.1f)
        {
            Debug.LogWarning(
                "Bala morreu ao nascer, colidiu com: " +
                collision.gameObject.name +
                " (layer " + LayerMask.LayerToName(collision.gameObject.layer) + ")");
        }

        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();

        if (target != null && !target.IsDead)
            target.TakeDamage(damage, shooter);

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (hasReturned)
            return;

        hasReturned = true;

        // Desfaz o "ignorar atirador" antes de reaproveitar a bala.
        foreach (Collider own in ignored)
        {
            if (own == null || !own.enabled || !own.gameObject.activeInHierarchy)
                continue;

            foreach (Collider mine in myColliders)
                Physics.IgnoreCollision(mine, own, false);
        }

        ignored.Clear();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (ProjectilePool.Instance != null)
            ProjectilePool.Instance.Release(this);
        else
            gameObject.SetActive(false);
    }
}
