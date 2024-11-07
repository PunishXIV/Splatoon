using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker;

public class Boss1_Spring_Crystal_Tower : SplatoonScript
{
    private int _crustalCastingCount;

    private State _state = State.None;
    public override HashSet<uint>? ValidTerritories => [1179, 1180];
    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is { RowId: 35496 })
        {
            _crustalCastingCount++;
            if (_crustalCastingCount != 3) return;
            var crystals = Svc.Objects.Where(x => x.DataId == 0x409D).ToArray();
            var hasBubbleCrystals = crystals.Where(x => float.Abs(x.Position.X) < 11f);
            var isEast = crystals.All(x => x.Position.X > 0);
            var isNorth = hasBubbleCrystals.Any(x => x.Position.Z < -10);

            var xOffset = isEast ? 1f : -1f;
            var yOffset = isNorth ? 1f : -1f;
            var diagonalOffset = C.Direction == Direction.East ? 1f : -1f;
            var position = C.PrioritizeCenter
                ? new Vector2(14f, 0f) * diagonalOffset
                : new Vector2(10f * xOffset, 14f * yOffset) * diagonalOffset;

            if (Controller.TryGetElementByName("Bait", out var element))
            {
                element.SetRefPosition(position.ToVector3());
                element.Enabled = true;
            }

            _state = State.Start;
        }

        if (set.Action is { RowId: 35499 } && _state is State.Start) _state = State.End;
    }

    public override void OnUpdate()
    {
        if (_state is State.Start)
            Controller.GetRegisteredElements()
                .Each(x => x.Value.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint());
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe)) Reset();
    }

    public override void OnCombatEnd()
    {
        Reset();
    }


    private void Reset()
    {
        _state = State.None;
        _crustalCastingCount = 0;
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            radius = 2f,
            thicc = 6f,
            tether = true
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGuiEx.EnumCombo("Direction", ref C.Direction);
            ImGui.Checkbox("Prioritize Center", ref C.PrioritizeCenter);
            ImGui.Text("Bait Color:");
            ImGuiComponents.HelpMarker(
                "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            if (ImGui.Button("Reset")) Reset();
            ImGui.Text($"State: {_state}");
        }
    }

    private enum Direction
    {
        West,
        East
    }

    private enum State
    {
        None,
        Start,
        End
    }

    private class Config : IEzConfig
    {
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public Direction Direction = Direction.West;
        public bool PrioritizeCenter;
    }
}