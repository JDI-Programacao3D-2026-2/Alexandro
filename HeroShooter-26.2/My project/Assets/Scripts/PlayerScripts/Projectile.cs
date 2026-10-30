using UnityEngine;

// A bala agora é movida pela física (Rigidbody), não mais por transform.position.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Configuração da bala")]
    public float speed = 30f;
    public int damage = 10;
    public float maxDistance = 100f;
    public float lifeTime = 5f;

    private Rigidbody rb;
    private Collider[] myColliders;
    private SoldierShootPool shooter;

    private Vector3 startPosition;
    private float spawnTime;
    private bool hasReturned = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myColliders = GetComponentsInChildren<Collider>(true);

        // Configura o Rigidbody por código, para não depender do prefab.
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // Chamado pelo SoldierShootPool depois de posicionar a bala.
    public void StartProjectile(Vector3 direction, SoldierShootPool shooter)
    {
        this.shooter = shooter;

        hasReturned = false;
        startPosition = transform.position;
        spawnTime = Time.time;

        // A bala nunca colide com o próprio atirador (corpo, armas, etc.).
        if (shooter != null)
        {
            foreach (Collider own in shooter.OwnColliders)
            {
                if (own == null) continue;

                foreach (Collider mine in myColliders)
                    Physics.IgnoreCollision(mine, own, true);
            }
        }

        // Quem move a bala agora é o Rigidbody.
        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (hasReturned)
            return;

        // Alcance máximo.
        float travelled = (rb.position - startPosition).magnitude;
        if (travelled >= maxDistance)
        {
            ReturnToPool();
            return;
        }

        // Tempo máximo de vida.
        if (Time.time - spawnTime >= lifeTime)
            ReturnToPool();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasReturned)
            return;

        // Aviso de diagnóstico: bala que morre logo depois de sair da arma.
        if (Time.time - spawnTime < 0.1f)
        {
            Debug.LogWarning(
                "Bala morreu ao nascer, colidiu com: " +
                collision.gameObject.name +
                " (layer " + LayerMask.LayerToName(collision.gameObject.layer) + ")");
        }

        // Aqui entra o dano quando você tiver o script de vida do inimigo:
        // EnemyHealth enemy = collision.gameObject.GetComponentInParent<EnemyHealth>();
        // if (enemy != null) enemy.TakeDamage(damage);

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (hasReturned)
            return;

        hasReturned = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (shooter != null)
            shooter.ReturnProjectile(gameObject);
        else
            gameObject.SetActive(false);
    }
}