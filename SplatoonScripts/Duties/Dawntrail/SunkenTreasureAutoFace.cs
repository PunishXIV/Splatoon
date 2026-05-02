// ABOUTME: Sunken Treasure auto-face script for Another Merchant's Tale (Territory 1317)
// ABOUTME: Detects safe corners via orb break timing, assigns player via Left/Right rule, and auto-faces for forced march

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

public unsafe class SunkenTreasureAutoFace : SplatoonScript
{
    public override Metadata Metadata => new(1, "Ahernika");
    public override HashSet<uint> ValidTerritories => [1317];

    private Config C => Controller.GetConfig<Config>();

    // Object BaseIds for orb AoEs at corners
    private const uint CircleAoeBaseId = 2015004;  // Big circle AoE (radius 18)
    private const uint DonutAoeBaseId = 2015005;   // Donut AoE

    // Buff IDs
    private const uint StackBuffId = 4726;
    private const uint ForwardMarchBuffId = 2161;
    private const uint AboutFaceBuffId = 2162;
    private const uint LeftFaceBuffId = 2163;
    private const uint RightFaceBuffId = 2164;

    // March direction offsets (radians) from player facing
    private static readonly Dictionary<uint, float> MarchOffsets = new()
    {
        { ForwardMarchBuffId, 0f },
        { RightFaceBuffId, -MathF.PI / 2f },
        { AboutFaceBuffId, MathF.PI },
        { LeftFaceBuffId, MathF.PI / 2f }
    };

    // Corner definitions (clockwise from North)
    private enum Corner { NE, SE, SW, NW }
    private static readonly Corner[] CornersClockwise = [Corner.NE, Corner.SE, Corner.SW, Corner.NW];

    // Orb tracking
    private class OrbData
    {
        public uint EntityId;
        public uint BaseId;
        public Vector3 Position;
        public bool IsBroke;  // True = AoE went off (unsafe)
    }

    private readonly List<OrbData> _orbDataList = [];
    private bool _autoRotDisabled;
    private Vector3 _lastPosition = Vector3.Zero;

    public override void OnSetup()
    {
        // Safe corner circles
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElement($"SafeCorner{i}", new Element(0)
            {
                color = 0xFF00FF00,
                radius = 2.0f,
                thicc = 3.0f,
                Filled = false,
                Enabled = false
            });
        }

        // March direction arrow
        Controller.RegisterElement("MarchArrow", new Element(0)
        {
            color = 0xFF008000,
            radius = 0.5f,
            thicc = 5.0f,
            tether = true,
            Filled = false,
            Enabled = false
        });

