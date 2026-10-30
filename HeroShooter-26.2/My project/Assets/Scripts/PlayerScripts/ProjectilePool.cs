using UnityEngine;
using UnityEngine.Pool;

// UMA pool compartilhada por todos (player e inimigos).
// Coloque este script em um objeto vazio da cena Game.
public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [Header("Prefab da bala (precisa ter Projectile + Rigidbody + Collider)")]
    public Projectile projectilePrefab;

    public int defaultCapacity = 100;
    public int maxSize = 400;

    private ObjectPool<Projectile> pool;

    void Awake()
    {
        Instance = this;

        pool = new ObjectPool<Projectile>(
            () => Instantiate(projectilePrefab, transform),
            p => p.gameObject.SetActive(true),
            p => p.gameObject.SetActive(false),
            p =>
            {
                if (p != null && Application.isPlaying)
                    Destroy(p.gameObject);
            },
            false,
            defaultCapacity,
            maxSize
        );
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Projectile Get()
    {
        return pool.Get();
    }

    public void Release(Projectile projectile)
    {
        pool.Release(projectile);
    }
}
