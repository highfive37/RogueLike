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
    private bool _hasReturned;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        _speed = GameBalance.Player.projectileSpeed;
        _lifetime = GameBalance.Player.projectileLifetime;
    }

    public void Initialize(Vector3 direction, float damage, Transform owner, float bonusLifetime = 0f)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _damage = damage;
        _owner = owner;
        _elapsed = 0f;
        _bonusLifetime = bonusLifetime;
        _hasReturned = false;
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        _elapsed += Time.deltaTime;
        if (_elapsed >= _lifetime + _bonusLifetime)
        {
            ReturnToPoolOnce();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage, _owner);
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
