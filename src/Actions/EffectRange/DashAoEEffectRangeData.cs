﻿namespace ActionEffectRange.Actions.EffectRange
{
    public class DashAoEEffectRangeData : EffectRangeData
    {
        public DashAoEEffectRangeData(uint actionId, uint actionCategory,
            bool isGT, bool isHarmful, sbyte range, byte effectRange,
            byte xAxisModifier, byte castType, bool isOriginal = false)
            : base(actionId, actionCategory, isGT, isHarmful,
                  range, effectRange, xAxisModifier, castType, isOriginal)
        { }

        public DashAoEEffectRangeData(Lumina.Excel.GeneratedSheets.Action actionRow)
            : this(actionRow.RowId, actionRow.ActionCategory.Row, actionRow.TargetArea,
                  ActionData.IsHarmfulAction(actionRow), actionRow.Range, 
                  actionRow.EffectRange, actionRow.XAxisModifier, 
                  actionRow.CastType, isOriginal: true)
        { }
    }
}
