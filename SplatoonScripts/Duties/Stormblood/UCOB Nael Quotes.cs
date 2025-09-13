using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Stormblood;

public class UCOB_Nael_Quotes : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [733];

    public override Metadata? Metadata => new(1, "Enthusiastus");

    private List<Element> _elements = [];
    private Element? InDonut;
    private Element? OutCircle;
    private Element? StackMarker;
    private Element? SpreadMarker;
    private bool active = false;
    private List<IPlayerCharacter> players = FakeParty.Get().ToList();

    private Config Conf => Controller.GetConfig<Config>();

    private string TestOverride = "";
    private IPlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is IPlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;

    public override void OnSetup()
    {
        var in_donut_text = "{ \"Name\":\"in_donut\",\"type\":1,\"radius\":5.7,\"Donut\":18.69,\"fillIntensity\":0.17,\"thicc\":5.0,\"refActorNPCID\":2612,\"refActorComparisonType\":4,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"mechanicType\":1}";
        InDonut = Controller.RegisterElementFromCode($"in_donut", in_donut_text);
        InDonut.Enabled = false;
        var out_circle_text = "{\"Name\":\"out_circle\",\"type\":1,\"radius\":10.0,\"fillIntensity\":0.17,\"thicc\":5.0,\"refActorNPCID\":2612,\"refActorComparisonType\":4,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"mechanicType\":1}";
        OutCircle = Controller.RegisterElementFromCode($"out_circle", out_circle_text);
        OutCircle.Enabled = false;
        var stack_marker_text = "{\"Name\":\"stack_self\",\"type\":1,\"Enabled\":false,\"radius\":4.0,\"color\":3355508480,\"fillIntensity\":0.5,\"refActorName\":\"Ieni Telara\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}";
        StackMarker = Controller.RegisterElementFromCode($"stack_marker", stack_marker_text);
        StackMarker.Enabled = false;
        var spread_marker_text = "{\"Name\":\"player circles\",\"type\":1,\"radius\":4.0,\"color\":3355507967,\"fillIntensity\":0.5,\"thicc\":4.0,\"refActorPlaceholder\":[\"<h1>\",\"<h2>\",\"<t1>\",\"<t2>\",\"<d1>\",\"<d2>\",\"<d3>\",\"<d4>\"],\"refActorComparisonType\":5,\"DistanceSourceX\":0.21287155,\"DistanceSourceY\":9.486858,\"DistanceMax\":24.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}";
        SpreadMarker = Controller.RegisterElementFromCode($"spread_marker", spread_marker_text);
        SpreadMarker.Enabled = false;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        //
    }

    public override void OnUpdate()
    {
        //
    }
    // From on high I descend, the hallowed moon to call!               -> Spread => In
    // From on high I descend, the iron path to walk!                   -> Spread => Out
    // Take fire, O hallowed moon!                                      -> Stack => In
    // Blazing path, lead me to iron rule!                              -> Stack => Out
    // O hallowed moon, take fire and scorch my foes!                   -> In => Stack
    // O hallowed moon, shine you the iron path!                        -> In => Out
    // Fleeting light! 'Neath the red moon, scorch you the earth!       -> Away from Tank => Stack
    // Fleeting light! Amid a rain of stars, exalt you the red moon!    -> Spread => Away from Tank
    // From on high I descend, the moon and stars to bring!             -> Spread => In
    // From hallowed moon I descend, a rain of stars to bring!          -> In => Spread
    // From hallowed moon I bare iron, in my descent to wield!          -> In => Out => Spread
    // From hallowed moon I descend, upon burning earth to tread!       -> In => Spread => Stack
    // Unbending iron, take fire and descend!                           -> Out => Stack => Spread
    // Unbending iron, descend with fiery edge!                         -> Out => Spread => Stack

    public override void OnMessage(string Message)
    {
        //if (Message.StartsWith(""))
        if(Message.StartsWith("From on high I descend, the hallowed moon to call!"))
        {
            //DuoLog.Debug($"Spread&In");
            Activate(SpreadMarker, false);
            Activate(InDonut, true);
        }
        if(Message.StartsWith("From on high I descend, the iron path to walk!"))
        {
            //DuoLog.Debug($"Spread&Out");
            Activate(SpreadMarker, false);
            Activate(OutCircle, true);
        }
        if(Message.StartsWith("Take fire, O hallowed moon!"))
        {
            //DuoLog.Debug($"Stack&In");
            Activate(StackMarker, false);
            Activate(InDonut, true);
        }
        if(Message.StartsWith("Blazing path, lead me to iron rule!"))
        {
            //DuoLog.Debug($"Stack&Out");
            Activate(StackMarker, false);
            Activate(OutCircle, true);
        }
        if(Message.StartsWith("O hallowed moon, take fire and scorch my foes!"))
        {
            //DuoLog.Debug($"In&Stack");
            Activate(InDonut, false);
            Activate(StackMarker, true);
        }
        if(Message.StartsWith("O hallowed moon, shine you the iron path!"))
        {
            //DuoLog.Debug($"In&Out");
            Activate(InDonut, false);
            Activate(OutCircle, true);
        }
        if(Message.StartsWith("Fleeting light! 'Neath the red moon, scorch you the earth!"))
        {
            //DuoLog.Debug($"TB&Stack");
            Activate(StackMarker, true);
        }
        if(Message.StartsWith("Fleeting light! Amid a rain of stars, exalt you the red moon!"))
        {
            //DuoLog.Debug($"Spread&TB");
            Activate(SpreadMarker, false);
        }
        if(Message.StartsWith("From on high I descend, the moon and stars to bring!"))
        {
            //DuoLog.Debug($"Spread&In");
            Activate(SpreadMarker, false);
            Activate(InDonut, true);
        }
        if(Message.StartsWith("From hallowed moon I descend, a rain of stars to bring!"))
        {
            //DuoLog.Debug($"In&Spread");
            Activate(InDonut, false);
            Activate(SpreadMarker, true);
        }
        if(Message.StartsWith("From hallowed moon I bare iron, in my descent to wield!"))
        {
            //DuoLog.Debug($"In&Out&Spread");
            Activate(InDonut, false);
            Activate(OutCircle, true);
            Activate3(SpreadMarker);
        }
        if(Message.StartsWith("From hallowed moon I descend, upon burning earth to tread!"))
        {
            //DuoLog.Debug($"In&Spread&Stack,");
            Activate(InDonut, false);
            Activate(SpreadMarker, true);
            Activate3(StackMarker);
        }
        if(Message.StartsWith("Unbending iron, take fire and descend!"))
        {
            //DuoLog.Debug($"Out&Stack&Spread,");
            Activate(OutCircle, false);
            Activate(StackMarker, true);
            Activate3(SpreadMarker);
        }
        if(Message.StartsWith("Unbending iron, descend with fiery edge!"))
        {
            //DuoLog.Debug($"Out&Spread&Stack,");
            Activate(OutCircle, false);
            Activate(SpreadMarker, true);
            Activate3(StackMarker);
        }
    }

    private void Activate(Element elem, bool later)
    {
        if(later)
        {
            Task.Delay(6000).ContinueWith(_ =>
            {
                elem.Enabled = true;
            });
            Task.Delay(9000).ContinueWith(_ =>
            {
                elem.Enabled = false;
            });
        }
        else
        {
            elem.Enabled = true;
            Task.Delay(6000).ContinueWith(_ =>
            {
                elem.Enabled = false;
            });
        }
    }
    private void Activate3(Element elem)
    {
        Task.Delay(9000).ContinueWith(_ =>
        {
            elem.Enabled = true;
        });
        Task.Delay(12000).ContinueWith(_ =>
        {
            elem.Enabled = false;
        });
    }
    /*
    private void Out(bool later)
    {
        if(later)
        {
            Task.Delay(6000).ContinueWith(_ =>
            {
                OutCircle.Enabled = true;
            });
            Task.Delay(9000).ContinueWith(_ =>
            {
                OutCircle.Enabled = false;
            });
        } else
        {
            OutCircle.Enabled = true;
            Task.Delay(6000).ContinueWith(_ =>
            {
                OutCircle.Enabled = false;
            });
        }
    }

    private void In(bool later)
    {
        if (later)
        {
            Task.Delay(6000).ContinueWith(_ =>
            {
                InDonut.Enabled = true;
            });
            Task.Delay(9000).ContinueWith(_ =>
            {
                InDonut.Enabled = false;
            });
        } else
        {
            InDonut.Enabled = true;
            Task.Delay(6000).ContinueWith(_ =>
            {
                InDonut.Enabled = false;
            });
        }
    }
    */

    private void Off()
    {
        InDonut.Enabled = false;
        OutCircle.Enabled = false;
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            Off();
        }
    }

    public override void OnSettingsDraw()
    {
        //
    }

    public class Config : IEzConfig
    {
    }
}
