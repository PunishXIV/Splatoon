using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class TurnAroundCmd : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata Metadata => new(1, "NightmareXIV");

    public override void OnEnable()
    {
        Svc.Commands.AddHandler("/turnaround", new(OnCommand));
    }

    private void OnCommand(string command, string arguments)
    {
        var direction = MathHelper.GetPointFromAngleAndDistance(Player.Position.ToVector2(), Player.Rotation + 180.DegreesToRadians(), 1).ToVector3();
        ActionManager.Instance()->AutoFaceTargetPosition(&direction);
    }

    public override void OnDisable()
    {
        Svc.Commands.RemoveHandler("/turnaround");
    }
}
