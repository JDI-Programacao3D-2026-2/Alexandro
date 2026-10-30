// Qualquer coisa que possa levar dano de uma bala.
public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float amount, HeroCombat attacker);
}
