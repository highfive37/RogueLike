using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    private PooledObject _pooledObject;
    private Vector3 _direction = Vector3.forward;
    private float _damage;
    private float _elapsed;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    public void Initialize(Vector3 direction, float damage)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _damage = damage;
        _elapsed = 0f;
    }

    private void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;

        _elapsed += Time.deltaTime;
        if (_elapsed >= lifetime)
        {
            _pooledObject.ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(_damage);
            Debug.Log($"Projectile hit: {other.name}");
        }

        _pooledObject.ReturnToPool();
    }
}
