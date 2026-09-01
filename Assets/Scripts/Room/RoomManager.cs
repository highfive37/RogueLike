using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private readonly List<HealthComponent> _enemies = new List<HealthComponent>();
    private int _remainingEnemies;
    private bool _roomCleared;

    public event Action RoomCleared;

    private void OnDestroy()
    {
        UnsubscribeAll();
    }

    public void BeginRoom(IEnumerable<HealthComponent> enemies)
    {
        UnsubscribeAll();
        _enemies.Clear();

        foreach (HealthComponent health in enemies)
        {
            if (health == null)
            {
                continue;
            }

            _enemies.Add(health);
            health.Health.Died += HandleEnemyDied;
        }

        _remainingEnemies = _enemies.Count;
        _roomCleared = false;
        Debug.Log($"Room started with {_remainingEnemies} enemies.");
    }

    private void UnsubscribeAll()
    {
        foreach (HealthComponent enemy in _enemies)
        {
            if (enemy != null)
            {
                enemy.Health.Died -= HandleEnemyDied;
            }
        }
    }

    private void HandleEnemyDied()
    {
        if (_roomCleared)
        {
            return;
        }

        _remainingEnemies--;

        if (_remainingEnemies <= 0)
        {
            _roomCleared = true;
            Debug.Log("Room Cleared");
            RoomCleared?.Invoke();
        }
    }
}
