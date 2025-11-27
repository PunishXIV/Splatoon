using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.Configuration;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using SharpDX.DirectWrite;
using Splatoon.Modules.TranslationWorkspace;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum40;
public unsafe class TFV_Bound_By_Sin : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1311];
    public override Metadata? Metadata => new(4, "lillylilim, NightmareXIV");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode($"line11", """{"Name":"","type":3,"refY":5.0,"offY":-7.0,"radius":0.0,"color":3355508480,"fillIntensity":0.345,"thicc":10.0,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"LineEndB":1}""");
        Controller.RegisterElementFromCode($"line12", """{"Name":"","type":3,"refY":5.0,"offY":-7.0,"radius":0.0,"color":3355508480,"fillIntensity":0.345,"thicc":10.0,"refActorObjectID":0,"refActorComparisonType":2,"includeRotation":true,"LineEndB":1}""");
        Controller.RegisterElementFromCode("AoeInCenter", """
            {"Name":"","refX":-600.0,"refY":-300.0,"radius":8.0,"fillIntensity":0.483}
            """);
        Controller.RegisterElementFromCode("DonutInCenter", """
            {"Name":"","refX":-600.0,"refY":-300.0,"radius":8.0,"Donut":20.0,"fillIntensity":0.483}
            """);

        Controller.RegisterElementFromCode("TowerBottomRight", """
            {"Name":"","refX":-592.0,"refY":-292.0,"radius":2.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("TowerBottomLeft", """
            {"Name":"","refX":-608.5,"refY":-292.0,"radius":2.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("TowerTopRight", """
            {"Name":"","refX":-592.0,"refY":-308.0,"radius":2.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("TowerTopLeft", """
            {"Name":"","refX":-608.0,"refY":-308.0,"radius":2.0,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
    }

    int Sequence = 0;
    int MechanicNum = 0;

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 44122)
        {
            ++Sequence;

            var sourceObject = source.GetObject();
            if(sourceObject == null) return;

            var isCircle = IsFacingTarget(sourceObject.Position.ToVector2(), sourceObject.Rotation, new(-600, -300));
            if(!isCircle)
            {
                PluginLog.Debug($"Not rotated in, skipping / {sourceObject.Position.ToVector2()} / {sourceObject.Rotation}");
                var e = Controller.GetElementByName("DonutInCenter");
                e.Enabled = true;
            }
            else
            {
                var canShow = true;
                if(MechanicNum == 0)
                {
                    var pos = this.Directions.FirstOrNull(x => Vector2.Distance(x.Value, source.GetObject().Position.ToVector2()) < 2);
                    if(pos != null)
                    {
                        if(C.Bos0Disabled.Contains(pos.Value.Key))
                        {
                            canShow = false;
                            PluginLog.Debug($"Can't show {Sequence}, {pos.Value.Key}");
                        }
                    }
                }
                if(MechanicNum == 2)
                {
                    var pos = this.Directions.FirstOrNull(x => Vector2.Distance(x.Value, source.GetObject().Position.ToVector2()) < 2);
                    if(pos != null)
                    {
                        if(C.Bos2Disabled.Contains(pos.Value.Key))
                        {
                            canShow = false;
                            PluginLog.Debug($"Can't show {Sequence}, {pos.Value.Key}");
                        }
                    }
                }
                if((Sequence == 11 || Sequence == 12) && canShow)
                {
                    var line = Controller.GetElementByName($"line{Sequence}")!;

                    line.Enabled = true;
                    line.refActorObjectID = source;

                    this.Controller.Schedule(() => line.Enabled = false, 3000);
                }
            }

            if(Sequence == 12)
            {
                var e = Controller.GetElementByName(isCircle ? "AoeInCenter" : "DonutInCenter");
                e.Enabled = true;
                this.Controller.Schedule(() => e.Enabled = false, 5000);
                Sequence = 0;
                //paired: 0
                //chained: 2
                MechanicNum++;
                if(MechanicNum == 1)
                {
                    this.Controller.Schedule(() =>
                    {
                        if(Controller.TryGetElementByName($"Tower{C.Tower}", out var tower))
                        {
                            tower.Enabled = true;
                            this.Controller.Schedule(() => tower.Enabled = false, 7000);
                        }
                    }, 3000);
                }
            }
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        //> [27.11.2025 20:05:09 +03:00] Message: MapEffect: 27, 1, 2
        if(position == 27 && data1 == 1 && data2 == 2)
        {
            if(MechanicNum == 3)
            {
                if(Controller.TryGetElementByName($"Tower{C.Tower}", out var tower))
                {
                    tower.Enabled = true;
                    this.Controller.Schedule(() => tower.Enabled = false, 15000);
                }
            }
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Sequence = 0;
        MechanicNum = 0;
    }

    public override void OnEnable()
    {
        this.Controller.Reset();
    }

    //copypasta
    public static bool IsFacingTarget(Vector2 posA, float rotA, Vector2 posB)
    {
        // Correct forward vector for clockwise rotations where 0 = up
        Vector2 forward = new Vector2(
            MathF.Sin(rotA),
            MathF.Cos(rotA)
        );

        Vector2 toTarget = posB - posA;

        if(toTarget.LengthSquared() == 0.0f)
        {
            return true;
        }

        toTarget = Vector2.Normalize(toTarget);

        float dot = Vector2.Dot(forward, toTarget);

        if(dot >= 0.0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public enum Tower { Disabled, TopLeft, TopRight, BottomLeft, BottomRight }

    public enum Direction { Top, Left, Bottom, Right }

    Config C => Controller.GetConfig<Config>();
    public class Config: IEzConfig
    {
        public Tower Tower = Tower.Disabled;
        public HashSet<Direction> Bos0Disabled = [];
        public HashSet<Direction> Bos2Disabled = [];
    }

    Dictionary<Direction, Vector2> Directions = new()
    {
        [Direction.Top] = new(-600.000f, -307.500f),
        [Direction.Bottom] = new(-600.000f, -293.000f),
        [Direction.Left] = new(-607.000f, -300.000f),
        [Direction.Right] = new(-593.000f, -300.000f),
    };

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Select tower to take", ref C.Tower);
        ImGui.Separator();
        ImGuiEx.Text($"Bounds of Sin 1 (+partners into towers): Highlighted Directions");
        foreach(var x in Enum.GetValues<Direction>())
        {
            ImGuiEx.CollectionCheckbox($"{x}##1", x, C.Bos0Disabled, true);
        }
        ImGui.Separator();
        ImGuiEx.Text($"Bounds of Sin 3 (+chains): Highlighted Directions");
        foreach(var x in Enum.GetValues<Direction>())
        {
            ImGuiEx.CollectionCheckbox($"{x}##2", x, C.Bos2Disabled, true);
        }

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputInt("MechanicNum", ref this.MechanicNum);
        }
    }
}