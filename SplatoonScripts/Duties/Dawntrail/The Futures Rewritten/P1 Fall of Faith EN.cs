using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System.Collections.Generic;
using System.Linq;
using TetherInfo = (uint ObjectID, bool IsFire);

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
public class P1_Fall_of_Faith_EN : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    private Config C => Controller.GetConfig<Config>();
    private List<TetherInfo> Tethers = [];
    private int PlayersRemaining => Svc.Objects.OfType<IPlayerCharacter>().Count(x => x.StatusList.Any(s => s.StatusId == 1051));
    private bool Active = false;
    bool IsBossCasting => Svc.Objects.OfType<IBattleNpc>().Any(x => x.IsTargetable && x.CastActionId.EqualsAny<uint>(40137, 40140));

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("TNorth1", "{\"Name\":\"North\",\"refX\":100.0,\"refY\":95.0,\"refZ\":9.536743E-07,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("TNorth2", "{\"Name\":\"North\",\"refX\":100.0,\"refY\":93.0,\"refZ\":9.536743E-07,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("TSouth1", "{\"Name\":\"South\",\"refX\":100.0,\"refY\":105.0,\"refZ\":9.536743E-07,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("TSouth2", "{\"Name\":\"South\",\"refX\":100.0,\"refY\":107.0,\"refZ\":9.536743E-07,\"color\":3355508503,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Active0", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"overlayVOffset\":1.0,\"thicc\":10.0,\"overlayText\":\"Fire\",\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Active1", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"overlayVOffset\":1.0,\"thicc\":10.0,\"overlayText\":\"Fire\",\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        for(int i = 0; i < 3; i++)
        {
            Controller.RegisterElementFromCode($"Line{i}", "{\"Name\":\"Line\",\"type\":3,\"refY\":10.0,\"radius\":0.0,\"color\":3372220160,\"fillIntensity\":0.345,\"refActorComparisonType\":2,\"includeRotation\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"faceplayer\":\"<2>\"}");
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Active)
        {
            var cnt = 0;
            List<IPlayerCharacter?> ActiveTethers = [];
            foreach(var x in Tethers)
            {
                if(x.ObjectID.GetObject() is IPlayerCharacter pc && !pc.StatusList.Any(s => s.StatusId == 1051)) continue;
                if(cnt < 2)
                {
                    ActiveTethers.Add(x.ObjectID.GetObject() as IPlayerCharacter);
                    var elem = Controller.GetElementByName($"Active{cnt}");
                    elem.Enabled = true;
                    elem.color = (x.IsFire ? EColor.RedBright : EColor.CyanBright).ToUint();
                    elem.overlayTextColor = elem.color;
                    elem.overlayText = x.IsFire ? "Fire tether" : "Lightning tether";
                    elem.refActorObjectID = x.ObjectID;
                    if(x.ObjectID == Player.Object.EntityId)
                    {
                        elem.overlayText += " (>>>YOU<<<)";
                    }
                }
                cnt++;
            }
            if(Tethers.TryGetFirst(x => x.ObjectID == Player.Object.EntityId, out var player))
            {
                if(!EzThrottler.Check(this.InternalData.FullName + "OnStartCast"))
                {
                    var index = Tethers.IndexOf(player);
                    var elem = index switch
                    {
                        0 => "TNorth1",
                        1 => "TSouth1",
                        2 => "TNorth2",
                        3 => "TSouth2",
                        _ => ""
                    };
                    if(Controller.TryGetElementByName(elem, out var element))
                    {
                        element.Enabled = true;
                    }
                }
            }
            else if(PlayersRemaining == 4)
            {
                if(ActiveTethers.All(x => Player.DistanceTo(x) > 3f))
                {
                    var index = C.Priority.GetOwnIndex(x => !Tethers.Any(t => t.ObjectID == x.IGameObject.EntityId));
                    if(index < 2)
                    {
                        Controller.GetElementByName("TNorth2").Enabled = true;
                    }
                    else
                    {
                        Controller.GetElementByName("TSouth2").Enabled = true;
                    }
                }
            }
            if(PlayersRemaining == 0) Active = false;
        }
    }

    //> Tether create: 0000024115F33E58 / Fatebreaker / 1073755346, 276392049/276392049, 0, 249, 15
    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(data3 == 249)
        {
            Tethers.Add((target, true));
            PluginLog.Information($"Fire tether on {target.GetObject()}");
        }
        else if(data3 == 287)
        {
            PluginLog.Information($"Lightning tether on {target.GetObject()}");
            Tethers.Add((target, false));
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(source.GetObject()?.IsTargetable != true) return;
        if(castId == 40137 || castId == 40140)
        {
            EzThrottler.Throttle(this.InternalData.FullName + "OnStartCast", 15000, true);
            Active = true;
            Tethers = [Tethers[^1]];
        }
    }

    public override void OnReset()
    {
        Active = false;
        Tethers.Clear();
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Configure conga line");
        C.Priority.Draw();
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("Active", ref Active);
            ImGuiEx.Text($"Tethers: \n{Tethers.Select(x => $"{x.ObjectID.GetObject()} - fire: {x.IsFire}").Print("\n")}");
            ImGuiEx.Text($"Players: {PlayersRemaining}");
        }
    }

    public class Config : IEzConfig
    {
        public PriorityData Priority = new() { NumPlayers = 8 };
    }
}
