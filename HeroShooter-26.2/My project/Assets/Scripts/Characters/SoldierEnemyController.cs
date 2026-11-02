using UnityEngine;

// IA específica do Soldier inimigo.
// Os valores abaixo podem ser ajustados pelo Inspector.
public class SoldierEnemyController : EnemyControllerBase
{
    [Header("Configuração do Soldier")]
    public float soldierDesiredRange = 14f;
    public float soldierMaxShootRange = 35f;

    protected override HeroClassManager.HeroClass CharacterClass
    {
        get { return HeroClassManager.HeroClass.Soldier; }
    }

    protected override void ConfigureClassBehavior()
    {
        desiredRange = soldierDesiredRange;
        maxShootRange = soldierMaxShootRange;
    }

    // O Soldier tenta usar o especial durante o combate.
    protected override bool ShouldUseSpecial()
    {
        return true;
    }
}
