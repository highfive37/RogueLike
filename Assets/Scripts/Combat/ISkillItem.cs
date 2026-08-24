public interface ISkillItem
{
    void OnActivatePressed();
    void OnActivateReleased();
    void Tick(float deltaTime);

    /// <summary>0 = ready to use, 1 = just used / fully on cooldown (or currently active).</summary>
    float CooldownProgress { get; }

    /// <summary>Remaining cooldown in seconds. 0 when ready.</summary>
    float CooldownRemaining { get; }

    /// <summary>True while the skill is in an uninterruptible, mid-use state (e.g. a grenade
    /// held and cooking). Callers such as SkillController read this to gate actions that would
    /// otherwise abandon that state, like switching skills.</summary>
    bool IsBusy { get; }
}
