using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Reflection;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public unsafe class P5_Fulgent_Blade_Dodge : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(3, "NightmareXIV");

    long MechanicStartTime = 0;
    long Phase => Environment.TickCount64 - MechanicStartTime;

    static Vector2[] DodgeCw = [new(5.700f, 10.800f), new(0.800f, 11.300f), new(2.3f, 6.2f), new(6.4f, 9.6f)];
    static Vector2[] DodgeCcw = [new(-5.500f, 12.900f), new(-0.400f, 12.300f), new(-2.400f, 6.100f), new(-5.600f, 10.700f)];

    Vector2[] DodgesCcw = [
        DodgeCcw[0],
        DodgeCcw[1],
        DodgeCcw[1],
        DodgeCcw[2],
        DodgeCcw[2],
        DodgeCcw[3],
        //DodgeCcw[3],
        ];
    Vector2[] DodgesCw = [
        DodgeCw[0],
        DodgeCw[1],
        DodgeCw[1],
        DodgeCw[2],
        DodgeCw[2],
        DodgeCw[3],
        //DodgeCw[3],
        ];

    IBattleNpc[] Npcs => Svc.Objects.OfType<IBattleNpc>().Where(x => x.NameId.EqualsAny<uint>(13561) && x.Struct()->GetCastInfo() != null && x.CastActionId.EqualsAny<uint>(40118, 40307) && x.IsCasting).ToArray();

    (Vector3 Position, float Rotation)? Reference = null;
    bool IsCCW = false;

    public override void OnSetup()
    {
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Dodge{i}", "{\"Name\":\"\",\"refX\":105.18051,\"refY\":101.17668,\"refZ\":9.536743E-07,\"color\":3355639552,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":8.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
        for(int i = 0; i < 8; i++)
        {
            Controller.RegisterElementFromCode($"Debug{i}", "{\"Name\":\"\",\"type\":3,\"refY\":5.0,\"radius\":0.0,\"color\":3356425984,\"fillIntensity\":0.345,\"thicc\":4.0,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
        Controller.RegisterElementFromCode("Debug", "{\"Name\":\"\",\"refX\":90.0,\"refY\":110.0,\"refZ\":1.9073486E-06,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"Ref{i}", "{\"Name\":\"\",\"refX\":90.0,\"refY\":110.0,\"refZ\":1.9073486E-06,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
    }

    public override void OnReset()
    {
        Reference = null;
        MechanicStartTime = 0;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40306)
        {
            this.Controller.Reset();
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements()
            //.Where(x => !x.Key.StartsWith("Ref"))
            .Each(x => x.Value.Enabled = false);
        var npcs = Npcs;
        if(Phase > 30000)
        {
            if(npcs.Length > 0)
            {
                MechanicStartTime = Environment.TickCount64;
            }
            Reference = null;
        }
        /*var validNpcs = npcs.Where(x => ((x.Rotation.RadToDeg() + MathHelper.GetRelativeAngle(x.Position, new(100, 0, 100))) % 360).InRange(180 - 90, 180 + 90)).ToArray();
        for(int i = 0; i < validNpcs.Length; i++)
        {
            if(Controller.TryGetElementByName($"Debug{i}", out var e))
            {
                e.Enabled = true;
                e.refActorObjectID = validNpcs[i].EntityId;
            }
        }*/
        if(npcs.Length == 8 && Reference == null)
        {
            var validNpcs = npcs.Where(x => ((x.Rotation.RadToDeg() + MathHelper.GetRelativeAngle(x.Position, new(100, 0, 100))) % 360).InRange(180 - 90, 180 + 90)).ToArray();
            var early = validNpcs.Where(x => x.CurrentCastTime > 1);
            var late = validNpcs.Where(x => x.CurrentCastTime < 1);
            IBattleNpc[] pair = null;
            foreach(var e in early)
            {
                foreach(var l in late)
                {
                    if(pair == null || Vector3.Distance(pair[0].Position, pair[1].Position) > Vector3.Distance(e.Position, l.Position))
                    {
                        pair = [e, l];
                    }
                }
            }
            var r = pair!.OrderBy(x => x.CurrentCastTime).ToArray();

            var lateRot = MathHelper.GetRelativeAngle(new(100f, 100f), r[0].Position.ToVector2());
            var earlyRot = (MathHelper.GetRelativeAngle(new(100f, 100f), r[1].Position.ToVector2()) - lateRot + 360) % 360;

            IsCCW = earlyRot < 180;
            PluginLog.Information($"Early rotation: {earlyRot}");

            var cross = GetIntersectionPoint(r[0].Position.ToVector2(), r[0].Rotation.RadToDeg(), r[1].Position.ToVector2(), r[1].Rotation.RadToDeg() + 90);

            if(Controller.TryGetElementByName("Ref0", out var e0) && Controller.TryGetElementByName("Ref1", out var e1))
            {
                e0.Enabled = true;
                e0.SetRefPosition(r[0].Position);
                e0.overlayText = "Ref0";
                e1.Enabled = true;
                e1.SetRefPosition(r[1].Position);
                e1.overlayText = "Ref1";
            }

            if(cross != null)
            {
                Reference = (cross.Value.ToVector3(0), r[0].Rotation);
                PluginLog.Information($"IsCCW: {IsCCW}, ref = {Reference} \n {r[0].Position.ToVector2()}, {r[0].Rotation.RadToDeg()} \n {r[1].Position.ToVector2()}, {r[1].Rotation.RadToDeg()}\n{MathHelper.GetRelativeAngle(r[1].Position, r[0].Position)}");
            }
            else
            {
                PluginLog.Error("Fulgent blade reference is null!");
            }
        }
        if(Reference != null)
        {
            var d = Controller.GetElementByName("Debug")!;
            d.Enabled = true;
            d.SetRefPosition(Reference.Value.Position);
            d.overlayText = "Ref";
            for(int i = 0; i < DodgesCcw.Length; i++)
            {
                if(Phase < 8700 + i * 2000)
                {
                    var e = Controller.GetElementByName($"Dodge{i}");
                    e.Enabled = true;
                    e.overlayText = $"{i + 1}";
                    var refAddPoint = Reference.Value.Position + ((IsCCW ? DodgesCcw : DodgesCw)[i]).ToVector3(0);
                    var extend = MathHelper.RotateWorldPoint(Reference.Value.Position, -Reference.Value.Rotation, refAddPoint);
                    e.SetRefPosition(extend);
                    return;
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        foreach(var x in Npcs)
        {
            var rot = (x.Rotation.RadToDeg() + MathHelper.GetRelativeAngle(x.Position, new(100, 0, 100))) % 360;
            ImGuiEx.Text($"{x}: {ExcelActionHelper.GetActionName(x.CastActionId, true)}/{x.CurrentCastTime} - {rot}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x0"></param>
    /// <param name="a0">Degrees</param>
    /// <param name="x1"></param>
    /// <param name="a1">Degrees</param>
    /// <returns></returns>
    public Vector2? GetIntersectionPoint(Vector2 a, float angle1, Vector2 b, float angle2)
    {
        double x1 = a.X, y1 = a.Y;
        double x2 = b.X, y2 = b.Y;

        double m1 = Math.Tan(angle1 * Math.PI / 180.0);
        double m2 = Math.Tan(angle2 * Math.PI / 180.0);

        double b1 = y1 - m1 * x1;
        double b2 = y2 - m2 * x2;

        if(m1 - m2 == 0) return null;

        double xIntersect = (b2 - b1) / (m1 - m2);
        double yIntersect = m1 * xIntersect + b1;
        return new((float)xIntersect, (float)yIntersect);
    }

}
