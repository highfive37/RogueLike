using System;

// Plain JsonUtility DTO for Assets/StreamingAssets/Balance/PlayerBaseStats.json.
// Any extra fields in the JSON (like "_guide") are simply ignored by
// JsonUtility since they have no matching field here - that's how the
// in-file documentation comments coexist with strict JSON parsing.
[Serializable]
public class PlayerBaseStatsData
{
    public float moveSpeed;
    public float rangedDamage;
    public float rangedFireCooldown;
    public float meleeDamage;
    public float meleeRange;
    public float projectileSpeed;
    public float projectileLifetime;
}
