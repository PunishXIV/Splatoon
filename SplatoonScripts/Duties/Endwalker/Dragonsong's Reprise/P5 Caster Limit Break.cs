using Dalamud.Game.Command;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P5_Caster_Limit_Break : SplatoonScript
{
    public enum Direction : byte
    {
        NorthNorthWest,
        WestNorthWest,
        WestSouthWest,
        SouthSouthWest,
        SouthSouthEast,
        EastSouthEast,
        EastNorthEast,
        NorthNorthEast
    }

    private const uint LimitBreak2Id = 204;
    public override HashSet<uint>? ValidTerritories => [];

    private Config C => Controller.GetConfig<Config>();

    public override Metadata? Metadata => new(2, "Garume");

    public override void OnEnable()
    {
        Svc.Commands.AddHandler("/limitBreak", new CommandInfo(OnCastLimitBreak));
    }

    public override void OnDisable()
    {
        Svc.Commands.RemoveHandler("/limitBreak");
    }

    private void OnCastLimitBreak(string command, string arguments)
    {
        var targetPosition = TargetPosition(C.Direction);
        var result = UseLimitBreak(targetPosition);
        if(result)
            DuoLog.Debug($"Limit Break casted to {targetPosition}");
        else
            DuoLog.Warning("Failed to cast Limit Break");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Limit Break Settings");
        ImGuiEx.EnumCombo("Direction", ref C.Direction);

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"My Position: {Player.Position}");
            ImGui.Text($"Target Position: {TargetPosition(C.Direction)}");
        }
    }

    private unsafe bool UseLimitBreak(Vector3 targetPosition)
    {
        const uint targetId = 0xE0000000;
        return ActionManager.Instance()->UseActionLocation(ActionType.Action, LimitBreak2Id, targetId, &targetPosition);
    }

    private Vector3 TargetPosition(Direction direction)
    {
        const float radius = 5.27f;
        Vector2 center = new(100, 100);
        const float angleOffset = 22f;
        var angle = direction switch
        {
            Direction.NorthNorthWest => 90f + angleOffset,
            Direction.WestNorthWest => 180f - angleOffset,
            Direction.WestSouthWest => 180f + angleOffset,
            Direction.SouthSouthWest => 270f - angleOffset,
            Direction.SouthSouthEast => 270f + angleOffset,
            Direction.EastSouthEast => 360f - angleOffset,
            Direction.EastNorthEast => 0f + angleOffset,
            Direction.NorthNorthEast => 90f - angleOffset,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        var x = center.X + radius * MathF.Cos(MathF.PI * angle / 180);
        var y = center.Y - radius * MathF.Sin(MathF.PI * angle / 180);
        return new Vector3(x, 0, y);
    }

    public class Config : IEzConfig
    {
        public Direction Direction = Direction.NorthNorthWest;
    }
}
