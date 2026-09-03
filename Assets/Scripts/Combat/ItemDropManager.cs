using System.Collections.Generic;
using UnityEngine;

// Central authority for ground item drops. Active items each drop at most once per run -
// _remainingActiveItems is the single ledger of "not yet confirmed dropped" active items,
// shared by BOTH the random per-kill path (TryDrop) and the guaranteed per-room path
// (SpawnGuaranteedRewards); either one removes an item from it the instant that item is
// handed out, so the same active item can never be given out twice regardless of which path
// gets to it first. (Previously SpawnGuaranteedRewards checked player ownership instead of
// this pool, which let a kill-dropped-but-not-yet-picked-up item also be handed out again by
// an Item Room/Boss Room, and vice versa - fixed by unifying on one pool.) Passive items have
// no such limit and can drop/be granted repeatedly, since their bonuses are meant to stack. A
// single kill drops at most one item total: first a roll decides whether anything drops at
// all, then - if so - a weighted roll between the active/passive categories decides which one
// (never both). EnemySpawner calls TryDrop() when a spawned enemy dies.
//
// Drop chance and every weight (category and per-item) come from GameBalance
// (Assets/StreamingAssets/Balance/ItemDropTable.json) rather than serialized
// fields here, so the whole drop table is hand-tunable from a text file.
public class ItemDropManager : MonoBehaviour
{
    [SerializeField] private ActiveItemData[] allActiveItems;
    [SerializeField] private PassiveItemData[] allPassiveItems;
    [SerializeField] private ItemPickup pickupPrefab;
    [SerializeField] private ItemPickup passivePickupPrefab;
    [SerializeField] private float dropHeight = 0.15f;

    // Used only by SpawnGuaranteedRewards below - unrelated to the random-drop fields above.
    [SerializeField] private float itemRoomSideOffset = 4f;

    private readonly List<ActiveItemData> _remainingActiveItems = new List<ActiveItemData>();

    // A kill next to a doorway can land a drop inside the door's pass-through trigger, which
    // reaches further into the room than the visible doorway does - walking to that item fires
    // the room transition instead of picking it up. SpawnPickup pulls such a drop this far
    // (world units) into the room from the door plane, measured so the player can stand on the
    // pickup with its capsule clear of the trigger.
    private const float DoorClearDistance = 3f;
    private static readonly Collider[] _doorProbe = new Collider[16];

    private void Awake()
    {
        _remainingActiveItems.AddRange(allActiveItems);
    }

    public void TryDrop(Vector3 position)
    {
        if (Random.value > GameBalance.ResolveDropChance())
        {
            return;
        }

        if (ShouldDropActiveCategory())
        {
            DropActive(position);
        }
        else
        {
            DropPassive(position);
        }
    }

    // Guaranteed reward placement - shares _remainingActiveItems with TryDrop below (see class
    // comment), only its own passive selection is independent. Called once each by
    // RoomController.EnterRoom() when a RoomType.Item room first activates, and by
    // RoomFlowManager when the boss room clears, so Item Room and Boss Room share the exact
    // same reward rule instead of each implementing their own. Left slot (+Z, one side of the
    // room) is the next active item still in the pool, or a passive if the pool is empty
    // (every active already handed out); right slot (-Z) is always a passive. Reuses the same
    // pickup prefabs/SpawnPickup helper as the random-drop path so pickup behavior is
    // identical either way.
    public void SpawnGuaranteedRewards(Transform roomOrigin)
    {
        ItemData leftItem = TakeRemainingActive();
        if (leftItem == null)
        {
            leftItem = PickRandomPassive();
        }

        ItemData rightItem = PickRandomPassive();

        if (leftItem != null)
        {
            SpawnPickup(leftItem is ActiveItemData ? pickupPrefab : passivePickupPrefab,
                roomOrigin.TransformPoint(new Vector3(0f, 0f, itemRoomSideOffset)), leftItem);
        }

        if (rightItem != null)
        {
            SpawnPickup(passivePickupPrefab,
                roomOrigin.TransformPoint(new Vector3(0f, 0f, -itemRoomSideOffset)), rightItem);
        }
    }

