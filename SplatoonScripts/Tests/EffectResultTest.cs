using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Tests;
public unsafe sealed class EffectResultTest : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = null;

    delegate void HandleEffectResultBasicPacket(uint target, nint packet, byte isReplay);
    [EzHook("40 53 41 54 41 55 48 83 EC 40", false)]
    EzHook<HandleEffectResultBasicPacket> HandleEffectResultBasicPacketHook;

    void HandleEffectResultBasicPacketDetour(uint target, nint packet, byte isReplay)
    {
        HandleEffectResultBasicPacketHook.Original(target, packet, isReplay);
        try
        {
            var num = *(byte*)packet;
            for(int i = 0; i < num; i++)
            {
                var entry = *(EffectResultBasicEntry*)(packet + 16 * i);
                PluginLog.Information($"EffectResultBasicEntry: tar={target} ({target.GetObject()}), obj={entry.ObjectID} ({entry.ObjectID.GetObject()}), HP={entry.CurrentHP}, seq={entry.RelatedActionSequence}, idx={entry.RelatedTargetIndex}, Action={ExcelActionHelper.GetActionName(entry.EffectEntry(target)?.ActionId ?? 0, true)} / {entry.EffectEntry(target)?.ActionType}");
            }
        } 
        catch(Exception ex)
        {
            ex.Log();
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct EffectResultBasicEntry
    {
        [FieldOffset(4)]
        public uint RelatedActionSequence;

        [FieldOffset(8)]
        public uint ObjectID;

        [FieldOffset(12)]
        public uint CurrentHP;

        [FieldOffset(16)]
        public byte RelatedTargetIndex;

        public ActionEffectHandler.EffectEntry? EffectEntry(uint targetId) => GetEntryForCharacter(targetId, RelatedActionSequence, this.RelatedTargetIndex);
    }

    public static ActionEffectHandler.EffectEntry? GetEntryForCharacter(uint targetId, uint seq, byte index)
    {
        var target = (Character*)(targetId.GetObject()?.Address ?? 0);
        if(target == null)
        {
            return null;
        }
        var ret = (ActionEffectHandler.EffectEntry*)FindIncomingEntry(target->GetActionEffectHandler(), seq, index);
        //PluginLog.Information($"ret: {(ret == null ? "null" : (nint)ret)}");
        return ret == null?null:*ret;
    }


    delegate nint HandleEffectResultPacket(uint target, nint packet, byte isReplay);
    [EzHook("48 8B C4 44 88 40 18 89 48 08", false)]
    EzHook<HandleEffectResultPacket> HandleEffectResultPacketHook;

    nint HandleEffectResultPacketDetour(uint target, nint packet, byte isReplay)
    {
        var ret = HandleEffectResultPacketHook.Original(target, packet, isReplay);
        try
        {
            var num = *(byte*)packet;
            for(int i = 0; i < num; i++)
            {
                var entry = *(EffectResultEntry*)(packet + 88 * i);
                var entry2 = entry;
                PluginLog.Information($"""
                    EffectResultEntry:
                      target={target} / {target.GetObject()}
                      ObjectID={entry.ObjectID} / {entry.ObjectID.GetObject()}
                      CurrentHP={entry.CurrentHP}
                      MaxHP={entry.MaxHP}
                      CurrentMP={entry.CurrentMP}
                      Job={entry.Job}
                      RelatedActionSequence={entry.RelatedActionSequence}
                      RelatedTargetIndex={entry.RelatedTargetIndex}
                      ShieldValue={entry.ShieldValue}
                      Unk1={entry.Unk1}
                      EffectCount={entry.EffectCount}
                      Action={ExcelActionHelper.GetActionName(entry.EffectEntry(target)?.ActionId ?? 0, true)} / {entry.EffectEntry(target)?.ActionType}
                      {new ReadOnlySpan<EffectResultEntry.IncomingStatus>(entry.IncomingStatusList, entry.EffectCount).ToArray().Select(x =>
                    $"""
                      Effect:
                        StatusID={x.StatusID} / {Svc.Data.GetExcelSheet<Status>().GetRowOrDefault(x.StatusID)?.Name}
                        Param={x.Param}
                        Duration={x.Duration}
                        SourceID={x.SourceID} / {x.SourceID.GetObject()}
                    """).Print("\n")}
                    """);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
        return ret;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public unsafe struct EffectResultEntry
    {
        [FieldOffset(4)]
        public uint RelatedActionSequence;

        [FieldOffset(8)]
        public uint ObjectID;

        [FieldOffset(12)]
        public uint CurrentHP;

        [FieldOffset(16)]
        public uint MaxHP;

        [FieldOffset(20)]
        public ushort CurrentMP;

        [FieldOffset(22)]
        public byte RelatedTargetIndex;

        [FieldOffset(23)]
        public Job Job;

        [FieldOffset(24)]
        public byte ShieldValue;

        [FieldOffset(25)]
        public byte EffectCount;

        [FieldOffset(26)]
        public ushort Unk1;

        [FieldOffset(28)]
        public fixed byte IncomingStatusList[4 * 16];

        public ActionEffectHandler.EffectEntry? EffectEntry(uint targetId) => GetEntryForCharacter(targetId, RelatedActionSequence, this.RelatedTargetIndex);

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
        public struct IncomingStatus
        {
            [FieldOffset(2)]
            public ushort StatusID;

            [FieldOffset(4)]
            public ushort Param;

            [FieldOffset(8)]
            public float Duration;

            [FieldOffset(12)]
            public uint SourceID; 
        }
    }

    public delegate nint FindIncomingEntryDelegate(ActionEffectHandler* handler, uint seq, byte index);
    public static FindIncomingEntryDelegate FindIncomingEntry = EzDelegate.Get<FindIncomingEntryDelegate>("85 D2 74 26 45 33 D2");

    delegate nint ActionEffectHandler_ApplySelfEffects(nint a1, nint a2, nint a3, nint a4, nint a5);
    [EzHook("48 8B C4 55 41 54 41 56 41 57")]
    EzHook<ActionEffectHandler_ApplySelfEffects> ActionEffectHandler_ApplySelfEffectsHook;



    public override void OnSetup()
    {
        EzSignatureHelper.Initialize(this);
    }

    public override void OnEnable()
    {
        HandleEffectResultBasicPacketHook?.Enable();
        HandleEffectResultPacketHook?.Enable();
    }

    public override void OnDisable()
    {
        HandleEffectResultBasicPacketHook?.Disable();
        HandleEffectResultPacketHook?.Disable();
    }

    public override void OnSettingsDraw()
    {
        if(Svc.Targets.FocusTarget is ICharacter chr)
        {
            var c = chr.Struct()->GetActionEffectHandler()->IncomingEffects;
            foreach(var x in c)
            {
                ImGuiEx.Text($"{x.GlobalSequence} / {x.ActionId} / {x.ActionType}");
            }
            ImGuiEx.TextCopy($"{(nint)chr.Struct()->GetActionEffectHandler()->IncomingEffects.GetPointer(0):X}");
        }
    }
}