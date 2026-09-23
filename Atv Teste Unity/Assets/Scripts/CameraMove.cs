using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;

    [Header("Configurações")]
    public float distance = 5f;
    public float mouseSensitivity = 0.2f;
    public float minPitch = -85f;   // olhar pra baixo (em 1ª pessoa) ou limite inferior
    public float maxPitch = 85f;    // olhar pra cima
    public bool lockCursorOnStart = true;

    private float yaw;   // rotação horizontal acumulada
    private float pitch; // rotação vertical acumulada

    void Start()
    {
        if (lockCursorOnStart)
            LockCursor(true);
    }

    void Update()
    {
        // Esc destrava o cursor (útil no Editor pra clicar nos Inspetores)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(false);

        // Clicar na janela do jogo trava de novo
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Só gira a câmera se o cursor estiver travado
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            yaw   += delta.x;
            pitch  = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Terceira pessoa: câmera posicionada atrás do player
        transform.position = player.position - (rotation * Vector3.forward * distance);
        transform.rotation = rotation;
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    void OnDisable()
    {
        // Garante que o cursor volte ao normal se o script for desligado
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}