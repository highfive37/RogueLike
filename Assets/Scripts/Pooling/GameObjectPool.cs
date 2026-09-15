using System.Collections;
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

    // Pre-creates and briefly activates `count` instances so the first *real* Get() during
    // gameplay doesn't pay for a cold Instantiate() plus the engine's first-ever draw of that
    // prefab's material/shader (2026-09-15 - VFX effect prefabs were flashing solid cyan for a
    // frame on their genuinely first spawn each session, then rendering correctly every time
    // after). Keeping each instance active for one real frame (not just Get()+Release() back to
    // back, which would deactivate it before a camera ever draws it) is what actually forces
    // that first-use cost to happen here instead of during a real hit/shot. Call from a caller's
    // Start(), not Awake() - this pool's own Awake() must have already built `_pool`.
    public void WarmUp(int count)
    {
        if (count > 0)
        {
            StartCoroutine(WarmUpRoutine(count));
        }
    }

    private IEnumerator WarmUpRoutine(int count)
    {
        var instances = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            instances[i] = _pool.Get();
        }

        yield return null;

        for (int i = 0; i < count; i++)
        {
            _pool.Release(instances[i]);
        }
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
