
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private float speed = 5f;
    public Rigidbody rb;

    [Header("Pulo")]
    public float jumpForce = 7f;
    private bool isGrounded = true;

    private Vector3 direction;

    void Update()
    {
        transform.rotation = Quaternion.Euler(
            0,
            Camera.main.transform.eulerAngles.y,
            0
        );

        direction = Vector3.zero;

        if (Keyboard.current[Key.W].isPressed)
            direction += transform.forward;

        if (Keyboard.current[Key.S].isPressed)
            direction -= transform.forward;

        if (Keyboard.current[Key.D].isPressed)
            direction += transform.right;

        if (Keyboard.current[Key.A].isPressed)
            direction -= transform.right;

        direction = Vector3.ClampMagnitude(direction, 1f);

        // Pula ao apertar Espaço, somente se estiver no chão.
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(
            direction.x * speed,
            rb.linearVelocity.y,
            direction.z * speed
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        // Permite pular novamente ao tocar uma superfície.
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
