using UnityEngine; // Importa as funções da Unity.

public class Projectile : MonoBehaviour
{
    public float speed = 10f; 
    private Vector3 direction; 
    private ShootPool Shootpool; 

    void OnEnable()
    {
        Invoke("SpawnTime", 5f); 
    }

    public void StartProjectile(Vector3 direction, ShootPool shooter)
    {
        this.direction = direction; 
        this.Shootpool = shooter;        
    }
    void Update()
    {       
        transform.position += direction * speed * Time.deltaTime;
    }
    void OnCollisionEnter(Collision collision)
    {
        CancelInvoke("SpawnTime");
        Shootpool.ReturnProjectile(gameObject);
    }
    void SpawnTime()
    {
         Shootpool.ReturnProjectile(gameObject);
    }
}