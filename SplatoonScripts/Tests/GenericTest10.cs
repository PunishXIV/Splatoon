using ECommons;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.SimpleHookManager;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Network;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Text;
using static FFXIVClientStructs.FFXIV.Client.Game.ActionManager;

namespace SplatoonScriptsOfficial.Tests;
#pragma warning disable SimpleHook
public unsafe class GenericTest10 : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"{Control.Instance()->LocalPlayerEntityId:X}");
        ImGuiEx.Text($"{Player.Object?.EntityId:X}");
    }

    SimpleHookDisposalToken[] Tokens;

    public override void OnEnable()
    {
        Tokens = SimpleHook.Initialize(this);
    }

    public override void OnDisable()
    {
        Tokens?.Each(x => x.Dispose());
    }

    [SimpleHook(typeof(ActionManager.Addresses), nameof(ActionManager.Addresses.UseAction))]
    public unsafe bool UseAction(nint thisPtr, ActionType actionType, uint actionId, ulong targetId, uint extraParam, UseActionMode mode, uint comboRouteId, nint outOptAreaTargeted)
    {
        try
        {
            PluginLog.Information($"Action {actionType}/{actionId}, target {targetId:X16}, param {extraParam}, mode {mode}, comboRouteId {comboRouteId}, outOptAreaTargeted {(nint)outOptAreaTargeted:X16}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return default;
    }
}