    // Simple first-available pick (not weighted like PickWeighted below) since this is a
    // single guaranteed slot, not a probability roll - removes immediately so TryDrop's random
    // path can never hand out the same item afterward.
    private ActiveItemData TakeRemainingActive()
    {
        if (_remainingActiveItems.Count == 0)
        {
            return null;
        }

        ActiveItemData item = _remainingActiveItems[0];
        _remainingActiveItems.RemoveAt(0);
        return item;
    }

    private PassiveItemData PickRandomPassive()
    {
        if ((allPassiveItems?.Length ?? 0) == 0)
        {
            return null;
        }

        return allPassiveItems[Random.Range(0, allPassiveItems.Length)];
    }

    // Active/passive are mutually exclusive per drop, chosen by relative category weight -
    // unlike PickWeighted below, an empty active pool (every active item already claimed
    // this run) just falls through to passive instead of wasting the roll on nothing.
    private bool ShouldDropActiveCategory()
    {
        if (_remainingActiveItems.Count == 0)
        {
            return false;
        }

        float activeWeight = Mathf.Max(0f, GameBalance.ResolveActiveCategoryWeight());
        float passiveWeight = Mathf.Max(0f, GameBalance.ResolvePassiveCategoryWeight());
        float totalWeight = activeWeight + passiveWeight;

        return totalWeight > 0f && Random.value * totalWeight < activeWeight;
    }

    private void DropActive(Vector3 position)
    {
        ActiveItemData item = PickWeighted(_remainingActiveItems, data => GameBalance.ResolveActiveDropWeight(data.ItemName));
        _remainingActiveItems.Remove(item);
        SpawnPickup(pickupPrefab, position, item);
    }

    private void DropPassive(Vector3 position)
    {
        if ((allPassiveItems?.Length ?? 0) == 0)
        {
            return;
        }

        PassiveItemData item = PickWeighted(allPassiveItems, data => GameBalance.ResolvePassiveDropWeight(data.Type));
        SpawnPickup(passivePickupPrefab, position, item);
    }

    // Weighted so Assets/StreamingAssets/Balance/ItemDropTable.json can bump an individual
    // item's relative chance (e.g. for fast manual testing) without disturbing the others,
    // which keep the neutral weight of 1 by default.
    private static T PickWeighted<T>(IReadOnlyList<T> items, System.Func<T, float> weightSelector)
    {
        float totalWeight = 0f;
        foreach (T item in items)
        {
            totalWeight += Mathf.Max(0f, weightSelector(item));
        }

        if (totalWeight <= 0f)
        {
            return items[Random.Range(0, items.Count)];
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (T item in items)
        {
            cumulative += Mathf.Max(0f, weightSelector(item));
            if (roll <= cumulative)
            {
                return item;
            }
        }

        return items[items.Count - 1];
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

        // Corrected here, before Instantiate, so the pickup only ever appears at its final
        // reachable spot - never briefly at the raw drop point and then sliding out.
        groundedPosition = NudgeClearOfDoors(groundedPosition);

        ItemPickup pickup = Instantiate(prefab, groundedPosition, prefab.transform.rotation);
        pickup.SetItem(item);
    }

    // Push a drop point out of any nearby door's pass-through trigger, along that door's
    // room-inward direction (from the door plane toward its EntryPoint). No-op for drops
    // already clear of every door - including the guaranteed rewards, which spawn well away
    // from any wall. An unlocked door only exposes its trigger collider here (its blocker is
    // disabled); a locked one adds its blocker too, but the second pass is already clear so it
    // changes nothing.
    private Vector3 NudgeClearOfDoors(Vector3 groundedPosition)
    {
        int count = Physics.OverlapSphereNonAlloc(
            groundedPosition, DoorClearDistance + 1f, _doorProbe, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            DoorController door = _doorProbe[i].GetComponent<DoorController>();
            if (door == null || door.EntryPoint == null)
            {
                continue;
            }

            Vector3 inward = door.EntryPoint.position - door.transform.position;
            inward.y = 0f;
            if (inward.sqrMagnitude < 1e-4f)
            {
                continue;
            }
            inward.Normalize();

            float along = Vector3.Dot(groundedPosition - door.transform.position, inward);
            if (along < DoorClearDistance)
            {
                groundedPosition += inward * (DoorClearDistance - along);
            }
        }

        return groundedPosition;
    }
}
