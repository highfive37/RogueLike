using System;

public interface IWeapon
{
    void Attack();
    void Tick(float deltaTime);

    // Fires exactly when the weapon actually executes its effect (projectile spawned / melee
    // hit applied) - not when the attack button was pressed. For ranged weapons those can differ:
    // a press made while on cooldown is buffered (see InputBuffer) and the real Fire() happens
    // later inside Tick(). PlayerCombat hooks this once in EquipWeapon() to trigger the attack
    // animation in sync with the actual attack, instead of the input press.
    Action OnFired { get; set; }
}
