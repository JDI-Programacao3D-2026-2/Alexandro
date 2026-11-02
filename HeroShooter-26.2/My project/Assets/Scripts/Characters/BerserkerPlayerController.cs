using UnityEngine;

// Controlador do jogador quando o prefab escolhido é o Berserker.
// Ajuste a velocidade abaixo pelo Inspector.
public class BerserkerPlayerController : PlayerControllerBase
{
    public float moveSpeed = 4.3f;

    protected override HeroClassManager.HeroClass CharacterClass
    {
        get { return HeroClassManager.HeroClass.Berserker; }
    }

    protected override float DefaultMoveSpeed
    {
        get { return moveSpeed; }
    }
}
