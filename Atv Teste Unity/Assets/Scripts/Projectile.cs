using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    private Vector3 direction;
    private shootpool Shootpool;

    void Awake()
    {
        Invoke("Deactivate", 5f); // Deactivate the projectile after 5 seconds if it doesn't hit anything
    }
    public void StartProjectile(Vector3 dir, shootpool shooter)
    {
        this.direction = dir;
        this.Shootpool = shooter;
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Handle collision logic here (e.g., damage, effects, etc.)
        // After handling the collision, return the projectile to the pool
        Shootpool.ReturnProjectile(gameObject);
    }

    private void Deactivate()
    {
        // Return the projectile to the pool if it hasn't hit anything
        Shootpool.ReturnProjectile(gameObject);
    }
}
