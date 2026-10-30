using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [Header("Referências")]
    public Rigidbody rb;

    [Header("Movimento")]
    public float speed = 5f;

    [Header("Pulo")]
    public float jumpForce = 7f;

    private Transform cam;
    private Vector3 moveDirection;
    private float cameraYaw;
    private bool jumpRequested;

    // Conta quantos objetos "Ground" estão tocando o player.
    private int groundContacts;
    private bool IsGrounded => groundContacts > 0;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Impede o player de tombar; o giro em Y é feito por código.
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnDisable()
    {
        // Evita ficar "no chão" depois de morrer/reviver.
        groundContacts = 0;
        moveDirection = Vector3.zero;
        jumpRequested = false;
    }

    // Só LÊ o input. Toda a física acontece no FixedUpdate.
    void Update()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        if (cam == null || Keyboard.current == null)
            return;

        cameraYaw = cam.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);

        Vector3 input = Vector3.zero;

        if (Keyboard.current[Key.W].isPressed) input.z += 1f;
        if (Keyboard.current[Key.S].isPressed) input.z -= 1f;
        if (Keyboard.current[Key.D].isPressed) input.x += 1f;
        if (Keyboard.current[Key.A].isPressed) input.x -= 1f;

        // Direção relativa para onde a câmera está olhando.
        moveDirection = yawRotation * Vector3.ClampMagnitude(input, 1f);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded)
            jumpRequested = true;
    }

    void FixedUpdate()
    {
        // Gira o player junto com a câmera, via Rigidbody.
        rb.MoveRotation(Quaternion.Euler(0f, cameraYaw, 0f));

        // Move mantendo a velocidade vertical (gravidade/pulo).
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * speed;
        velocity.z = moveDirection.z * speed;
        rb.linearVelocity = velocity;

        if (jumpRequested)
        {
            jumpRequested = false;

            if (IsGrounded)
            {
                // Zera a velocidade vertical para o pulo ter sempre a mesma altura.
                rb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            groundContacts++;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            groundContacts = Mathf.Max(0, groundContacts - 1);
    }
}