using ECommons.EzHookManager;
using ECommons.EzIpcManager;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Tests;

public class CustomOpenerTest : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    /// <summary>
    /// ActionType,<br />
    /// action ID<br />
    /// time in miliseconds for how long to blacklist
    /// </summary>
    [EzIPC] public Action<ActionType, uint, int> RequestBlacklist;
    /// <summary>
    /// ActionType,<br />
    /// action ID
    /// </summary>
    [EzIPC] public Action<ActionType, uint> ResetBlacklist;
    [EzIPC] public Action ResetAllBlacklist;
    /// <summary>
    /// ActionType, <br />
    /// action ID, <br />
    /// remaining cooldown
    /// </summary>
    [EzIPC] public Func<ActionType, uint, float> GetArtificialCooldown;
    /// <summary>
    /// ActionType, <br />
    /// action ID, <br />
    /// time in miliseconds for how long request is valid, <br />
    /// whether to use action as gcd, where true is use only at GCD time, false use only at OGCD time (no clipping), and null - use asap (with clipping)
    /// </summary>
    [EzIPC] public Action<ActionType, uint, int, bool?> RequestActionUse;
    /// <summary>
    /// ActionType,<br />
    /// action ID
    /// </summary>
    [EzIPC] public Action<ActionType, uint> ResetRequest;
    [EzIPC] public Action ResetAllRequests;

    List<uint> CurrentGcdSequence = [];
    List<uint> CurrentOgcdSequence = [];

    private delegate void SendActionDelegate(ulong targetObjectId, ActionType actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9);
    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9 41 0F B7 D9", false)]
    private EzHook<SendActionDelegate> SendActionHook;
    private unsafe void SendActionDetour(ulong targetObjectId, ActionType actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
    {
        if(CurrentGcdSequence.Count > 0 && actionType == ActionType.Action && actionId == CurrentGcdSequence[0])
        {
            CurrentGcdSequence.RemoveAt(0);
        }

        if(CurrentOgcdSequence.Count > 0 && actionType == ActionType.Action && actionId == CurrentOgcdSequence[0])
        {
            CurrentOgcdSequence.RemoveAt(0);
        }
    }

    public override void OnSetup()
    {
        EzIPC.Init(this, "WrathCombo.ActionRequest");
        EzSignatureHelper.Initialize(this);
    }

    public override void OnEnable()
    {
        this.SendActionHook?.Enable();
    }
    public override void OnDisable()
    {
        this.SendActionHook?.Disable();
    }

    public override void OnUpdate()
    {
        if(this.CurrentGcdSequence.Count > 0 || this.CurrentOgcdSequence.Count > 0)
        {
            this.ResetAllRequests(); //first reset all requests so we have clean environment to work with
            if(this.CurrentGcdSequence.Count > 0)
            {
                this.RequestActionUse(ActionType.Action, this.CurrentGcdSequence[0], 100, true);
            }
            if(this.CurrentOgcdSequence.Count > 0)
            {
                this.RequestActionUse(ActionType.Action, this.CurrentOgcdSequence[0], 100, true);
            }
        }
    }

    void RequestSequence(params int[] actionId)
    {
        this.CurrentGcdSequence.Clear();
        this.CurrentOgcdSequence.Clear();
        foreach(var x in actionId)
        {
            if(x > 0)
            {
                this.CurrentGcdSequence.Add((uint)x);
            }
            else
            {
                this.CurrentOgcdSequence.Add((uint)-x);
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.Button("Try mch opener via sequence request"))
        {

        }
    }
}
