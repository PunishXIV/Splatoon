
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Generic;
internal class TargetCircle : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [];

    public override Metadata? Metadata => new(0, "sourpuh");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Front", GetConeString("Front", 4278190335, 0));
        Controller.RegisterElementFromCode("FrontGradient", GetOuterFanString("Front Gradient", MultiplyAlpha(4278190335, 0.6f), 0));

        Controller.RegisterElementFromCode("Left", GetConeString("Left", 4278255615, MathF.PI / 2 * 3));
        Controller.RegisterElementFromCode("LeftGradient", GetOuterFanString("LeftGradient", MultiplyAlpha(4278255615, 0.6f), MathF.PI / 2 * 3));

        Controller.RegisterElementFromCode("Right", GetConeString("Right", 4278255615, MathF.PI / 2));
        Controller.RegisterElementFromCode("RightGradient", GetOuterFanString("RightGradient", MultiplyAlpha(4278255615, 0.6f), MathF.PI / 2));

        Controller.RegisterElementFromCode("Back", GetConeString("Back", 4278255360, MathF.PI));
        Controller.RegisterElementFromCode("BackGradient", GetOuterFanString("BackGradient", MultiplyAlpha(4278255360, 0.6f), MathF.PI));
    }

    public string GetConeString(string name, uint color, float addAngle)
    {
        return $$"""
        {
            "Name": "{{name}}",
            "Enabled": true,
            "type": 4,
            "refActorType": 2,
            "Enabled": true,
            "Filled": true,
            "fillIntensity": 0.06,
            "radius": 0.0,
            "includeHitbox": true,
            "color": {{color}},
            "thicc": 1,
            "coneAngleMin": -45,
            "coneAngleMax": 45,
            "includeRotation": true,
            "AdditionalRotation": {{addAngle}}
        }
        """;
    }

    public string GetOuterFanString(string name, uint color, float addAngle)
    {
        return $$"""
        {
            "Name": "{{name}}",
            "Enabled": true,
            "type": 4,
            "refActorType": 2,
            "Enabled": true,
            "Filled": true,
            "fillIntensity": 0.06,
            "radius": -0.5,
            "Donut": 0.5,
            "includeHitbox": true,
            "overrideFillColor": true,
            "originFillColor": {{MultiplyAlpha(color, 0)}},
            "endFillColor": {{color}},
            "thicc": 0,
            "coneAngleMin": -45,
            "coneAngleMax": 45,
            "includeRotation": true,
            "AdditionalRotation": {{addAngle}}
        }
        """;
    }

    public static IGameObject? CurrentTarget => Svc.Targets.Target;
    public override void OnUpdate()
    {
        var show = Svc.Condition[ConditionFlag.InCombat] && TargetNeedsPositionals(CurrentTarget);
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = show);
    }

    public static bool TargetNeedsPositionals(IGameObject? obj)
    {
        if(obj == null) return false;
        if(TargetHasEffect(obj, 3808)) return false; // Directional Disregard Effect (Patch 7.01)
        if(Svc.Data.GetExcelSheet<BNpcBase>().TryGetFirst(x => x.RowId == obj.DataId, out var bnpc))
            return !bnpc.IsOmnidirectional;
        return true;
    }
    public static bool TargetHasEffect(IGameObject obj, uint effectId)
    {
        if(obj is not IBattleChara chara) return false;
        return chara.StatusList.Where(status => status.StatusId == effectId).Any();
    }
    public static uint MultiplyAlpha(uint color, float alphaMultiplier)
    {
        var alpha = (uint)(((color & 0xFF000000) >> 24) * alphaMultiplier);
        return color & 0x00FFFFFF | alpha << 24;
    }
}
