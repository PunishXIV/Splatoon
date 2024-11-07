using ECommons;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.ImGuiMethods;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;

namespace SplatoonScriptsOfficial.Duties.Endwalker;

public unsafe class Aloalo_Bombs : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new() { 1179, 1180 };

    public override Metadata? Metadata => new(4, "NightmareXIV");

    const uint BombNameID = 0x30E8;

    uint Unsafe
    {
        get
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>().Where(s => s.NameId == BombNameID))
            {
                if(x.Struct()->Timeline.TimelineSequencer.TimelineIds.Contains((ushort)145))
                {
                    return x.EntityId;
                }
            }
            return 0;
        }
    }

    public override void OnSetup()
    {
        for (int i = 0; i < 3; i++)
        {
            Controller.RegisterElementFromCode($"Bomb{i}", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":12.0,\"color\":2013266175,\"thicc\":4.0,\"refActorObjectID\":21845,\"refActorComparisonType\":2,\"Filled\":true}");
        }
    }

    public override void OnEnable()
    {
        //SignatureHelper.Initialise(this);
    }

    public override void OnDisable()
    {
    }

    public override void OnUpdate()
    {
        if (Unsafe != 0)
        {
            var unsafeArray = GetBombSets().FirstOrDefault(z => z.Any(e => e.EntityId == Unsafe))?.ToArray();
            if (unsafeArray != null)
            {
                for (int i = 0; i < unsafeArray.Length; i++)
                {
                    if (Controller.TryGetElementByName($"Bomb{i}", out var e))
                    {
                        e.Enabled = true;
                        e.refActorObjectID = unsafeArray[i].EntityId;
                    }
                }
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
        foreach(var x in Svc.Objects.OfType<IBattleNpc>().Where(s => s.NameId == BombNameID))
        {
            ImGuiEx.Text($"{x} - {x.Struct()->Vfx.Tethers.ToArray().Select(s => $"{s.Id}/{s.TargetId.ObjectId.GetObject()}/{s.Progress}").Print()} \n   {x.Struct()->Timeline.TimelineSequencer.TimelineIds.ToArray().Print("\n")}");
        }
        ImGuiEx.Text($"Bomb sets:\n{GetBombSets().Select(x => x.Select(s => s.ToString()).Print()).Print("\n")}");
    }

    List<List<IBattleNpc>> GetBombSets()
    {
        var ret = new List<List<IBattleNpc>>();
        foreach(var x in Svc.Objects.OfType<IBattleNpc>().Where(s => s.NameId == BombNameID))
        {
            var set = GetBombSetFor(x);
            if(set.Count == 3)
            {
                if(!ret.Any(s => s.Select(a => a.EntityId).SequenceEqual(set.Select(a => a.EntityId)))) ret.Add(set);
            }
        }
        return ret;
    }

    List<IBattleNpc> GetBombSetFor(IBattleNpc? bnpc)
    {
        var ret = new List<IBattleNpc>() { bnpc! };
        for(int i = 0; i < 100; i++)
        {
            var newBnpc = bnpc!.Struct()->Vfx.Tethers.ToArray().SafeSelect(0).TargetId.ObjectId.GetObject() as IBattleNpc;
            if(newBnpc == null || newBnpc.AddressEquals(ret[0]))
            {
                break;
            }
            else
            {
                ret.Add(newBnpc);
                bnpc = newBnpc;
            }
        }
        return [.. ret.OrderBy(x => x.EntityId)];
    }
}
