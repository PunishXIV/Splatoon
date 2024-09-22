using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class CheckEmote : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [];
    public override void OnSettingsDraw()
    {
        if(Svc.Targets.Target is IPlayerCharacter pc)
        {
            var em = pc.Struct()->EmoteController.EmoteId;
            ImGuiEx.Text($"Emote: {em}/{Svc.Data.GetExcelSheet<Emote>().GetRow(em)?.Name}");
        }
    }
}
