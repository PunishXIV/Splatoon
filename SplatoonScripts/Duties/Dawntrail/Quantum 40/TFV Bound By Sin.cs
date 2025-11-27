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
    public override Metadata? Metadata => new(3, "lillylilim, NightmareXIV");

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

                if(Sequence == 11 || Sequence == 12)
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

    Config C => Controller.GetConfig<Config>();
    public class Config: IEzConfig
    {
        public Tower Tower = Tower.Disabled;
    }

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f);
        ImGuiEx.EnumCombo("Select tower to take", ref C.Tower);

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputInt("MechanicNum", ref this.MechanicNum);
        }
    }
}