using System.Collections.Generic;
using System.Numerics;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ForceSetDirection : SplatoonScript
{
    private static ActionManager* ActionManager => FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
    public override HashSet<uint>? ValidTerritories => new();
    public override Metadata? Metadata => new(1, "Garume");

    private Config C => Controller.GetConfig<Config>();

    private void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
    {
        ActionManager->AutoFaceTargetPosition(&position, unkObjId);
    }

    private void FaceDirection(Vector2 direction)
    {
        var player = Player.Object;
        if (player != null)
            FaceTarget(player.Position + direction.ToVector3());
    }

    public override void OnUpdate()
    {
        if (C.Enabled)
        {
            var direction = Vector2.Normalize(C.Direction);
            FaceDirection(direction);
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.Checkbox("Enabled", ref C.Enabled))
            ImGui.DragFloat2("Direction", ref C.Direction);
    }

    private class Config : IEzConfig
    {
        public Vector2 Direction = new(0, 1);
        public bool Enabled = true;
    }
}