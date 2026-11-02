using UnityEngine;
using UnityEngine.InputSystem;

// Movimento comum aos dois personagens jogadores.
// A velocidade é definida pelo SoldierPlayerController ou BerserkerPlayerController.
public class PlayerMove : MonoBehaviour
{
    [Header("Referências")]
    public Rigidbody rb;

    [Header("Movimento")]
    public float speed = 5f;

    [Header("Corrida")]
    public Key runKey = Key.LeftShift;
    public float runMultiplier = 1.6f;          // velocidade correndo = speed x isto
    public float runBlockTimeAfterShot = 0.5f;  // depois de atirar, fica sem correr por este tempo

    [Header("Pulo")]
    public float jumpForce = 7f;

    private Transform cam;
    private Vector3 moveDirection;
    private float cameraYaw;
    private bool jumpRequested;

    private HeroCombat combat;
    private bool isRunning;
    private float runBlockedUntil;

    public bool IsRunning => isRunning; // útil para animação ou efeitos

    // Conta quantos objetos com a tag Ground estão tocando o jogador.
    private int groundContacts;
    private bool IsGrounded => groundContacts > 0;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("PlayerMove precisa de um Rigidbody: " + name);
            enabled = false;
            return;
        }

        combat = GetComponent<HeroCombat>();

        // Evita tombar para os lados. A rotação horizontal é controlada pela câmera.
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnEnable()
    {
        // Passa a ouvir os tiros para interromper a corrida.
        if (combat != null)
            combat.OnShot += HandleShot;
    }

    void OnDisable()
    {
        if (combat != null)
            combat.OnShot -= HandleShot;

        // Limpa o estado ao morrer ou desativar o personagem.
        groundContacts = 0;
        moveDirection = Vector3.zero;
        jumpRequested = false;
        isRunning = false;
    }

    // Atirar cancela a corrida e bloqueia por um instante.
    // Quem chama: o evento OnShot do HeroCombat, a cada tiro.
    void HandleShot()
    {
        isRunning = false;
        runBlockedUntil = Time.time + runBlockTimeAfterShot;
    }

    // Lê o teclado e prepara a direção. A física fica no FixedUpdate.
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

        // O movimento acompanha a direção horizontal da câmera.
        moveDirection = yawRotation * Vector3.ClampMagnitude(input, 1f);

        // Corre ao segurar o Shift, se estiver andando e não tiver atirado há pouco.
        isRunning = Keyboard.current[runKey].isPressed &&
                    moveDirection.sqrMagnitude > 0.01f &&
                    Time.time >= runBlockedUntil;

        // Espaço solicita o pulo somente quando está tocando o chão.
        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded)
            jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        rb.MoveRotation(Quaternion.Euler(0f, cameraYaw, 0f));

        // Altera apenas X e Z, preservando a velocidade vertical da gravidade.
        Vector3 velocity = rb.linearVelocity;
        float currentSpeed = isRunning ? speed * runMultiplier : speed;
        velocity.x = moveDirection.x * currentSpeed;
        velocity.z = moveDirection.z * currentSpeed;
        rb.linearVelocity = velocity;

        if (jumpRequested)
        {
            jumpRequested = false;

            if (IsGrounded)
            {
                // Zera a queda antes do impulso para o pulo ficar consistente.
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