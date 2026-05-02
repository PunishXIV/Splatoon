// ABOUTME: Swimming in Air auto-face script for Another Merchant's Tale (Territory 1317)
// ABOUTME: Detects safe corners, assigns player via CW/CCW rule, and auto-faces for forced march

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using Dalamud.Bindings.ImGui;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon;
using Splatoon.SplatoonScripting;
using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.AnotherMerchantTale;

public unsafe class SwimmingInAirAutoFace : SplatoonScript
{
    public override Metadata Metadata => new(1, "Ahernika");
    public override HashSet<uint>? ValidTerritories => [1317];

    private Config C => Controller.GetConfig<Config>();

    // NPC and buff IDs
    private const uint AoeNpcId = 2015003;
    private const uint StackBuffId = 4726;
    private const uint ForwardMarchBuffId = 2161;
    private const uint AboutFaceBuffId = 2162;
    private const uint LeftFaceBuffId = 2163;
    private const uint RightFaceBuffId = 2164;

    // March direction offsets (radians) - from forced march preset AdditionalRotation values
    // These represent how the march direction is offset from the player's facing
    private static readonly Dictionary<uint, float> MarchOffsets = new()
    {
        { ForwardMarchBuffId, 0f },                    // March forward (same as facing)
        { RightFaceBuffId, -MathF.PI / 2f },           // March to the right
        { AboutFaceBuffId, MathF.PI },                 // March backward
        { LeftFaceBuffId, MathF.PI / 2f }              // March to the left
    };

    // Corner definitions (clockwise from North)
    // Using standard FFXIV coordinates: X+ = East, Z+ = South
    private enum Corner { NE, SE, SW, NW }

    private static readonly Corner[] CornersClockwise = [Corner.NE, Corner.SE, Corner.SW, Corner.NW];

    // Track whether we've toggled autorotation off
    private bool _autoRotDisabled;

    // JP FirstSafe mode tracking
    private float? _sweepStartAngleCW;
    private readonly HashSet<ulong> _seenAoeObjects = new();

    public override void OnSetup()
    {
        // Safe corner circles
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElement($"SafeCorner{i}", new Element(0)
            {
                color = 0xFF00FF00,   // Green
                radius = 2.0f,
                thicc = 3.0f,
                Filled = false,
                Enabled = false
            });
        }

        // March direction arrow
        Controller.RegisterElement("MarchArrow", new Element(0)
        {
            color = 0xFF008000,   // Dark green
            radius = 0.5f,
            thicc = 5.0f,
            tether = true,
            Filled = false,
            Enabled = false
        });

