using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class EX6_Aetherial_Ray_FinalPosition : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "Le-Vagabond");
    public override HashSet<uint>? ValidTerritories { get; } = [1308];

    // Source of truth: bossmod Ex7Doomtrain Intermission.cs (class AetherialRay).
    private const uint GhostTrainNameId         = 14406;  // BNpcName for parked controllers AND visible train
    private const uint IntermissionTrainDataId  = 0x4B7F; // Horn icon fires on this
    private const uint AetherIntermissionDataId = 0x4A3A; // intermission orb; cones clear when this dies
    private const uint SidDistance              = 4541;   // pivot direction encoded in Param
    private const uint IconHorn                 = 639;    // snapshot trigger (on IntermissionTrain)
    private const uint IconAetherialRay         = 412;    // headmarker on baited players
    private const uint AidAetherialRay          = 45693;  // the cone cast itself
    private const ushort ParamLongPivot         = 0x578;  // 170 degree pivot
    private const ushort ParamShortPivot        = 0x960;  // 106 degree pivot

    // Arena center for the INTERMISSION phase (bossmod Ex7DoomtrainStates.cs:131).
    private static readonly Vector2 ArenaCenter = new(-400f, -400f);
    private const float ProjectionDistance      = 25f;
    private const float ConeRange               = 50f;
    private const int ConeHalfAngleDeg          = 18; // bossmod uses 17.5; widen by 0.5 for safety
    private const int MaxCones                  = 2;  // 2 tankbuster baits

    // ABGR palette: blue, orange.
    private static readonly uint[] ConeColors = { 0xFFFF0000, 0xFF0080FF };

    private float _pivotRotationRad;
    private Vector3? _hornTrainPos;
    private Vector3? _predictedSource;
    private readonly HashSet<uint> _baitedPlayers = new();
    private bool _aetherSeenAlive;

    public override void OnSetup()
    {
        // type 5 = Cone at fixed coordinates. refX/Y/Z anchors the apex; AdditionalRotation
        // drives facing. Splatoon uses ABGR uint color packing.
        for(var i = 0; i < MaxCones; i++)
        {
            Controller.RegisterElement($"Cone{i}", new Element(5)
            {
                Name = $"AetherialRayFinal{i}",
                radius = ConeRange,
                coneAngleMin = -ConeHalfAngleDeg,
                coneAngleMax = ConeHalfAngleDeg,
                color = ConeColors[i % ConeColors.Length],
                fillIntensity = 0.3f,
                thicc = 3.0f,
                Filled = true,
            });
        }
    }

    public override void OnReset()
    {
        _aetherSeenAlive = false;
        ClearState();
    }

    private void ClearState()
    {
        _pivotRotationRad = 0f;
        _hornTrainPos = null;
        _predictedSource = null;
        _baitedPlayers.Clear();
        try
        {
            foreach(var kv in Controller.GetRegisteredElements())
                kv.Value.Enabled = false;
        }
        catch(Exception e)
        {
            PluginLog.Warning($"[AetherialRay] clear state failed: {e.Message}");
        }
    }

    private static bool TryParsePivot(ushort param, out float radians)
    {
        radians = param switch
        {
            ParamLongPivot  => 170f.DegToRad(),
            ParamShortPivot => 106f.DegToRad(),
            _ => 0f
        };
        return radians != 0f;
    }

    // Idempotent: recomputes _predictedSource when both inputs (pivot rotation and the
    // Horn-time train position) are available. Call from every code path that updates
    // either input.
    private void TryComputePrediction()
    {
        if(_pivotRotationRad == 0f || _hornTrainPos is not Vector3 trainPos)
            return;

        // bossmod: g = (gh.Position - Arena.Center).ToAngle() + _nextRotation;
        //         offset = g.ToDirection() * 25
        // FFXIV: angle 0 = south (+Z), CW. ToAngle = atan2(X,Z); ToDirection = (sin,cos).
        var dx = trainPos.X - ArenaCenter.X;
        var dz = trainPos.Z - ArenaCenter.Y; // Vector2.Y stores world Z
        var finalAngle = MathF.Atan2(dx, dz) + _pivotRotationRad;
        var ox = MathF.Sin(finalAngle) * ProjectionDistance;
        var oz = MathF.Cos(finalAngle) * ProjectionDistance;
        _predictedSource = new Vector3(ArenaCenter.X + ox, trainPos.Y, ArenaCenter.Y + oz);
    }

    // Splatoon's OnGainBuffEffect uses frame-to-frame status array diffing and skips
    // statuses that already exist when an entity is first observed
    // (BuffEffectProcessor.cs "New object" branch). Cycle 1's SID.Distance is on the
    // train at spawn time and gets missed - poll the status arrays directly to catch it.
    private void PollSidDistance()
    {
        if(_pivotRotationRad != 0f) return;
        foreach(var obj in Svc.Objects)
        {
            if(obj is not IBattleChara chr) continue;
            foreach(var s in chr.StatusList)
            {
                if(s.StatusId == SidDistance && TryParsePivot(s.Param, out var rad))
                {
                    _pivotRotationRad = rad;
                    TryComputePrediction();
                    return;
                }
            }
        }
    }

    // Mirrors bossmod's filter: refActorNPCNameID 14406 + DrawObject visible.
    private static ICharacter? FindLiveGhostTrain()
    {
        foreach(var obj in Svc.Objects)
        {
            if(obj is ICharacter chr && chr.NameId == GhostTrainNameId && chr.IsCharacterVisible())
                return chr;
        }
        return null;
    }

    private static bool IsAetherAlive()
    {
        foreach(var obj in Svc.Objects)
        {
            if(obj is IBattleNpc bn && bn.DataId == AetherIntermissionDataId && !bn.IsDead)
                return true;
        }
        return false;
    }

    public override void OnUpdate()
    {
        try
        {
            // Tear down all script state once the intermission orb dies (end of phase),
            // so cones from the last cycle don't linger into the post-intermission fight.
            var aetherLive = IsAetherAlive();
            if(aetherLive) _aetherSeenAlive = true;
            else if(_aetherSeenAlive)
            {
                _aetherSeenAlive = false;
                ClearState();
                return;
            }

            PollSidDistance();

            for(var i = 0; i < MaxCones; i++)
            {
                if(Controller.TryGetElementByName($"Cone{i}", out var c)) c.Enabled = false;
            }

            if(_predictedSource is not Vector3 src || _baitedPlayers.Count == 0)
                return;

            var idx = 0;
            foreach(var entityId in _baitedPlayers)
            {
                if(idx >= MaxCones) break;
                if(!entityId.TryGetObject(out var target)) continue;
                if(!Controller.TryGetElementByName($"Cone{idx}", out var cone)) continue;

                // Use the target player's Y for cone height as a ground-level reference.
                var coneOrigin = new Vector3(src.X, target.Position.Y, src.Z);
                cone.Enabled = true;
                cone.SetRefPosition(coneOrigin);

                // Splatoon's cone rotation runs CCW (south->west->north->east) while
                // FFXIV's atan2(X, Z) is CW, so we negate dx to match conventions.
                // Verified against Pictomancy PathArcTo offset = (cos(PI/2+α), sin(PI/2+α)).
                var tdx = target.Position.X - coneOrigin.X;
                var tdz = target.Position.Z - coneOrigin.Z;
                cone.AdditionalRotation = MathF.Atan2(-tdx, tdz);
                cone.FaceMe = false;

                idx++;
            }
        }
        catch(Exception e)
        {
            PluginLog.Warning($"[AetherialRay] cone update failed: {e.Message}");
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        try
        {
            if(set.Action?.RowId == AidAetherialRay)
                ClearState();
        }
        catch(Exception e)
        {
            PluginLog.Warning($"[AetherialRay] action effect handling failed: {e.Message}");
        }
    }

    // Head markers arrive via ActorControl command 34 (TargetIcon). p1 is the icon id;
    // sourceId is the marked actor; p2 echoes it for self markers.
    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        try
        {
            if(command != 34) return;

            if(p1 == IconAetherialRay && sourceId == p2)
            {
                _baitedPlayers.Add(sourceId);
                return;
            }

            if(p1 == IconHorn)
            {
                if(!sourceId.TryGetBattleNpc(out var inter) || inter.DataId != IntermissionTrainDataId)
                    return;

                var ghost = FindLiveGhostTrain();
                if(ghost == null)
                {
                    PluginLog.Warning("[AetherialRay] Horn fired but no visible GhostTrain found");
                    return;
                }

                _hornTrainPos = ghost.Position;
                TryComputePrediction();
            }
        }
        catch(Exception e)
        {
            PluginLog.Warning($"[AetherialRay] actor control handling failed: {e.Message}");
        }
    }

    public override void OnGainBuffEffect(uint sourceId, FFXIVClientStructs.FFXIV.Client.Game.Status Status)
    {
        try
        {
            if(Status.StatusId != SidDistance) return;
            if(!TryParsePivot(Status.Param, out var rad))
            {
                PluginLog.Warning($"[AetherialRay] unrecognized SID.Distance Param 0x{Status.Param:X4}");
                return;
            }
            _pivotRotationRad = rad;
            TryComputePrediction();
        }
        catch(Exception e)
        {
            PluginLog.Warning($"[AetherialRay] buff handling failed: {e.Message}");
        }
    }
}
