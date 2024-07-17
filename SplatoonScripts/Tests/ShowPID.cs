using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
public class ShowPID : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new();

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"PID: {Environment.ProcessId}");
    }
}
