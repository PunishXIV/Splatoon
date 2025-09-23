using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Schedulers;
using ECommons.Throttlers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace SplatoonScriptsOfficial.Duties.Endwalker.Dragonsong_s_Reprise;
public sealed class P4_Dives : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [Raids.Dragonsongs_Reprise_Ultimate];

    int DiveCnt;
    bool? IsCWBait = null;
    List<uint> DiveTargets = [];

    IPlayerCharacter BasePlayer
    {
        get
        {
            return Player.Object;
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Hint", """{"Name":"","radius":1.0,"color":3359309568,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    public override void OnReset()
    {
        DiveCnt = 0;
        IsCWBait = null;
        DiveTargets.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public static List<T> SortQuad<T>(List<T> points) where T:IGameObject
    {
        // Sort by Y ascending (smaller Y = higher, because inverted axis)
        var sortedByY = points.OrderBy(p => p.Position.Y).ToList();

        // First two are the upper points
        var upper = sortedByY.Take(2).OrderByDescending(p => p.Position.X).ToList();
        // Last two are the lower points
        var lower = sortedByY.Skip(2).OrderByDescending(p => p.Position.X).ToList();

        return
        [
            upper[0], // upper right
            upper[1],  // upper left
            lower[1], // lower left
            lower[0], // lower right
        ];
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Source is not IPlayerCharacter && set.Action != null)
        {
            //PluginLog.Information($"Action: {ExcelActionHelper.GetActionName(set.Action.Value.RowId, true)} target: {set.TargetEffects.Select(x => ((uint)x.TargetID).GetObject()).Print()}");

            if(set.Action?.RowId == 26820)
            {
                DiveCnt++;
                this.DiveTargets.Add(set.TargetEffects.Select(x => (uint)x.TargetID).ToArray());
                if(DiveTargets.Count == 2)
                {
                    if(DiveTargets.Contains(BasePlayer.EntityId))
                    {
                        var players = Controller.GetPartyMembers().Where(x => x.StatusList.Any(s => s.StatusId == 2775)).ToList();
                        var orderedPlayers = SortQuad(players).Where(x => x.EntityId.EqualsAny(DiveTargets));
                        PluginLog.Information($"Ordered players: {orderedPlayers.Print()}");
                        IsCWBait = orderedPlayers.First().AddressEquals(BasePlayer);
                        PluginLog.Information($"Determined 3rd bait: {(IsCWBait == true ? "Clockwise" : "CounterClockwise")}");
                    }
                }
                if(DiveTargets.Count == 6 && IsCWBait != null)
                {
                    var players = Controller.GetPartyMembers().Where(x => x.StatusList.Any(s => s.StatusId == 2775)).ToList();
                    var orderedPlayers = SortQuad(players).Where(x => x.EntityId.EqualsAny(DiveTargets[4..6])).ToList();
                    PluginLog.Information($"Ordered players: {orderedPlayers.Print()}");
                    Controller.GetElementByName("Hint").SetRefPosition(orderedPlayers[IsCWBait.Value ? 0 : 1].Position);
                    Controller.GetElementByName("Hint").Enabled = true;
                    Controller.Schedule(() => Controller.GetElementByName("Hint").Enabled = false, 5000);
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        var players = Controller.GetPartyMembers().Where(x => x.StatusList.Any(s => s.StatusId == 2775)).ToList();
        var orderedPlayers = SortQuad(players);
        ImGuiEx.Text($"{orderedPlayers.Print("\n")}");
    }
}