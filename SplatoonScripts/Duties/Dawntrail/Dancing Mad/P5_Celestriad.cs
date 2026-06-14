using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using ECommons;
using ECommons.CSExtensions;
using ECommons.DalamudServices;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

public class P5_Celestriad : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1363];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("""{"Name":"Bait","refX":100.0,"refY":110.0,"radius":2.5,"Donut":0.5,"fillIntensity":0.5,"thicc":8.0,"tether":true}""");
    }

    const uint TowerLightning = 2015296;
    const uint TowerFire = 2015294;
    const uint TowerIce = 2015295;
    const uint DebuffFire = 2902;
    const uint DebuffIce = 2903;
    const uint DebuffLightning = 2998;

    bool? IsNoDebuff = null;

    public override void OnReset()
    {
        IsNoDebuff = null;
    }

    uint[] GetOwnHighestDebuffs()
    {
        return BasePlayer.StatusList.Where(x => x.StatusId.EqualsAny(DebuffFire, DebuffIce, DebuffLightning)).OrderByDescending(x => x.RemainingTime).Take(2).Select(x => x.StatusId).ToArray();
    }

    IEnumerable<IEventObj> ActiveTowers => Svc.Objects.OfType<IEventObj>().Where(x => x.DataId.EqualsAny(TowerFire, TowerIce, TowerLightning) && x.AnimationId == 16);

    public override void OnObjectEffect(uint target, uint entityId, uint actionId)
    {
        if(entityId.EqualsAny(TowerFire, TowerIce, TowerLightning)) EzThrottler.Throttle("Hold", 500, true);
    }

    public override void OnUpdate()
    {
        Controller.Hide(layouts: false);
        if(ActiveTowers.Any() && EzThrottler.Check("Hold"))
        {
            IsNoDebuff ??= GetOwnHighestDebuffs().Length == 0;
            IEventObj myTower;
            if(!IsNoDebuff.Value) //normal case
            {
                var pointZero = ActiveTowers.First(x => x.DataId == GetOwnHighestDebuffs()[0] switch
                {
                    DebuffFire => TowerFire,
                    DebuffIce => TowerIce,
                    DebuffLightning => TowerLightning
                });
                var eligibleTowers = ActiveTowers.Where(x => !(x.DataId switch
                {
                    TowerFire => GetOwnHighestDebuffs().Contains(DebuffFire),
                    TowerIce => GetOwnHighestDebuffs().Contains(DebuffIce),
                    TowerLightning => GetOwnHighestDebuffs().Contains(DebuffLightning),
                }));
                myTower = MathHelper.EnumerateObjectsClockwise(eligibleTowers, x => x.Position.ToVector2(), new(100, 100), pointZero.Position.ToVector2())[0];
            }
            else
            {
                //no debuff case
                var towers = ActiveTowers.GroupBy(x => x.DataId).First(g => g.Count() > 1);
                var pointZero = ActiveTowers.First(x => x.DataId != towers.First().DataId);
                myTower = MathHelper.EnumerateObjectsClockwise(towers, x => x.Position.ToVector2(), new(100, 100), pointZero.Position.ToVector2())[1];
            }
            var e = Controller.GetElementByName("Bait")!;
            e.Enabled = true;
            e.color = Controller.AttentionColor;
            e.RefPosition = myTower.Position;
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Debuffs: {GetOwnHighestDebuffs().Print()}");
            ImGuiEx.Text($"IsNoDebuff: {IsNoDebuff}");
            if(ImGui.Button("Yes")) IsNoDebuff = true;
            if(ImGui.Button("No")) IsNoDebuff = false;
        }
    }
}
