using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class Another_Merchants_Tale_Alluring_Order_1 : SplatoonScript<Another_Merchants_Tale_Alluring_Order_1.Config>
{
    public override Metadata Metadata { get; } = new(1, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1317];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("NE", """{"Name":"","refX":390.0,"refY":515.0,"refZ":-29.5,"radius":3.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("NW", """{"Name":"","refX":360.0,"refY":515.0,"refZ":-29.5,"radius":3.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SW", """{"Name":"","refX":360.0,"refY":545.0,"refZ":-29.5,"radius":3.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
        Controller.RegisterElementFromCode("SE", """{"Name":"","refX":390.0,"refY":545.0,"refZ":-29.5,"radius":3.0,"color":3358457600,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}""");
    }

    private HashSet<string> Taken = [];
    Vector3? Start = null;

    public override void OnReset()
    {
        Taken.Clear();
        Start = null;
    }

    public override void OnUpdate()
    {
        Controller.Hide();
        if(Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId.EqualsAny(4726u))))
        {
            foreach(var x in Svc.Objects.OfType<IEventObj>())
            {
                if(x.DataId == 2015003 && Vector2.Distance(new(375.000f, 530.000f), x.Position.ToVector2()) > 3)
                {
                    Start ??= x.Position;
                    foreach(var el in Controller.GetRegisteredElements())
                    {
                        if(Vector3.Distance(el.Value.RefPosition, x.Position) < 5)
                        {
                            Taken.Add(el.Key);
                        }
                    }
                }
            }
            if(Taken.Count == 2 && Start != null)
            {
                var position = MathHelper.EnumerateObjectsClockwise(Controller.GetRegisteredElements().Where(x => !Taken.Contains(x.Key)), x => x.Value.RefPosition.ToVector2(), new(375.000f, 530.000f), Start.Value.ToVector2()).ElementAt(C.IsFirst?0:1);
                position.Value.Enabled = true;
                position.Value.color = Controller.AttentionColor;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text("Your Group:");
        ImGuiEx.RadioButtonBool("1", "2", ref C.IsFirst);
    }

    public class Config
    {
        public bool IsFirst = false;
    }
}
