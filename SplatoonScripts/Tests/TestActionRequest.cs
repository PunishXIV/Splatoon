using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.IPC;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Tests;

public class TestActionRequest : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "-");
    public override HashSet<uint>? ValidTerritories { get; } = [];


    public class Sge
    {
        public const uint Surecast = 7559;
        public const uint Eukrasia = 24290;
        public const uint Panhaima = 24311;
    }

    public override void OnReset()
    {
        ECommonsIPC.WrathComboIPC.ResetAllBlacklist();
        ECommonsIPC.WrathComboIPC.ResetAllRequests();
        EzThrottler.Reset($"UseAction1{InternalData.FullName}");
        EzThrottler.Reset($"UseAction2{InternalData.FullName}");
    }

    bool IsTime(float sec) => Controller.CombatSeconds.InRange(sec, sec + 5);
    bool IsTime(float min, float sec) => Controller.CombatSeconds.InRange(min * 60 + sec, min * 60 + sec + 5);

    public override void OnEnable()
    {
        DuoLog.Warning("Disable TestActionRequest once you finish testing");
    }

    void Request(uint action)
    {
        if(EzThrottler.Throttle($"UseAction1{InternalData.FullName}{action}", 6000))
        {
            ECommonsIPC.WrathComboIPC.RequestActionUse(ActionType.Action, action, 5000, (Action.Get(action).CooldownGroup == 58 || Action.Get(action).AdditionalCooldownGroup == 58) ? null : false);
        }
    }

    public override void OnUpdate()
    {
        if(Player.Job == Job.SGE)
        {
            if(IsTime(10)) Request( Sge.Eukrasia);
            if(IsTime(20)) Request( Sge.Surecast);
            if(IsTime(30)) Request( Sge.Panhaima);
        }
    }
}
