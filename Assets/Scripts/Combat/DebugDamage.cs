using UnityEngine;

// Dev/test switch: while EnemyOneShot is on, every hit that lands on a non-player
// HealthComponent is rewritten to a fixed lethal amount at the single choke point
// (HealthComponent.TakeDamage), overriding whatever the weapon/skill computed.
//
// It only replaces the final number - it never touches PlayerPassiveStats or any weapon field -
// so toggling it back off resumes the normal "base + all collected AttackPower bonuses" path
// with nothing to restore, including bonuses picked up while it was on.
//
// Toggled by F2 (DebugOverlayController). Reset per Play session the same way GameBalance /
// PauseState do, so an editor static left set doesn't carry into the next run.
public static class DebugDamage
{
    public const float OneShotAmount = 99999f;

    public static bool EnemyOneShot { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetForNewSession()
    {
        EnemyOneShot = false;
    }
}
