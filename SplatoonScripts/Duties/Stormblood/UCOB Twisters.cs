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
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Stormblood;

public class UCOB_Twisters : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => new() { 733 };

    public override Metadata? Metadata => new(1, "Enthusiastus");    

    private List<Element> _elements = new List<Element>();
    private bool active = false;
    private List<IPlayerCharacter> players = FakeParty.Get().ToList();

    Config Conf => this.Controller.GetConfig<Config>();

    string TestOverride = "";
    IPlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is IPlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;

    public override void OnSetup()
    {
        for(var i = 0; i < 8; i++)
        {
            var elem = new Element(0) { Enabled = false, radius = 1.0f, thicc = 2f, color = 0xFF03BAFC };
            this.Controller.TryRegisterElement($"twister{i}", elem);
            _elements.Add(elem);
        }
    }

    //> [08.09.2024 21:37:07 + 02:00] Message: Twintania starts casting 9898 (1482>9898)
    public override void OnStartingCast(uint source, uint castId) {
        if(castId == 9898)
        {
            //DuoLog.Debug($"Twistin' time. src {source}");
            active = true;
            this.Controller.ScheduleReset(2300);
            for (int i = 0; i < players.Count; ++i)
            {
                var p = players[i];
                var elem = _elements[i];
                elem.SetRefPosition(p.Position);
                elem.Enabled = true;
            }
        }
    }

    public override void OnReset()
    {
        Off();
    }

    public override void OnUpdate()
    {
        if(active)
        {
            for (int i = 0; i < players.Count; ++i)
            {
                var p = players[i];
                var elem = _elements[i];
                elem.SetRefPosition(p.Position);
                elem.Enabled = true;
            }
        }
    }

    public override void OnMessage(string Message)
    {
        //
    }

    void Off()
    {
        players = FakeParty.Get().ToList();
        active = false;
        foreach (var elem in _elements)
        {
            elem.Enabled = false;
            elem.color = 0xFF03BAFC;
        }
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
        {
            Off();
        }
    }

    public void LockTwisters()
    {
        foreach (var e in _elements) {
            e.color = 0xFF0000FF;
        }
        active = false;
    }

    public override void OnSettingsDraw()
    {
        //
    }

    public class Config : IEzConfig
    {
    }
}
