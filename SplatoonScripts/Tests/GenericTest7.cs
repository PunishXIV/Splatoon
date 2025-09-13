using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using ECommons.EzHookManager;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;
public unsafe class GenericTest7 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; }

    public override Metadata? Metadata => new(5);
}
