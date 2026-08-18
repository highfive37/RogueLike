using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class GrenadeProjectile : MonoBehaviour
{
    [SerializeField] private float arcHeight = 0.5f;

    private PooledObject _pooledObject;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _throwDuration;
    private float _throwElapsed;
    private float _fuseRemaining;
    private float _damage;
    private float _explosionRadius;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();

        // Pooled prefabs are instantiated as children of their pool by GameObjectPool.
        // Detach so nothing (e.g. the throwing character's Transform) can ever be an
        // ancestor and drag this object along while it is flying/waiting to explode.
        transform.SetParent(null, true);
    }

    public void Initialize(Vector3 targetPosition, float throwDuration, float fuseRemaining, float damage, float explosionRadius)
    {
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _throwDuration = throwDuration;
        _throwElapsed = 0f;
        _fuseRemaining = fuseRemaining;
        _damage = damage;
        _explosionRadius = explosionRadius;
    }

    private void Update()
    {
        // Recompute and re-assert world position every frame (not only while the
        // throw arc is in progress) so the grenade keeps holding its landed spot
        // for the rest of the fuse instead of silently drifting.
        _throwElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_throwElapsed / _throwDuration);
        Vector3 flatPosition = Vector3.Lerp(_startPosition, _targetPosition, t);
        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = flatPosition + Vector3.up * height;

        _fuseRemaining -= Time.deltaTime;
        if (_fuseRemaining <= 0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }
        }

        _pooledObject.ReturnToPool();
    }
}
