using Dalamud.Interface.Components;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Generic;

public unsafe class ForceSetDirection : SplatoonScript
{
    private Vector3 _lastPosition = Vector3.Zero;
    private static ActionManager* ActionManager => FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
    public override HashSet<uint>? ValidTerritories => [];
    public override Metadata? Metadata => new(4, "Garume");

    private Config C => Controller.GetConfig<Config>();

    private float ToRadian(float degree)
    {
        return degree * (MathF.PI / 180f);
    }

    private float ToDegree(float radian)
    {
        return radian * (180f / MathF.PI);
    }

    private void FaceTarget(Vector3 position, ulong unkObjId = 0xE0000000)
    {
        ActionManager->AutoFaceTargetPosition(&position, unkObjId);
    }

    private void FaceDirection(Vector2 direction)
    {
        var normalized = Vector2.Normalize(direction);
        var radian = MathF.Atan2(normalized.Y, normalized.X);
        radian += MathF.PI / 2f;
        var player = Player.Object;

        if(player == null)
        {
            Alert("Player object is null, skipping FaceTarget.");
            return;
        }

        if(_lastPosition != player.Position && C.OnlyStop)
        {
            Alert("Player position has changed, skipping FaceTarget.");
            return;
        }

        var playerAngle = -Player.Rotation + MathF.PI;

        var angleDiff = MathF.Abs(DeltaAngle(playerAngle, radian));
        if(angleDiff < ToRadian(C.AngleToleranceDeg)) return;

        Alert($"Executing FaceTarget: Difference {ToDegree(angleDiff)} exceeds {C.AngleToleranceDeg} degrees");

        FaceTarget(player.Position + normalized.ToVector3());
    }

    private void Alert(string message, bool force = false)
    {
        if(C.Debug || force)
            DuoLog.Information(message);
    }

    private float DeltaAngle(float current, float target)
    {
        var delta = (target - current) % (2f * MathF.PI);

        if(delta > MathF.PI)
            delta -= 2f * MathF.PI;
        else if(delta < -MathF.PI) delta += 2f * MathF.PI;

        return delta;
    }

    public override void OnUpdate()
    {
        if(!C.Enabled)
            return;
        var direction = C.Mode switch
        {
            Mode.Angle => new Vector2(
                MathF.Cos((C.Angle <= 270f ? C.Angle - 90f : C.Angle + 270f) * MathF.PI / 180f),
                MathF.Sin((C.Angle <= 270f ? C.Angle - 90f : C.Angle + 270f) * MathF.PI / 180f)),
            Mode.Direction => C.Direction,
            Mode.ToTarget => C.Target - Player.Position.ToVector2(),
            Mode.Camera => new Vector2(-MathF.Sin(Camera.GetRadianX()), -MathF.Cos(Camera.GetRadianX())),
            _ => Vector2.Zero
        };

        if(EzThrottler.Throttle("FaceDirection", 50))
        {
            FaceDirection(direction);
            _lastPosition = Player.Position;
        }
    }


    public override void OnSettingsDraw()
    {
        ImGui.Text("Force the player to face a specific direction.");
        ImGui.Checkbox("Enabled", ref C.Enabled);
        if(!C.Enabled)
            return;
        ImGui.Indent();
        ImGuiEx.EnumCombo("##Mode", ref C.Mode);
        switch(C.Mode)
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

        ImGui.Text("Angle Tolerance");
        ImGuiComponents.HelpMarker(
            "The angle tolerance in degrees. Force reorientation if shifted more than the value.");
        ImGui.SliderFloat("##Angle Tolerance", ref C.AngleToleranceDeg, 0f, 360f);
        ImGui.Checkbox("Enable only when stopped", ref C.OnlyStop);
        ImGuiEx.InfoMarker(
            "Only force the player to face the direction when the player is stopped. \nIt is always recommended to turn it on because it is dangerous!",
            EColor.Red);
        ImGui.Unindent();
        ImGui.Checkbox("Debug", ref C.Debug);
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
        public float AngleToleranceDeg = 10.0f;
        public bool Debug;
        public Vector2 Direction = new(0, 1);
        public bool Enabled = true;
        public Mode Mode;
        public bool OnlyStop = true;
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
        if(cameraAddressPtr == nint.Zero)
            throw new Exception("Camera address was zero");
        _xPtr = (float*)(cameraAddressPtr + 0x130);
    }

    internal static float GetRadianX()
    {
        if(_xPtr == null)
            return 0;
        return *_xPtr;
    }
}