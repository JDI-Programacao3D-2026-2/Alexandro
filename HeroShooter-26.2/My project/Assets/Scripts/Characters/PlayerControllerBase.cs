using UnityEngine;
using UnityEngine.InputSystem;

// Base compartilhada pelos controladores de jogador.
// Ela lê teclado/mouse e chama as ações do HeroCombat.
// Os scripts SoldierPlayerController e BerserkerPlayerController definem a classe e a velocidade.
public abstract class PlayerControllerBase : MonoBehaviour
{
    [Header("Mira")]
    public Camera aimCamera;
    public float aimDistance = 200f;
    public LayerMask aimMask = ~0;
    public bool holdToFire = false; // true = segura o botão para disparar continuamente
    public bool debugShots = false;

    protected HeroCombat combat;
    private readonly RaycastHit[] aimHits = new RaycastHit[16];

    // Cada controlador informa qual classe representa.
    protected abstract HeroClassManager.HeroClass CharacterClass { get; }
    protected abstract float DefaultMoveSpeed { get; }

    protected virtual void Awake()
    {
        combat = GetComponent<HeroCombat>();

        if (combat != null)
            combat.isPlayer = true;
        else
            Debug.LogError("Este personagem precisa ter o componente HeroCombat: " + name);
    }

    protected virtual void Start()
    {
        if (combat == null)
        {
            enabled = false;
            return;
        }

        // Aplica os atributos da classe escolhida para este prefab.
        combat.ConfigureClass(CharacterClass);
        combat.isPlayer = true;

        // A velocidade é definida pelo controlador da classe.
        PlayerMove movement = GetComponent<PlayerMove>();
        if (movement != null)
            movement.speed = DefaultMoveSpeed;
    }

    protected virtual void Update()
    {
        if (combat == null || combat.IsDead || Mouse.current == null || Keyboard.current == null)
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera == null)
            return;

        // Disparo com botão esquerdo do mouse.
        bool fire = holdToFire
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (fire)
        {
            if (!combat.TryShoot(GetAimPoint()) &&
                !combat.IsReloading && combat.currentAmmo < 1)
            {
                Debug.Log("Sem munição! Pressione R para recarregar.");
            }
        }

        // R = recarregar.
        if (Keyboard.current[Key.R].wasPressedThisFrame)
            combat.Reload();

        // Q = habilidade especial.
        if (Keyboard.current[Key.Q].wasPressedThisFrame)
            combat.TryActivateSpecial();
    }

    // Cria um raio no centro da tela e encontra o ponto que o jogador está mirando.
    // O próprio personagem e os projéteis são ignorados.
    Vector3 GetAimPoint()
    {
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 aimPoint = ray.GetPoint(aimDistance);
        float closest = float.MaxValue;

        int count = Physics.RaycastNonAlloc(
            ray, aimHits, aimDistance, aimMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hitCollider = aimHits[i].collider;

            if (combat.OwnColliders != null &&
                System.Array.IndexOf(combat.OwnColliders, hitCollider) >= 0)
                continue;

            if (hitCollider.GetComponentInParent<Projectile>() != null)
                continue;

            if (aimHits[i].distance < closest)
            {
                closest = aimHits[i].distance;
                aimPoint = aimHits[i].point;
            }
        }

        if (debugShots && combat.firePoint != null)
            Debug.DrawLine(combat.firePoint.position, aimPoint, Color.red, 2f);

        return aimPoint;
    }
}
