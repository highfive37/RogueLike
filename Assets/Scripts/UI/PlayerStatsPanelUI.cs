using System;
using UnityEngine;
using UnityEngine.UI;

// Binding of Isaac-style stat display: each stat is shown as a color-coded
// icon + a 10-pip bar instead of a verbose sentence. Isaac's own pause-menu
// bars aren't a percentage either - the wiki calls them "an imprecise scale"
// that buckets the raw stat value into a fixed per-attribute range table.
// Its exact brackets are tied to Isaac's sqrt damage-soft-cap formula, which
// doesn't apply to our flat-per-stack bonuses, so we use the same idea in a
// form that fits this project.
//
// All four stats are additive bonuses on top of a base stat (Attack Speed
// used to be a multiplier, but was switched to a flat attacks/sec bonus so
// it stacks the same way as the other three), so every bar uses the same
// formula: (base + bonus) against (base + PassiveItemData.CapCount stacks
// worth of bonus) - a fresh character already shows some fill from their
// base stat alone, and the bar reaches full at the same stack count that
// was already tuned via CapCount.
//
// Building and refreshing each row is wrapped defensively (try/catch, error
// logged) and isolated per row - a live incident showed one row's pip build
// throwing (from a script recompile hitting mid-Play) can otherwise abort
// Show() before it reaches panel.SetActive(true), silently breaking the
// whole ESC menu instead of just that one row.
public class PlayerStatsPanelUI : MonoBehaviour
{
    private const int TotalPips = 10;
    private const float PipSize = 18f;
    private const float FilledAlpha = 1f;
    private const float EmptyAlpha = 0.2f;

    [SerializeField] private StatRow attackPowerRow;
    [SerializeField] private StatRow moveSpeedRow;
    [SerializeField] private StatRow projectileRangeRow;
    [SerializeField] private StatRow attackSpeedRow;

    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Projectile projectilePrefab;

    private Image[] _attackPowerPips;
    private Image[] _moveSpeedPips;
    private Image[] _projectileRangePips;
    private Image[] _attackSpeedPips;

    [System.Serializable]
    private class StatRow
    {
        public PassiveItemData data;
        public Image icon;
        public RectTransform pipsContainer;
    }

    private void Awake()
    {
        _attackPowerPips = SafeBuildRow(attackPowerRow, nameof(attackPowerRow));
        _moveSpeedPips = SafeBuildRow(moveSpeedRow, nameof(moveSpeedRow));
        _projectileRangePips = SafeBuildRow(projectileRangeRow, nameof(projectileRangeRow));
        _attackSpeedPips = SafeBuildRow(attackSpeedRow, nameof(attackSpeedRow));
    }

    public void Refresh(PlayerPassiveStats stats)
    {
        float baseDamage = playerCombat != null ? playerCombat.EquippedWeaponDamage : 0f;
        float baseMoveSpeed = playerMovement != null ? playerMovement.BaseMoveSpeed : 0f;
        float baseLifetime = projectilePrefab != null ? projectilePrefab.BaseLifetime : 0f;
        float baseFireCooldown = playerCombat != null ? playerCombat.EquippedFireCooldown : 0f;
        float baseAttackRate = baseFireCooldown > 0f ? 1f / baseFireCooldown : 0f;

        SafeRefreshRow(nameof(attackPowerRow), _attackPowerPips, baseDamage, stats.AttackDamageBonus, attackPowerRow.data);
        SafeRefreshRow(nameof(moveSpeedRow), _moveSpeedPips, baseMoveSpeed, stats.MoveSpeedBonus, moveSpeedRow.data);
        SafeRefreshRow(nameof(projectileRangeRow), _projectileRangePips, baseLifetime, stats.ProjectileLifetimeBonus, projectileRangeRow.data);
        SafeRefreshRow(nameof(attackSpeedRow), _attackSpeedPips, baseAttackRate, stats.AttackSpeedBonus, attackSpeedRow.data);
    }

    private static Image[] SafeBuildRow(StatRow row, string rowName)
    {
        try
        {
            return BuildRow(row);
        }
        catch (Exception e)
        {
            Debug.LogError($"PlayerStatsPanelUI: failed to build the '{rowName}' stat row's pip bar ({e.GetType().Name}: {e.Message}). That row's bar will stay empty until the panel is reloaded.");
            return null;
        }
    }

    private static void SafeRefreshRow(string rowName, Image[] pips, float baseValue, float bonus, PassiveItemData data)
    {
        try
        {
            ApplyFill(pips, AdditiveRatio(baseValue, bonus, data));
        }
        catch (Exception e)
        {
            Debug.LogError($"PlayerStatsPanelUI: failed to refresh the '{rowName}' stat row ({e.GetType().Name}: {e.Message}). Skipping just this row so the rest of the panel still opens.");
        }
    }

    private static Image[] BuildRow(StatRow row)
    {
        row.icon.color = row.data.Color;

        Image[] pips = new Image[TotalPips];
        for (int i = 0; i < TotalPips; i++)
        {
            GameObject pipGo = new GameObject($"Pip{i}", typeof(RectTransform), typeof(Image));
            pipGo.transform.SetParent(row.pipsContainer, false);
            pipGo.GetComponent<RectTransform>().sizeDelta = new Vector2(PipSize, PipSize);

            Image pipImage = pipGo.GetComponent<Image>();
            pipImage.color = row.data.Color;
            pips[i] = pipImage;
        }

        return pips;
    }

    private static float AdditiveRatio(float baseValue, float bonus, PassiveItemData data)
    {
        float fullBonus = data.CapCount * data.Amount;
        float denominator = baseValue + fullBonus;
        return denominator > 0f ? (baseValue + bonus) / denominator : 0f;
    }

    private static void ApplyFill(Image[] pips, float ratio)
    {
        if (pips == null)
        {
            throw new NullReferenceException("pip array is null - its row failed to build.");
        }

        int filled = Mathf.Clamp(Mathf.RoundToInt(ratio * TotalPips), 0, TotalPips);

        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null)
            {
                continue;
            }

            Color color = pips[i].color;
            color.a = i < filled ? FilledAlpha : EmptyAlpha;
            pips[i].color = color;
        }
    }
}
