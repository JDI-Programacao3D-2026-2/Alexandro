
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
        if (hasReturned)
            return;

        // Calcula quanto a bala percorre neste frame.
        float distanceThisFrame = speed * Time.deltaTime;

        // Move a bala na direção definida pelo controlador.
        transform.position += direction * distanceThisFrame;

        // Acumula a distância percorrida.
        distanceTravelled += distanceThisFrame;

        // Devolve a bala à pool ao atingir o alcance máximo.
        if (distanceTravelled >= maxDistance)
        {
            ReturnToPool();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bala colidiu com: " + collision.gameObject.name);
        if (hasReturned)
            return;

        // Procura o componente de vida no objeto atingido.
        //EnemyHealth enemy =
        //    collision.gameObject.GetComponent<EnemyHealth>();

        //if (enemy != null)
        {
         //   enemy.TakeDamage(damage);
        }

        ReturnToPool();
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
