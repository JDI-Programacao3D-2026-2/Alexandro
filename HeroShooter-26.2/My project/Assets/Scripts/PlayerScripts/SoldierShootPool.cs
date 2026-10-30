using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using System.Collections;

public class SoldierShootPool : MonoBehaviour
{
    [Header("Arma principal")]
    public GameObject projectilePrefab;
    public Transform shootPoint;

    [Header("Arma especial")]
    public GameObject secondWeapon;
    public Transform secondShootPoint;

    [Header("Mira")]
    public Camera aimCamera;
    public float aimDistance = 200f;
    public LayerMask aimMask = ~0; // No Inspector, desmarque Player e Projectile.
    public bool debugShots = false; // Liga logs e uma linha vermelha na Scene view.
    private readonly RaycastHit[] aimHits = new RaycastHit[16];

    [Header("Municao")]
    public int magazineSize = 30;
    public int currentAmmo;
    public int reserveAmmo = 90;

    [Header("Pool de projeteis")]
    public int poolsize = 40;
    private int activeProjectiles = 0;
    private ObjectPool<GameObject> pool;

    [Header("Recarga")]
    public float reloadTime = 1.3f;
    private bool isReloading = false;

    [Header("Especial")]
    public float specialDuration = 15f;
    public float specialCooldown = 30f;   // valor configurado
    private float currentCooldown = 0f;   // contador em uso
    private bool isSpecialActive = false;

    [Header("Vida")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public float respawnDelay = 2f;

    // Colliders do próprio soldado; as balas ignoram colisão com eles.
    public Collider[] OwnColliders { get; private set; }

    void Awake()
    {
        currentAmmo = magazineSize;

        OwnColliders = GetComponentsInChildren<Collider>(true);

        if (secondWeapon != null)
            secondWeapon.SetActive(false);

        pool = new ObjectPool<GameObject>(
            () => Instantiate(projectilePrefab),
            projectile => projectile.SetActive(true),
            projectile => projectile.SetActive(false),
            projectile =>
            {
                // Evita o erro de Destroy ao sair do Play Mode.
                if (projectile != null && Application.isPlaying)
                    Destroy(projectile);
            },
            false,
            poolsize,
            poolsize
        );
    }

    void Start()
    {
        UpdateAmmoUI();
        UpdateLifeUI();
    }

    void OnDisable()
    {
        // Corrotinas param quando o objeto é desativado (morte).
        // Sem isto, isReloading e isSpecialActive ficariam travados em true.
        isReloading = false;

        if (isSpecialActive)
        {
            isSpecialActive = false;

            if (secondWeapon != null)
                secondWeapon.SetActive(false);
        }
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Shoot();

        if (Keyboard.current[Key.R].wasPressedThisFrame)
            StartCoroutine(Reload());

        // Ativa o especial com Q.
        if (Keyboard.current[Key.Q].wasPressedThisFrame)
        {
            if (!isSpecialActive && currentCooldown <= 0f)
                StartCoroutine(ActivateSpecial());
        }

        // O cooldown começa a contar quando o especial é ativado.
        if (currentCooldown > 0f)
            currentCooldown = Mathf.Max(0f, currentCooldown - Time.deltaTime);
    }

    void Shoot()
    {
        if (isReloading)
            return;

        if (shootPoint == null || aimCamera == null)
        {
            Debug.LogError("Configure o Shoot Point e a Aim Camera.");
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("Sem municao! Pressione R para recarregar.");
            return;
        }

        int shotsNeeded = isSpecialActive ? 2 : 1;

        if (activeProjectiles + shotsNeeded > poolsize)
        {
            Debug.LogWarning("Limite de projeteis ativos atingido!");
            return;
        }

        // Raio saindo do centro da tela.
        Ray cameraRay = aimCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        // Procura o ponto mirado ignorando o próprio soldado e as balas,
        // mesmo que algum filho do prefab esteja numa layer errada.
        Vector3 aimPoint = cameraRay.GetPoint(aimDistance);
        float closest = float.MaxValue;

        int hitCount = Physics.RaycastNonAlloc(
            cameraRay,
            aimHits,
            aimDistance,
            aimMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider c = aimHits[i].collider;

            if (System.Array.IndexOf(OwnColliders, c) >= 0)
                continue;

            if (c.GetComponentInParent<Projectile>() != null)
                continue;

            if (aimHits[i].distance < closest)
            {
                closest = aimHits[i].distance;
                aimPoint = aimHits[i].point;
            }
        }

        if (debugShots)
        {
            Debug.Log("Mira em: " + aimPoint + " (distancia " + closest + ")");
            Debug.DrawLine(shootPoint.position, aimPoint, Color.red, 2f);
        }

        FireProjectile(shootPoint, aimPoint, cameraRay.direction);

        if (isSpecialActive && secondShootPoint != null)
            FireProjectile(secondShootPoint, aimPoint, cameraRay.direction);

        currentAmmo--;
        UpdateAmmoUI();
    }

    void FireProjectile(Transform shotPoint, Vector3 aimPoint, Vector3 cameraForward)
    {
        Vector3 direction = aimPoint - shotPoint.position;

        // Se o ponto mirado ficou atrás do cano (parede colada no player),
        // atira para frente da câmera em vez de atirar para trás.
        if (direction.sqrMagnitude < 0.0001f ||
            Vector3.Dot(direction, cameraForward) <= 0f)
        {
            direction = cameraForward;
        }

        direction.Normalize();

        GameObject projectile = pool.Get();
        activeProjectiles++;

        projectile.transform.SetPositionAndRotation(
            shotPoint.position,
            Quaternion.LookRotation(direction)
        );

        Projectile projectileScript = projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.StartProjectile(direction, this);
        }
        else
        {
            Debug.LogError("O prefab do projetil precisa ter o script Projectile.");
            ReturnProjectile(projectile);
        }
    }

