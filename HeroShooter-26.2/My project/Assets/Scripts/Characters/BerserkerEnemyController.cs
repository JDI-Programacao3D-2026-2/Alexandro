using UnityEngine;

// IA específica do Berserker inimigo.
// Ele prefere lutar mais perto e guarda o especial para quando estiver ferido.
public class BerserkerEnemyController : EnemyControllerBase
{
    [Header("Configuração do Berserker")]
    public float berserkerDesiredRange = 5f;
    public float berserkerMaxShootRange = 11f;
    [Range(0.1f, 1f)] public float healthToUseSpecial = 0.6f;

    protected override HeroClassManager.HeroClass CharacterClass
    {
        get { return HeroClassManager.HeroClass.Berserker; }
    }

    protected override void ConfigureClassBehavior()
    {
        desiredRange = berserkerDesiredRange;
        maxShootRange = berserkerMaxShootRange;
    }

    // Ativa o especial quando a vida estiver abaixo da porcentagem configurada.
    protected override bool ShouldUseSpecial()
    {
        return combat.currentHealth < combat.maxHealth * healthToUseSpecial;
    }
}
