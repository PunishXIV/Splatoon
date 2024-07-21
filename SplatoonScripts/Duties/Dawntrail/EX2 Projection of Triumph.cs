using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class EX2_Projection_of_Triumph : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1201];

    public override Metadata? Metadata => new(3, "NightmareXIV");

    private IBattleChara[] Donuts => [.. Svc.Objects.OfType<IBattleChara>().Where(x => x.DataId == 16727)];

    private IBattleChara[] Circles => [.. Svc.Objects.OfType<IBattleChara>().Where(x => x.DataId == 16726)];

    private IBattleChara[] Towers => [.. Svc.Objects.OfType<IBattleChara>().Where(x => x.DataId == 17079)];

    private List<uint> RightMovers = [];
    private List<uint> LeftMovers = [];
    private int? RotateModifier = null;

    public override void OnSetup()
    {
        Controller.RegisterElement("Donut", new(0)
        {
            color = ImGuiEx.Vector4FromRGBA(0xFF8200C8).ToUint(),
            refX = 100, refY = 100,
        });
        Controller.RegisterElement("Circle", new(0)
        {
            color = ImGuiEx.Vector4FromRGBA(0xFF8200C8).ToUint(),
            refX = 100,            refY = 100,
        });
    }

    //northwest is virtual north
    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Values.Each(x => x.Enabled = false);
        if (Towers.Length == 0)
        {
            RotateModifier = null;
            LeftMovers.Clear();
            RightMovers.Clear();
            return;
        }
        if(Donuts.Length + Circles.Length >= 2)
        {
            CalculateModifier();
        }
        if (RotateModifier == null) return;
        foreach (var x in Donuts.Concat(Circles))
        {
            var xPos = RotateRelative(x.Position);
            if (xPos.Z > 120f) LeftMovers.Add(x.EntityId);
            if (xPos.Z < 80f) RightMovers.Add(x.EntityId);
        }
        for (var i = 0; i < Towers.Length; i++)
        {
            var t = Towers[i];
            var name = $"Tower{i}";
            if (!Controller.TryGetElementByName(name, out _)) Controller.RegisterElement(name, new(0));
            var element = Controller.GetElementByName(name);
            var originalPos = t.Position;
            var rotatedPos = RotateRelative(originalPos);
            element.SetRefPosition(originalPos);
            //element.overlayText = $"Original: {originalPos}\nRotated:{rotatedPos}";
            element.overlayVOffset = 2;
            foreach (var x in Donuts.Concat(Circles))
            {
                var xPos = RotateRelative(x.Position);
                var isDonut = x.DataId == 16727;
                if (!xPos.Z.InRange(80, 120)) continue;
                if (LeftMovers.Contains(x.EntityId))
                {
                    var distance = xPos.Z - rotatedPos.Z;
                    if (distance.InRange(0, 8))
                    {
                        Assign(element, isDonut, distance);
                        //element.overlayText += $"\n{(isDonut ? "Donut" : "Circle")}/{xPos}";
                    }
                }
                if (RightMovers.Contains(x.EntityId))
                {
                    var distance = rotatedPos.Z - xPos.Z;
                    if (distance.InRange(0, 8))
                    {
                        Assign(element, isDonut, distance);
                        //element.overlayText += $"\n{(isDonut ? "Donut" : "Circle")}/{xPos}";
                    }
                }
            }
        }
    }

    void CalculateModifier()
    {
        if(Donuts.Concat(Circles).Any(x => Vector3.Distance(x.Position, new(85,0,115)) < 3))
        {
            RotateModifier = -45;
        }
        else if(Donuts.Concat(Circles).Any(x => Vector3.Distance(x.Position, new(85, 0, 85)) < 3))
        {
            RotateModifier = 45;
        }
    }

    void Assign(Element e, bool isDonut, float distance)
    {
        e.Enabled = true;
        var percent = Math.Clamp(1f - distance / 8f, 0f, 1f);
        if (isDonut)
        {
            e.Filled = false;
            e.Donut = 6f;
            e.radius = 2f;
            e.SetDisplayStyle(Controller.GetElementByName("Donut").GetDisplayStyle());
        }
        else
        {
            e.Filled = true;
            e.Donut = 0f;
            e.radius = 4f;
            e.SetDisplayStyle(Controller.GetElementByName("Circle").GetDisplayStyle());
        }
    }

    private Vector3 RotateRelative(Vector3 s)
    {
        var swapped = new Vector3(s.X, s.Z, s.Y);
        var rotated = Utils.RotatePoint(100, 100, this.RotateModifier.Value.DegreesToRadians(), swapped);
        return new(rotated.X, rotated.Z, rotated.Y);
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"""
            Towers: {Towers.Print()}
            Circles: {Circles.Print()}
            Donuts: {Donuts.Print()}
            RotateModifier: {RotateModifier}
            """);
    }
}
