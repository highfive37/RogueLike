using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Single source of truth for hand-tunable gameplay numbers (player base
// stats, passive item effects), loaded from plain JSON files under
// Assets/StreamingAssets/Balance/ so they can be edited with a text editor
// instead of going through the Unity Editor/Inspector for every tweak.
//
// Lazy-loaded on first access rather than tied to any GameObject's Awake/
// script execution order - whichever system reads GameBalance.Player or
// calls a Resolve*() method first triggers the load, so there's no
// ordering dependency to get wrong.
//
// If a file is missing or fails to parse, this falls back to the built-in
// defaults below (matching whatever the last known-good tuning was) and
// logs a warning rather than throwing - a bad JSON edit should degrade to
// "you get default numbers" during a playtest, not crash the game.
public static class GameBalance
{
    private const string BalanceFolder = "Balance";
    private const string PlayerStatsFileName = "PlayerBaseStats.json";
    private const string PassiveItemsFileName = "PassiveItems.json";
    private const string ItemDropTableFileName = "ItemDropTable.json";
    private const string ActiveSkillsFileName = "ActiveSkills.json";
    private const float DefaultDropWeight = 1f;
    private const float DefaultDropChance = 0.5f;

    // 1:5 mirrors the old independent 0.1/0.5 active/passive chances' relative rarity - active
    // items are the rarer, bigger deal, passives are the common stat trickle.
    private const float DefaultActiveCategoryWeight = 1f;
    private const float DefaultPassiveCategoryWeight = 5f;

    private static bool _loaded;
    private static PlayerBaseStatsData _player;
    private static Dictionary<PassiveItemType, PassiveItemEntryData> _passiveByType;
    private static Dictionary<string, float> _activeDropWeightsByName;
    private static Dictionary<PassiveItemType, float> _passiveDropWeightsByType;
    private static float _dropChance;
    private static float _activeCategoryWeight;
    private static float _passiveCategoryWeight;
    private static GrenadeSkillData _grenadeSkill;
    private static EnhancedShotSkillData _enhancedShotSkill;
    private static FlamethrowerSkillData _flamethrowerSkill;

    // Static fields aren't guaranteed to reset between Play sessions purely
    // from a domain reload (observed directly in this project's automation
    // environment - the cached values survived a Stop -> Play cycle with no
    // reload happening in between). RuntimeInitializeOnLoadMethod instead
    // hooks Unity's actual runtime-init pipeline, which reliably runs once
    // per Play session (and once per launch in a build) independent of
    // whether the C# domain was reloaded - so an edited JSON file is always
    // picked up fresh the next time the game starts, not just "usually."
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetForNewSession()
    {
        _loaded = false;
        _player = null;
        _passiveByType = null;
        _activeDropWeightsByName = null;
        _passiveDropWeightsByType = null;
        _dropChance = DefaultDropChance;
        _activeCategoryWeight = DefaultActiveCategoryWeight;
        _passiveCategoryWeight = DefaultPassiveCategoryWeight;
        _grenadeSkill = null;
        _enhancedShotSkill = null;
        _flamethrowerSkill = null;
    }

    public static PlayerBaseStatsData Player
    {
        get
        {
            EnsureLoaded();
            return _player;
        }
    }

    // GrenadeActiveItemData/EnhancedShotActiveItemData/FlamethrowerActiveItemData read every
    // gameplay number through these instead of their own serialized fields, so
    // Assets/StreamingAssets/Balance/ActiveSkills.json is the sole authority - same pattern as
    // Player above. skillName/Color stay on the ScriptableObject asset since those are
    // identity/visual, not balance.
    public static GrenadeSkillData GrenadeSkill
    {
        get
        {
            EnsureLoaded();
            return _grenadeSkill;
        }
    }

    public static EnhancedShotSkillData EnhancedShotSkill
    {
        get
        {
            EnsureLoaded();
            return _enhancedShotSkill;
        }
    }

    public static FlamethrowerSkillData FlamethrowerSkill
    {
        get
        {
            EnsureLoaded();
            return _flamethrowerSkill;
        }
    }

    // ItemData/PassiveItemData call these instead of reading their own
    // serialized fields directly, so the JSON stays the sole authority for
    // these two numbers per passive item type.
    public static float ResolvePassiveAmount(PassiveItemType type)
    {
        EnsureLoaded();
        return _passiveByType.TryGetValue(type, out PassiveItemEntryData entry) ? entry.amount : DefaultAmount(type);
    }

    public static int ResolvePassiveCapCount(PassiveItemType type)
    {
        EnsureLoaded();
        return _passiveByType.TryGetValue(type, out PassiveItemEntryData entry) ? entry.capCount : DefaultCapCount(type);
    }

    // ItemDropManager calls these instead of hand-rolling its own weights, so
    // Assets/StreamingAssets/Balance/ItemDropTable.json stays the sole
    // authority for how likely each item is to drop relative to the others.
    // Missing entries (item not listed in the file) fall back to the neutral
    // weight of 1 rather than a per-item hardcoded default, since there's no
    // "correct" balance value to fall back to here the way there is for
    // stats - 1 just means "no special treatment".
    public static float ResolveActiveDropWeight(string itemName)
    {
        EnsureLoaded();
        return _activeDropWeightsByName.TryGetValue(itemName, out float weight) ? weight : DefaultDropWeight;
    }

    public static float ResolvePassiveDropWeight(PassiveItemType type)
    {
        EnsureLoaded();
        return _passiveDropWeightsByType.TryGetValue(type, out float weight) ? weight : DefaultDropWeight;
    }

    // Per-kill chance that anything drops at all (active or passive, never both - see
    // ItemDropManager.TryDrop). Which of the two it is, if it does, is then decided by
    // ResolveActiveCategoryWeight/ResolvePassiveCategoryWeight below.
    public static float ResolveDropChance()
    {
        EnsureLoaded();
        return _dropChance;
    }

    public static float ResolveActiveCategoryWeight()
    {
        EnsureLoaded();
        return _activeCategoryWeight;
    }

    public static float ResolvePassiveCategoryWeight()
    {
        EnsureLoaded();
        return _passiveCategoryWeight;
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _player = LoadPlayerBaseStats();
        _passiveByType = LoadPassiveItems();
        LoadItemDropTable(out _activeDropWeightsByName, out _passiveDropWeightsByType, out _dropChance, out _activeCategoryWeight, out _passiveCategoryWeight);
        LoadActiveSkills(out _grenadeSkill, out _enhancedShotSkill, out _flamethrowerSkill);
    }

    private static PlayerBaseStatsData LoadPlayerBaseStats()
    {
        string path = Path.Combine(Application.streamingAssetsPath, BalanceFolder, PlayerStatsFileName);

        try
        {
            return ParsePlayerBaseStats(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameBalance: failed to load '{path}' ({e.Message}). Using built-in fallback player stats.");
            return DefaultPlayerBaseStats();
        }
    }

    // Parse step split from file I/O so EditMode tests can feed JSON strings directly. Returns
    // the built-in defaults (never null, never throws) for null/empty/garbage input - the same
    // all-or-nothing degrade the file-load path uses.
    internal static PlayerBaseStatsData ParsePlayerBaseStats(string json)
    {
        try
        {
            PlayerBaseStatsData data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<PlayerBaseStatsData>(json);
            return data ?? DefaultPlayerBaseStats();
        }
        catch (Exception)
        {
            return DefaultPlayerBaseStats();
        }
    }

    private static Dictionary<PassiveItemType, PassiveItemEntryData> LoadPassiveItems()
    {
        var result = new Dictionary<PassiveItemType, PassiveItemEntryData>();
        string path = Path.Combine(Application.streamingAssetsPath, BalanceFolder, PassiveItemsFileName);

        try
        {
            string json = File.ReadAllText(path);
            PassiveItemBalanceData data = JsonUtility.FromJson<PassiveItemBalanceData>(json);
            if (data?.items == null)
            {
                throw new Exception("Parsed data or its items array was null.");
            }

            foreach (PassiveItemEntryData entry in data.items)
            {
                if (!Enum.TryParse(entry.type, out PassiveItemType type))
                {
                    Debug.LogWarning($"GameBalance: unrecognized passive item type '{entry.type}' in '{path}', skipping.");
                    continue;
                }

                // ProjectileRange is authored in game units (matching how
                // range gets discussed/tuned) but consumed internally as a
                // projectile-lifetime bonus in seconds - convert once here
                // so PlayerPassiveStats/Projectile never need to know units
                // were involved.
                if (type == PassiveItemType.ProjectileRange && _player.projectileSpeed > 0f)
                {
                    entry.amount /= _player.projectileSpeed;
                }

                result[type] = entry;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameBalance: failed to load '{path}' ({e.Message}). Using built-in fallback passive item values.");
        }

        return result;
    }

    private static void LoadItemDropTable(out Dictionary<string, float> activeWeights, out Dictionary<PassiveItemType, float> passiveWeights, out float dropChance, out float activeCategoryWeight, out float passiveCategoryWeight)
    {
        activeWeights = new Dictionary<string, float>();
        passiveWeights = new Dictionary<PassiveItemType, float>();
        dropChance = DefaultDropChance;
        activeCategoryWeight = DefaultActiveCategoryWeight;
        passiveCategoryWeight = DefaultPassiveCategoryWeight;
        string path = Path.Combine(Application.streamingAssetsPath, BalanceFolder, ItemDropTableFileName);

        try
        {
            string json = File.ReadAllText(path);
            ItemDropTableData data = JsonUtility.FromJson<ItemDropTableData>(json);
            if (data == null)
            {
                throw new Exception("Parsed data was null.");
            }

            dropChance = data.dropChance;
            activeCategoryWeight = data.activeCategoryWeight;
            passiveCategoryWeight = data.passiveCategoryWeight;

            if (data.activeWeights != null)
            {
                foreach (ActiveDropWeightEntry entry in data.activeWeights)
                {
                    activeWeights[entry.name] = entry.weight;
                }
            }

            if (data.passiveWeights != null)
            {
                foreach (PassiveDropWeightEntry entry in data.passiveWeights)
                {
                    if (!Enum.TryParse(entry.type, out PassiveItemType type))
                    {
                        Debug.LogWarning($"GameBalance: unrecognized passive item type '{entry.type}' in '{path}', skipping.");
                        continue;
                    }

                    passiveWeights[type] = entry.weight;
                }
            }
        }
        catch (Exception e)
        {
            dropChance = DefaultDropChance;
            activeCategoryWeight = DefaultActiveCategoryWeight;
            passiveCategoryWeight = DefaultPassiveCategoryWeight;
            Debug.LogWarning($"GameBalance: failed to load '{path}' ({e.Message}). Using built-in fallback drop chance ({DefaultDropChance}), active/passive category weight ({DefaultActiveCategoryWeight}/{DefaultPassiveCategoryWeight}), and the neutral drop weight of {DefaultDropWeight} for all items.");
        }
    }

    private static void LoadActiveSkills(out GrenadeSkillData grenade, out EnhancedShotSkillData enhancedShot, out FlamethrowerSkillData flamethrower)
    {
        grenade = DefaultGrenadeSkill();
        enhancedShot = DefaultEnhancedShotSkill();
        flamethrower = DefaultFlamethrowerSkill();
        string path = Path.Combine(Application.streamingAssetsPath, BalanceFolder, ActiveSkillsFileName);

        try
        {
            string json = File.ReadAllText(path);
            ActiveSkillBalanceData data = JsonUtility.FromJson<ActiveSkillBalanceData>(json);
            if (data == null)
            {
                throw new Exception("Parsed data was null.");
            }

            // Each sub-object defaults to null (not the field's own defaults) when its key is
            // missing from the JSON entirely, so fall back per-item rather than per-field -
            // matching LoadPlayerBaseStats' all-or-nothing fallback for the same reason.
            if (data.grenade != null)
            {
                grenade = data.grenade;
            }

            if (data.enhancedShot != null)
            {
                enhancedShot = data.enhancedShot;
            }

            if (data.flamethrower != null)
            {
                flamethrower = data.flamethrower;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameBalance: failed to load '{path}' ({e.Message}). Using built-in fallback active skill values.");
        }
    }

    // Built-in fallbacks below mirror the last hand-tuned values agreed on
    // this session, so a missing/broken JSON file degrades to "the game
    // still plays the way it did" rather than default/placeholder numbers.
    private static PlayerBaseStatsData DefaultPlayerBaseStats()
    {
        return new PlayerBaseStatsData
        {
            moveSpeed = 5f,
            rangedDamage = 8f,
            rangedFireCooldown = 0.65f,
            meleeDamage = 15f,
            meleeRange = 1.5f,
            projectileSpeed = 15f,
            projectileLifetime = 0.2f,
            dashSpeedMultiplier = 3f,
            dashDuration = 0.2f
        };
    }

    private static float DefaultAmount(PassiveItemType type)
    {
        switch (type)
        {
            case PassiveItemType.AttackPower: return 2.5f;
            case PassiveItemType.MoveSpeed: return 1.5f;
            case PassiveItemType.ProjectileRange: return 2f / DefaultPlayerBaseStats().projectileSpeed;
            case PassiveItemType.AttackSpeed: return 0.15f;
            default: return 0f;
        }
    }

    private static int DefaultCapCount(PassiveItemType type)
    {
        switch (type)
        {
            case PassiveItemType.AttackPower: return 8;
            case PassiveItemType.MoveSpeed: return 6;
            case PassiveItemType.ProjectileRange: return 6;
            case PassiveItemType.AttackSpeed: return 24;
            default: return 1;
        }
    }

    private static GrenadeSkillData DefaultGrenadeSkill()
    {
        return new GrenadeSkillData
        {
            fuseTime = 2.5f,
            throwDistance = 2.5f,
            throwTravelTime = 0.25f,
            explosionRadius = 1f,
            damage = 15f,
            cooldown = 3f
        };
    }

    private static EnhancedShotSkillData DefaultEnhancedShotSkill()
    {
        return new EnhancedShotSkillData
        {
            damage = 16f,
            cooldown = 2f,
            range = 25f
        };
    }

    private static FlamethrowerSkillData DefaultFlamethrowerSkill()
    {
        return new FlamethrowerSkillData
        {
            damagePerTick = 3f,
            maxDuration = 7f,
            zoneLength = 4.5f,
            zoneWidth = 2.25f,
            cooldown = 3f,
            tickInterval = 1f
        };
    }
}
