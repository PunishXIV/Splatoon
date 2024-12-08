using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
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
    public override Metadata? Metadata => new(3, "Errer, NightmareXIV");
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
        Controller.RegisterElementFromCode("TankLine", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3372209152,\"fillIntensity\":0.345,\"thicc\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Line2", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"fillIntensity\":0.345,\"thicc\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineRot1", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3355508484,\"fillIntensity\":0.345,\"thicc\":4.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("LineRot2", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3355508484,\"fillIntensity\":0.345,\"thicc\":4.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
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
            if(C.ShowInitialApocMove && phase < 15000)
            {
                if(Controller.TryGetElementByName("Line2", out var line))
                {
                    line.Enabled = true;
                    var linePos1 = Positions[1].ToVector3(0);
                    var linePos2 = Positions[-1].ToVector3(0);
                    line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                    line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                }
            }
            if(C.ShowMoveGuide && phase < C.TankDelayMS)
            {
                {
                    if(Controller.TryGetElementByName("LineRot1", out var line))
                    {
                        line.Enabled = true;
                        var linePos1 = Positions[isClockwise ? -4 : 2].ToVector3(0);
                        var linePos2 = Positions[1].ToVector3(0);
                        line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                        line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                    }
                }
                {
                    if(Controller.TryGetElementByName("LineRot2", out var line))
                    {
                        line.Enabled = true;
                        var linePos1 = Positions[isClockwise ? 4 : -2].ToVector3(0);
                        var linePos2 = Positions[-1].ToVector3(0);
                        line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                        line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                    }
                }
            }
            if(phase > C.DelayMS)
            {
                /*{
                    if(Controller.TryGetElementByName("Line", out var line))
                    {
                        line.Enabled = true;
                        var linePos1 = Positions[isClockwise ? 4 : 2].ToVector3(0);
                        var linePos2 = Positions[isClockwise ? -4 : -2].ToVector3(0);
                        line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                        line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                    }
                }*/
                

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
            if(C.ShowTankGuide && phase > C.TankDelayMS)
            {
                if(Controller.TryGetElementByName("TankLine", out var line))
                {
                    line.Enabled = true;
                    var linePos1 = Positions[3].ToVector3(0);
                    var linePos2 = Positions[-3].ToVector3(0);
                    line.SetRefPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos1));
                    line.SetOffPosition(MathHelper.RotateWorldPoint(new(100, 0, 100), angle.DegreesToRadians(), linePos2));
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.SliderIntAsFloat("Delay before displaying AOE", ref C.DelayMS, 0, 12000);
        ImGui.Checkbox("Show initial movement", ref C.ShowInitialApocMove);
        ImGui.Checkbox("Show move guide for party (arrows to safe spot from initial movement)", ref C.ShowMoveGuide);
        ImGui.Checkbox("Show tank bait guide (beta)", ref C.ShowTankGuide);
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.SliderIntAsFloat("Hide move guide and switch to tank bait guide at", ref C.TankDelayMS, 0, 30000);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public int DelayMS = 6000;
        public bool ShowInitialApocMove = true;
        public bool ShowMoveGuide = false;
        public bool ShowTankGuide = false;
        public int TankDelayMS = 18000;
    }
}
