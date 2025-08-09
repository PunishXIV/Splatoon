using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Tests;
internal class RedmoonTest2 :SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(1, "Redmoon");

    string _id = string.Empty;
    Action? _action = default;

    public override void OnSettingsDraw()
    {
        ImGui.InputText("castID", ref _id, 10);
        ImGui.SameLine();
        if (ImGui.Button("Show"))
        {
            if (uint.TryParse(_id, out uint id2))
            {
                _action = Svc.Data.GetExcelSheet<Action>()!.GetRowOrDefault(id2);
                if (_action == null)
                {
                    ImGui.Text("Parse Fail");
                }
                else
                {
                    ImGui.Text($"RowId: {_action.Value.RowId}");
                    ImGui.Text($"Name: {_action.Value.Name}");
                    ImGui.Text($"UnlockLink: {_action.Value.UnlockLink}");
                    ImGui.Text($"Icon: {_action.Value.Icon}");
                    ImGui.Text($"VFX: {_action.Value.VFX}");
                    ImGui.Text($"ActionTimelineHit: {_action.Value.ActionTimelineHit}");
                    ImGui.Text($"PrimaryCostValue: {_action.Value.PrimaryCostValue}");
                    ImGui.Text($"SecondaryCostValue: {_action.Value.SecondaryCostValue}");
                    ImGui.Text($"ActionCombo: {_action.Value.ActionCombo}");
                    ImGui.Text($"Cast100ms: {_action.Value.Cast100ms}");
                    ImGui.Text($"Recast100ms: {_action.Value.Recast100ms}");
                    ImGui.Text($"ActionProcStatus: {_action.Value.ActionProcStatus}");
                    ImGui.Text($"StatusGainSelf: {_action.Value.StatusGainSelf}");
                    ImGui.Text($"Omen: {_action.Value.Omen}");
                    ImGui.Text($"OmenAlt: {_action.Value.OmenAlt}");
                }
            }
            else
            {
                ImGui.Text("Invalid ID");
            }
        }
        else
        {
            if (!_action.HasValue) return;
            ImGui.Text($"RowId: {_action.Value.RowId}");
            ImGui.Text($"Name: {_action.Value.Name}");
            ImGui.Text($"UnlockLink: {_action.Value.UnlockLink}");
            ImGui.Text($"Icon: {_action.Value.Icon}");
            ImGui.Text($"VFX: {_action.Value.VFX}");
            ImGui.Text($"ActionTimelineHit: {_action.Value.ActionTimelineHit}");
            ImGui.Text($"PrimaryCostValue: {_action.Value.PrimaryCostValue}");
            ImGui.Text($"SecondaryCostValue: {_action.Value.SecondaryCostValue}");
            ImGui.Text($"ActionCombo: {_action.Value.ActionCombo}");
            ImGui.Text($"Cast100ms: {_action.Value.Cast100ms}");
            ImGui.Text($"Recast100ms: {_action.Value.Recast100ms}");
            ImGui.Text($"ActionProcStatus: {_action.Value.ActionProcStatus}");
            ImGui.Text($"StatusGainSelf: {_action.Value.StatusGainSelf}");
            ImGui.Text($"Omen: {_action.Value.Omen}");
            ImGui.Text($"OmenAlt: {_action.Value.OmenAlt}");
        }
    }
}
