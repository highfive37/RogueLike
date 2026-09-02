using UnityEngine;

public class FlamethrowerActiveItem : IActiveItem
{
    // Height above the owner's pivot the debug rectangle is drawn at, just enough to avoid
    // z-fighting with floor geometry.
    private const float DebugVisualGroundOffset = 0.05f;
    private const float DebugVisualLineWidth = 0.08f;

    private static Material _debugLineMaterial;

    private readonly Transform _owner;
    private readonly FlamethrowerActiveItemData _data;
    private readonly float _damagePerTick;

    // Sourced from data rather than hardcoded so a future level curve or passive/item
    // modifier can shorten it (faster ticks) without changing how ticking itself works.
    private readonly float _tickInterval;

    private bool _isActive;
    private float _activeElapsed;
    private float _totalDamageDealt;
    private float _cooldownRemaining;
    private float _tickTimer;

    // Lazily created only while spraying with the F1 debug overlay on; torn down the moment
    // either condition stops holding so it never lingers as a stray scene object.
    private GameObject _debugVisual;
    private LineRenderer _debugLine;

    public FlamethrowerActiveItem(Transform owner, FlamethrowerActiveItemData data, int level)
    {
        _owner = owner;
        _data = data;
        _damagePerTick = ActiveItemLeveling.ScaleDamage(data.DamagePerTick, level);
        _tickInterval = data.TickInterval;
    }

    public float CooldownProgress => _isActive
        ? 1f
        : (_data.Cooldown > 0f ? Mathf.Clamp01(_cooldownRemaining / _data.Cooldown) : 0f);
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
    public bool IsBusy => false;

    public void OnActivatePressed()
    {
        if (_isActive || _cooldownRemaining > 0f)
        {
            return;
        }

        _isActive = true;
        _activeElapsed = 0f;
        _totalDamageDealt = 0f;

        // Start already "due" so the first damage tick lands on the same frame the spray
        // starts, instead of the player waiting a full tickInterval for the first hit.
        _tickTimer = _tickInterval;
        Debug.Log("Flamethrower activated");
    }

    public void OnActivateReleased()
    {
        EndActivation();
    }

    public void Tick(float deltaTime)
    {
        if (_isActive)
        {
            ApplyDamage(deltaTime);
            UpdateDebugVisual();

            _activeElapsed += deltaTime;
            if (_activeElapsed >= _data.MaxDuration)
            {
                EndActivation();
            }

            return;
        }

        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= deltaTime;
        }
    }

    private void EndActivation()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _cooldownRemaining = _data.Cooldown;
        DestroyDebugVisual();
        Debug.Log($"Flamethrower ended (active for {_activeElapsed:F2}s, total damage dealt: {_totalDamageDealt:F1})");
    }

    // Gated to once per _tickInterval (a while loop rather than a plain if, so a stray long
    // frame that spans more than one interval still lands the right number of ticks instead
    // of silently dropping them) so each hit lands as one meaningful lump of damage - matches
    // how DebugOverlayController's popup and HealthComponent's per-hit event are meant to be
    // read, and avoids running OverlapBox/TakeDamage every single frame.
    private void ApplyDamage(float deltaTime)
    {
        _tickTimer += deltaTime;

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyDamageTick();
        }
    }

    private void ApplyDamageTick()
    {
        Vector3 center = _owner.position + _owner.forward * (_data.ZoneLength * 0.5f) + Vector3.up * 0.5f;
        Vector3 halfExtents = new Vector3(_data.ZoneWidth * 0.5f, 0.5f, _data.ZoneLength * 0.5f);
        Collider[] hits = Physics.OverlapBox(center, halfExtents, _owner.rotation);

        foreach (Collider hit in hits)
        {
            if (hit.transform == _owner)
            {
                continue;
            }

            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damagePerTick, _owner);
                _totalDamageDealt += _damagePerTick;
            }
        }
    }

    // Draws the zone's ground footprint (ZoneWidth x ZoneLength, same origin/orientation as
    // ApplyDamage's OverlapBox) as a flat red outline so testers can see exactly what the
    // flamethrower is actually hitting. Toggled by the same F1 overlay as the rest of the
    // debug HUD, checked every frame so it appears/disappears immediately even mid-spray.
    private void UpdateDebugVisual()
    {
        if (!DebugOverlayController.IsDebugVisible)
        {
            DestroyDebugVisual();
            return;
        }

        if (_debugVisual == null)
        {
            _debugVisual = new GameObject("FlamethrowerRangeDebug");
            _debugLine = _debugVisual.AddComponent<LineRenderer>();
            _debugLine.loop = true;
            _debugLine.positionCount = 4;
            _debugLine.useWorldSpace = true;
            _debugLine.startWidth = _debugLine.endWidth = DebugVisualLineWidth;
            _debugLine.material = GetDebugLineMaterial();
            _debugLine.startColor = _debugLine.endColor = Color.red;
        }

        Vector3 center = _owner.position + _owner.forward * (_data.ZoneLength * 0.5f);
        Vector3 right = _owner.right * (_data.ZoneWidth * 0.5f);
        Vector3 forward = _owner.forward * (_data.ZoneLength * 0.5f);
        float groundY = _owner.position.y + DebugVisualGroundOffset;

        Vector3 c0 = center - right - forward;
        Vector3 c1 = center + right - forward;
        Vector3 c2 = center + right + forward;
        Vector3 c3 = center - right + forward;
        c0.y = c1.y = c2.y = c3.y = groundY;

        _debugLine.SetPosition(0, c0);
        _debugLine.SetPosition(1, c1);
        _debugLine.SetPosition(2, c2);
        _debugLine.SetPosition(3, c3);
    }

    private void DestroyDebugVisual()
    {
        if (_debugVisual == null)
        {
            return;
        }

        Object.Destroy(_debugVisual);
        _debugVisual = null;
        _debugLine = null;
    }

    private static Material GetDebugLineMaterial()
    {
        if (_debugLineMaterial == null)
        {
            _debugLineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return _debugLineMaterial;
    }
}
