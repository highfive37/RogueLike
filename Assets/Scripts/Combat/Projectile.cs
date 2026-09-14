using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class Projectile : MonoBehaviour
{
    // Speed/lifetime are hand-tunable balance numbers pulled from
    // GameBalance (Assets/StreamingAssets/Balance/PlayerBaseStats.json)
    // instead of serialized fields - safe to set per-instance here since
    // every pooled Projectile is its own clone, never the source prefab
    // asset itself.
    private float _speed;
    private float _lifetime;

    public float BaseLifetime => _lifetime;

    private PooledObject _pooledObject;
    private Vector3 _direction = Vector3.forward;
    private float _damage;
    private Transform _owner;
    private float _elapsed;
    private float _bonusLifetime;
    private float _rangeOverride;
    private bool _hasReturned;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        _speed = GameBalance.Player.projectileSpeed;
        _lifetime = GameBalance.Player.projectileLifetime;
        _rangeOverride = -1f;
    }

    // rangeOverride (world units, >= 0) pins this projectile's travel
    // distance regardless of the shared GameBalance projectile lifetime.
    // Enhanced Shot passes it so the skill's range lives on its own data
    // asset and isn't dragged around by basic-attack range tuning; the
    // basic attack / passive-range path leaves it at -1 and keeps using
    // lifetime + bonusLifetime.
    public void Initialize(Vector3 direction, float damage, Transform owner, float bonusLifetime = 0f, float rangeOverride = -1f)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _damage = damage;
        _owner = owner;
        _elapsed = 0f;
        _bonusLifetime = bonusLifetime;
        _rangeOverride = rangeOverride;
        _hasReturned = false;
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        _elapsed += Time.deltaTime;

        float lifeLimit = _rangeOverride >= 0f && _speed > 0f
            ? _rangeOverride / _speed
            : _lifetime + _bonusLifetime;

        if (_elapsed >= lifeLimit)
        {
            ReturnToPoolOnce();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == _owner)
        {
            return;
        }

        // Projectiles pass straight through one another - an enemy shot and a
        // player shot don't cancel out or do anything on contact. A deliberate
        // mutual-cancel / blank mechanic can be added later if wanted.
        if (other.TryGetComponent<Projectile>(out _))
        {
            return;
        }

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage, _owner, _direction);
        }

        ReturnToPoolOnce();
    }

    // A projectile can overlap more than one trigger collider in the same
    // physics step (e.g. two enemies standing close together), and a hit can
    // land in the same frame the lifetime timer expires - either case would
    // call PooledObject.ReturnToPool() twice without this guard.
    private void ReturnToPoolOnce()
    {
        if (_hasReturned)
        {
            return;
        }

        _hasReturned = true;
        _pooledObject.ReturnToPool();
    }
}