        // Direction text overlay
        Controller.RegisterElementFromCode("DirectionText",
            """{"Name":"","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":0.0,"overlayText":"","refActorType":1}""");
    }

    public override void OnReset()
    {
        _orbDataList.Clear();
        ReenableAutoRotation();
        for(int i = 0; i < 2; i++)
            if(Controller.TryGetElementByName($"SafeCorner{i}", out var c)) c.Enabled = false;
        if(Controller.TryGetElementByName("MarchArrow", out var el)) el.Enabled = false;
        if(Controller.TryGetElementByName("DirectionText", out var txt)) txt.Enabled = false;
    }

    public override void OnObjectCreation(nint newObjectPtr)
    {
        Controller.Schedule(() =>
        {
            var obj = Svc.Objects.FirstOrDefault(x => x.Address == newObjectPtr);
            if(obj == null || (obj.DataId != CircleAoeBaseId && obj.DataId != DonutAoeBaseId)) return;
            _orbDataList.Add(new OrbData
            {
                EntityId = obj.EntityId,
                BaseId = obj.DataId,
                Position = obj.Position,
                IsBroke = false,
            });
        }, 0);
    }

    public override void OnObjectEffect(uint target, uint data1, uint data2)
    {
        if(_orbDataList.Count == 0) return;

        if(data1 == 16 && data2 == 32)
        {
            // Orb AoE went off — mark as broke (unsafe)
            var orb = _orbDataList.FirstOrDefault(x => x.EntityId == target);
            if(orb != null) orb.IsBroke = true;
        }
        else if(data1 == 4 && data2 == 8)
        {
            // Object removed
            var index = _orbDataList.FindIndex(x => x.EntityId == target);
            if(index != -1) _orbDataList.RemoveAt(index);
        }
    }

    public override void OnUpdate()
    {
        // Disable all elements by default
        for(int i = 0; i < 2; i++)
            if(Controller.TryGetElementByName($"SafeCorner{i}", out var c)) c.Enabled = false;
        if(Controller.TryGetElementByName("MarchArrow", out var arrowEl)) arrowEl.Enabled = false;
        if(Controller.TryGetElementByName("DirectionText", out var dirText)) dirText.Enabled = false;

        var player = Svc.ClientState.LocalPlayer;
        if(player == null) return;

        // Detect safe corners from orb data
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

        DisableAutoRotation();

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

        if(float.IsNaN(faceTarget.X) || float.IsNaN(faceTarget.Z) ||
            float.IsInfinity(faceTarget.X) || float.IsInfinity(faceTarget.Z))
            return;

        if(EzThrottler.Throttle("SunkenAutoFace", 20))
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
    /// Determines safe corners by finding which circle AoE orb (2015004) hasn't broken.
    /// Circle orbs sit at cardinal edges (not corners). The broke orb's side is unsafe.
    /// The 2 intercardinal corners on the non-broke orb's side are safe.
    /// </summary>
    private List<Corner> GetSafeCorners()
    {
        var circleOrbs = _orbDataList.Where(x => x.BaseId == CircleAoeBaseId).ToList();
        if(circleOrbs.Count < 2) return [];

        // Need at least one to have broke to determine which side is unsafe
        var brokeOrb = circleOrbs.FirstOrDefault(x => x.IsBroke);
        var safeOrb = circleOrbs.FirstOrDefault(x => !x.IsBroke);
        if(brokeOrb == null || safeOrb == null) return [];

        var center = C.ArenaCenter;
        var safeOrbPos = new Vector2(safeOrb.Position.X, safeOrb.Position.Z);

        // Determine which 2 intercardinal corners are on the safe orb's side
        // by checking which corners are closer to the safe orb than to the broke orb
        var brokeOrbPos = new Vector2(brokeOrb.Position.X, brokeOrb.Position.Z);

        var safeCorners = new List<Corner>();
        foreach(var corner in CornersClockwise)
        {
            var cornerPos = GetCornerWorldPosition(corner);
            var distToSafe = Vector2.Distance(cornerPos, safeOrbPos);
            var distToBroke = Vector2.Distance(cornerPos, brokeOrbPos);

            if(distToSafe < distToBroke)
                safeCorners.Add(corner);
        }

        return safeCorners;
    }

    /// <summary>
    /// Gets the world position (X, Z) for a given corner.
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
    /// Determines which safe corner the player should go to.
    /// From center, look toward safe side midpoint. Left/Right is determined via cross product.
    /// </summary>
    private Corner? GetAssignedCorner(List<Corner> safeCorners)
    {
        if(safeCorners.Count != 2) return null;

        var group = GetPlayerGroup();
        if(group == null) return null;

        var (leftCorner, rightCorner) = OrderCornersByLeftRight(safeCorners[0], safeCorners[1]);

        if(C.Group1Direction == GroupDirection.Left)
            return group == PlayerGroup.Group1 ? leftCorner : rightCorner;
        else
            return group == PlayerGroup.Group1 ? rightCorner : leftCorner;
    }

    /// <summary>
    /// Orders two safe corners as Left and Right when looking from center toward the safe side.
    /// Uses 2D cross product: positive = right of direction, negative = left.
    /// </summary>
    private (Corner left, Corner right) OrderCornersByLeftRight(Corner a, Corner b)
    {
        var center = C.ArenaCenter;
        var posA = GetCornerWorldPosition(a);
        var posB = GetCornerWorldPosition(b);

        // Safe direction = from center toward midpoint of the two safe corners
        var midpoint = (posA + posB) / 2f;
        var safeDir = midpoint - center;

        // Vector from center to corner A
        var toA = posA - center;

        // 2D cross product: safeDir.X * toA.Y - safeDir.Y * toA.X
        // In FFXIV coords (X+ = East, Z/Y+ = South):
        //   Positive cross = A is to the RIGHT of safeDir
        //   Negative cross = A is to the LEFT of safeDir
        var cross = safeDir.X * toA.Y - safeDir.Y * toA.X;

        if(cross < 0)
            return (a, b);  // A is left, B is right
        else
            return (b, a);  // B is left, A is right
    }

    private PartyRole GetPartyRole(IPlayerCharacter player)
    {
        var role = player.GetRole();
        if(role == CombatRole.Tank) return PartyRole.Tank;
        if(role == CombatRole.Healer) return PartyRole.Healer;
        return IsD1(player) ? PartyRole.D1 : PartyRole.D2;
    }

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

    private PlayerGroup? GetPlayerGroup()
    {
        var player = Svc.ClientState.LocalPlayer;
        if(player == null) return null;

        var myRole = GetPartyRole(player);
        var baseGroup = GetBaseGroupForRole(myRole);

        if(!NeedsSwap(out var swapGroup)) return baseGroup;

        if(swapGroup == baseGroup && IsFlexPlayer(player))
            return baseGroup == PlayerGroup.Group1 ? PlayerGroup.Group2 : PlayerGroup.Group1;

        if(swapGroup != baseGroup && IsFlexPlayer(player))
            return swapGroup;

        return baseGroup;
    }

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

    private bool IsFlexPlayer(IPlayerCharacter player)
    {
        var myRole = GetPartyRole(player);
        var myGroup = GetBaseGroupForRole(myRole);

        if(myGroup == PlayerGroup.Group1)
            return myRole == C.Group1Flex;
        else
            return myRole == C.Group2Flex;
    }

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
        var jobId = player.ClassJob.RowId;
        return jobId is 2 or 20 or 4 or 22 or 29 or 30 or 34 or 39 or 41;
    }


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
        ImGui.Text("Sunken Treasure — Auto-Face for Forced March");
        ImGui.Separator();

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

        ImGuiEx.EnumCombo("Group Pairing", ref C.Pairing);
        ImGuiEx.HelpMarker(
            "Defines which roles pair together by default.\n" +
            "TH_D = T+H / D1+D2 (standard)\n" +
            "TD1_HD2 = T+D1 / H+D2\n" +
            "TD2_HD1 = T+D2 / H+D1\n" +
            "HD1_TD2 = H+D1 / T+D2\n" +
            "HD2_TD1 = H+D2 / T+D1");

        ImGuiEx.EnumCombo("Group 1 Goes", ref C.Group1Direction);
        ImGuiEx.HelpMarker("Which side Group 1 goes to when facing the safe orb from center (default: Left = TH)");

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

        // Orb tracking
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 1, 0.5f, 1), "=== Orb Data ===");
        foreach(var orb in _orbDataList)
            ImGui.Text($"  Entity: {orb.EntityId} BaseId: {orb.BaseId} Pos: ({orb.Position.X:F1}, {orb.Position.Z:F1}) Broke: {orb.IsBroke}");

        var safeCorners = GetSafeCorners();
        ImGui.Text($"Orbs tracked: {_orbDataList.Count} | Broke: {_orbDataList.Count(x => x.IsBroke)} | Safe Corners: {string.Join(", ", safeCorners)}");

        // Party / stacks / groups
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

        // Swap logic
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

        // Assignment
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0, 1, 1, 1), "=== Assignment ===");
        if(safeCorners.Count == 2)
        {
            var (leftCorner, rightCorner) = OrderCornersByLeftRight(safeCorners[0], safeCorners[1]);
            ImGui.Text($"Left Corner: {leftCorner}  |  Right Corner: {rightCorner}");
            ImGui.Text($"Group 1 Direction: {C.Group1Direction}");

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

        // Forced march
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
                }
            }
        }
        else
        {
            ImGui.Text("No forced march debuff active");
        }
    }

    // ===== Enums =====
    public enum GroupDirection { Left, Right }
    public enum DpsPosition { Auto, ForceD1, ForceD2 }
    public enum PartyRole { Tank, Healer, D1, D2 }
    public enum DefaultPairing
    {
        TH_D,
        TD1_HD2,
        TD2_HD1,
        HD1_TD2,
        HD2_TD1,
    }
    private enum PlayerGroup { Group1, Group2 }

    // ===== Config =====
    public class Config : IEzConfig
    {
        public bool EnableAutoFace = true;
        public float ActivationThreshold = 3.0f;
        public bool OnlyWhenStopped = true;

        public GroupDirection Group1Direction = GroupDirection.Left;

        public DefaultPairing Pairing = DefaultPairing.TH_D;
        public DpsPosition MyDpsPosition = DpsPosition.Auto;

        public PartyRole Group1Flex = PartyRole.Healer;
        public PartyRole Group2Flex = PartyRole.D1;

        public Vector2 ArenaCenter = new(375f, 530f);
        public float CornerOffset = 16f;
        public float CornerDetectionRadius = 12f;

        public bool ShowMarchArrow = true;

        public bool Debug = false;

        public bool DisableAutoRotDuringLock = false;
        public List<string> AutoRotToggleCommands = new()
        {
            "/vbm ar toggle Xan Melle",
            "/rotation auto"
        };
    }
}