        // Countdown text
        Controller.RegisterElementFromCode("DirectionText",
            """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":0.0,"overlayText":"","refActorType":1}""");
    }

    public override void OnReset()
    {
        ReenableAutoRotation();
        _sweepStartAngleCW = null;
        _seenAoeObjects.Clear();
        for(int i = 0; i < 2; i++)
            if(Controller.TryGetElementByName($"SafeCorner{i}", out var c)) c.Enabled = false;
        if(Controller.TryGetElementByName("MarchArrow", out var el)) el.Enabled = false;
        if(Controller.TryGetElementByName("DirectionText", out var txt)) txt.Enabled = false;
    }

    public override void OnUpdate()
    {
        // Disable all elements by default
        for(int i = 0; i < 2; i++)
            if(Controller.TryGetElementByName($"SafeCorner{i}", out var c)) c.Enabled = false;
        if(Controller.TryGetElementByName("MarchArrow", out var arrowEl)) arrowEl.Enabled = false;
        if(Controller.TryGetElementByName("DirectionText", out var dirText)) dirText.Enabled = false;

        var player = Svc.ClientState.LocalPlayer ;
        if(player == null) return;

        // Track the first non-center AoE to determine the start of the CW sweep
        if(_sweepStartAngleCW == null)
        {
            var currentAoes = Svc.Objects.Where(x => x.DataId == AoeNpcId).ToList();
            foreach(var aoe in currentAoes)
            {
                if(!_seenAoeObjects.Contains(aoe.GameObjectId))
                {
                    _seenAoeObjects.Add(aoe.GameObjectId);
                    var pos = new Vector2(aoe.Position.X, aoe.Position.Z);

                    // If it's a perimeter point (not Center)
                    if(Vector2.Distance(pos, C.ArenaCenter) > 5f)
                    {
                        _sweepStartAngleCW = GetAngleCWFromCenter(pos, C.ArenaCenter);
                        DuoLog.Information($"[SwimInAir] First perimeter AoE detected at {_sweepStartAngleCW:F0}°");
                        break; // Found the start of the sweep
                    }
                }
            }
        }

        // Detect safe corners immediately (don't wait for forced march)
        var safeCorners = GetSafeCorners();
        if(safeCorners.Count != 2) return;

        // Show green circles at safe corners
        for(int i = 0; i < 2; i++)
        {
            if(Controller.TryGetElementByName($"SafeCorner{i}", out var circleEl))
            {
                var pos = GetCornerWorldPosition(safeCorners[i]);
                circleEl.Enabled = true;
                circleEl.refX = pos.X;
                circleEl.refY = pos.Y;
                circleEl.refZ = 0;
            }
        }

        // Check for forced march debuff
        var (marchBuffId, marchTimeRemaining) = GetForcedMarchDebuff(player);
        if(marchBuffId == 0)
        {
            // March resolved or not active — re-enable autorotation if we disabled it
            ReenableAutoRotation();
            return;
        }

        // Determine assigned corner
        var assignedCorner = GetAssignedCorner(safeCorners);
        if(assignedCorner == null) return;

        var cornerWorldPos = GetCornerWorldPosition(assignedCorner.Value);

        // Calculate required facing
        if(!MarchOffsets.TryGetValue(marchBuffId, out var marchOffset)) return;

        var playerPos = player.Position;
        var angleToSafe = MathF.Atan2(cornerWorldPos.X - playerPos.X, cornerWorldPos.Y - playerPos.Z);
        var requiredFacing = angleToSafe - marchOffset;

        // Show march direction arrow
        if(C.ShowMarchArrow && arrowEl != null)
        {
            var marchAngle = requiredFacing + marchOffset;
            var marchDest = new Vector2(
                playerPos.X + 16f * MathF.Sin(marchAngle),
                playerPos.Z + 16f * MathF.Cos(marchAngle));
            arrowEl.Enabled = true;
            arrowEl.refX = marchDest.X;
            arrowEl.refY = marchDest.Y;
            arrowEl.refZ = playerPos.Y;
        }

        // Show countdown
        if(dirText != null)
        {
            dirText.Enabled = true;
            dirText.overlayText = $"{assignedCorner.Value} | {marchTimeRemaining:F0}s";
        }

        // Auto-face: only within threshold
        if(!C.EnableAutoFace) return;
        if(marchTimeRemaining > C.ActivationThreshold) return;

        // Disable autorotation while direction locking
        DisableAutoRotation();

        // Clear target to prevent game's auto-face-target from fighting our direction lock
        if(Svc.Targets.Target != null && Svc.Targets.Target.GameObjectId != player.GameObjectId)
            Svc.Targets.Target = player;

        if(C.OnlyWhenStopped && Player.Object.Position != _lastPosition)
        {
            _lastPosition = Player.Object.Position;
            return;
        }
        _lastPosition = Player.Object.Position;

        var faceTarget = playerPos + new Vector3(
            MathF.Sin(requiredFacing),
            0,
            MathF.Cos(requiredFacing)
        );

        // Safety: skip if vector contains NaN or infinity
        if(float.IsNaN(faceTarget.X) || float.IsNaN(faceTarget.Z) ||
            float.IsInfinity(faceTarget.X) || float.IsInfinity(faceTarget.Z))
            return;

        if(EzThrottler.Throttle("SwimAutoFace", 20))
        {
            try
            {
                var actionManager = ActionManager.Instance();
                if(actionManager != null)
                    actionManager->AutoFaceTargetPosition(&faceTarget);
            }
            catch(Exception ex)
            {
                DuoLog.Error($"AutoFace error: {ex.Message}");
            }
        }
    }

    private Vector3 _lastPosition = Vector3.Zero;

    private void DisableAutoRotation()
    {
        if(_autoRotDisabled || !C.DisableAutoRotDuringLock) return;
        _autoRotDisabled = true;
        foreach(var cmd in C.AutoRotToggleCommands)
        {
            if(!string.IsNullOrWhiteSpace(cmd))
                Chat.Instance.SendMessage(cmd);
        }
    }

    private void ReenableAutoRotation()
    {
        if(!_autoRotDisabled) return;
        _autoRotDisabled = false;
        foreach(var cmd in C.AutoRotToggleCommands)
        {
            if(!string.IsNullOrWhiteSpace(cmd))
                Chat.Instance.SendMessage(cmd);
        }
    }

    /// <summary>
    /// Finds which 2 corners are safe by checking which corners have no AoE NPC nearby.
    /// </summary>
    private List<Corner> GetSafeCorners()
    {
        var aoePositions = Svc.Objects
            .Where(x => x.DataId == AoeNpcId)
            .Select(x => new Vector2(x.Position.X, x.Position.Z))
            .ToList();

        if(aoePositions.Count < 2) return [];

        var safeCorners = new List<Corner>();
        foreach(var corner in CornersClockwise)
        {
            var cornerPos = GetCornerWorldPosition(corner);
            var hasAoe = aoePositions.Any(aoe =>
                Vector2.Distance(aoe, cornerPos) < C.CornerDetectionRadius);

            if(!hasAoe)
                safeCorners.Add(corner);
        }

        return safeCorners;
    }

    /// <summary>
    /// Gets the world position (X, Z) for a given corner.
    /// Configurable via settings to match the arena layout.
    /// </summary>
    private Vector2 GetCornerWorldPosition(Corner corner)
    {
        var center = C.ArenaCenter;
        var offset = C.CornerOffset;
        return corner switch
        {
            Corner.NE => new Vector2(center.X + offset, center.Y - offset),
            Corner.SE => new Vector2(center.X + offset, center.Y + offset),
            Corner.SW => new Vector2(center.X - offset, center.Y + offset),
            Corner.NW => new Vector2(center.X - offset, center.Y - offset),
            _ => center
        };
    }

    /// <summary>
    /// Determines which safe corner the player should go to based on role,
    /// assignment mode, and swap logic for stack marker conflicts.
    /// </summary>
    private Corner? GetAssignedCorner(List<Corner> safeCorners)
    {
        if(safeCorners.Count != 2) return null;

        // Determine player's group assignment
        var group = GetPlayerGroup();
        if(group == null) return null;

        Corner firstCorner, secondCorner;

        if(C.Assignment == AssignmentMode.FirstSafe)
        {
            if(_sweepStartAngleCW.HasValue)
            {
                // JP strat: Group 1 goes to the safe corner that appears chronologically first
                // This is the corner with the smallest CW distance from the first perimeter AoE
                var angleA = GetCornerAngleCW(safeCorners[0]);
                var angleB = GetCornerAngleCW(safeCorners[1]);

                var distA = NormalizeTo360(angleA - _sweepStartAngleCW.Value);
                var distB = NormalizeTo360(angleB - _sweepStartAngleCW.Value);

                if(distA <= distB)
                {
                    firstCorner = safeCorners[0];
                    secondCorner = safeCorners[1];
                }
                else
                {
                    firstCorner = safeCorners[1];
                    secondCorner = safeCorners[0];
                }
            }
            else
            {
                // Fallback: closer to North
                var angleA = GetCornerAngleCW(safeCorners[0]);
                var angleB = GetCornerAngleCW(safeCorners[1]);
                if(angleA <= angleB) { firstCorner = safeCorners[0]; secondCorner = safeCorners[1]; }
                else { firstCorner = safeCorners[1]; secondCorner = safeCorners[0]; }
            }
        }
        else
        {
            // CW/CCW from reference direction
            var (cwCorner, ccwCorner) = OrderCornersByCwCcw(safeCorners[0], safeCorners[1]);
            if(C.Group1Direction == GroupDirection.CCW)
            {
                firstCorner = ccwCorner;
                secondCorner = cwCorner;
            }
            else
            {
                firstCorner = cwCorner;
                secondCorner = ccwCorner;
            }
        }

        return group == PlayerGroup.Group1 ? firstCorner : secondCorner;
    }

    /// <summary>
    /// Identifies a player's party role (Tank, Healer, D1, D2).
    /// Uses melee=D1/ranged=D2 with party list fallback, or manual override.
    /// </summary>
    private PartyRole GetPartyRole(IPlayerCharacter player)
    {
        var role = player.GetRole();
        if(role == CombatRole.Tank) return PartyRole.Tank;
        if(role == CombatRole.Healer) return PartyRole.Healer;
        return IsD1(player) ? PartyRole.D1 : PartyRole.D2;
    }

    /// <summary>
    /// Maps a party role to Group1 or Group2 based on the configured pairing.
    /// </summary>
    private PlayerGroup GetBaseGroupForRole(PartyRole role)
    {
        return C.Pairing switch
        {
            DefaultPairing.TH_D => role is PartyRole.Tank or PartyRole.Healer
                ? PlayerGroup.Group1 : PlayerGroup.Group2,
            DefaultPairing.TD1_HD2 => role is PartyRole.Tank or PartyRole.D1
                ? PlayerGroup.Group1 : PlayerGroup.Group2,
            DefaultPairing.TD2_HD1 => role is PartyRole.Tank or PartyRole.D2
                ? PlayerGroup.Group1 : PlayerGroup.Group2,
            DefaultPairing.HD1_TD2 => role is PartyRole.Healer or PartyRole.D1
                ? PlayerGroup.Group1 : PlayerGroup.Group2,
            DefaultPairing.HD2_TD1 => role is PartyRole.Healer or PartyRole.D2
                ? PlayerGroup.Group1 : PlayerGroup.Group2,
            _ => PlayerGroup.Group1
        };
    }

    /// <summary>
    /// Determines the player's group, accounting for swap logic.
    /// When both players in a group have stack, the flex from that group swaps OUT
    /// and the flex from the OTHER group swaps IN.
    /// </summary>
    private PlayerGroup? GetPlayerGroup()
    {
        var player = Svc.ClientState.LocalPlayer;
        if(player == null) return null;

        var myRole = GetPartyRole(player);
        var baseGroup = GetBaseGroupForRole(myRole);

        if(!NeedsSwap(out var swapGroup)) return baseGroup;

        // Flex player in the problematic group swaps OUT
        if(swapGroup == baseGroup && IsFlexPlayer(player))
            return baseGroup == PlayerGroup.Group1 ? PlayerGroup.Group2 : PlayerGroup.Group1;

        // Flex player in the OTHER group swaps IN
        if(swapGroup != baseGroup && IsFlexPlayer(player))
            return swapGroup;

        return baseGroup;
    }

    /// <summary>
    /// Checks if a swap is needed because both players in a group have stack markers.
    /// </summary>
    private bool NeedsSwap(out PlayerGroup swapGroup)
    {
        swapGroup = PlayerGroup.Group1;

        var party = Svc.Objects.OfType<IPlayerCharacter>().ToList();
        if(party.Count == 0) return false;

        int group1Stacks = 0, group2Stacks = 0;

        foreach(var member in party)
        {
            if(!member.StatusList.Any(s => s.StatusId == StackBuffId)) continue;

            var memberRole = GetPartyRole(member);
            var memberGroup = GetBaseGroupForRole(memberRole);
            if(memberGroup == PlayerGroup.Group1) group1Stacks++;
            else group2Stacks++;
        }

        if(group1Stacks >= 2)
        {
            swapGroup = PlayerGroup.Group1;
            return true;
        }
        if(group2Stacks >= 2)
        {
            swapGroup = PlayerGroup.Group2;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the given player is the designated flex/swap player for their group.
    /// </summary>
    private bool IsFlexPlayer(IPlayerCharacter player)
    {
        var myRole = GetPartyRole(player);
        var myGroup = GetBaseGroupForRole(myRole);

        if(myGroup == PlayerGroup.Group1)
            return myRole == C.Group1Flex;
        else
            return myRole == C.Group2Flex;
    }

    /// <summary>
    /// Determines if a DPS player is D1 (melee) or D2 (ranged/caster).
    /// If both DPS are the same type, falls back to party list order.
    /// </summary>
    private bool IsD1(IPlayerCharacter player)
    {
        var localPlayer = Svc.ClientState.LocalPlayer;
        if(localPlayer != null && localPlayer.GetRole() == CombatRole.DPS && C.MyDpsPosition != DpsPosition.Auto)
        {
            bool isMe = player.GameObjectId == localPlayer.GameObjectId;
            if(C.MyDpsPosition == DpsPosition.ForceD1) return isMe;
            if(C.MyDpsPosition == DpsPosition.ForceD2) return !isMe;
        }

        List<IPlayerCharacter> dpsMembers = new();
        if(Svc.Party != null && Svc.Party.Length > 0)
        {
            foreach(var member in Svc.Party)
            {
                if(member.GameObject is IPlayerCharacter pc && pc.GetRole() == CombatRole.DPS)
                {
                    dpsMembers.Add(pc);
                }
            }
        }
        else
        {
            dpsMembers = Svc.Objects.OfType<IPlayerCharacter>()
                .Where(x => x.GetRole() == CombatRole.DPS)
                .ToList();
        }

        // Sort by GameObjectId to guarantee an identical, objective tie-breaker across all clients
        dpsMembers = dpsMembers.OrderBy(x => x.GameObjectId).ToList();

        if(dpsMembers.Count < 2)
            return dpsMembers.Count > 0 && dpsMembers[0].GameObjectId == player.GameObjectId;

        var isMelee = IsMeleeDps(player);
        var otherDps = dpsMembers.FirstOrDefault(x => x.GameObjectId != player.GameObjectId);

        if(otherDps != null)
        {
            var otherIsMelee = IsMeleeDps(otherDps);

            // If one melee and one ranged, melee = D1
            if(isMelee != otherIsMelee)
                return isMelee;
        }

        // Both same type — fall back to GameObjectId (first = D1)
        return dpsMembers[0].GameObjectId == player.GameObjectId;
    }

    private static bool IsMeleeDps(IPlayerCharacter player)
    {
        // Melee DPS job IDs: MNK(2/20), DRG(4/22), NIN(29/30), SAM(34), RPR(39), VPR(41)
        var jobId = player.ClassJob.RowId;
        return jobId is 2 or 20 or 4 or 22 or 29 or 30 or 34 or 39 or 41;
    }

    /// <summary>
    /// Orders two corners as CW and CCW from the configured reference direction.
    /// Reference direction default: North (treats NE as first CW, NW as first CCW).
    /// </summary>
    private (Corner cw, Corner ccw) OrderCornersByCwCcw(Corner a, Corner b)
    {
        // Get the angular position of each corner on the compass (CW from North)
        var angleA = GetCornerAngleCW(a);
        var angleB = GetCornerAngleCW(b);
        var refAngle = GetReferenceAngleCW();

        // Normalized CW distance from reference
        var distA = NormalizeTo360(angleA - refAngle);
        var distB = NormalizeTo360(angleB - refAngle);

        // Smaller CW distance from reference = the CW corner
        if(distA < distB)
            return (a, b); // a is CW, b is CCW
        else
            return (b, a); // b is CW, a is CCW
    }

    /// <summary>
    /// Gets the clockwise angle (degrees) from North for a corner.
    /// NE=45, SE=135, SW=225, NW=315
    /// </summary>
    private static float GetCornerAngleCW(Corner corner)
    {
        return corner switch
        {
            Corner.NE => 45f,
            Corner.SE => 135f,
            Corner.SW => 225f,
            Corner.NW => 315f,
            _ => 0f
        };
    }

    /// <summary>
    /// Gets the clockwise angle from North for the configured reference direction.
    /// </summary>
    private float GetReferenceAngleCW()
    {
        return C.ReferenceDirection switch
        {
            CompassDirection.N => 0f,
            CompassDirection.NE => 45f,
            CompassDirection.E => 90f,
            CompassDirection.SE => 135f,
            CompassDirection.S => 180f,
            CompassDirection.SW => 225f,
            CompassDirection.W => 270f,
            CompassDirection.NW => 315f,
            _ => 0f
        };
    }

    private static float NormalizeTo360(float degrees)
    {
        return ((degrees % 360f) + 360f) % 360f;
    }

    /// <summary>
    /// Gets the CW angle in degrees (0 = North) for any position relative to center
    /// </summary>
    private static float GetAngleCWFromCenter(Vector2 pos, Vector2 center)
    {
        var dx = pos.X - center.X;
        var dz = pos.Y - center.Y; // Z axis in FFXIV
        var rad = MathF.Atan2(dx, -dz);
        var deg = rad * 180f / MathF.PI;
        return NormalizeTo360(deg);
    }

    /// <summary>
    /// Gets the player's forced march debuff and remaining time.
    /// Returns (buffId, remainingTime) or (0, 0) if no forced march debuff.
    /// </summary>
    private (uint buffId, float remainingTime) GetForcedMarchDebuff(IPlayerCharacter player)
    {
        foreach(var status in player.StatusList)
        {
            if(MarchOffsets.ContainsKey(status.StatusId))
                return (status.StatusId, status.RemainingTime);
        }
        return (0, 0);
    }

    // ===== Settings UI =====
    public override void OnSettingsDraw()
    {
        ImGui.Text("Swimming in the Air — Auto-Face for Forced March");
        ImGui.Separator();

        // Auto-face toggle
        ImGui.Checkbox("Enable Auto-Face", ref C.EnableAutoFace);
        ImGuiEx.HelpMarker(
            "Automatically rotates your character to face the correct direction for forced march.\n" +
            "WARNING: Do NOT use while streaming. Ensure no other plugins implement similar functionality.\n\n" +
            "強制移動の方向を自動で調整します。配信中は使用しないでください。");

        if(C.EnableAutoFace)
        {
            ImGui.Indent();
            ImGui.SliderFloat("Activation (seconds before resolve)", ref C.ActivationThreshold, 1.0f, 5.0f);
            ImGuiEx.HelpMarker("Start auto-facing this many seconds before forced march resolves");

            ImGui.Checkbox("Only when stopped", ref C.OnlyWhenStopped);
            ImGuiEx.HelpMarker("Only auto-face when the player is standing still (recommended for safety)");

            ImGui.Separator();
            ImGui.Checkbox("Disable Auto-Rotation During Lock", ref C.DisableAutoRotDuringLock);
            ImGuiEx.HelpMarker(
                "Toggle autorotation plugins off when direction lock activates, and back on when forced march resolves.\n" +
                "Uses the chat commands listed below (same command to toggle on/off).");
            if(C.DisableAutoRotDuringLock)
            {
                ImGui.Indent();
                for(int i = 0; i < C.AutoRotToggleCommands.Count; i++)
                {
                    var cmd = C.AutoRotToggleCommands[i];
                    if(ImGui.InputText($"Command {i + 1}", ref cmd, 256))
                        C.AutoRotToggleCommands[i] = cmd;
                    ImGui.SameLine();
                    if(ImGui.Button($"X##{i}"))
                    {
                        C.AutoRotToggleCommands.RemoveAt(i);
                        i--;
                    }
                }
                if(ImGui.Button("+ Add Command"))
                    C.AutoRotToggleCommands.Add("");
                ImGui.Unindent();
            }

            ImGui.Unindent();
        }

        ImGui.Separator();
        ImGui.Text("Assignment");
        ImGui.Indent();

        // Assignment mode
        ImGuiEx.EnumCombo("Assignment Mode", ref C.Assignment);
        ImGuiEx.HelpMarker(
            "CW_CCW = Assign Group 1 to CW or CCW corner from a reference direction.\n" +
            "FirstSafe = JP PF strat (安置出た順): Group 1 goes to whichever safe corner appears first.");

        // Group pairing
        ImGuiEx.EnumCombo("Group Pairing", ref C.Pairing);
        ImGuiEx.HelpMarker(
            "Defines which roles pair together by default.\n" +
            "TH_D = T+H / D1+D2 (standard)\n" +
            "TD1_HD2 = T+D1 / H+D2\n" +
            "TD2_HD1 = T+D2 / H+D1\n" +
            "HD1_TD2 = H+D1 / T+D2\n" +
            "HD2_TD1 = H+D2 / T+D1");

        if(C.Assignment == AssignmentMode.CW_CCW)
        {
            // Reference direction (only relevant for CW/CCW mode)
            ImGuiEx.EnumCombo("Reference Direction", ref C.ReferenceDirection);
            ImGuiEx.HelpMarker(
                "The compass direction used as starting point for CW/CCW corner assignment.\n" +
                "Default: North");

            // Group direction assignment
            ImGuiEx.EnumCombo("Group 1 Goes", ref C.Group1Direction);
            ImGuiEx.HelpMarker("Which direction from reference Group 1's safe corner is (default: CCW)");
        }

        ImGui.Separator();
        ImGui.Text("Flex Roles");
        ImGuiEx.EnumCombo("Group 1 Flex", ref C.Group1Flex);
        ImGuiEx.HelpMarker("Which role swaps out of Group 1 if both members have stack");

        ImGuiEx.EnumCombo("Group 2 Flex", ref C.Group2Flex);
        ImGuiEx.HelpMarker("Which role swaps out of Group 2 if both members have stack");

        ImGuiEx.EnumCombo("My DPS Position", ref C.MyDpsPosition);
        ImGuiEx.HelpMarker("Auto = melee is D1, ranged/caster is D2. If both same type, uses party list order.\nForceD1/ForceD2 = manual override.");

        ImGui.Unindent();

        ImGui.Separator();
        ImGui.Checkbox("Show March Direction Arrow", ref C.ShowMarchArrow);
        ImGuiEx.HelpMarker("Shows a dark green arrow from player toward calculated march destination");

        ImGui.Separator();
        ImGui.Checkbox("Debug", ref C.Debug);

        if(C.Debug)
        {
            ImGui.Indent();

            ImGui.Text("Arena Layout");
            ImGui.DragFloat2("Arena Center", ref C.ArenaCenter, 0.5f);
            ImGui.DragFloat("Corner Offset from Center", ref C.CornerOffset, 0.5f, 5f, 30f);
            ImGui.DragFloat("Corner Detection Radius", ref C.CornerDetectionRadius, 0.5f, 3f, 20f);

            ImGui.Separator();
            DrawDebugInfo();
            ImGui.Unindent();
        }
    }

    private void DrawDebugInfo()
    {
        var player = Svc.ClientState.LocalPlayer;
        if(player == null) { ImGui.Text("Player not found"); return; }

        ImGui.Text($"Pos: ({player.Position.X:F1}, {player.Position.Z:F1}) Facing: {player.Rotation:F3} rad ({player.Rotation * 180f / MathF.PI:F1}°)");
        ImGui.Text($"Center: ({C.ArenaCenter.X:F1}, {C.ArenaCenter.Y:F1}) Offset: {C.CornerOffset:F1}");

        var safeCorners = GetSafeCorners();
        var aoeCount = Svc.Objects.Count(x => x.DataId == AoeNpcId);
        ImGui.Text($"AoEs: {aoeCount} | Safe Corners: {string.Join(", ", safeCorners)}");

        // ===== PARTY / STACKS / GROUPS =====
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 1, 1), "=== Party & Stacks ===");
        var party = Svc.Objects.OfType<IPlayerCharacter>().ToList();
        int g1Stacks = 0, g2Stacks = 0;
        foreach(var member in party)
        {
            var pRole = GetPartyRole(member);
            var memberGroup = GetBaseGroupForRole(pRole);
            var groupLabel = memberGroup == PlayerGroup.Group1 ? "G1" : "G2";
            var hasStack = member.StatusList.Any(s => s.StatusId == StackBuffId);
            var hasMarch = member.StatusList.Any(s => MarchOffsets.ContainsKey(s.StatusId));
            var marchDir = "none";
            if(hasMarch)
            {
                var marchStatus = member.StatusList.First(s => MarchOffsets.ContainsKey(s.StatusId));
                marchDir = marchStatus.StatusId switch
                {
                    ForwardMarchBuffId => "Forward",
                    AboutFaceBuffId => "Back",
                    LeftFaceBuffId => "Left",
                    RightFaceBuffId => "Right",
                    _ => "?"
                };
            }
            var isMe = member.GameObjectId == player.GameObjectId;
            var prefix = isMe ? ">>> " : "    ";
            var stackText = hasStack ? " [STACK]" : "";
            ImGui.Text($"{prefix}{member.Name} ({pRole}/{groupLabel}) March={marchDir}{stackText}");
            if(hasStack) { if(memberGroup == PlayerGroup.Group1) g1Stacks++; else g2Stacks++; }
        }
        ImGui.Text($"Pairing: {C.Pairing} | G1 Stacks: {g1Stacks} | G2 Stacks: {g2Stacks}");

        // ===== SWAP LOGIC =====
        ImGui.Separator();
        ImGui.TextColored(new Vector4(1, 0.8f, 0, 1), "=== Swap Logic ===");
        var needsSwap = NeedsSwap(out var swapGrp);
        ImGui.Text($"Swap Needed: {needsSwap}");
        if(needsSwap)
        {
            ImGui.Text($"Swap Group: {swapGrp}");
            ImGui.Text($"G1 Flex: {C.Group1Flex} | G2 Flex: {C.Group2Flex}");
            ImGui.Text($"Am I Flex? {IsFlexPlayer(player)}");
        }
        var myGroup = GetPlayerGroup();
        ImGui.Text($"My Role: {GetPartyRole(player)} | My Final Group: {myGroup}");

        // ===== ASSIGNMENT =====
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0, 1, 1, 1), "=== Assignment ===");
        if(safeCorners.Count == 2)
        {
            if(C.Assignment == AssignmentMode.FirstSafe)
            {
                ImGui.Text("Mode: FirstSafe");
                if(_sweepStartAngleCW.HasValue)
                {
                    ImGui.Text($"Sweep Start Angle: {_sweepStartAngleCW.Value:F0}°");

                    var angle0 = GetCornerAngleCW(safeCorners[0]);
                    var dist0 = NormalizeTo360(angle0 - _sweepStartAngleCW.Value);
                    ImGui.Text($"Corner 1 ({safeCorners[0]} at {angle0}°): CW Dist {dist0:F0}°");

                    var angle1 = GetCornerAngleCW(safeCorners[1]);
                    var dist1 = NormalizeTo360(angle1 - _sweepStartAngleCW.Value);
                    ImGui.Text($"Corner 2 ({safeCorners[1]} at {angle1}°): CW Dist {dist1:F0}°");
                }
                else
                {
                    ImGui.Text("Sweep Start Angle: Not Detected (waiting to sweep)");
                }
            }
            else
            {
                ImGui.Text("Mode: CW_CCW");
                var (cwCorner, ccwCorner) = OrderCornersByCwCcw(safeCorners[0], safeCorners[1]);
                ImGui.Text($"Reference Dir: {C.ReferenceDirection}");
                ImGui.Text($"CW Corner: {cwCorner}  |  CCW Corner: {ccwCorner}");
                ImGui.Text($"Group 1 Direction: {C.Group1Direction}");
            }

            var assignedCorner = GetAssignedCorner(safeCorners);
            ImGui.Text($"Assigned Corner: {assignedCorner}");
            if(assignedCorner != null)
            {
                var cornerPos = GetCornerWorldPosition(assignedCorner.Value);
                ImGui.Text($"Corner World Pos: ({cornerPos.X:F2}, {cornerPos.Y:F2})");
            }
        }
        else
        {
            ImGui.Text("Cannot assign — need exactly 2 safe corners");
        }

        // ===== FORCED MARCH & ORIENTATION =====
        ImGui.Separator();
        ImGui.TextColored(new Vector4(1, 0, 1, 1), "=== Forced March & Orientation ===");
        var (marchBuff, marchTime) = GetForcedMarchDebuff(player);
        if(marchBuff != 0)
        {
            var marchName = marchBuff switch
            {
                ForwardMarchBuffId => "Forward",
                AboutFaceBuffId => "Back",
                LeftFaceBuffId => "Left",
                RightFaceBuffId => "Right",
                _ => "?"
            };
            var marchOffsetRad = MarchOffsets.GetValueOrDefault(marchBuff, 0f);
            ImGui.Text($"March Direction: {marchName}");
            ImGui.Text($"March Offset: {marchOffsetRad:F3} rad ({marchOffsetRad * 180f / MathF.PI:F1}°)");
            ImGui.Text($"Time Remaining: {marchTime:F1}s");
            ImGui.Text($"AutoFace Active: {marchTime <= C.ActivationThreshold}");

            if(safeCorners.Count == 2)
            {
                var assignedCorner = GetAssignedCorner(safeCorners);
                if(assignedCorner != null)
                {
                    var cornerPos = GetCornerWorldPosition(assignedCorner.Value);
                    var playerPos = player.Position;

                    var angleToSafe = MathF.Atan2(cornerPos.X - playerPos.X, cornerPos.Y - playerPos.Z);
                    var requiredFacing = angleToSafe - marchOffsetRad;
                    var currentFacing = player.Rotation;

                    ImGui.TextColored(new Vector4(1, 1, 0.5f, 1), "Orientation Calculation:");
                    ImGui.Text($"  Angle to Safe Corner: {angleToSafe:F3} rad ({angleToSafe * 180f / MathF.PI:F1}°)");
                    ImGui.Text($"  Required Facing: {requiredFacing:F3} rad ({requiredFacing * 180f / MathF.PI:F1}°)");
                    ImGui.Text($"  Current Facing: {currentFacing:F3} rad ({currentFacing * 180f / MathF.PI:F1}°)");
                    var faceDiff = MathF.Abs(requiredFacing - currentFacing);
                    if(faceDiff > MathF.PI) faceDiff = 2f * MathF.PI - faceDiff;
                    ImGui.Text($"  Facing Error: {faceDiff:F3} rad ({faceDiff * 180f / MathF.PI:F1}°)");

                    var correctMarchAngle = requiredFacing + marchOffsetRad;
                    var correctMarchTarget = new Vector2(
                        playerPos.X + 16f * MathF.Sin(correctMarchAngle),
                        playerPos.Z + 16f * MathF.Cos(correctMarchAngle));
                    ImGui.Text($"  March Destination: ({correctMarchTarget.X:F1}, {correctMarchTarget.Y:F1})");
                    ImGui.Text($"  Target Corner: ({cornerPos.X:F1}, {cornerPos.Y:F1})");
                }
            }
        }
        else
        {
            ImGui.Text("No forced march debuff active");
        }
    }

    // ===== Enums =====
    public enum CompassDirection { N, NE, E, SE, S, SW, W, NW }
    public enum GroupDirection { CCW, CW }
    public enum AssignmentMode { CW_CCW, FirstSafe }
    public enum DpsPosition { Auto, ForceD1, ForceD2 }
    public enum PartyRole { Tank, Healer, D1, D2 }
    public enum DefaultPairing
    {
        TH_D,    // Group1=T+H, Group2=D1+D2 (standard)
        TD1_HD2, // Group1=T+D1, Group2=H+D2
        TD2_HD1, // Group1=T+D2, Group2=H+D1
        HD1_TD2, // Group1=H+D1, Group2=T+D2
        HD2_TD1, // Group1=H+D2, Group2=T+D1
    }
    private enum PlayerGroup { Group1, Group2 }

    // ===== Config =====
    public class Config : IEzConfig
    {
        // Auto-face settings
        public bool EnableAutoFace = true;
        public float ActivationThreshold = 3.0f;
        public bool OnlyWhenStopped = true;

        // Assignment settings
        public AssignmentMode Assignment = AssignmentMode.CW_CCW;
        public CompassDirection ReferenceDirection = CompassDirection.N;
        public GroupDirection Group1Direction = GroupDirection.CCW;

        // Group pairing
        public DefaultPairing Pairing = DefaultPairing.TH_D;
        public DpsPosition MyDpsPosition = DpsPosition.Auto;

        // Flex settings (who swaps out of their group when both have stack)
        public PartyRole Group1Flex = PartyRole.Healer;
        public PartyRole Group2Flex = PartyRole.D1;

        // Arena layout
        public Vector2 ArenaCenter = new(375f, 530f);
        public float CornerOffset = 16f;
        public float CornerDetectionRadius = 12f;

        // Visuals
        public bool ShowMarchArrow = true;

        // Debug
        public bool Debug = false;

        // Autorotation toggle
        public bool DisableAutoRotDuringLock = false;
        public List<string> AutoRotToggleCommands = new()
        {
            "/vbm ar toggle Xan Melle",
            "/rotation auto"
        };
    }
}
