using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;
using static Splatoon.Splatoon;
#pragma warning disable CS0618

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class EX7_Gaze_of_the_Void : SplatoonScript<EX7_Gaze_of_the_Void.Config>
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1362];

    public uint TankBallId = 19910;
    public uint DpsBallId = 19909;
    public uint Vuln = 2941;

    List<uint> FastBalls = [];
    List<uint> SlowBalls = [];
    int PickedOrbs = 0;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Point", """
            {"Name":"Point","type":1,"offY":2.0,"radius":2.0,"color":3372220415,"Filled":false,"fillIntensity":0.3,"thicc":6.0,"refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true,"refActorTetherParam2":407,"refActorIsTetherLive":true}
            """);
        Controller.RegisterElementFromCode("PointDanger", """
            {"Name":"Point","type":1,"offY":2.0,"radius":2.0,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190335,"overlayTextColor":4294967295,"overlayFScale":2.0,"thicc":3.0,"overlayText":"!!! WAIT !!!","refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"onlyVisible":true,"tether":true,"refActorTetherParam2":407,"refActorIsTetherLive":true}
            """);
    }

    public override void OnReset()
    {
        FastBalls.Clear();
        SlowBalls.Clear();
        PickedOrbs = 0;
    }

    public uint TetherFast = 407;
    public uint TetherSlow = 406;

    public override void OnUpdate()
    {
        Controller.Hide();
        var tankBalls = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == this.TankBallId && x.GetTethers().Count != 0);
        var dpsBalls = Svc.Objects.OfType<IBattleNpc>().Where(x => x.DataId == this.DpsBallId && x.GetTethers().Count != 0);
        if(!tankBalls.Any() && !dpsBalls.Any())
        {
            OnReset();
        }
        else if(FastBalls.Count == 0 && tankBalls.Count() == 2 && dpsBalls.Count() == 6)
        {
            FastBalls = MathHelper.EnumerateObjectsClockwise(dpsBalls.Where(x => x.GetTethers().Any(t => t.Id == this.TetherFast)), x => x.Position.ToVector2(), new(100, 100), tankBalls.First().Position.ToVector2()).Select(x => x.ObjectId).ToList();
            SlowBalls = MathHelper.EnumerateObjectsClockwise(dpsBalls.Where(x => x.GetTethers().Any(t => t.Id == this.TetherSlow)), x => x.Position.ToVector2(), new(100, 100), tankBalls.First().Position.ToVector2()).Select(x => x.ObjectId).ToList();
        }
        if(FastBalls.Count == 3 && SlowBalls.Count == 3)
        {
            if(PickedOrbs == 0)
            {
                var e = Controller.GetElementByName("Point");
                e.Enabled = true;
                e.color = Controller.AttentionColor;
                e.refActorObjectID = BasePlayer.Job.IsDps()?FastBalls[C.Priority - 1]:tankBalls.FirstOrDefault(x => x.GetTethers().Any(t => t.Id == this.TetherFast))?.ObjectId ?? 0;
            }
            else if(PickedOrbs == 1)
            {
                var effectRemains = BasePlayer.StatusList.FirstOrDefault(s => s.StatusId == this.Vuln)?.RemainingTime ?? 0f;
                if(effectRemains == 0)
                {
                    var e = Controller.GetElementByName("Point");
                    e.Enabled = true;
                    e.color = Controller.AttentionColor;
                    e.refActorObjectID = BasePlayer.Job.IsDps() ? SlowBalls[C.Priority - 1] : tankBalls.FirstOrDefault(x => x.GetTethers().Any(t => t.Id == this.TetherSlow))?.ObjectId ?? 0;
                }
                else
                {
                    var e = Controller.GetElementByName("PointDanger");
                    e.Enabled = true;
                    e.refActorObjectID = BasePlayer.Job.IsDps() ? SlowBalls[C.Priority - 1] : tankBalls.FirstOrDefault(x => x.GetTethers().Any(t => t.Id == this.TetherSlow))?.ObjectId ?? 0;
                    e.overlayText = effectRemains > 0 ? $"!!! Wait {effectRemains:F1}s !!!" : "";
                }
            }
            else
            {
                this.OnReset();
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId.EqualsAny<uint>(50006, 50007) == true && set.TargetEffects.Any(s => s.TargetID == BasePlayer.ObjectId))
        {
            PickedOrbs++;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.TextWrapped($"Your position as DPS/Healer, clockwise starting from tank orbs (if you're tank, this setting is irrelevant for you):");
        ImGui.SetNextItemWidth(200f);
        ImGui.SliderInt("##pos", ref C.Priority.ValidateRange(1, 3), 1, 3);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Picked orbs: {PickedOrbs}");
        }
    }

    public class Config
    {
        public int Priority = 1;
    }
}
