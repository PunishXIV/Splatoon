using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Command;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;

public class P5_Caster_Limit_Break : SplatoonScript
{
    private const uint LimitBreak2Id = 204;
    public override HashSet<uint>? ValidTerritories => [];

    public Config C => Controller.GetConfig<Config>();

    public override void OnEnable()
    {
        Svc.Commands.AddHandler("/limitBreak", new CommandInfo(OnCastLimitBreak));
    }

    public override void OnDisable()
    {
        Svc.Commands.RemoveHandler("/limitBreak");
    }

    public void OnCastLimitBreak(string command, string arguments)
    {
        var result = UseLimitBreak();
        if (result)
            DuoLog.Debug($"Limit Break casted to {C.TargetPosition}");
        else
            DuoLog.Warning("Failed to cast Limit Break");
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Limit Break Settings");
        ImGui.InputFloat3("Target Position", ref C.TargetPosition);

        if (ImGuiEx.CollapsingHeader("Debug")) ImGui.Text($"My Position: {Player.Position}");
    }

    public unsafe bool UseLimitBreak()
    {
        var targetPosition = C.TargetPosition;
        var targetId = 0xE0000000;
        return ActionManager.Instance()->UseActionLocation(ActionType.Action, LimitBreak2Id, targetId, &targetPosition);
    }

    public class Config : IEzConfig
    {
        public Vector3 TargetPosition = new(0, 0, 0);
    }
}