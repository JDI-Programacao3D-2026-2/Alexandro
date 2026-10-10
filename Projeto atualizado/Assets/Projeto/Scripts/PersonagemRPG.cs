using UnityEngine;

public class PersonagemRPG : MonoBehaviour
{
public string nome = "Arthas";
public int vida = 50; 
public float velocidade = 7.5f;
 public int nivel = 3;
 public bool estaVivo = true;
   public const int VIDA_MAXIMA = 100;
   public const float GRAVIDADE = 0;

    void Start()
    {
        nome = "Arthas";
        vida = 50;
        velocidade = 7.5f;
        nivel = 3;
        estaVivo = true;
        string mensagem = $"=== Ficha do Personagem === Nome: {nome} | Nível: {nivel} | Vida: {vida}/{VIDA_MAXIMA} | Velocidade: {velocidade} ";
        Debug.Log(mensagem);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