    // Chamado pelo Projectile quando a bala termina.
    public void ReturnProjectile(GameObject projectile)
    {
        activeProjectiles = Mathf.Max(0, activeProjectiles - 1);
        pool.Release(projectile);
    }

    IEnumerator Reload()
    {
        if (isReloading) yield break;
        if (currentAmmo >= magazineSize) yield break;
        if (reserveAmmo <= 0) yield break;

        isReloading = true;

        yield return new WaitForSeconds(reloadTime);

        int missingAmmo = magazineSize - currentAmmo;
        int ammoToLoad = Mathf.Min(missingAmmo, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;
        UpdateAmmoUI();
    }

    IEnumerator ActivateSpecial()
    {
        if (secondWeapon == null || secondShootPoint == null)
        {
            Debug.LogWarning("Configure a segunda arma e o segundo Shoot Point.");
            yield break;
        }

        isSpecialActive = true;
        currentCooldown = specialCooldown;

        secondWeapon.SetActive(true);

        yield return new WaitForSeconds(specialDuration);

        isSpecialActive = false;
        secondWeapon.SetActive(false);
    }

    public void RecoverSpecialCooldown(float amount)
    {
        currentCooldown = Mathf.Max(0f, currentCooldown - amount);
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        UpdateLifeUI();

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Soldier morreu!");

        // A contagem do respawn fica no inGameManager, porque este objeto
        // vai ser desativado (e uma corrotina aqui morreria junto).
        gameObject.SetActive(false);

        if (inGameManager.Instance != null)
            inGameManager.Instance.RespawnPlayer(respawnDelay, this);
    }

    // Chamado pelo inGameManager ao reviver.
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateLifeUI();
    }

    void UpdateAmmoUI()
    {
        if (inGameManager.Instance != null)
            inGameManager.Instance.UpdateAmmo(currentAmmo, reserveAmmo);
    }

    void UpdateLifeUI()
    {
        if (inGameManager.Instance != null)
            inGameManager.Instance.UpdateLife(currentHealth, maxHealth);
    }
}