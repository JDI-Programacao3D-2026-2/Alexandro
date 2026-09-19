using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;

public class shootpool : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform SpawnBulletPoint;
    public int poolSize = 20;
    public int currentAmmo;
    private int activeProjectiles = 0;
    private ObjectPool<GameObject> pool;

    void Awake()
    {
        currentAmmo = poolSize;

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab),
            actionOnGet: (projectile) => projectile.SetActive(true),
            actionOnRelease: (projectile) => projectile.SetActive(false),
            actionOnDestroy: (projectile) => Destroy(projectile),
            collectionCheck: false,
            defaultCapacity: poolSize,
            maxSize: poolSize
        );
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (currentAmmo <= 0 || activeProjectiles >= poolSize)
        {
            Debug.Log("No ammo or max projectiles reached!");
            return;
        }

        GameObject projectile = pool.Get();
        currentAmmo--;
        activeProjectiles++;

        projectile.transform.SetPositionAndRotation(SpawnBulletPoint.position, SpawnBulletPoint.rotation);
        projectile.GetComponent<Projectile>().StartProjectile(SpawnBulletPoint.forward, this);
        
    }


}
