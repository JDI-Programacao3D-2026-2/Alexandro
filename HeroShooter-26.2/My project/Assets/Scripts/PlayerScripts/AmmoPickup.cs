using UnityEngine;

// Item de munição que cai quando alguém morre.
// Quem passa por cima coleta e recebe parte da munição de reserva que o morto tinha:
// mesma classe do morto = 60%, classe diferente = 30% (valores no Inspector).
// Fica em um prefab, que é arrastado para o SpawnManager.
public class AmmoPickup : MonoBehaviour
{
    [Header("Munição recuperada")]
    [Range(0f, 1f)] public float sameClassPercent = 0.6f;
    [Range(0f, 1f)] public float otherClassPercent = 0.3f;

    [Header("Geral")]
    public float lifeTime = 30f;          // some sozinho depois desse tempo
    public bool enemiesCanCollect = true; // desligue para só o player poder coletar
    public float spinSpeed = 90f;         // giro visual (0 = parado)

    private int storedAmmo;
    private HeroClassManager.HeroClass droppedByClass;
    private bool collected;

    // Garante que todos os colliders do item são Trigger (não bloqueiam ninguém).
    // Quem chama: a Unity.
    void Awake()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;
    }

    // Guarda quanta munição o item tem e de qual classe ela veio.
    // Quem chama: SpawnManager.DropAmmo(), logo depois de criar o item.
    public void Setup(int ammo, HeroClassManager.HeroClass dropperClass)
    {
        storedAmmo = ammo;
        droppedByClass = dropperClass;

        Destroy(gameObject, lifeTime);
    }

    // Gira o item para chamar atenção. Quem chama: a Unity.
    void Update()
    {
        if (spinSpeed != 0f)
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    // Quando alguém encosta: calcula a porcentagem, entrega a munição e some.
    // Quem chama: a Unity (física).
    void OnTriggerEnter(Collider other)
    {
        if (collected || storedAmmo <= 0)
            return;

        HeroCombat collector = other.GetComponentInParent<HeroCombat>();

        if (collector == null || collector.IsDead)
            return;

        if (!collector.isPlayer && !enemiesCanCollect)
            return;

        float percent = collector.heroClass == droppedByClass
            ? sameClassPercent
            : otherClassPercent;

        int gain = Mathf.Max(1, Mathf.RoundToInt(storedAmmo * percent));

        collector.AddReserveAmmo(gain);

        collected = true;
        Destroy(gameObject);
    }
}
