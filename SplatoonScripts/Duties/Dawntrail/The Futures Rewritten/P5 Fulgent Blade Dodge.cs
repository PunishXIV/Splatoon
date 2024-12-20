Doesn't works yet!
/*using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public unsafe class P5_Fulgent_Blade_Dodge : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(1, "NightmareXIV");

    long MechanicStartTime = 0;
    long Phase => Environment.TickCount64 - MechanicStartTime;

    static Vector2[] DodgeSpots = [new(5.6f, 15.7f), new(8.2f, 8.4f), new(3.8f, 6.7f), new(1.8f, 11.7f)];

    Vector2[] Dodges = [
        DodgeSpots[0],
        DodgeSpots[1],
        DodgeSpots[1],
        DodgeSpots[2],
        DodgeSpots[2],
        DodgeSpots[3],
        DodgeSpots[3],
        ];
    Vector2[] Dodges2 = [
        new(3.6f, 8.9f),
        new(8.6f, 6.8f),
        new(8.6f, 6.8f),
        new(10.5f, 11f),
        new(10.5f, 11f),
        new(3.5f, 14.5f),
        new(3.5f, 14.5f),
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
            Controller.RegisterElementFromCode($"Debug{i}", "{\"Name\":\"\",\"type\":1,\"offY\":2.0,\"radius\":0.68,\"color\":3355508712,\"fillIntensity\":0.5,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
        Controller.RegisterElementFromCode("Debug", "{\"Name\":\"\",\"refX\":90.0,\"refY\":110.0,\"refZ\":1.9073486E-06,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var npcs = Npcs;
        if(Phase > 30000)
        {
            if(npcs.Length > 0)
            {
                MechanicStartTime = Environment.TickCount64;
            }
            Reference = null;
        }
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
            Reference = (r[1].Position, r[1].Rotation);
            IsCCW = MathHelper.GetRelativeAngle(r[0].Position, r[1].Position) < 180;
            DuoLog.Information($"IsCCW: {IsCCW}");
        }
        if(Reference != null)
        {
            var d = Controller.GetElementByName("Debug")!;
            d.Enabled = true;
            d.SetRefPosition(Reference.Value.Position);
            for(int i = 0; i < Dodges.Length; i++)
            {
                if(Phase < 8700 + i * 2000)
                {
                    var e = Controller.GetElementByName($"Dodge{i}");
                    e.Enabled = true;
                    e.overlayText = $"{i + 1}";
                    var refAddPoint = Reference.Value.Position + (Dodges[i] with { X = Dodges[i].X * -1f }).ToVector3(0);
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
}
*/