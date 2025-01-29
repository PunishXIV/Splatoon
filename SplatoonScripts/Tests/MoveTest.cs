using ECommons.Automation;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
internal unsafe class MoveTest :SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new();
    public override void OnSettingsDraw()
    {
        if (ImGui.Button($"Move"))
        {
            Chat.Instance.ExecuteCommand("/vnav moveto -1.564 0 -2.587");
        }

        if (ImGui.Button($"stop"))
        {
            Chat.Instance.ExecuteCommand("/vnav stop");
        }
    }
}
