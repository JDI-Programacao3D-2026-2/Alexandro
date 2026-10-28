
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Configuração da bala")]
    public float speed = 10f;
    public int damage = 10;
    public float maxDistance = 100f;

    private Vector3 direction;
    private SoldierShootPool shooter;

    private Vector3 startPosition;
    private float distanceTravelled;

    private bool hasReturned;

    void OnEnable()
    {
        hasReturned = false;
        distanceTravelled = 0f;
        startPosition = transform.position;

        CancelInvoke();
        Invoke(nameof(SpawnTime), 10f);
    }

    public void StartProjectile(
        Vector3 direction,
        SoldierShootPool shooter)
    {
        this.direction = direction.normalized;
        this.shooter = shooter;

        // Guarda a posição inicial real do disparo.
        startPosition = transform.position;
        distanceTravelled = 0f;
    }

    void Update()
    {
        if (hasReturned) return;

        float distanceThisFrame = speed * Time.deltaTime;

        // Olha se tem algo no caminho deste frame
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distanceThisFrame))
        {
            Debug.Log("Bala colidiu com: " + hit.collider.gameObject.name);
            ReturnToPool();
            return;
        }

        transform.position += direction * distanceThisFrame;
        distanceTravelled += distanceThisFrame;

        if (distanceTravelled >= maxDistance)
            ReturnToPool();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasReturned)
            return;
        Debug.Log("Bala colidiu com: " + collision.gameObject.name);
        ReturnToPool();
        

        // Procura o componente de vida no objeto atingido.
        //EnemyHealth enemy =
        //    collision.gameObject.GetComponent<EnemyHealth>();

        //if (enemy != null)
        //{
         //   enemy.TakeDamage(damage);
        //}

    }

    void SpawnTime()
    {
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (hasReturned)
            return;

        hasReturned = true;

        CancelInvoke(nameof(SpawnTime));

        if (shooter != null)
        {
            shooter.ReturnProjectile(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

}
