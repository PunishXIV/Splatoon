using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using Moodle = (Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter Player, System.Guid UniqueID, int IconID, string Title, string Description, int StatusType, bool Dispelable, int Stacks, System.TimeSpan? ExpiresIn);

namespace SplatoonScriptsOfficial.Tests;
public class IPCExample : SplatoonScript
{
    [EzIPC] Action<Moodle> AddOrUpdateMoodle = null!;
    public override HashSet<uint>? ValidTerritories { get; } = []; 

    public override void OnSetup()
    {
        EzIPC.Init(this, "Moodles");
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(target == Player.Object.EntityId && vfxPath == "pathToBombVfx")
        {
            AddOrUpdateMoodle((Player.Object, Guid.NewGuid(), 12345, "Title", "Description", 0, false, 1, TimeSpan.FromSeconds(5)));
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.Button("Test"))
        {
            AddOrUpdateMoodle((Player.Object, Guid.NewGuid(), (int)Svc.Data.GetExcelSheet<Status>().GetRandom().Icon, "Title", "Description", 0, false, 1, TimeSpan.FromSeconds(5)));
        }
    }
}
