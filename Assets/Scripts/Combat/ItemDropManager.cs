using System.Collections.Generic;
using UnityEngine;

// Central authority for ground item drops. Active items each drop at most
// once per run (removed from the pool once rolled, so it's never seen
// again); passive items have no such limit and can drop repeatedly, since
// their bonuses are meant to stack. Active and passive each get their own
// independent roll per kill (different chances by design), so a single kill
// can drop neither, either, or both. EnemySpawner calls TryDrop() when a
// spawned enemy dies.
public class ItemDropManager : MonoBehaviour
{
    [SerializeField] private ActiveItemData[] allActiveItems;
    [SerializeField] private PassiveItemData[] allPassiveItems;
    [SerializeField] private ItemPickup pickupPrefab;
    [SerializeField] private ItemPickup passivePickupPrefab;
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float passiveDropChance = 0.5f;
    [SerializeField] private float dropHeight = 0.15f;

    private readonly List<ActiveItemData> _remainingActiveItems = new List<ActiveItemData>();

    private void Awake()
    {
        _remainingActiveItems.AddRange(allActiveItems);
    }

    public void TryDrop(Vector3 position)
    {
        TryDropActive(position);
        TryDropPassive(position);
    }

    private void TryDropActive(Vector3 position)
    {
        if (_remainingActiveItems.Count == 0 || Random.value > dropChance)
        {
            return;
        }

        int index = Random.Range(0, _remainingActiveItems.Count);
        ActiveItemData item = _remainingActiveItems[index];
        _remainingActiveItems.RemoveAt(index);
        SpawnPickup(pickupPrefab, position, item);
    }

    private void TryDropPassive(Vector3 position)
    {
        int passiveCount = allPassiveItems?.Length ?? 0;
        if (passiveCount == 0 || Random.value > passiveDropChance)
        {
            return;
        }

        PassiveItemData item = allPassiveItems[Random.Range(0, passiveCount)];
        SpawnPickup(passivePickupPrefab, position, item);
    }

    private void SpawnPickup(ItemPickup prefab, Vector3 position, ItemData item)
    {
        // Kill positions come from the enemy's own transform, which sits at
        // its capsule pivot height (well above the floor) rather than the
        // ground - drop at a fixed, small height above the floor instead so
        // pickups rest on the ground rather than floating at head height.
        // Passing Quaternion.identity here would also silently override
        // whatever rotation each prefab was authored with (e.g. the passive
        // pickup's diagonal lie-on-its-side rotation), so use the prefab's
        // own rotation instead.
        Vector3 groundedPosition = new Vector3(position.x, dropHeight, position.z);
        ItemPickup pickup = Instantiate(prefab, groundedPosition, prefab.transform.rotation);
        pickup.SetItem(item);
    }
}
