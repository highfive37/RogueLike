using UnityEngine;

public class PooledObject : MonoBehaviour
{
    private GameObjectPool _pool;

    public void SetPool(GameObjectPool pool)
    {
        _pool = pool;
    }

    public void ReturnToPool()
    {
        if (_pool != null)
        {
            _pool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
