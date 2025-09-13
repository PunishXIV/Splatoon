using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public class AutoExec : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnEnable()
    {
        ProperOnLogin.RegisterInteractable(OnLogin);
    }

    public override void OnDisable()
    {
        ProperOnLogin.Unregister(OnLogin);
    }

    private void OnLogin()
    {
        foreach(var x in C.Commands.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Chat.Instance.SendMessage(x);
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.InputTextMultilineExpanding("##id", ref C.Commands, 2000, 10, 100);
    }

    public Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public string Commands = "";
    }
}
