using Dalamud.Game.ClientState.Conditions;
using ECommons.EzHookManager;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Splatoon.SplatoonScripting;

namespace Splatoon.Memory;

/// <summary>
/// Links EffectResult packets (the moment the server applies an action to a target: HP, shield, statuses)
/// back to the ActionEffect they belong to, and raises <see cref="SplatoonScript.OnActionEffectApplied"/>.
/// <para>
/// ActionEffect sets are kept by their global sequence. Each EffectResult entry carries the same sequence and the index of the target
/// in the set; this is also how the game itself finds the queued effect. Effects are copied when the ActionEffect is received,
/// because <see cref="TargetEffect"/> points into the packet.
/// </para>
/// </summary>
internal unsafe class EffectResultProcessor : IDisposable
{
    // EffectResult arrives up to a couple of seconds after the ActionEffect; anything older is not going to be linked anymore.
    private const long KeepMs = 10000;

    // Packet: count byte, then entries every Stride bytes starting at +4 (entry offsets below are from packet + Stride * i).
    // +4 global sequence, +8 target ID of the ActionEffect, +0xC HP of the recipient.
    // EffectResult (stride 88): +0x16 target index, +0x18 shield %. EffectResultBasic (stride 16): +0x10 target index.
    private const int ResultStride = 88;
    private const int ResultIndexOffset = 0x16;
    private const int ResultShieldOffset = 0x18;
    private const int BasicStride = 16;
    private const int BasicIndexOffset = 0x10;

    private delegate nint EffectResultDelegate(uint recipientId, byte* packet, byte isReplay);
    private delegate void EffectResultBasicDelegate(uint recipientId, byte* packet, byte isReplay);

    private EzHook<EffectResultDelegate> EffectResultHook;
    private EzHook<EffectResultBasicDelegate> EffectResultBasicHook;

    private const uint InvalidId = 0xE0000000;

    // IDs are taken as numbers when the ActionEffect arrives: the source object may be gone by the time the result lands
    private sealed record Stored(ActionEffectSet Set, uint SourceId, uint[] TargetIds, EffectEntry[][] Effects, long Time);
    private readonly Dictionary<uint, Stored> Sets = [];
    private readonly Queue<(uint Sequence, long Time)> Order = new();

    private readonly record struct Pending(Stored? Stored, uint Sequence, uint EntryTargetId, int TargetIndex, uint HpBefore, uint HpAfter, int ShieldBefore, int ShieldAfter);
    private readonly List<Pending> PendingResults = [];

    internal EffectResultProcessor()
    {
        try
        {
            EffectResultHook = new("48 8B C4 44 88 40 18 89 48 08 53 55 57 48 81 EC ?? ?? ?? ?? 83 3D ?? ?? ?? ?? 03", EffectResultDetour);
            EffectResultBasicHook = new("40 53 41 54 41 55 48 83 EC 40 83 3D ?? ?? ?? ?? 03 45 0F B6 E0 48 8B DA 44 8B E9", EffectResultBasicDetour);
            ActionEffect.ActionEffectEvent += OnActionEffect;
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Could not create EffectResult hooks.");
            ex.Log();
        }
    }

    public void Dispose()
    {
        ActionEffect.ActionEffectEvent -= OnActionEffect;
        EffectResultHook?.Disable();
        EffectResultBasicHook?.Disable();
        EffectResultHook = null;
        EffectResultBasicHook = null;
        Clear();
        PendingResults.Clear();
    }

    /// <summary>
    /// Forget all stored ActionEffects. Sequences keep increasing on the server, so stale entries can't be confused with new ones;
    /// this only frees memory, e.g. on zone change.
    /// </summary>
    internal void Clear()
    {
        Sets.Clear();
        Order.Clear();
    }

