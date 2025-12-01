using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Tests;

public class ArkveldMitigations : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1306];

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

    Dictionary<Job, uint> Mitigations90s = new()
    {
        [Job.MCH] = Mch.Tactician
    };

    Dictionary<Job, uint> Mitigations120s = new()
    {
        [Job.MCH] = Mch.Dismantle
    };

    public override void OnSetup()
    {
        EzIPC.Init(this, "WrathCombo.ActionRequest");
    }

    public override void OnReset()
    {
        this.ResetAllBlacklist();
        this.ResetAllRequests();
    }

    bool IsTime(float sec) => Controller.CombatSeconds.InRange(sec, sec + 5);
    bool IsTime(float min, float sec) => Controller.CombatSeconds.InRange(min*60+sec, min * 60 + sec + 5);

    public override void OnUpdate()
    {
        {
            if(Mitigations90s.TryGetValue(Player.Job, out var acId))
            {
                if((
                    IsTime(30)
                    || IsTime(2, 23)
                    || IsTime(3, 55)
                    || IsTime(5, 30)
                    || IsTime(7, 02)
                    || IsTime(8, 36)
                    )
                    && EzThrottler.Throttle($"UseTactician{InternalData.FullName}", 10000))
                {
                    this.RequestActionUse(ActionType.Action, acId, 5000, false);
                }
            }
        }
        {
            if(Mitigations120s.TryGetValue(Player.Job, out var acId))
            {
                if((
                    IsTime(49)
                    || IsTime(3, 32)
                    || IsTime(5, 43)
                    || IsTime(8, 06)
                    )
                    && EzThrottler.Throttle($"UseDismantle{InternalData.FullName}", 10000))
                {
                    this.RequestActionUse(ActionType.Action, acId, 5000, false);
                }
            }
        }
    }
}
