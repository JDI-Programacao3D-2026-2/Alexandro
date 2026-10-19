
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMov : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;

    [Header("Posição da câmera")]
    public Vector3 offset = new Vector3(0.7f, 1.1f, -2.5f);

    [Header("Movimento do mouse")]
    public float mouseSensitivity = 0.15f;
    public float minPitch = -40f;
    public float maxPitch = 70f;

    [Header("Colisão com paredes")]
    public LayerMask obstacleLayers = ~0;
    public float collisionRadius = 0.2f;
    public float wallPadding = 0.1f;

    // Guarda a rotação horizontal e vertical da câmera.
    private float yaw;
    private float pitch;

    void Start()
    {
        // Começa com o cursor preso na janela do jogo.
        LockCursor(true);

        // Inicializa a rotação com base na câmera atual.
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        if (pitch > 180f)
            pitch -= 360f;
    }

    void Update()
    {
        // ESC libera o mouse para usar outras janelas.
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
        }

        // Clique esquerdo na janela do jogo prende o mouse novamente.
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
        }

        // Só controla a câmera quando o mouse está preso.
        if (Cursor.lockState == CursorLockMode.Locked &&
            Mouse.current != null)
        {
            Vector2 mouse = Mouse.current.delta.ReadValue();

            yaw += mouse.x * mouseSensitivity;
            pitch -= mouse.y * mouseSensitivity;

            // Impede que a câmera gire demais para cima ou para baixo.
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        // Calcula para onde a câmera deve olhar.
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Posição desejada atrás e ao lado do jogador.
        Vector3 desiredPosition =
            player.position + rotation * offset;

        // Ponto de referência para verificar obstáculos.
        Vector3 pivot = player.position + Vector3.up * offset.y;

        // Direção e distância entre o jogador e a câmera.
        Vector3 direction = desiredPosition - pivot;
        float distance = direction.magnitude;

        // Verifica se existe uma parede entre o jogador e a câmera.
        if (Physics.SphereCast(
            pivot,
            collisionRadius,
            direction.normalized,
            out RaycastHit hit,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore))
        {
            // Aproxima a câmera para que ela não atravesse a parede.
            float safeDistance = Mathf.Max(
                0f, hit.distance - wallPadding
            );

            transform.position =
                pivot + direction.normalized * safeDistance;
        }
        else
        {
            // Sem obstáculos, usa a posição normal.
            transform.position = desiredPosition;
        }

        // Aplica a rotação da câmera.
        transform.rotation = rotation;
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    void OnDisable()
    {
        // Libera o mouse quando o script é desativado.
        LockCursor(false);
    }
}
