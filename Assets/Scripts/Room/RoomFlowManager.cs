using UnityEngine;

// Prototype room loop: clear the room -> upgrade pick -> equip swap -> confirm
// respawns a room. There is no real multi-room content yet, so "next room"
// alternates between a normal-enemy stage and a boss stage on repeat.
public class RoomFlowManager : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private UpgradeSelectionUI upgradeSelectionUI;
    [SerializeField] private EquipSelectionUI equipSelectionUI;
    [SerializeField] private PlayerSkillInventory playerInventory;
    [SerializeField] private GameObjectPool enemyPool;
    [SerializeField] private Vector3 enemySpawnPosition = new Vector3(10f, 1f, 10f);
    [SerializeField] private GameObjectPool bossPool;
    [SerializeField] private Vector3 bossSpawnPosition = new Vector3(-10f, 1.5f, 10f);
    [SerializeField] private GameObjectPool projectilePool;

    // The very first stage (already placed in the scene) is a normal enemy,
    // so the first pool-spawned stage after that should be the boss.
    private bool _nextStageIsBoss = true;

    private void OnEnable()
    {
        roomManager.RoomCleared += HandleRoomCleared;
    }

    private void OnDisable()
    {
        roomManager.RoomCleared -= HandleRoomCleared;
    }

    private void HandleRoomCleared()
    {
        upgradeSelectionUI.Show(OnUpgradeChosen);
    }

    private void OnUpgradeChosen(SkillItemData chosen)
    {
        playerInventory.Acquire(chosen);
        equipSelectionUI.Show(playerInventory, OnEquipConfirmed);
    }

    private void OnEquipConfirmed()
    {
        GameObjectPool poolToSpawn = _nextStageIsBoss ? bossPool : enemyPool;
        Vector3 spawnPosition = _nextStageIsBoss ? bossSpawnPosition : enemySpawnPosition;

        if (poolToSpawn != null)
        {
            GameObject instance = poolToSpawn.Get(spawnPosition, Quaternion.identity);
            if (instance.TryGetComponent<EnemyStateMachine>(out var enemyStateMachine))
            {
                enemyStateMachine.SetProjectilePool(projectilePool);
            }
        }

        _nextStageIsBoss = !_nextStageIsBoss;
        roomManager.BeginRoom();
    }
}
