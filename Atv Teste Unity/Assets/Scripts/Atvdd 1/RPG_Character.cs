using UnityEngine;

public class RPG_Character : MonoBehaviour
{
    public string name;
    public int life;
    public float speed;
    public int level;
    public bool isAlive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        const int Max_Life = 100;
        const float Gravity = 10f;

        string msg1 = "===== Ficha do Personagem =====\nNome: " + name + "| Nível: " + level + "| Vida: " + life + "/" + Max_Life + "| Velocidade: " + speed + "| Status: " + (isAlive ? "Vivo" : "Morto");

        Debug.Log(msg1);

        life -= 50;

        string msg2 = "===== Ficha do Personagem =====\nNome: " + name + "| Nível: " + level + "| Vida: " + life + "/" + Max_Life + "| Velocidade: " + speed + "| Status: " + (isAlive ? "Vivo" : "Morto");

        Debug.Log(msg2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