    private void OnActionEffect(ActionEffectSet set)
    {
        try
        {
            var now = Environment.TickCount64;
            while(Order.TryPeek(out var oldest) && now - oldest.Time > KeepMs)
            {
                Order.Dequeue();
                if(Sets.TryGetValue(oldest.Sequence, out var s) && s.Time == oldest.Time) Sets.Remove(oldest.Sequence);
            }
            var targets = set.TargetEffects;
            if(targets.Length == 0) return;
            var effects = new EffectEntry[targets.Length][];
            var targetIds = new uint[targets.Length];
            for(var i = 0; i < targets.Length; i++)
            {
                var copy = new EffectEntry[8];
                for(var j = 0; j < copy.Length; j++) copy[j] = targets[i][j];
                effects[i] = copy;
                targetIds[i] = (uint)targets[i].TargetID;
            }
            var sourceId = set.SourceCharacter?.EntityId ?? (uint)(set.Source?.GameObjectId ?? InvalidId);
            var sequence = set.Header.GlobalEffectCounter;
            Sets[sequence] = new(set, sourceId, targetIds, effects, now);
            Order.Enqueue((sequence, now));
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private nint EffectResultDetour(uint recipientId, byte* packet, byte isReplay)
    {
        var any = Collect(recipientId, packet, isReplay, ResultStride, ResultIndexOffset, ResultShieldOffset);
        var ret = EffectResultHook.Original(recipientId, packet, isReplay);
        if(any) Raise(recipientId, false);
        return ret;
    }

    private void EffectResultBasicDetour(uint recipientId, byte* packet, byte isReplay)
    {
        var any = Collect(recipientId, packet, isReplay, BasicStride, BasicIndexOffset, -1);
        EffectResultBasicHook.Original(recipientId, packet, isReplay);
        if(any) Raise(recipientId, true);
    }

    /// <summary>
    /// Reads the entries before the game applies them, so the recipient's current HP and shield are the values before.
    /// Every entry is reported; entries that can't be linked to a stored ActionEffect are reported without one.
    /// </summary>
    private bool Collect(uint recipientId, byte* packet, byte isReplay, int stride, int indexOffset, int shieldOffset)
    {
        PendingResults.Clear();
        if(packet == null) return false;
        try
        {
            // During duty recorder playback the game applies the recorded packets (isReplay = 1) and ignores live ones
            if((isReplay != 0) != Svc.Condition[ConditionFlag.DutyRecorderPlayback]) return false;
            var recipient = (Character*)CharacterManager.Instance()->LookupBattleCharaByEntityId(recipientId);
            uint hpBefore = recipient == null ? 0 : recipient->CharacterData.Health;
            int shieldBefore = recipient == null ? -1 : recipient->CharacterData.ShieldValue;
            int count = packet[0];
            for(var i = 0; i < count; i++)
            {
                var entry = packet + stride * i;
                var sequence = *(uint*)(entry + 4);
                var entryTargetId = *(uint*)(entry + 8);
                int targetIndex = entry[indexOffset];
                Stored stored = null;
                if(sequence != 0 && Sets.TryGetValue(sequence, out var s) && targetIndex < s.TargetIds.Length && s.TargetIds[targetIndex] == entryTargetId)
                {
                    stored = s;
                }
                PendingResults.Add(new(stored, sequence, entryTargetId, targetIndex, hpBefore, *(uint*)(entry + 0xC), shieldBefore, shieldOffset < 0 ? -1 : entry[shieldOffset]));
            }
        }
        catch(Exception e)
        {
            e.Log();
            PendingResults.Clear();
        }
        return PendingResults.Count > 0;
    }

    private void Raise(uint recipientId, bool basic)
    {
        var now = Environment.TickCount64;
        // Copy first: a script may cause another EffectResult to be processed
        var results = PendingResults.ToArray();
        PendingResults.Clear();
        foreach(var r in results)
        {
            var stored = r.Stored;
            if(stored != null)
            {
                var targetId = stored.TargetIds[r.TargetIndex];
                var isSource = recipientId != targetId && recipientId == stored.SourceId;
                ScriptingProcessor.OnActionEffectApplied(stored.Set, r.TargetIndex,
                    new(true, r.Sequence, stored.SourceId, targetId, recipientId, isSource, basic, stored.Effects[r.TargetIndex],
                        r.HpBefore, r.HpAfter, r.ShieldBefore, r.ShieldAfter, now - stored.Time));
            }
            else
            {
                // Without the ActionEffect the source is unknown. Sequence 0 is not an action at all (damage over time, fall damage, HP sync):
                // the target is the recipient. Otherwise the entry still names the target of the action.
                var targetId = r.Sequence != 0 && r.EntryTargetId is not (0 or InvalidId) ? r.EntryTargetId : recipientId;
                ScriptingProcessor.OnActionEffectApplied(default, r.TargetIndex,
                    new(false, r.Sequence, InvalidId, targetId, recipientId, false, basic, [],
                        r.HpBefore, r.HpAfter, r.ShieldBefore, r.ShieldAfter, -1));
            }
        }
    }
}
