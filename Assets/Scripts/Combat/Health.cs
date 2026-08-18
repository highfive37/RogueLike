using System;

public class Health
{
    public event Action Died;
    public event Action<float, float> Changed;

    public float Max { get; }
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;
    public bool IsInvulnerable { get; private set; }

    public Health(float max)
    {
        Max = max;
        Current = max;
    }

    public void SetInvulnerable(bool invulnerable)
    {
        IsInvulnerable = invulnerable;
    }

    public void ResetHealth()
    {
        Current = Max;
        IsInvulnerable = false;
        Changed?.Invoke(Current, Max);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvulnerable || amount <= 0f)
        {
            return;
        }

        Current -= amount;
        if (Current < 0f)
        {
            Current = 0f;
        }

        Changed?.Invoke(Current, Max);

        if (Current <= 0f)
        {
            Died?.Invoke();
        }
    }
}
