﻿using ActionEffectRange.Actions.Data;
using ActionEffectRange.Actions.Enums;
using Lumina.Excel;
using GeneratedSheets = Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;


namespace ActionEffectRange.Actions
{
    public static class ActionData
    {
        public static readonly ActionBlacklist ActionBlacklist = new(Plugin.Config);

        public static readonly ExcelSheet<GeneratedSheets.Action>? ActionExcelSheet 
            = Plugin.DataManager.GetExcelSheet<GeneratedSheets.Action>();

        public static readonly ExcelSheet<GeneratedSheets.ActionCategory>? ActionCategoryExcelSheet
            = Plugin.DataManager.GetExcelSheet<GeneratedSheets.ActionCategory>();

        public static GeneratedSheets.Action? GetActionExcelRow(uint actionId)
            => ActionExcelSheet?.GetRow(actionId);

        public static IEnumerable<GeneratedSheets.Action>? GetAllPartialMatchActionExcelRows
            (string input, bool alsoMatchId, int maxCount, System.Func<GeneratedSheets.Action, bool>? filter)
            => ActionExcelSheet?.Where(row => row != null 
                && (row.Name.RawString.Contains(input, System.StringComparison.CurrentCultureIgnoreCase)
                || alsoMatchId && row.RowId.ToString().Contains(input))
                && (filter == null || filter(row)))
            .Take(maxCount);

        public static EffectRangeData? GetActionEffectRangeDataRaw(uint actionId)
        {
            var row = GetActionExcelRow(actionId);
            return row != null ? new(row) : null;
        }

        // Unk46: 0 - No direct effect, e.g. nonattacking move action like AM and En Avant, pet summon&ordering actions;
        //        1 - attacking-type;
        //        2 - healing-type;
        // From observation, not sure.
        // But grounded attacking AoE (salted earth, doton, the removed ShadowFlare etc.) are all 2,
        // Celetial Opposition(pve) is 1 (probably a legacy design where it used to stun enemies?).
        public static bool IsHarmfulAction(GeneratedSheets.Action actionRow)
            => actionRow.Unknown46 == 1;

        public static ushort GetRecast100ms(GeneratedSheets.Action actionRow)
            => actionRow.Recast100ms;

        public static bool IsPlayerCombatAction(GeneratedSheets.Action actionRow)
            => actionRow.IsPlayerAction 
            && IsCombatActionCategory((ActionCategory)actionRow.ActionCategory.Row);

        public static bool IsCombatActionCategory(ActionCategory actionCategory)
            => actionCategory is ActionCategory.Ability or ActionCategory.AR 
            or ActionCategory.LB or ActionCategory.Spell or ActionCategory.WS;

        public static bool IsSpecialOrArtilleryActionCategory(ActionCategory actionCategory)
            => actionCategory is ActionCategory.Special or ActionCategory.Artillery;

        public static string GetActionCategoryName(ActionCategory actionCategory)
            => ActionCategoryExcelSheet?.GetRow((uint)actionCategory)?.Name ?? string.Empty;

        public static bool IsRuledOutAction(uint actionId) => RuledOutActions.HashSet.Contains(actionId);

        public static bool CheckPetAction(EffectRangeData ownerActionData, out HashSet<EffectRangeData?>? petActionEffectRangeDataSet)
        {
            petActionEffectRangeDataSet = null;
            if (!PetActionMap.Dictionary.TryGetValue(ownerActionData.ActionId, out var petActionIds) || petActionIds == null) return false;
            petActionEffectRangeDataSet = petActionIds
                .Select(id => GetActionEffectRangeDataRaw(id))
                .Where(data => data != null && data.EffectRange > 0)
                .ToHashSet();
            return petActionEffectRangeDataSet.Any();
        }

        public static bool CheckPetLikeAction(EffectRangeData ownerActionData, out HashSet<EffectRangeData?>? petLikeActionEffectRangeDataSet)
        {
            petLikeActionEffectRangeDataSet = null;
            if (!PetLikeActionMap.Dictionary.TryGetValue(ownerActionData.ActionId, out var petActionIds) || petActionIds == null) return false;
            petLikeActionEffectRangeDataSet = petActionIds
                .Select(id => GetActionEffectRangeDataRaw(id))
                .Where(data => data != null && data.EffectRange > 0)
                .ToHashSet();
            return petLikeActionEffectRangeDataSet.Any();
        }

        public static HashSet<EffectRangeData> CheckEffectRangeDataOverriding(EffectRangeData original)
        {
            // TODO: check CastType / harmfulness (predefined / user)
            // ** flamethrower etc.

            var updated = CheckConeAoEAngleOverriding(original);
            var updatedSet = EffectRangeCornerCases.GetUpdatedEffectDataSet(updated);
            if (!updatedSet.Any()) updatedSet.Add(updated);
            return updatedSet;
        }

        private static EffectRangeData CheckConeAoEAngleOverriding(EffectRangeData original)
        {
            if (original.AoEType != ActionAoEType.Cone 
                && original.AoEType != ActionAoEType.Cone2)
                return original;

            if (ConeAoEAngleMap.PredefinedSpecial.TryGetValue(original.ActionId, out var coneData))
                return EffectRangeData.OverrideAsConeAoE(
                    original, coneData.CentralAngleBy2pi, coneData.RotationOffset);

            // TODO: check user overriding

            if (ConeAoEAngleMap.DefaultAnglesByRange.TryGetValue(original.EffectRange, out var angle))
                return EffectRangeData.OverrideAsConeAoE(original, angle);

            return original;
        }


        public static bool IsActionBlacklisted(uint actionId) 
            => ActionBlacklist.Contains(actionId);
    }
}
