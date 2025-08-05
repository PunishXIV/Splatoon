using Dalamud.Game.ClientState.Conditions;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Tests;
internal unsafe class FaceLockTest : SplatoonScript
{
    public override HashSet<uint> ValidTerritories => [];
    private static ActionManager* ActionManager => FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
    private Config C => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public bool LockFace = false;
        public float Angle = 0;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Lock Face", ref C.LockFace);
        ImGui.SliderFloat("Angle", ref C.Angle, 0, 360);
    }

    public override void OnUpdate()
    {
        if(C.LockFace)
        {
            FaceTarget(C.Angle);
        }
    }

    private void FaceTarget(float rotation, ulong unkObjId = 0xE0000000)
    {
        if(Svc.Condition[ConditionFlag.DutyRecorderPlayback] && EzThrottler.Throttle("FaceTarget", 10000))
        {
            DuoLog.Information($"FaceTarget {rotation}");
            EzThrottler.Throttle("FaceTarget", 1000, true);
        }

        var adjustedRotation = (rotation + 270) % 360;
        var direction = new Vector2(
            MathF.Cos(adjustedRotation * MathF.PI / 180),
            MathF.Sin(adjustedRotation * MathF.PI / 180)
        );

        var player = Player.Object;
        var normalized = Vector2.Normalize(direction);

        if(player == null)
        {
            PluginLog.LogError("Player is null");
            return;
        }

        var position = player.Position + normalized.ToVector3();

        ActionManager->AutoFaceTargetPosition(&position, unkObjId);
    }
}
