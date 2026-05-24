using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using static Splatoon.Splatoon;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal class BSOD_Adjuster : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [1122];
    public override Metadata? Metadata => new(4, "Redmoon, mirage");

    private const int SceneBsod = 5;

    public class CastID
    {
        public const uint StackMarker = 22393u;
        public const uint StackCannon = 31615u;
        public const uint BSOD = 31611u;
    }

    private Config Conf => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public bool DebugPrint = false;
        public PriorityData PriorityData = new()
        {
            Name = "BSOD left/right",
            Description = "Order from NorthWest to NorthEast (positions 1–8).",
        };
    }

    private List<IPlayerCharacter> _sortedList = [];
    private List<IPlayerCharacter> _stackedList = [];
    private bool _mechanicActive = false;

    public override void OnSettingsDraw()
    {
        ImGui.Text("Priority settings (NorthWest → NorthEast)");
        Conf.PriorityData.Draw();

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("DebugPrint", ref Conf.DebugPrint);
            ImGui.Text($"_mechanicActive : {_mechanicActive}");
            ImGui.Text($"_stackedList : {_stackedList.Print()}");
            ImGui.Text($"_sortedList : {_sortedList.Print()}");
            ImGui.Text($"BasePlayer: {BasePlayer?.Name.ToString() ?? "null"}");
            ImGui.Text($"Controller.Scene: {Controller.Scene}");
        }
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("StackLeft", "{\"Name\":\"\",\"refX\":95.74,\"refY\":112.62,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("StackRight", "{\"Name\":\"\",\"Enabled\":false,\"refX\":103.92,\"refY\":112.46,\"refZ\":-5.4569678E-12,\"radius\":0.3,\"color\":3355508546,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":5.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnUpdate()
    {
        if(Controller.Scene == SceneBsod)
        {
            if(TryGetPriorityList(out _sortedList))
                _mechanicActive = true;
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(source.GetObject() is IBattleNpc && castId == CastID.BSOD)
            OnReset();
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_mechanicActive || set.Action == null)
            return;

        if(set.Action.Value.RowId == CastID.StackMarker)
        {
            try
            {
                DebugLog($"StackMarker: {set.Target.Name}");
                if(set.Target is not IPlayerCharacter pcObj)
                    return;

                _stackedList.Add(pcObj);
                DebugLog($"_stackedList: {_stackedList.Print()}");
                if(_stackedList.Count == 2)
                {
                    if(BasePlayer == null)
                        return;

                    if(_stackedList.Exists(x => x.GameObjectId == BasePlayer.GameObjectId))
                    {
                        DebugLog("Stacker");
                        // stacker show element
                        var myStacker = _stackedList.First(x => x.GameObjectId == BasePlayer.GameObjectId);
                        var otherStacker = _stackedList.First(x => x.GameObjectId != BasePlayer.GameObjectId);
                        DebugLog($"myStacker: {myStacker.Name}, otherStacker: {otherStacker.Name}");
                        var myIndex = _sortedList.IndexOf(myStacker);
                        var otherIndex = _sortedList.IndexOf(otherStacker);
                        if(myIndex == -1 || otherIndex == -1)
                        {
                            DuoLog.Warning($"Could not find player in priority list");
                            _mechanicActive = false;
                            OnReset();
                            return;
                        }
                        var myPos = myIndex < otherIndex ? "Left" : "Right";
                        var otherPos = myIndex < otherIndex ? "Right" : "Left";

                        Controller.GetElementByName($"Stack{myPos}").Enabled = true;
                        Controller.GetElementByName($"Stack{myPos}").tether = true;

                        DebugLog($"myStacker: {myStacker.Name} {myPos}, otherStacker: {otherStacker.Name} {otherPos}");
                        var noneStackers = _sortedList.Where(x => !_stackedList.Contains(x)).ToList();
                        foreach(var x in noneStackers)
                        {
                            var dpos = noneStackers.IndexOf(x) < 3 ? "Left" : "Right";
                            DebugLog($"noneStacker: {x.Name} {dpos}");
                        }
                    }
                    else
                    {
                        DebugLog("Non stacker");
                        // non stacker show element
                        var noneStackers = _sortedList.Where(x => !_stackedList.Contains(x)).ToList();
                        if(BasePlayer == null)
                            return;

                        var myIndex = noneStackers.FindIndex(x => x.GameObjectId == BasePlayer.GameObjectId);
                        if(myIndex == -1)
                        {
                            DuoLog.Warning($"Could not find player in priority list");
                            _mechanicActive = false;
                            OnReset();
                            return;
                        }
                        var myPos = myIndex < 3 ? "Left" : "Right";

                        Controller.GetElementByName($"Stack{myPos}").Enabled = true;
                        Controller.GetElementByName($"Stack{myPos}").tether = true;

                        //Debug
                        var myStacker = _stackedList.First();
                        var otherStacker = _stackedList.Last();
                        myIndex = _sortedList.IndexOf(myStacker);
                        var otherIndex = _sortedList.IndexOf(otherStacker);
                        if(myIndex == -1 || otherIndex == -1)
                        {
                            DuoLog.Warning($"Could not find player in priority list");
                            _mechanicActive = false;
                            OnReset();
                            return;
                        }
                        myPos = myIndex < otherIndex ? "Left" : "Right";
                        var otherPos = myIndex < otherIndex ? "Right" : "Left";
                        DebugLog($"FirstStacker: {myStacker.Name} {myPos}, LastStacker: {otherStacker.Name} {otherPos}");

                        foreach(var x in noneStackers)
                        {
                            var dpos = noneStackers.IndexOf(x) < 3 ? "Left" : "Right";
                            DebugLog($"noneStacker: {x.Name} {dpos}");
                        }
                    }
                }
            }
            catch(Exception e)
            {
                DuoLog.Error(e.Message);
            }
            return;
        }

        if(set.Action.Value.RowId == CastID.StackCannon)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            _stackedList.Clear();
            DebugLog("===============================");
            return;
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        _stackedList.Clear();
        _mechanicActive = false;
    }

    private bool TryGetPriorityList([NotNullWhen(true)] out List<IPlayerCharacter> values)
    {
        var players = Conf.PriorityData.GetPlayers(_ => true);
        if(players == null || players.Count == 0)
        {
            values = [];
            return false;
        }

        values = players
            .Select(x => x.IGameObject as IPlayerCharacter)
            .Where(x => x != null)
            .Cast<IPlayerCharacter>()
            .ToList();

        return values.Count > 0;
    }

    private void DebugLog(string log, [CallerLineNumber] int lineNum = 0)
    {
        if(Conf.DebugPrint)
            DuoLog.Information(log + $" : L({lineNum})");
    }
}
