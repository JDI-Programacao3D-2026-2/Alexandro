using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using System.Collections;

public class shootpool : MonoBehaviour
{
    [Header("Pool and Bullets Settings")]
    public GameObject projectilePrefab;
    public Transform SpawnBulletPoint;
    public int poolSize = 20;
    public int currentAmmo;
    public int BulletShell;
    public int maxBulletShell = 3;
    private int activeProjectiles = 0;
    private ObjectPool<GameObject> pool;

    [Header("Shooting Settings")]
    public bool isEnemy = false;
    private bool canShoot = false;
    private float fireRate = 0.5f;
    private float nextFireTime = 0f;
    public LayerMask layerMask;
    public Transform playerTransform;
    public Transform LauncherTransform;

    //Criar categoria para objetos da ui
    [Header("UI Elements")]
    public TMPro.TextMeshProUGUI ammoText;
    public TMPro.TextMeshProUGUI bulletShellText;

    void Awake()
    {
        currentAmmo = poolSize;
        BulletShell = maxBulletShell;

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab),
            actionOnGet: (projectile) => projectile.SetActive(true),
            actionOnRelease: (projectile) => projectile.SetActive(false),
            actionOnDestroy: (projectile) => Destroy(projectile),
            collectionCheck: false,
            defaultCapacity: poolSize,
            maxSize: poolSize
        );

        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {   
        //Comandos para o inimigo
        if (isEnemy && canShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }

        if (isEnemy && currentAmmo <= 0 && BulletShell > 0 && IsInvoking("Reload"))
        {
            Invoke("Reload", 2f);
            StartCoroutine(ReloadAnim());
            //Se ficar sem pente precisa carregar por mais tempo
            if (BulletShell <= 0)
            {
                canShoot = false;
                StartCoroutine(EnemyReloadCoroutine());
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && !isEnemy)
        {
            Shoot();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(ReloadAnim());
            Invoke("Reload", 2f);
        }

        UpdateUI();
    }

    void FixedUpdate()
    {
        canShoot = false;
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            canShoot = true;
            Debug.DrawLine(transform.position, hit.point, Color.green);

        }else
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * 100f, Color.red);
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

    public void ReturnProjectile(GameObject projectile)
    {
        activeProjectiles--;
        pool.Release(projectile);
    }

    void UpdateUI()
    {
        if (isEnemy) return;
        ammoText.text = "Balas: " + currentAmmo+"/" + poolSize;
        bulletShellText.text = "Cartuchos: " + BulletShell + "/" + maxBulletShell;
    }

    private void Reload()
    {
        if (BulletShell > 0 && currentAmmo < poolSize)
        {
            BulletShell--;
            currentAmmo = poolSize;
            UpdateUI();
        }
    }

    IEnumerator EnemyReloadCoroutine()
    {
        StartCoroutine(ReloadAnim());
        yield return new WaitForSeconds(3f); // Tempo de recarga do inimigo
        BulletShell = maxBulletShell;
        currentAmmo = poolSize;

    }

    IEnumerator ReloadAnim()
    {
        float tempo = 0f;

            // 1. Roda até 55 graus (em 1.5 segundos)
            while (tempo < 1.5f)
            {
                tempo += Time.deltaTime;
                float anguloX = Mathf.Lerp(0, 55, tempo / 0.5f);
                LauncherTransform.localRotation = Quaternion.Euler(anguloX, 0, 0);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f); // Espera 0.5 segundos com o ângulo em 55 graus
            tempo = 0f;
            while (tempo < 2f)
            {
                tempo += Time.deltaTime;
                float anguloX = Mathf.Lerp(55, 0, tempo / 1f);
                LauncherTransform.localRotation = Quaternion.Euler(anguloX, 0, 0);
                yield return null;
            }
    }
}
