using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    private Vector3 direction;
    private ShootPool ShootPool;

    void OnEnable()
    {
        Invoke("SpawnTime",5f);
    }
    public void StartProjectile(Vector3 direction, ShootPool shooter)
    {
        this.direction = direction;
        this.ShootPool = shooter;
    }
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        CancelInvoke("SpawnTime");
        ShootPool.ReturnProjectile(gameObject);
    }
    void SpawnTime()
    {
        ShootPool.ReturnProjectile(gameObject);
    }
}
