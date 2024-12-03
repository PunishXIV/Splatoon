using ECommons;
using ECommons.DalamudServices;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P3_Apocalypse : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Errer, NightmareXIV");
    public long StartTime = 0;

    Dictionary<int, Vector2> Positions = new()
    {
        [0] = new(100, 100),
        [1] = new(100, 86),
        [2] = new(109.9f, 90.1f),
        [3] = new(114, 100),
        [4] = new(109.9f, 109.9f),
        [-4] = new(90.1f, 90.1f),
        [-3] = new(86, 100),
        [-2] = new(90.1f, 109.9f),
        [-1] = new(100, 114),
    };

    public override void OnSetup()
    {
        for(int i = 0; i < 6; i++)
        {
            Controller.RegisterElementFromCode($"Circle{i}", "{\"Name\":\"Circle\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":9.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
            Controller.RegisterElementFromCode($"EarlyCircle{i}", "{\"Name\":\"Circle\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":9.0,\"color\":3355508223,\"fillIntensity\":0.25,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }
    }

    int[][] Clockwise = [[0, 1, -1], [0, 1, -1, 2, -2], [1, 2, 3, -1, -2, -3], [2, 3, 4, -2, -3, -4], [1, 3, 4, -1, -3, -4], [1,2,4,-1,-2,-4]];
    int[][] CounterClockwise = [[0, 1, -1], [0, 1, -1, 4, -4], [1, 3, 4, -1, -3, -4], [2, 3, 4, -2, -3, -4], [1, 2, 3, -1, -2, -3], [1, 2, 4, -1, -2, -4]];

    void Draw(int[] values, int rotation, bool early = false)
    {
        for(int i = 0; i < values.Length; i++)
        {
            if(Controller.TryGetElementByName((early?"Early":"")+$"Circle{i}", out var e))
            {
                e.Enabled = true;
                var pos = Positions[values[i]].ToVector3(0);
                var rotated = MathHelper.RotateWorldPoint(new(100, 0, 100), rotation.DegreesToRadians(), pos);
                e.SetRefPosition(rotated);
            }
        }
    }

    int InitialDelay = 14000 + 4000;
    int Delay = 2000;

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40296) StartTime = Environment.TickCount64;
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var obj = Svc.Objects.Where(x => x.DataId == 2011391);
        var close = obj.FirstOrDefault(x => Vector3.Distance(x.Position, Positions[0].ToVector3(0)) < 1f);
        var far = obj.FirstOrDefault(x => Vector3.Distance(x.Position, Positions[1].ToVector3(0)) < 1f);
        if(close != null && far != null)
        {
            var rot = close.Rotation.RadToDeg();
            var angle = 0;
            if(rot.InRange(22, 22 + 45) || rot.InRange(180 + 22, 180 + 22 + 45)) angle = 45*3;
            if(rot.InRange(22 + 45, 22 + 45 * 2) || rot.InRange(180 + 22 + 45, 180 + 22 + 45 * 2)) angle = 45*2;
            if(rot.InRange(22 + 45*2, 22 + 45 * 3) || rot.InRange(180 + 22 + 45*2, 180 + 22 + 45 * 3)) angle = 45;
            var isClockwise = far.Rotation.RadToDeg().InRange(45,45+90);
            var set = isClockwise ? Clockwise : CounterClockwise;
            var phase = Environment.TickCount64 - StartTime;
            if(phase > 6000)
            {
                for(int i = 0; i < 6; i++)
                {
                    if(phase < InitialDelay + Delay * i)
                    {
                        Draw(set[i], angle);
                        if(i < 5)
                        {
                            Draw(set[i + 1].Where(x => !set[i].Contains(x)).ToArray(), angle, true);
                        }
                        break;
                    }
                }
            }
        }
    }
}
