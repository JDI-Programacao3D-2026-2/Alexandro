using UnityEngine;
using UnityEngine.InputSystem;

// Lê mouse/teclado do jogador e manda o HeroCombat atirar na direção da mira.
[RequireComponent(typeof(HeroCombat))]
public class PlayerCombatInput : MonoBehaviour
{
    [Header("Mira")]
    public Camera aimCamera;            // o inGameManager preenche
    public float aimDistance = 200f;
    public LayerMask aimMask = ~0;
    public bool holdToFire = false;     // true = segurar o botão atira em sequência
    public bool debugShots = false;

    private HeroCombat combat;
    private readonly RaycastHit[] aimHits = new RaycastHit[16];

    void Awake()
    {
        combat = GetComponent<HeroCombat>();
        combat.isPlayer = true;
    }

    void Update()
    {
        if (combat.IsDead || Mouse.current == null || Keyboard.current == null)
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera == null)
            return;

        bool fire = holdToFire
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (fire)
        {
            if (!combat.TryShoot(GetAimPoint()) &&
                !combat.IsReloading &&
                combat.currentAmmo < 1)
            {
                Debug.Log("Sem munição! Pressione R para recarregar.");
            }
        }

        if (Keyboard.current[Key.R].wasPressedThisFrame)
            combat.Reload();

        if (Keyboard.current[Key.Q].wasPressedThisFrame)
            combat.TryActivateSpecial();
    }

    // Ponto que está no centro da tela, ignorando o próprio player e as balas.
    Vector3 GetAimPoint()
    {
        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint = ray.GetPoint(aimDistance);
        float closest = float.MaxValue;

        int count = Physics.RaycastNonAlloc(
            ray, aimHits, aimDistance, aimMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider c = aimHits[i].collider;

            if (System.Array.IndexOf(combat.OwnColliders, c) >= 0)
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
            Debug.DrawLine(combat.firePoint.position, aimPoint, Color.red, 2f);

        return aimPoint;
    }
}
