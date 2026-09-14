// Remembers a button press for a short window so it isn't silently dropped when the requested
// action is blocked (on cooldown, mid-action) at the instant of the press - if the block clears
// before the window expires, the caller fires the action then instead of forcing a re-press.
// Countdown-based (Tick(deltaTime) counts a remaining-seconds field down to zero) to match the
// _cooldownRemaining pattern already used throughout Combat/Player, rather than storing an
// absolute timestamp. DefaultWindowSeconds (0.15s) sits in the middle of the ~100-200ms range
// commonly used for dodge/attack input buffering in action games (e.g. platformer jump buffers
// around 4-6 frames at 60fps, fighting-game buffers around 3-10 frames) - long enough to rescue a
// near-miss press, short enough that it doesn't feel like the input is queued/delayed. Retune
// this one constant if playtesting says otherwise.
public struct InputBuffer
{
    public const float DefaultWindowSeconds = 0.15f;

    private float _remaining;

    public bool HasPending => _remaining > 0f;

    public void Record(float windowSeconds)
    {
        _remaining = windowSeconds;
    }

    public void Tick(float deltaTime)
    {
        if (_remaining > 0f)
        {
            _remaining -= deltaTime;
        }
    }

    // Returns true (and clears the pending press) exactly once per Record() call, as long as the
    // window hasn't expired. Callers should only invoke this once the blocking condition has
    // actually cleared - it does not know what it's gating.
    public bool TryConsume()
    {
        if (_remaining <= 0f)
        {
            return false;
        }

        _remaining = 0f;
        return true;
    }
}
