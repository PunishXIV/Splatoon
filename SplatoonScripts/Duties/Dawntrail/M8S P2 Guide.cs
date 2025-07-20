using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe sealed class M8S_P2_Guide : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1263];

    long TimeStart = long.MaxValue;
    //Data ID: 18222
    IBattleNpc? WolfP2 =>  Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.DataId == 18222 && x.IsTargetable);
    long TimeInCombat => Environment.TickCount64 - TimeStart;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("UV", """{"Name":"","refX":86.638016,"refY":106.175446,"refZ":-150.0,"radius":1.0,"Donut":0.3,"color":3355508490,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("South", """{"Name":"","refX":99.76935,"refY":117.93043,"refZ":-150.0,"radius":3.0,"Donut":0.3,"color":3355508490,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("LWL", """{"Name":"","refX":116.77617,"refY":105.80841,"refZ":-150.0,"radius":3.0,"Donut":0.3,"color":3355508490,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("West", """{"Name":"","refX":89.67121,"refY":85.856964,"refZ":-150.0,"radius":3.0,"Donut":0.3,"color":3355508490,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("Southwest", """{"Name":"","refX":83.42895,"refY":105.599915,"refZ":-150.0,"radius":3.0,"Donut":0.3,"color":3355508490,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    List<HintData> Hints = [
        new("0:01", 18, "UV"),
        new("0:25", 8, "South"),
        new("0:48", 5, "UV"),
        new("1:00", 8, "UV"),
        new("1:10", 7, "South"),
        new("1:18", 4, "West"),
        new("1:33", 5, "West"),
        new("2:25", 10, "South"),
        new("3:05", 15, "UV"),
        new("3:32", 5, "South"),
        new("3:41", 7, "LWL"),
        new("4:28", 7, "UV"),
        new("4:41", 5, "South"),
        new("5:02", 5, "Southwest"),
        ];

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(WolfP2 == null)
        {
            TimeStart = Environment.TickCount64;
        }
        else
        {
            foreach(var x in Hints)
            {
                if(TimeInCombat.InRange(x.TimeStart, x.TimeEnd) && Controller.TryGetElementByName(x.Element, out var e))
                {
                    e.Enabled = true;
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Time in combat: {TimeInCombat}");
    }

    public class HintData
    {
        public long TimeStart;
        public long TimeEnd;
        public string Element;

        public HintData(string time, int durationSeconds, string element)
        {
            var min = int.Parse(time.Split(":")[0]);
            var sec = int.Parse(time.Split(":")[1]);
            TimeStart = (min * 60 + sec) * 1000;
            TimeEnd = TimeStart + durationSeconds * 1000;
            Element = element;
        }
    }
}