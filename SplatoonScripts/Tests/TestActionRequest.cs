using ECommons;
using ECommons.DalamudServices;
using ECommons.EzIpcManager;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;

public class TestActionRequest : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [];

    [EzIPC] public Action<ActionType, uint, int> RequestBlacklist;
    [EzIPC] public Action<ActionType, uint> ResetBlacklist;
    [EzIPC] public Action ResetAllBlacklist;
    [EzIPC] public Func<ActionType, uint, float> GetArtificialCooldown;
    [EzIPC] public Action<ActionType, uint, int, bool?> RequestActionUse;
    [EzIPC] public Action<ActionType, uint> ResetRequest;
    [EzIPC] public Action ResetAllRequests; 

    public class Mch
    {
        public static readonly uint Tactician = 16889;
        public static readonly uint Dismantle = 2887;
        public static readonly uint Wildfire = 2878;
        public static readonly uint Robot = 16501;
        public static readonly uint Stabilizer = 7414;

        public static readonly uint[] MchBursts = [Wildfire, Robot, Stabilizer];
    }

    public override void OnSetup()
    {
        EzIPC.Init(this, "WrathCombo.ActionRequest");
    }

    public override void OnReset()
    {
        this.ResetAllBlacklist();
        this.ResetAllRequests();
    }

    public override void OnUpdate()
    {
        //of course don't check for name in real script
        if(Svc.Targets.Target?.Name.ToString() == "Striking Dummy")
        {
            if((
                Controller.CombatSeconds.InRange(30, 35)
                || Controller.CombatSeconds.InRange(140, 145)
                )
                && EzThrottler.Throttle("UseTactician", 10000))
            {
                this.RequestActionUse(ActionType.Action, Mch.Tactician, 5000, false);
            }

            if((
                Controller.CombatSeconds.InRange(10, 15)
                || Controller.CombatSeconds.InRange(142, 147)
                )
                && EzThrottler.Throttle("UseDismantle", 10000))
            {
                this.RequestActionUse(ActionType.Action, Mch.Dismantle, 5000, false);
            }

            Mch.MchBursts.Each(x => this.ResetBlacklist(ActionType.Action, x));
            if(
                Controller.CombatSeconds.InRange(100, 150)
                || Controller.CombatSeconds.InRange(170, 200)
                )
            {
                Mch.MchBursts.Each(x => this.RequestBlacklist(ActionType.Action, x, 10000));
            }
        }
    }
}
