using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Element = Splatoon.Element;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M12S_P1_Snake : SplatoonScript
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];

    public enum Debuff
    {
        Pos1 = 3004,
        Pos2 = 3005,
        Pos3 = 3006,
        Pos4 = 3451,
        Alpha = 4752,
        Beta = 4754,
    }

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("TowerWaiting", """{"Name":"","refX":96.0,"refY":96.0,"radius":4.5,"Donut":0.5,"color":3355508719,"fillIntensity":0.281}""");
        Controller.RegisterElementFromCode("TowerGet", """
            {"Name":"","refX":96.0,"refY":96.0,"radius":4.5,"Donut":0.5,"color":3357277952,"fillIntensity":1.0,"thicc":5.0,"tether":true}
            """);
    }

    bool MechanicActive => Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => HasStatus(x, Debuff.Pos4)));
    List<Vector3> Towers = [];
    Debuff? MyDebuff = null;

    public override void OnReset()
    {
        Towers.Clear();
        MyDebuff = null;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(x.NameId == 14378 && x.IsCasting(46262) && !Towers.Any(a => a.ApproximatelyEquals(x.Position, 0.5f)))
            {
                Towers.Add(x.Position);
            }
            if(x.NameId == 14381 && x.IsCasting(46263) && x.CurrentCastTime >= 2.5f)
            {
                for(int i = 0; i < Towers.Count; i++)
                {
                    Vector3 t = Towers[i];
                    if(t.ApproximatelyEquals(x.Position, 0.5f))
                    {
                        Towers[i] = default;
                    }
                }
            }
        }

        if(MechanicActive && (HasStatus(Debuff.Alpha) || MyDebuff != null))
        {
            var close = Controller.GetElementByName("Close");
            var far = Controller.GetElementByName("Far");
            var exit = Controller.GetElementByName("Exit");
            Element? e = null;
            if(HasStatus(Debuff.Pos1) || MyDebuff == Debuff.Pos1)
            {
                MyDebuff = Debuff.Pos1;
                if(Towers.SafeSelect(2) != default)
                {
                    e = Controller.GetElementByName("TowerWaiting");
                    if(GetRemainingTime(Debuff.Alpha) <= 0)
                    {
                        e = Controller.GetElementByName("TowerGet");
                    }
                    e?.SetRefPosition(Towers.SafeSelect(2));
                }
            }
            if(HasStatus(Debuff.Pos2) || MyDebuff == Debuff.Pos2)
            {
                MyDebuff = Debuff.Pos2;
                if(Towers.SafeSelect(3) != default)
                {
                    e = Controller.GetElementByName("TowerWaiting");
                    if(GetRemainingTime(Debuff.Alpha) <= 0)
                    {
                        e = Controller.GetElementByName("TowerGet");
                    }
                    e?.SetRefPosition(Towers.SafeSelect(3));
                }
            }
            if(HasStatus(Debuff.Pos3))
            {
                MyDebuff = Debuff.Pos3;
                if(Towers.SafeSelect(0) != default)
                {
                    e = Controller.GetElementByName("TowerWaiting");
                    if(GetRemainingTime(Debuff.Alpha) < 10f)
                    {
                        e = Controller.GetElementByName("TowerGet");
                    }
                    e?.SetRefPosition(Towers.SafeSelect(0));
                }
                else if(GetRemainingTime(Debuff.Alpha) < 10f)
                {
                    close.refActorObjectID = Controller.GetPartyMembers().FirstOrDefault(x => HasStatus(x, Debuff.Pos3) && HasStatus(x, Debuff.Beta))?.EntityId ?? 0;
                }
            }
            if(HasStatus(Debuff.Pos4))
            {
                MyDebuff = Debuff.Pos4;
                if(Towers.SafeSelect(1) != default)
                {
                    e = Controller.GetElementByName("TowerWaiting");
                    if(GetRemainingTime(Debuff.Alpha) < 10f)
                    {
                        e = Controller.GetElementByName("TowerGet");
                    }
                    e?.SetRefPosition(Towers.SafeSelect(1));
                }
            }
            
            e?.Enabled = true;
        }
    }

    bool HasStatus(Debuff d)
    {
        return BasePlayer.StatusList.Any(x => x.StatusId == (uint)d);
    }

    float GetRemainingTime(Debuff d)
    {
        return BasePlayer.StatusList.TryGetFirst(x => x.StatusId == (uint)d, out var status)?status.RemainingTime:0f;
    }

    bool HasStatus(IBattleChara b, Debuff d)
    {
        return b.StatusList.Any(x => x.StatusId == (uint)d);
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
        }
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
    }
}
