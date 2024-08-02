using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.Throttlers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class R4S_Chain_Lightning : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1232];
    public override Metadata? Metadata => new(1, "NightmareXIV");
    uint TowerID = 13061;
    List<List<uint>> Towers = [];
    Layout VoidZone = null!;
    long ResetAt = long.MaxValue;

    IEnumerable<IBattleChara> GetTowers() => Svc.Objects.OfType<IBattleNpc>().Where(x => x.NameId == TowerID);

    public override void OnSetup()
    {
        for(int i = 0; i < 6; i++)
        {
            Controller.RegisterElementFromCode($"Tower{i}", "{\"Name\":\"Tower\",\"type\":1,\"radius\":7.0,\"color\":3355508223,\"fillIntensity\":0.3,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}");
        }
        Controller.TryRegisterLayoutFromCode("~Lv2~{\"Name\":\"void zone lol\",\"Group\":\"\",\"ZoneLockH\":[1232],\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":110.0,\"refY\":150.0,\"offX\":110.0,\"offY\":180.0,\"radius\":0.0,\"color\":4278190335,\"fillIntensity\":0.345,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"thicc\":10.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]},{\"Name\":\"\",\"type\":2,\"refX\":90.0,\"refY\":150.0,\"offX\":90.0,\"offY\":180.0,\"radius\":0.0,\"color\":4278190335,\"fillIntensity\":0.345,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"thicc\":10.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorTetherConnectedWithPlayer\":[]}]}", out VoidZone!);
    }

    public override void OnUpdate()
    {
        if(Environment.TickCount64 > ResetAt) Reset();
        this.Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Towers.Count > 0)
        {
            VoidZone.Enabled = true;
            uint[] towersConcat = [.. Towers.SafeSelect(0), .. Towers.SafeSelect(1) ?? []];
            for(int i = 0; i < towersConcat.Length; i++)
            {
                if(towersConcat[i].GetObject() == null) Reset();
                if(Controller.TryGetElementByName($"Tower{i}", out var e))
                {
                    e.Enabled = true;
                    e.refActorObjectID = towersConcat[i];
                }
            }
        }
        else
        {
            VoidZone.Enabled = false;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            Reset();
            ResetAt = long.MaxValue;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/channeling/eff/chn_chainlightning_0t1.avfx")
        {
            if(EzThrottler.Throttle("ChainLightning.Recast", 500))
            {
                Reset();
                Towers.Add([]);
            }
            Towers.Last().Add(target);
            ResetAt = Environment.TickCount64 + 30 * 1000;
        }
        if(vfxPath == "vfx/channeling/eff/chn_chainlightning_1t1.avfx")
        {
            if(EzThrottler.Throttle("ChainLightning.Recast", 500))
            {
                Towers.Add([]);
            }
            Towers.Last().Add(target);
            ResetAt = Environment.TickCount64 + 30 * 1000;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        //PluginLog.Information($"Cast: {set.Action.RowId}");
        if(set.Action?.RowId.EqualsAny(38426u, 38427u) == true)
        {
            PluginLog.Information($"Cast detected");
            if(Towers.Count > 0 && EzThrottler.Throttle("ChainLightning.Remove", 500))
            {
                Towers.RemoveAt(0);
            }
        }
    }

    void Reset()
    {
        Towers.Clear();
    }
}
