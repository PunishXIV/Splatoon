using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class M8S_Quad_Beckon_Moonlight_JP_Game8 : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1263];
    public override Metadata? Metadata => new(9, "NightmareXIV,Alex");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("SafeCone", """
            {"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":12.0,"coneAngleMax":90,"color":4278255376,"Filled":false,"fillIntensity":0.5,"thicc":10.0,"includeRotation":true,"FillStep":99.0}
            """);
        Controller.RegisterElementFromCode("UnsafeCone", """
            {"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":12.0,"coneAngleMax":270,"color":4278225151,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"includeRotation":true,"FillStep":99.0}
            """);
        Controller.RegisterElementFromCode($"{Position.Far_Left}", """
            {"Name":"Far Left","refX":88.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{Position.Far_Right}", """
            {"Name":"Far Right","refX":111.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{Position.Bottom_Left}", """
            {"Name":"Bottom Left","refX":94.0,"refY":110.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{Position.Bottom_Right}", """
            {"Name":"Bottom Right","refX":94.0,"refY":110.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("Stack", """
            {"Name":"stack","refX":100.7,"refY":100.7,"radius":0.6,"color":3355505151,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
    }

    private Dictionary<Position, Vector2> SpreadPositions = new()
    {
        [Position.Far_Left] = new(88.5f, 100.5f),
        [Position.Far_Right] = new(111.5f, 100.5f),
        [Position.Bottom_Left] = new(94f, 110f),
        [Position.Bottom_Right] = new(106f, 110f),
    };

    private Vector2 StackPosition = new(99.3f, 100.7f);

    private Dictionary<Quadrant, int> Rotations = new()
    {
        [Quadrant.SouthWest] = 0,
        [Quadrant.NorthWest] = 90,
        [Quadrant.NorthEast] = 180,
        [Quadrant.SouthEast] = 270,
    };

    public enum Position { Disabled, Far_Right, Far_Left, Bottom_Right, Bottom_Left };
    public enum DirectionalQuadrant { North, South, East, West }
    
    private DirectionalQuadrant? FirstShadowSafeZone = null;
    private DirectionalQuadrant? FourthShadowSafeZone = null;
    private Dictionary<DirectionalQuadrant, int> DirectionalRotations = new()
    {
        [DirectionalQuadrant.North] = 180,
        [DirectionalQuadrant.South] = 0,
        [DirectionalQuadrant.East] = 270,
        [DirectionalQuadrant.West] = 90
    };

    private Quadrant? SafeZone1 = null;
    private Quadrant? SafeZone2 = null;
    private IBattleNpc[] Shadows => GetShadows().ToArray().OrderBy(x => Order.IndexOf(x.EntityId)).ToArray();
    private StringBuilder DebugInfo = new StringBuilder();
    
    private DirectionalQuadrant? lastFirstShadowSafeZone = null;
    private DirectionalQuadrant? lastFourthShadowSafeZone = null;
    private Quadrant? lastSafeZone1 = null;
    private Quadrant? lastSafeZone2 = null;
    
    private bool safeZonePreviewActive = false;
    private bool spreadPreviewActive = false;
    private bool stackPreviewActive = false;
    private bool safeAndUnsafePreviewActive = false;
    private Quadrant previewSafeZone = Quadrant.SouthWest;
    private DirectionalQuadrant previewSpreadDirection = DirectionalQuadrant.South;
    private Quadrant previewStackQuadrant = Quadrant.SouthWest;
    
    public override void OnUpdate()
    {
        if (safeAndUnsafePreviewActive)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            PreviewSafeAndUnsafeZone(previewSafeZone);
            return;
        }
        
        if (safeZonePreviewActive || spreadPreviewActive || stackPreviewActive)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            
            if (safeZonePreviewActive) PreviewSafeZone(previewSafeZone);
            else if (spreadPreviewActive) PreviewSpreadPoint(previewSpreadDirection);
            else if (stackPreviewActive) PreviewStackPoint(previewStackQuadrant);
            
            return;
        }
        
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Shadows.Length == 0) return;
        
        if(Shadows.Length >= 1 && FirstShadowSafeZone == null)
        {
            FirstShadowSafeZone = GetDirectionalSafeQuadrant(
                Shadows[0].Position.ToVector2(), 
                Shadows[0].GetTransformationID() == 6);
        }
        
        if(Shadows.Length >= 4 && FourthShadowSafeZone == null)
        {
            FourthShadowSafeZone = GetDirectionalSafeQuadrant(
                Shadows[3].Position.ToVector2(), 
                Shadows[3].GetTransformationID() == 6);
        }
        
        if(Shadows.Length == 4)
        {
            SafeZone1 ??= FindSafeQuadrants(
                Shadows[0].Position.ToVector2(), Shadows[0].GetTransformationID() == 6,
                Shadows[1].Position.ToVector2(), Shadows[1].GetTransformationID() == 6).First();
            SafeZone2 ??= FindSafeQuadrants(
                Shadows[2].Position.ToVector2(), Shadows[2].GetTransformationID() == 6,
                Shadows[3].Position.ToVector2(), Shadows[3].GetTransformationID() == 6).First();
        }
        else if(Shadows.Length == 2)
        {
            SafeZone1 ??= FindSafeQuadrants(
                Shadows[0].Position.ToVector2(), Shadows[0].GetTransformationID() == 6,
                Shadows[1].Position.ToVector2(), Shadows[1].GetTransformationID() == 6).First();
        }
        
        RecordDebugInfoOnChange();
        
        if(NumActions < 4)
        {
            if(NumActions < 2)
            {
                if(SafeZone1 != null && Controller.TryGetElementByName($"SafeCone", out var safeConeElement))
                {
                    safeConeElement.Enabled = true;
                    safeConeElement.AdditionalRotation = Rotations[SafeZone1.Value].DegreesToRadians();
                    DrawSpreadPoints();    
                    DrawStackPoint(SafeZone1.Value); 
                }
                if(SafeZone2 != null && Controller.TryGetElementByName($"UnsafeCone", out var unsafeConeElement))
                {
                    unsafeConeElement.Enabled = true;
                    unsafeConeElement.AdditionalRotation = Rotations[SafeZone2.Value].DegreesToRadians();
                }
            }
            else
            {
                if(SafeZone2 != null && Controller.TryGetElementByName($"SafeCone", out var safeConeElement))
                {
                    safeConeElement.AdditionalRotation = Rotations[SafeZone2.Value].DegreesToRadians();
                    safeConeElement.Enabled = true;                   
                    DrawSpreadPoints();       
                    DrawStackPoint(SafeZone2.Value); 
                }
            }
        }
    }
    
    private void PreviewSafeAndUnsafeZone(Quadrant quadrant)
    {
        safeAndUnsafePreviewActive = true;
        DebugInfo.Clear();
        DebugInfo.AppendLine("=== Safe/Unsafe Zone Combined Preview ===");
        
        int safeRotation = Rotations[quadrant];
        
        // Safe zone is 90° area
        if (Controller.TryGetElementByName("SafeCone", out var safeCone))
        {
            safeCone.Enabled = true;
            safeCone.AdditionalRotation = safeRotation.DegreesToRadians();
            DebugInfo.AppendLine($"Safe Zone Direction: {quadrant}");
            DebugInfo.AppendLine($"  Safe Angles: {safeRotation}° - {(safeRotation + 90) % 360}°");
        }
        
        // Unsafe zone is 270° area (excluding safe zone)
        if (Controller.TryGetElementByName("UnsafeCone", out var unsafeCone))
        {
            unsafeCone.Enabled = true;
            // Unsafe zone starts from end of safe zone (safe zone + 90°)
            int unsafeRotation = (safeRotation + 90) % 360;
            unsafeCone.AdditionalRotation = unsafeRotation.DegreesToRadians();
            DebugInfo.AppendLine($"Unsafe Range: {unsafeRotation}° - {(unsafeRotation + 270) % 360}°");
        }
        
        EzThrottler.Throttle("ForceRefresh", 100, true);
    }

    private void RecordDebugInfoOnChange()
    {
        if (FirstShadowSafeZone != lastFirstShadowSafeZone || 
            FourthShadowSafeZone != lastFourthShadowSafeZone ||
            SafeZone1 != lastSafeZone1 || 
            SafeZone2 != lastSafeZone2)
        {
            DebugInfo.Clear();
            
            if (FirstShadowSafeZone != null || FourthShadowSafeZone != null)
            {
                DebugInfo.AppendLine("=== Spread Point Information ===");
                
                if (FirstShadowSafeZone != null)
                {
                    DebugInfo.AppendLine($"1st Shadow Safe Zone: {FirstShadowSafeZone}");
                    DebugInfo.AppendLine($"  Rotation: {DirectionalRotations[FirstShadowSafeZone.Value]}°");
                }
                
                if (FourthShadowSafeZone != null)
                {
                    DebugInfo.AppendLine($"4th Shadow Safe Zone: {FourthShadowSafeZone}");
                    DebugInfo.AppendLine($"  Rotation: {DirectionalRotations[FourthShadowSafeZone.Value]}°");
                }
                
                if (SpreadPositions.TryGetValue(C.Position, out var spreadPos))
                {
                    int rotationDegrees = GetCurrentSpreadRotation();
                    Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                        Center.ToVector3(), 
                        rotationDegrees.DegreesToRadians(), 
                        spreadPos.ToVector3());
                    
                    DebugInfo.AppendLine($"Current Spread Point: {C.Position}");
                    DebugInfo.AppendLine($"  Original Coordinates: {spreadPos.X:F2}, {spreadPos.Y:F2}");
                    DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedPos.X:F2}, {rotatedPos.Z:F2}");
                }
            }
            
            if (SafeZone1 != null || SafeZone2 != null)
            {
                DebugInfo.AppendLine("\n=== Stack Point Information ===");
                
                if (SafeZone1 != null)
                {
                    Vector3 rotatedStackPos1 = MathHelper.RotateWorldPoint(
                        Center.ToVector3(), 
                        Rotations[SafeZone1.Value].DegreesToRadians(), 
                        StackPosition.ToVector3());
                    
                    DebugInfo.AppendLine($"Safe Zone 1: {SafeZone1}");
                    DebugInfo.AppendLine($"  Rotation: {Rotations[SafeZone1.Value]}°");
                    DebugInfo.AppendLine($"  Stack Point Coordinates: {rotatedStackPos1.X:F2}, {rotatedStackPos1.Z:F2}");
                }
                
                if (SafeZone2 != null)
                {
                    Vector3 rotatedStackPos2 = MathHelper.RotateWorldPoint(
                        Center.ToVector3(), 
                        Rotations[SafeZone2.Value].DegreesToRadians(), 
                        StackPosition.ToVector3());
                    
                    DebugInfo.AppendLine($"Safe Zone 2: {SafeZone2}");
                    DebugInfo.AppendLine($"  Rotation: {Rotations[SafeZone2.Value]}°");
                    DebugInfo.AppendLine($"  Stack Point Coordinates: {rotatedStackPos2.X:F2}, {rotatedStackPos2.Z:F2}");
                }
            }
            
            lastFirstShadowSafeZone = FirstShadowSafeZone;
            lastFourthShadowSafeZone = FourthShadowSafeZone;
            lastSafeZone1 = SafeZone1;
            lastSafeZone2 = SafeZone2;
        }
    }
    
    private int GetCurrentSpreadRotation()
    {
        if (NumActions < 2 && FirstShadowSafeZone != null)
        {
            return DirectionalRotations[FirstShadowSafeZone.Value];
        }
        else if (FourthShadowSafeZone != null)
        {
            return DirectionalRotations[FourthShadowSafeZone.Value];
        }
        return 0;
    }
    
    private DirectionalQuadrant GetDirectionalSafeQuadrant(Vector2 position, bool isAttackingRight)
    {
        if (Vector2.Distance(position, new(100, 88)) < 5f)
        {
            return isAttackingRight ? DirectionalQuadrant.East : DirectionalQuadrant.West;
        }
        else if (Vector2.Distance(position, new(100, 112)) < 5f)
        {
            return isAttackingRight ? DirectionalQuadrant.West : DirectionalQuadrant.East;
        }
        else if (Vector2.Distance(position, new(112, 100)) < 5f)
        {
            return isAttackingRight ? DirectionalQuadrant.South : DirectionalQuadrant.North;
        }
        else if (Vector2.Distance(position, new(88, 100)) < 5f)
        {
            return isAttackingRight ? DirectionalQuadrant.North : DirectionalQuadrant.South;
        }
        
        return DirectionalQuadrant.East;
    }

    private void DrawSpreadPoints()
    {
        if (FirstShadowSafeZone == null && FourthShadowSafeZone == null) 
            return;
        
        int rotationDegrees = GetCurrentSpreadRotation();
        
        if(!EzThrottler.Check("BeckonSpread") && 
           Controller.TryGetElementByName($"{C.Position}", out var spread) && 
           SpreadPositions.TryGetValue(C.Position, out var pos))
        {
            spread.Enabled = true;
            Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                rotationDegrees.DegreesToRadians(), 
                pos.ToVector3());
            spread.SetRefPosition(rotatedPos);
        }
    }
        
    private void DrawStackPoint(Quadrant quadrant)
    {
        if(!EzThrottler.Check("BeckonStack") && 
           Controller.TryGetElementByName("Stack", out var stack))
        {
            stack.Enabled = true;
            Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                Rotations[quadrant].DegreesToRadians(),
                StackPosition.ToVector3());
            stack.SetRefPosition(rotatedStackPos);
        }
    }

    private void PreviewSafeZone(Quadrant quadrant)
    {
        safeZonePreviewActive = true;
        previewSafeZone = quadrant;
        spreadPreviewActive = false;
        stackPreviewActive = false;
        
        DebugInfo.Clear();
        DebugInfo.AppendLine("=== Safe Zone Preview ===");
        
        if(Controller.TryGetElementByName("SafeCone", out var safeCone))
        {
            safeCone.Enabled = true;
            safeCone.AdditionalRotation = Rotations[quadrant].DegreesToRadians();
            DebugInfo.AppendLine($"Safe Cone: {quadrant}");
            DebugInfo.AppendLine($"  Rotation: {Rotations[quadrant]}°");
        }
        
        if(Controller.TryGetElementByName($"{C.Position}", out var spread) && 
           SpreadPositions.TryGetValue(C.Position, out var pos))
        {
            spread.Enabled = true;
            Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                Rotations[quadrant].DegreesToRadians(), 
                pos.ToVector3());
            spread.SetRefPosition(rotatedPos);
            
            DebugInfo.AppendLine($"Spread Point: {C.Position}");
            DebugInfo.AppendLine($"  Original Coordinates: {pos.X:F2}, {pos.Y:F2}");
            DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedPos.X:F2}, {rotatedPos.Z:F2}");
        }
        
        if(Controller.TryGetElementByName("Stack", out var stack))
        {
            stack.Enabled = true;
            Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                Rotations[quadrant].DegreesToRadians(), 
                StackPosition.ToVector3());
            stack.SetRefPosition(rotatedStackPos);
            
            DebugInfo.AppendLine($"Stack Point:");
            DebugInfo.AppendLine($"  Original Coordinates: {StackPosition.X:F2}, {StackPosition.Y:F2}");
            DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedStackPos.X:F2}, {rotatedStackPos.Z:F2}");
        }
    }

    private void PreviewSpreadPoint(DirectionalQuadrant directionalQuadrant)
    {
        spreadPreviewActive = true;
        previewSpreadDirection = directionalQuadrant;
        safeZonePreviewActive = false;
        stackPreviewActive = false;
        
        DebugInfo.Clear();
        DebugInfo.AppendLine("=== Spread Point Preview ===");
        
        int rotationDegrees = DirectionalRotations[directionalQuadrant];
        DebugInfo.AppendLine($"Cardinal Safe Zone: {directionalQuadrant}");
        DebugInfo.AppendLine($"  Rotation: {rotationDegrees}°");
        
        if(Controller.TryGetElementByName($"{C.Position}", out var spread) && 
           SpreadPositions.TryGetValue(C.Position, out var pos))
        {
            spread.Enabled = true;
            Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                rotationDegrees.DegreesToRadians(), 
                pos.ToVector3());
            spread.SetRefPosition(rotatedPos);
            
            DebugInfo.AppendLine($"Spread Point: {C.Position}");
            DebugInfo.AppendLine($"  Original Coordinates: {pos.X:F2}, {pos.Y:F2}");
            DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedPos.X:F2}, {rotatedPos.Z:F2}");
        }
    }

    private void PreviewStackPoint(Quadrant quadrant)
    {
        stackPreviewActive = true;
        safeZonePreviewActive = false;
        spreadPreviewActive = false;
        previewStackQuadrant = quadrant;
        
        DebugInfo.Clear();
        DebugInfo.AppendLine("=== Stack Point Preview ===");
        
        Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
            Center.ToVector3(), 
            Rotations[quadrant].DegreesToRadians(), 
            StackPosition.ToVector3());
        
        DebugInfo.AppendLine($"Safe Quadrant: {quadrant}");
        DebugInfo.AppendLine($"  Rotation: {Rotations[quadrant]}°");
        DebugInfo.AppendLine($"  Original Coordinates: {StackPosition.X:F2}, {StackPosition.Y:F2}");
        DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedStackPos.X:F2}, {rotatedStackPos.Z:F2}");
        
        if(Controller.TryGetElementByName("Stack", out var stack))
        {
            stack.Enabled = true;
            stack.SetRefPosition(rotatedStackPos);
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if(vfxPath == "vfx/lockon/eff/target_ae_s5f.avfx" && target.GetObject()?.Address == Player.Object.Address)
        {
            EzThrottler.Throttle("BeckonSpread", 5000, true);
        }
        if(vfxPath == "vfx/lockon/eff/com_share1f.avfx" && target.TryGetObject(out var go) && go is IPlayerCharacter pc)
        {
            if(pc.GetJob().IsDps() == Player.Job.IsDps())
            {
                EzThrottler.Throttle("BeckonStack", 5000, true);
            }
        }
    }

    private HashSet<uint> Casted = [];
    private List<uint> Order = [];
    private int NumActions => Casted.Count - GetShadows().Count(x => x.IsCasting());
    
    private IEnumerable<IBattleNpc> GetShadows()
    {
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(x.DataId == 18217 && x.IsCharacterVisible() && x.GetTransformationID().EqualsAny<byte>(6, 7))
            {
                if(!Order.Contains(x.EntityId))
                {
                    Order.Add(x.EntityId);
                }
                if(!Casted.Contains(x.EntityId) || x.IsCasting())
                {
                    if(x.IsCasting())
                    {
                        Casted.Add(x.EntityId);
                    }
                    yield return x;
                }
            }
        }
        yield break;
    }

    private Config C => Controller.GetConfig<Config>();
    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f.Scale());
        ImGuiEx.EnumCombo("Spread position", ref C.Position);
        
        ImGuiEx.Text($"1st Shadow Safe Zone: {FirstShadowSafeZone?.ToString() ?? "Undetermined"}");
        ImGuiEx.Text($"4th Shadow Safe Zone: {FourthShadowSafeZone?.ToString() ?? "Undetermined"}");
        
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Separator();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "Combined Preview:");
            ImGui.SetNextItemWidth(100f.Scale());
            Quadrant debugQuadrant = C.DebugQuadrant;
            ImGuiEx.EnumCombo("Preview Safe Zone", ref debugQuadrant);
            C.DebugQuadrant = debugQuadrant;
            
            if (ImGui.Button("Preview Safe/Unsafe Zone"))
            {
            safeZonePreviewActive = false;
            spreadPreviewActive = false;
            stackPreviewActive = false;
            safeAndUnsafePreviewActive = true;
            previewSafeZone = C.DebugQuadrant;
            }
            ImGui.SameLine();            
            if (ImGui.Button("Reset Combined Preview"))
            {
                safeZonePreviewActive = false;
                spreadPreviewActive = false;
                stackPreviewActive = false;
                safeAndUnsafePreviewActive = false;
                DebugInfo.Clear();
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            }            
                       
            ImGui.Separator();
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Spread Point Preview:");
            ImGui.SetNextItemWidth(100f.Scale());
            DirectionalQuadrant debugDirectionalQuadrant = C.DebugDirectionalQuadrant;
            ImGuiEx.EnumCombo("Preview Cardinal Safe Zone", ref debugDirectionalQuadrant);
            C.DebugDirectionalQuadrant = debugDirectionalQuadrant;
            
            if (ImGui.Button("Preview Spread Point"))
            {
            safeAndUnsafePreviewActive = false;
            safeZonePreviewActive = false;
            stackPreviewActive = false;
            spreadPreviewActive = true;                           
                PreviewSpreadPoint(C.DebugDirectionalQuadrant);
            }
            ImGui.SameLine();            
            if (ImGui.Button("Reset Spread Preview"))
            {
                safeZonePreviewActive = false;
                spreadPreviewActive = false;
                stackPreviewActive = false;
                safeAndUnsafePreviewActive = false;
                DebugInfo.Clear();
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            }            
            
            ImGui.Separator();
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Stack Point Preview:");
            ImGui.SetNextItemWidth(100f.Scale());
            Quadrant debugStackQuadrant = C.DebugStackQuadrant;
            ImGuiEx.EnumCombo("Preview Quadrant", ref debugStackQuadrant);
            C.DebugStackQuadrant = debugStackQuadrant;
            
            if (ImGui.Button("Preview Stack Point"))
            {
            safeAndUnsafePreviewActive = false;
            safeZonePreviewActive = false;
            stackPreviewActive = true;
            spreadPreviewActive = false;                 
                PreviewStackPoint(C.DebugStackQuadrant);
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Reset Stack Preview"))
            {
                safeZonePreviewActive = false;
                spreadPreviewActive = false;
                stackPreviewActive = false;
                safeAndUnsafePreviewActive = false;
                DebugInfo.Clear();
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            }
            
            if (DebugInfo.Length > 0)
            {
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0, 1, 1, 1), "Debug Information:");
                ImGui.TextWrapped(DebugInfo.ToString());
            }
            
            ImGui.Separator();
            ImGuiEx.Text($"Shadows:\n{Shadows.Select(x => $"{x} - {GetUnsafeQuadrants(x.Position.ToVector2(), x.GetTransformationID() == 6).Print()}").Print("\n")}");
            ImGuiEx.Text($"""
            {SafeZone1}
            {SafeZone2}
            """);
            ImGuiEx.Text($"Order:\n{Order.Print("\n")}");
        }
    }

    public override void OnReset()
    {
        FirstShadowSafeZone = null;
        FourthShadowSafeZone = null;
        Casted.Clear();
        Order.Clear();
        SafeZone1 = null;
        SafeZone2 = null;
        safeZonePreviewActive = false;
        spreadPreviewActive = false;
        stackPreviewActive = false;
        safeAndUnsafePreviewActive = false;
        DebugInfo.Clear();
        lastFirstShadowSafeZone = null;
        lastFourthShadowSafeZone = null;
        lastSafeZone1 = null;
        lastSafeZone2 = null;
    }

    private static readonly Vector2 Center = new(100, 100);

    public enum Quadrant
    { SouthWest, NorthWest, NorthEast, SouthEast }

    private Quadrant[] FindSafeQuadrants(Vector2 pos1, bool right1, Vector2 pos2, bool right2)
    {
        return Enum.GetValues<Quadrant>().Where(x => !((Quadrant[])[.. GetUnsafeQuadrants(pos1, right1), .. GetUnsafeQuadrants(pos2, right2)]).Contains(x)).ToArray();
    }

    private List<Quadrant> GetUnsafeQuadrants(Vector2 pos, bool isAttackingRight)
    {
        List<Quadrant> ret = [];
        if(Vector2.Distance(pos, new(100, 88)) < 5f) ret = [Quadrant.NorthEast, Quadrant.SouthEast];
        else if(Vector2.Distance(pos, new(100, 112)) < 5f) ret = [Quadrant.NorthWest, Quadrant.SouthWest];
        else if(Vector2.Distance(pos, new(88, 100)) < 5f) ret = [Quadrant.NorthWest, Quadrant.NorthEast];
        else if(Vector2.Distance(pos, new(112, 100)) < 5f) ret = [Quadrant.SouthWest, Quadrant.SouthEast];
        else return [];
        
        return isAttackingRight ? 
            Enum.GetValues<Quadrant>().Where(x => !ret.Contains(x)).ToList() : 
            ret;
    }

    public class Config : IEzConfig
    {
        public Position Position = Position.Disabled;
        public Quadrant DebugQuadrant = Quadrant.SouthWest;
        public DirectionalQuadrant DebugDirectionalQuadrant = DirectionalQuadrant.South;
        public Quadrant DebugStackQuadrant = Quadrant.SouthWest;
    }
}
