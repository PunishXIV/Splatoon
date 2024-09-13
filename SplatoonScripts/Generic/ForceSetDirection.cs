using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ForceSetDirection : SplatoonScript
{
    private static ActionManager* ActionManager => FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
    public override HashSet<uint>? ValidTerritories => [];
    public override Metadata? Metadata => new(2, "Garume");

    private Config C => Controller.GetConfig<Config>();

    private void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
    {
        ActionManager->AutoFaceTargetPosition(&position, unkObjId);
    }

    private void FaceDirection(Vector2 direction)
    {
        var player = Player.Object;
        var normalized = Vector2.Normalize(direction);
        if (player != null)
            FaceTarget(player.Position + normalized.ToVector3());
    }

    public override void OnUpdate()
    {
        if (!C.Enabled) return;
        var direction = C.Mode switch
        {
            Mode.Angle => new Vector2(MathF.Cos(C.Angle * MathF.PI / 180), MathF.Sin(C.Angle * MathF.PI / 180)),
            Mode.Direction => C.Direction,
            Mode.ToTarget => C.Target - Player.Position.ToVector2(),
            Mode.Camera => new Vector2(-MathF.Sin(Camera.GetRadianX()), -MathF.Cos(Camera.GetRadianX())),
            _ => Vector2.Zero
        };

        FaceDirection(direction);
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Force the player to face a specific direction.");
        ImGui.Checkbox("Enabled", ref C.Enabled);
        if (!C.Enabled) return;
        ImGui.Indent();
        ImGuiEx.EnumCombo("##Mode", ref C.Mode);
        switch (C.Mode)
        {
            case Mode.Angle:
                ImGui.SliderFloat("Angle", ref C.Angle, 0, 360);
                break;
            case Mode.Direction:
                ImGui.SliderFloat("X", ref C.Direction.X, -1, 1);
                ImGui.SliderFloat("Y", ref C.Direction.Y, -1, 1);
                break;
            case Mode.ToTarget:
                ImGui.InputFloat2("Target", ref C.Target);
                break;
            case Mode.Camera:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnSetup()
    {
        Camera.Init();
    }


    private enum Mode : byte
    {
        Angle,
        Direction,
        ToTarget,
        Camera
    }

    private class Config : IEzConfig
    {
        public float Angle;
        public Vector2 Direction = new(0, 1);
        public bool Enabled = true;
        public Mode Mode;
        public Vector2 Target = new(0, 0);
    }
}

internal static unsafe class Camera
{
    private static float* _xPtr;

    public static void Init()
    {
        var cameraAddressPtr =
            *(nint*)Svc.SigScanner.GetStaticAddressFromSig(
                "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 11 48 8B 01");
        if (cameraAddressPtr == nint.Zero) throw new Exception("Camera address was zero");
        _xPtr = (float*)(cameraAddressPtr + 0x130);
    }

    internal static float GetRadianX()
    {
        if (_xPtr == null) return 0;
        return *_xPtr;
    }
}