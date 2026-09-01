using UnityEngine;
using UnityEngine.Pool;

public class GameObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxSize = 50;
    [SerializeField] private string existingInstanceTag;

    private ObjectPool<GameObject> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<GameObject>(
            CreatePooledInstance,
            OnGetFromPool,
            OnReleaseToPool,
            OnDestroyPooledInstance,
            collectionCheck: true,
            defaultCapacity,
            maxSize);

        RegisterExistingInstances();
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject instance = _pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public void Release(GameObject instance)
    {
        _pool.Release(instance);
    }

    private GameObject CreatePooledInstance()
    {
        GameObject instance = Instantiate(prefab, transform);
        RegisterInstance(instance);
        return instance;
    }

    private void RegisterInstance(GameObject instance)
    {
        if (!instance.TryGetComponent<PooledObject>(out var pooledObject))
        {
            pooledObject = instance.AddComponent<PooledObject>();
        }

        pooledObject.SetPool(this);
    }

    private void RegisterExistingInstances()
    {
        if (string.IsNullOrEmpty(existingInstanceTag))
        {
            return;
        }

        GameObject[] existingInstances = GameObject.FindGameObjectsWithTag(existingInstanceTag);
        foreach (GameObject instance in existingInstances)
        {
            RegisterInstance(instance);
        }
    }

    private static void OnGetFromPool(GameObject instance)
    {
        instance.SetActive(true);
        if (instance.TryGetComponent<PooledObject>(out var pooledObject))
        {
            pooledObject.NotifyTakenFromPool();
        }
    }

    private static void OnReleaseToPool(GameObject instance)
    {
        instance.SetActive(false);
    }

    private static void OnDestroyPooledInstance(GameObject instance)
    {
        Destroy(instance);
    }
}
