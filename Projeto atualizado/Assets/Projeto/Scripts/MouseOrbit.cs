using UnityEngine;
using UnityEngine.InputSystem;

public class MouseOrbit : MonoBehaviour
{
    public Transform player;
    public float distance = 5f;
    public float MouseSensitivity = 0.2f;
    private float rotX;
    private float rotY;

void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void LateUpdate()
    {
        if (player != null) {
        Vector2 delta = Mouse.current.delta.ReadValue() * MouseSensitivity;
        rotX += delta.x;            
        
        rotY = Mathf.Clamp(rotY - delta.y,30,60);  
        Quaternion rotation = Quaternion.Euler(rotY, rotX, 0);
        transform.position = player.position - (rotation * Vector3.forward * distance);
        transform.rotation = rotation;
        }
    }
}



























//Mathf.Clamp(0,0,0);




// rotY = Mathf.Clamp(rotY - delta.y, -30f, 60f);