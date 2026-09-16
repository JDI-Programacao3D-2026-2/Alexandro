using UnityEngine;
using UnityEngine.InputSystem;
public class Mov : MonoBehaviour
{
    public Rigidbody rb;
    private float vel = 5f;
    private float jumpForce = 5f;
    public float gravity = -9.81f;

    private Vector3 direction;
    // Update is called once per frame
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        Mover();
        if (Keyboard.current[Key.Space].wasPressedThisFrame)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        rb.AddForce(Vector3.down * gravity, ForceMode.Force);
    }
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(direction.x * vel, direction.y , direction.z * vel);
    }
    void Mover()
    {
        transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);

        direction = Vector3.zero;

        if (Keyboard.current[Key.W].isPressed)
        {
            direction += transform.forward;
        }
        else if (Keyboard.current[Key.A].isPressed)
        {
            direction -= transform.right;
        }
        else if (Keyboard.current[Key.D].isPressed)
        {
            direction += transform.right;
        }
        else if (Keyboard.current[Key.S].isPressed)
        {
            direction -= transform.forward;
        }

        direction = Vector3.ClampMagnitude(direction, 1f);  
    }

}