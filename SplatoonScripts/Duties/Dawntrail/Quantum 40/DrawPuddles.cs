using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Quantum_40;

public unsafe class DrawPuddles : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1311];

    delegate nint HandleActorCastPacket(uint a1, ActorCastPacket* a2);
    [EzHook("40 53 57 48 81 EC ?? ?? ?? ?? 48 8B FA 8B D1", false)]
    EzHook<HandleActorCastPacket> HandleActorCastPacketHook;

    int Num = 0;
    nint HandleActorCastPacketDetour(uint a1, ActorCastPacket* a2)
    {
        try
        {
            if(a1.TryGetObject(out var obj) && obj is IBattleNpc npc)
            {
                //PluginLog.Information($"{npc.NameId} / {a2->CastType} / {a2->Flags:X} / {a2->Idk} / {ExcelActionHelper.GetActionName(a2->ActionID, true)}");
                if(npc.NameId == 14037 && a2->ActionID == 44156)
                {
                    //PluginLog.Information($"{a2->GetActualX()} {a2->GetActualY()} {a2->GetActualZ()}");
                    if(Controller.TryGetElementByName($"{Num++ % 9}", out var l))
                    {
                        l.Enabled = true;
                        l.SetRefPosition(new System.Numerics.Vector3(a2->GetActualX(), a2->GetActualY(), a2->GetActualZ()));
                        this.Controller.Schedule(() => l.Enabled = false, 3000);
                    }
                }
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return HandleActorCastPacketHook.Original(a1, a2);
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ActorCastPacket
    {
        public ushort ActionID;
        public byte SkillType;
        public byte Unknown;
        public uint Unknown1; 
        public float CastTime;
        public uint TargetID;
        public float Rotation; 
        public uint Unknown2;
        public ushort PosX;
        public ushort PosY;
        public ushort PosZ;
        public ushort Unknown3;
        public float GetActualX() => ((PosX * 3.0518043f) * 0.0099999998f) - 1000.0f;
        public float GetActualY() => ((PosY * 3.0518043f) * 0.0099999998f) - 1000.0f;
        public float GetActualZ() => ((PosZ * 3.0518043f) * 0.0099999998f) - 1000.0f;
    }

    public override void OnSetup()
    {
        EzSignatureHelper.Initialize(this);
        for(int i = 0; i < 9; i++)
        {
            Controller.RegisterElementFromCode($"{i}", """{"Name":"","Enabled":false,"radius":6.0,"fillIntensity":0.5,"thicc":8.0,"refActorPlaceholder":["<d1>","<d2>","<h1>"],"refActorComparisonType":5}""");
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnEnable()
    {
        HandleActorCastPacketHook?.Enable();
    }

    public override void OnDisable()
    {
        HandleActorCastPacketHook?.Pause();
    }
}
