using UnityEngine;

// Controlador do jogador quando o prefab escolhido é o Soldier.
// Ajuste a velocidade abaixo pelo Inspector.
public class SoldierPlayerController : PlayerControllerBase
{
    public float moveSpeed = 5f;

    protected override HeroClassManager.HeroClass CharacterClass
    {
        get { return HeroClassManager.HeroClass.Soldier; }
    }

    protected override float DefaultMoveSpeed
    {
        get { return moveSpeed; }
    }
}
