using UnityEngine;

// Trigger hitbox for ChargeStrikePattern's dash - a plain C# pattern class can't receive
// OnTriggerEnter itself, so this small companion component lives as an (authored-inactive) child
// of the boss, and the pattern arms/disarms it around the dash window instead of the old "check
// distance once when a post-dash pause ends" approach (replaced outright, not layered alongside
// it - see DevLog 2026-09-15 for why running both at once would have let damage and knockback
// resolve at different moments). One-shot per arm - disarms itself the instant it lands a hit, so
// overlapping the player for multiple physics steps in a row can't multi-hit.
[RequireComponent(typeof(BoxCollider))]
public class EnemyChargeHitbox : MonoBehaviour
{
    private BoxCollider _collider;
    private Transform _owner;
    private float _damage;
    private float _knockbackMultiplier;
    private bool _hasHit;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    public void Arm(Transform owner, float damage, float knockbackMultiplier = 1f)
    {
        _owner = owner;
        _damage = damage;
        _knockbackMultiplier = knockbackMultiplier;
        _hasHit = false;
        gameObject.SetActive(true);
    }

    public void Disarm()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit || !other.CompareTag("Player"))
        {
            return;
        }

        if (!other.TryGetComponent<IDamageable>(out var damageable))
        {
            return;
        }

        _hasHit = true;
        damageable.TakeDamage(_damage, _owner, _owner.forward * _knockbackMultiplier);
        Disarm();
    }
}
