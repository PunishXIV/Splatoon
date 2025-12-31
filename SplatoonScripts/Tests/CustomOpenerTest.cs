using ECommons;
using ECommons.EzHookManager;
using ECommons.EzIpcManager;
using ECommons.ImGuiMethods;
using ECommons.IPC;
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


    private delegate void SendActionDelegate(ulong targetObjectId, ActionType actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9);
    [EzHook("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9 41 0F B7 D9", false)]
    private EzHook<SendActionDelegate> SendActionHook;

    List<OpenerAction> OpenerActions = [];

    private unsafe void SendActionDetour(ulong targetObjectId, ActionType actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
    {
        try
        {
            if(OpenerActions.Count > 0 && actionType == ActionType.Action)
            {
                var a = OpenerActions[0];
                if(a.Gcd == actionId)
                {
                    a.Sequence++;
                    if(a.Ogcd1 == 0) OpenerActions.RemoveAt(0);
                }
                else if(a.Ogcd1 == actionId)
                {
                    a.Sequence++;
                    if(a.Ogcd2 == 0) OpenerActions.RemoveAt(0);
                }
                else if(a.Ogcd2 == actionId)
                {
                    OpenerActions.RemoveAt(0);
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        SendActionHook.Original(targetObjectId, actionType, actionId, sequence, a5, a6, a7, a8, a9);
    }

    public override void OnSetup()
    {
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
        if(OpenerActions.Count > 0)
        {
            var o = OpenerActions[0];
            if(o.Sequence == 0)
            {
                ECommonsIPC.WrathCombo.ResetAllRequests();
                ECommonsIPC.WrathCombo.RequestActionUse(ActionType.Action, o.Gcd, 100, true);
            }
            else
            {
                if(o.Sequence == 1 && o.Ogcd1 != 0)
                {
                    ECommonsIPC.WrathCombo.ResetAllRequests();
                    ECommonsIPC.WrathCombo.RequestActionUse(ActionType.Action, o.Ogcd1, 100, false);
                }
                else if(o.Sequence == 2 && o.Ogcd2 != 0)
                {
                    ECommonsIPC.WrathCombo.ResetAllRequests();
                    ECommonsIPC.WrathCombo.RequestActionUse(ActionType.Action, o.Ogcd2, 100, false);
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.Button("Try mch opener via sequence request"))
        {
            this.OpenerActions = [
                new(7411, 36979, 36980),
                new(7412, 36979, 36980),
                new(7413, 36979, 36980),
                new(16498, 2876, 7414),
                new(25788, 0, 0),
                ];
        }
    }

    public class OpenerAction
    {
        public int Sequence = 0;
        public uint Gcd;
        public uint Ogcd1;
        public uint Ogcd2;

        public OpenerAction(uint gcd, uint ogcd1, uint ogcd2)
        {
            Gcd = gcd;
            Ogcd1 = ogcd1;
            Ogcd2 = ogcd2;
        }
    }
}
