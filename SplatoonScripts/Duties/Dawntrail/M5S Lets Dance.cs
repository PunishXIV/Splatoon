using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class M5S_Lets_Dance : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1256, 1257];
    public override Metadata? Metadata => new(2, "NightmareXIV");

    public enum DanceDirection: uint
    {
        West = 7,
        East = 5,
        South = 31,
        North = 32,
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("North", "{\"Name\":\"\",\"type\":2,\"refX\":100.0,\"refY\":100.0,\"refZ\":9.536743E-07,\"offX\":100.0,\"offY\":80.0,\"radius\":20.0,\"color\":3355448319,\"fillIntensity\":0.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("South", "{\"Name\":\"\",\"type\":2,\"refX\":100.0,\"refY\":100.0,\"refZ\":9.536743E-07,\"offX\":100.0,\"offY\":120.0,\"radius\":20.0,\"color\":3355448319,\"fillIntensity\":0.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("West", "{\"Name\":\"\",\"type\":2,\"refX\":90.0,\"refY\":120.0,\"refZ\":9.536743E-07,\"offX\":90.0,\"offY\":80.0,\"radius\":10.0,\"color\":3355448319,\"fillIntensity\":0.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("East", "{\"Name\":\"\",\"type\":2,\"refX\":110.0,\"refY\":120.0,\"refZ\":9.536743E-07,\"offX\":110.0,\"offY\":80.0,\"radius\":10.0,\"color\":3355448319,\"fillIntensity\":0.6,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("NorthEarly", "{\"Name\":\"\",\"type\":2,\"refX\":100.0,\"refY\":100.0,\"refZ\":9.536743E-07,\"offX\":100.0,\"offY\":80.0,\"radius\":20.0,\"color\":3355508719,\"fillIntensity\":0.1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("SouthEarly", "{\"Name\":\"\",\"type\":2,\"refX\":100.0,\"refY\":100.0,\"refZ\":9.536743E-07,\"offX\":100.0,\"offY\":120.0,\"radius\":20.0,\"color\":3355508719,\"fillIntensity\":0.1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WestEarly", "{\"Name\":\"\",\"type\":2,\"refX\":90.0,\"refY\":120.0,\"refZ\":9.536743E-07,\"offX\":90.0,\"offY\":80.0,\"radius\":10.0,\"color\":3355508719,\"fillIntensity\":0.1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("EastEarly", "{\"Name\":\"\",\"type\":2,\"refX\":110.0,\"refY\":120.0,\"refZ\":9.536743E-07,\"offX\":110.0,\"offY\":80.0,\"radius\":10.0,\"color\":3355508719,\"fillIntensity\":0.1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    IBattleNpc[] GetDancers() => [.. Svc.Objects.OfType<IBattleNpc>().Where(x => x.NameId == 13779)];
    DanceDirection[] Directions = [];
    int CurrentIndex = 0;

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Directions.Length > 0 && CurrentIndex >= Directions.Length)
        {
            this.Controller.Reset();
        }
        var dancers = GetDancingDancers();
        if(dancers.Length == 8)
        {
            Directions = dancers.OrderBy(x => x.Position.X).Select(x => (DanceDirection)x.GetTransformationID()).ToArray();
        }
        if(CurrentIndex < Directions.Length && Controller.TryGetElementByName($"{Directions[CurrentIndex]}", out var e))
        {
            e.Enabled = true;
            if(C.ShowNext && (!C.ShowNextOnlyNorth || Directions.Any(x => x == DanceDirection.South || x == DanceDirection.North)) && CurrentIndex < Directions.Length - 1 && Controller.TryGetElementByName($"{Directions[CurrentIndex+1]}Early", out var early))
            {
                early.Enabled = true;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Additionally show one future AOE", ref C.ShowNext);
        ImGui.Indent();
        ImGui.Checkbox("Only show during Let's Dance Remix", ref C.ShowNextOnlyNorth);
        ImGui.Unindent();
        if(ImGui.CollapsingHeader("Dancers"))
        {
            ImGuiEx.Text(GetDancingDancers().Print("\n"));
            ImGui.Separator();
        }
    }

    IBattleNpc[] GetDancingDancers()
    {
        return GetDancers().Where(x => Enum.GetValues<DanceDirection>().Contains((DanceDirection)x.GetTransformationID())).ToArray();
    }

    public override void OnReset()
    {
        Directions = [];
        CurrentIndex = 0;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.Action.Value.RowId.EqualsAny(41877u, 39901u, 39900u))
        {
            CurrentIndex++;
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool ShowNext = false;
        public bool ShowNextOnlyNorth = false;
    }
}
