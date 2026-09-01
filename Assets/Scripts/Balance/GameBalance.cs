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

    private static bool _loaded;
    private static PlayerBaseStatsData _player;
    private static Dictionary<PassiveItemType, PassiveItemEntryData> _passiveByType;

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
    }

    public static PlayerBaseStatsData Player
    {
        get
        {
            EnsureLoaded();
            return _player;
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

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        _player = LoadPlayerBaseStats();
        _passiveByType = LoadPassiveItems();
    }

    private static PlayerBaseStatsData LoadPlayerBaseStats()
    {
        string path = Path.Combine(Application.streamingAssetsPath, BalanceFolder, PlayerStatsFileName);

        try
        {
            string json = File.ReadAllText(path);
            PlayerBaseStatsData data = JsonUtility.FromJson<PlayerBaseStatsData>(json);
            if (data == null)
            {
                throw new Exception("Parsed data was null.");
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameBalance: failed to load '{path}' ({e.Message}). Using built-in fallback player stats.");
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
            projectileLifetime = 0.2f
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
}
