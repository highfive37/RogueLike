using UnityEngine;

public class PooledObject : MonoBehaviour
{
    private GameObjectPool _pool;
    private bool _isReleased;

    public void SetPool(GameObjectPool pool)
    {
        _pool = pool;
    }

    // Called by GameObjectPool when this instance is handed out again, so a
    // stale release-guard from its previous life in the pool doesn't block
    // it from ever being returned again.
    public void NotifyTakenFromPool()
    {
        _isReleased = false;
    }

    public void ReturnToPool()
    {
        if (_isReleased)
        {
            return;
        }

        _isReleased = true;

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
