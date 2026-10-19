
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
    public LayerMask aimMask = ~0;

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
    public float specialCooldown = 30f;

    private bool isSpecialActive = false;
    private bool specialReady = true;

    void Awake()
    {
        // Comeca com o pente cheio.
        currentAmmo = magazineSize;

        // A segunda arma comeca desativada.
        if (secondWeapon != null)
        {
            secondWeapon.SetActive(false);
        }

        // Cria a pool de projeteis.
        pool = new ObjectPool<GameObject>(
            () => Instantiate(projectilePrefab),
            projectile => projectile.SetActive(true),
            projectile => projectile.SetActive(false),
            projectile => Destroy(projectile),
            false,
            poolsize,
            poolsize
        );
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

        // Disparo com o botao esquerdo do mouse.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }

        // Recarga com R.
        if (Keyboard.current[Key.R].wasPressedThisFrame)
        {
            StartCoroutine(Reload());
        }

        // Ativa o especial com Q.
        if (Keyboard.current[Key.Q].wasPressedThisFrame)
        {
            if (specialReady && !isSpecialActive)
            {
                StartCoroutine(ActivateSpecial());
            }
        }

        if (specialCooldown > 0f && !specialReady)
        {
            specialCooldown -= Time.deltaTime / 2;
        }

    }

    void Shoot()
    {
        // Impede disparar durante a recarga.
        if (isReloading)
            return;

        if (shootPoint == null || aimCamera == null)
        {
            Debug.LogError("Configure o Shoot Point e a Aim Camera.");
            return;
        }

        // Verifica se existe municao no pente.
        if (currentAmmo <= 0)
        {
            Debug.Log("Sem municao! Pressione R para recarregar.");
            return;
        }

        // No especial, dois projeteis precisam estar disponiveis.
        int shotsNeeded = isSpecialActive ? 2 : 1;

        if (activeProjectiles + shotsNeeded > poolsize)
        {
            Debug.Log("Limite de projeteis ativos atingido!");
            return;
        }

        // Cria um raio saindo do centro da tela.
        Ray cameraRay = aimCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        Vector3 aimPoint;

        // Procura o ponto atingido pela mira da camera.
        if (Physics.Raycast(
            cameraRay,
            out RaycastHit cameraHit,
            aimDistance,
            aimMask))
        {
            aimPoint = cameraHit.point;
        }
        else
        {
            // Se nao acertar nada, mira para longe.
            aimPoint = cameraRay.GetPoint(aimDistance);
        }

        // Dispara a arma principal.
        FireProjectile(shootPoint, aimPoint);

        // Durante o especial, dispara tambem pela segunda arma.
        if (isSpecialActive && secondShootPoint != null)
        {
            FireProjectile(secondShootPoint, aimPoint);
        }

        // Apenas uma bala e gasta por clique.
        currentAmmo--;
    }

    void FireProjectile(Transform shotPoint, Vector3 aimPoint)
    {
        // Calcula a direcao entre a arma e o ponto mirado.
        Vector3 direction = (aimPoint - shotPoint.position).normalized;

        // Pega um projetil disponivel na pool.
        GameObject projectile = pool.Get();

        activeProjectiles++;

        // Posiciona o projetil na saida da arma.
        projectile.transform.SetPositionAndRotation(
            shotPoint.position,
            Quaternion.LookRotation(direction)
        );

        // Entrega a direcao e a referencia deste controlador
        // ao script Projectile, que movimenta a bala.
        Projectile projectileScript =
            projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.StartProjectile(direction, this);
        }
        else
        {
            Debug.LogError(
                "O prefab do projetil precisa ter o script Projectile."
            );

            ReturnProjectile(projectile);
        }
    }

    // Chamado pelo Projectile quando a bala termina.
    public void ReturnProjectile(GameObject projectile)
    {
        activeProjectiles--;

        pool.Release(projectile);
    }

    IEnumerator Reload()
    {
        // Evita recargas simultaneas.
        if (isReloading)
            yield break;

        // Nao recarrega se ja estiver cheio.
        if (currentAmmo >= magazineSize)
            yield break;

        // Nao recarrega se nao houver reserva.
        if (reserveAmmo <= 0)
            yield break;

        isReloading = true;

        // Aguarda o tempo da recarga.
        yield return new WaitForSeconds(reloadTime);

        // Calcula quantas balas faltam no pente.
        int missingAmmo = magazineSize - currentAmmo;

        // Usa somente as balas disponiveis na reserva.
        int ammoToLoad = Mathf.Min(missingAmmo, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;
    }

    IEnumerator ActivateSpecial()
    {
        if (secondWeapon == null || secondShootPoint == null)
        {
            Debug.LogWarning(
                "Configure a segunda arma e o segundo Shoot Point."
            );
            yield break;
        }

        // Bloqueia novas ativacoes.
        specialReady = false;
        isSpecialActive = true;

        // Mostra a segunda arma.
        secondWeapon.SetActive(true);

        // Espera a duracao do especial.
        yield return new WaitForSeconds(specialDuration);

        // Desativa o especial.
        isSpecialActive = false;
        secondWeapon.SetActive(false);

        // Permite usar o especial novamente.
        specialReady = true;
    }

    public void RecoverSpecialCooldown(float amount)
    {
        specialCooldown = Mathf.Max(0f, specialCooldown - amount);
    }
}
