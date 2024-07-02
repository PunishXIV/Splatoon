using Dalamud.Hooking;
using ECommons.Logging;
using Dalamud.Utility.Signatures;
using ECommons;
using ECommons.DalamudServices.Legacy;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using FFXIVClientStructs.FFXIV.Client.Game;
using ECommons.ImGuiMethods;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;

namespace SplatoonScriptsOfficial.Duties.Endwalker;

public unsafe class Aloalo_Bombs : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new() { 1179, 1180 };

    public override Metadata? Metadata => new(2, "NightmareXIV");

    const uint BombNameID = 0x30E8;

    delegate nint ActionTimelineManager_unk(nint a1, uint a2, int a3, nint a4);
    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 8B FA 48 03 F9", DetourName =nameof(ActionTimelineManager_unkDetour))]
    Hook<ActionTimelineManager_unk> ActionTimelineManager_unkHook;

    List<HashSet<uint>> BombSets = new();
    uint Unsafe = 0;

    public override void OnSetup()
    {
        for (int i = 0; i < 3; i++)
        {
            Controller.RegisterElementFromCode($"Bomb{i}", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":12.0,\"color\":2013266175,\"thicc\":4.0,\"refActorObjectID\":21845,\"refActorComparisonType\":2,\"Filled\":true}");
        }
    }

    public override void OnEnable()
    {
        SignatureHelper.Initialise(this);
        ActionTimelineManager_unkHook?.Enable();
    }

    public override void OnDisable()
    {
        ActionTimelineManager_unkHook?.Dispose();
    }

    public override void OnUpdate()
    {
        if (Unsafe != 0)
        {
            var unsafeArray = BombSets.FirstOrDefault(z => z.Contains(Unsafe))?.ToArray();
            if (unsafeArray != null)
            {
                for (int i = 0; i < unsafeArray.Length; i++)
                {
                    if (Controller.TryGetElementByName($"Bomb{i}", out var e))
                    {
                        e.Enabled = true;
                        e.refActorObjectID = unsafeArray[i];
                    }
                }
            }
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(source.GetObject() is ICharacter c && c.NameId == BombNameID)
        {
            if(BombSets.TryGetFirst(x => x.Contains(source) || x.Contains(target), out var set))
            {
                set.Add(source);
                set.Add(target);
            }
            else
            {
                BombSets.Add(new() { source, target });
            }
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            Reset();
        }
    }

    void Reset()
    {
        BombSets.Clear();
        Unsafe = 0;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        {
            if (set.Target is ICharacter c && c.NameId == BombNameID && set.Action?.RowId.EqualsAny(35165u, 35194u) == true)
            {
                Reset();
            }
        }
        {
            //if (set.Target is Character c && c.NameId == BombNameID) PluginLog.Information($"{set.Action?.RowId}");
        }
    }

    public override void OnSettingsDraw()
    {
        foreach(var x in BombSets)
        {
            ImGuiEx.Text($"{x.Select(z => $"{z:X8}").Print()}");
        }
    }

    nint ActionTimelineManager_unkDetour(nint a1, uint a2, int a3, nint a4)
    {
        try
        {
            /*var x = (ActionTimelineManager*)a1;
            if(x->Parent != null && x->Parent->NameID == BombNameID && a3 == 1)
            {
                Unsafe = x->Parent->IGameObject.EntityId;
            }
            PluginLog.Information($"{x->Parent->IGameObject.EntityId:X} {a1:X16} - {a2}: {a3}, {a4}");*/
        }
        catch(Exception e)
        {
            e.Log();
        }
        var ret = ActionTimelineManager_unkHook.Original(a1, a2, a3, a4);
        return ret;
    }
}
