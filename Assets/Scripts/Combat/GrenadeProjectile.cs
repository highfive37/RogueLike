using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class GrenadeProjectile : MonoBehaviour
{
    [SerializeField] private float arcHeight = 0.5f;

    private PooledObject _pooledObject;
    private Transform _owner;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _throwDuration;
    private float _throwElapsed;
    private float _fuseRemaining;
    private float _damage;
    private float _explosionRadius;
    private bool _isThrown;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    // Attaches the grenade to the holder (the throwing character) at localOffset so it
    // visibly rides along with them while armed, and starts the fuse immediately.
    public void Hold(Transform holder, Vector3 localOffset, float fuseTime, float damage, float explosionRadius)
    {
        _isThrown = false;
        _owner = holder;
        transform.SetParent(holder, false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;

        _fuseRemaining = fuseTime;
        _damage = damage;
        _explosionRadius = explosionRadius;
    }

    // Releases the grenade from its holder and starts its throw arc toward targetPosition.
    // The fuse keeps counting down from wherever Hold() left it.
    public void Throw(Vector3 targetPosition, float throwDuration)
    {
        // Detach so nothing (e.g. the throwing character's Transform) can drag this
        // object along while it is flying/waiting to explode.
        transform.SetParent(null, true);

        _isThrown = true;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _throwDuration = throwDuration;
        _throwElapsed = 0f;
    }

    private void Update()
    {
        if (_isThrown)
        {
            // Recompute and re-assert world position every frame (not only while the
            // throw arc is in progress) so the grenade keeps holding its landed spot
            // for the rest of the fuse instead of silently drifting.
            _throwElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_throwElapsed / _throwDuration);
            Vector3 flatPosition = Vector3.Lerp(_startPosition, _targetPosition, t);
            float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = flatPosition + Vector3.up * height;
        }

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
                damageable.TakeDamage(_damage, _owner);
            }
        }

        // Detach in case this explodes while still held, so the pooled/inactive
        // instance doesn't stay nested under the holder's transform.
        transform.SetParent(null, true);
        _pooledObject.ReturnToPool();
    }
}
