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
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class M8S_Quad_Beckon_Moonlight_Universal : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1263];
    public override Metadata? Metadata => new(10, "NightmareXIV,Alex");

    public override void OnSetup()
    {
        // Register all possible elements, but enable/disable based on configuration
        Controller.RegisterElementFromCode("SafeCone", """
            {"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":12.0,"coneAngleMax":90,"color":4278255376,"Filled":false,"fillIntensity":0.5,"thicc":10.0,"includeRotation":true,"FillStep":99.0}
            """);
        Controller.RegisterElementFromCode("UnsafeCone", """
            {"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":12.0,"coneAngleMax":270,"color":4278225151,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"includeRotation":true,"FillStep":99.0}
            """);
        
        // Shadow reference point elements
        Controller.RegisterElementFromCode($"{PositionShadow.Far_Left}", """
            {"Name":"Far Left","refX":88.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{PositionShadow.Far_Right}", """
            {"Name":"Far Right","refX":111.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{PositionShadow.Bottom_Left}", """
            {"Name":"Bottom Left","refX":94.0,"refY":110.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{PositionShadow.Bottom_Right}", """
            {"Name":"Bottom Right","refX":94.0,"refY":110.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        
        // Safe zone reference point elements
        Controller.RegisterElementFromCode($"{PositionSafeZone.Ranged_Right}", """
            {"Name":"Ranged Right","refX":111.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{PositionSafeZone.Ranged_Left}", """
            {"Name":"Ranged Left","refX":88.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{PositionSafeZone.Melee_Right}", """
            {"Name":"Melee Right","refX":106.0,"refY":110.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{PositionSafeZone.Melee_Left}", """
            {"Name":"Melee Left","refX":94.0,"refY":110.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
        
        Controller.RegisterElementFromCode("Stack", """
            {"Name":"stack","refX":100.7,"refY":100.7,"radius":0.6,"color":3355505151,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"tether":true}
            """);
    }

    // Spread positions for shadow reference points
    private Dictionary<PositionShadow, Vector2> ShadowSpreadPositions = new()
    {
        [PositionShadow.Far_Left] = new(88.5f, 100.5f),
        [PositionShadow.Far_Right] = new(111.5f, 100.5f),
        [PositionShadow.Bottom_Left] = new(94f, 110f),
        [PositionShadow.Bottom_Right] = new(106f, 110f),
    };
    
    // Spread positions for safe zone reference points
    private Dictionary<PositionSafeZone, Vector2> SafeZoneSpreadPositions = new()
    {
        [PositionSafeZone.Ranged_Left] = new(88.5f, 100.5f),
        [PositionSafeZone.Ranged_Right] = new(99.5f, 111.5f),
        [PositionSafeZone.Melee_Left] = new(95.0f, 100.5f),
        [PositionSafeZone.Melee_Right] = new(99.5f, 105.0f),
    };

    // Stack position for shadow reference points
    private Vector2 ShadowStackPosition = new(99.3f, 100.7f);
    
    // Stack position for safe zone reference points
    private Vector2 SafeZoneStackPosition = new(92f, 108f);

    private Dictionary<Quadrant, int> Rotations = new()
    {
        [Quadrant.SouthWest] = 0,
        [Quadrant.NorthWest] = 90,
        [Quadrant.NorthEast] = 180,
        [Quadrant.SouthEast] = 270,
    };

    // Position options for shadow reference points
    public enum PositionShadow { Disabled, Far_Right, Far_Left, Bottom_Right, Bottom_Left };
    
    // Position options for safe zone reference points
    public enum PositionSafeZone { Disabled, Ranged_Right, Ranged_Left, Melee_Right, Melee_Left };
    
    public enum DirectionalQuadrant { North, South, East, West }
    
    private DirectionalQuadrant? FirstShadowPosition = null;
    private bool? FirstShadowAttackRight = null;
    private DirectionalQuadrant? FourthShadowPosition = null;
    private bool? FourthShadowAttackRight = null;
    
    // New field: stores safe zone directions for first and fourth shadows
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
    
    private DirectionalQuadrant? lastFirstShadowPosition = null;
    private bool? lastFirstShadowAttackRight = null;
    private DirectionalQuadrant? lastFourthShadowPosition = null;
    private bool? lastFourthShadowAttackRight = null;
    private Quadrant? lastSafeZone1 = null;
    private Quadrant? lastSafeZone2 = null;
    
    private bool safeZonePreviewActive = false;
    private bool spreadPreviewActive = false;
    private bool stackPreviewActive = false;
    private bool safeAndUnsafePreviewActive = false;
    private Quadrant previewSafeZone = Quadrant.SouthWest;
    private DirectionalQuadrant previewSpreadDirection = DirectionalQuadrant.South;
    private Quadrant previewStackQuadrant = Quadrant.SouthWest;
    
    // New: track if info has been printed
    private bool firstShadowInfoPrinted = false;
    private bool fourthShadowInfoPrinted = false;
    private bool safeZone1Printed = false;
    private bool safeZone2Printed = false;
    
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
        
        if(Shadows.Length >= 1 && FirstShadowPosition == null)
        {
            FirstShadowPosition = GetPositionDirection(Shadows[0].Position.ToVector2());
            FirstShadowAttackRight = Shadows[0].GetTransformationID() == 6;
            // Calculate and store safe zone for first shadow
            FirstShadowSafeZone = GetSafeQuadrant(FirstShadowPosition.Value, FirstShadowAttackRight.Value);
        }
        
        if(Shadows.Length >= 4 && FourthShadowPosition == null)
        {
            FourthShadowPosition = GetPositionDirection(Shadows[3].Position.ToVector2());
            FourthShadowAttackRight = Shadows[3].GetTransformationID() == 6;
            // Calculate and store safe zone for fourth shadow
            FourthShadowSafeZone = GetSafeQuadrant(FourthShadowPosition.Value, FourthShadowAttackRight.Value);
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
        
        // Output info after fourth shadow and second safe zone appear
        if (SafeZone2 != null && FourthShadowPosition != null && !fourthShadowInfoPrinted && !safeZone2Printed)
        {
            PrintShadowInfo();
            fourthShadowInfoPrinted = true;
            safeZone2Printed = true;
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
    
    // Print shadow and safe zone info to chat channel
    private void PrintShadowInfo()
    {
        if (FirstShadowPosition != null && FirstShadowAttackRight != null && FirstShadowSafeZone != null)
        {
            DirectionalQuadrant dangerZone = GetOppositeDirection(FirstShadowSafeZone.Value);
            string positionName = ConvertDirectionToName(FirstShadowPosition.Value);
            string safeName = ConvertDirectionToName(FirstShadowSafeZone.Value);
            string dangerName = ConvertDirectionToName(dangerZone);
            
            PrintToChat($"1st Shadow: {positionName} | Attack:{(FirstShadowAttackRight.Value ? "Right" : "Left")} | Safe:{safeName} | Danger:{dangerName}");
        }
        
        if (FourthShadowPosition != null && FourthShadowAttackRight != null && FourthShadowSafeZone != null)
        {
            DirectionalQuadrant dangerZone = GetOppositeDirection(FourthShadowSafeZone.Value);
            string positionName = ConvertDirectionToName(FourthShadowPosition.Value);
            string safeName = ConvertDirectionToName(FourthShadowSafeZone.Value);
            string dangerName = ConvertDirectionToName(dangerZone);
            
            PrintToChat($"4th Shadow: {positionName} | Attack:{(FourthShadowAttackRight.Value ? "Right" : "Left")} | Safe:{safeName} | Danger:{dangerName}");
        }
        
        if (SafeZone1 != null)
        {
            string quadrantName = ConvertQuadrantToName(SafeZone1.Value);
            PrintToChat($"Safe Zone 1: {quadrantName}");
        }
        
        if (SafeZone2 != null)
        {
            string quadrantName = ConvertQuadrantToName(SafeZone2.Value);
            PrintToChat($"Safe Zone 2: {quadrantName}");
        }
    }
    
    // Output to specified chat channel based on configuration
    private void PrintToChat(string message)
    {
        if (C.OutputChannel == OutputChannel.None) return;
        
        XivChatType channel = XivChatType.Echo; // Default to Echo channel
        switch (C.OutputChannel)
        {
            case OutputChannel.Echo:
                channel = XivChatType.Echo;
                break;
            case OutputChannel.Debug:
                channel = XivChatType.Debug;
                break;
            case OutputChannel.Party:
                channel = XivChatType.Party;
                break;
            case OutputChannel.Alliance:
                channel = XivChatType.Alliance;
                break;
        }
        
        Svc.Chat.Print(new XivChatEntry
        {
            Message = message,
            Type = channel
        });
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
        if (FirstShadowPosition != lastFirstShadowPosition || 
            FirstShadowAttackRight != lastFirstShadowAttackRight ||
            FourthShadowPosition != lastFourthShadowPosition ||
            FourthShadowAttackRight != lastFourthShadowAttackRight ||
            SafeZone1 != lastSafeZone1 || 
            SafeZone2 != lastSafeZone2)
        {
            DebugInfo.Clear();
            
            if (FirstShadowPosition != null || FourthShadowPosition != null)
            {
                DebugInfo.AppendLine("=== Spread Point Information ===");
                
                if (FirstShadowPosition != null && FirstShadowAttackRight != null && FirstShadowSafeZone != null)
                {
                    DirectionalQuadrant dangerZone = GetOppositeDirection(FirstShadowSafeZone.Value);
                    
                    DebugInfo.AppendLine($"1st Shadow Position: {FirstShadowPosition}");
                    DebugInfo.AppendLine($"  Attack Direction: {(FirstShadowAttackRight.Value ? "Right" : "Left")}");
                    DebugInfo.AppendLine($"  Safe Zone: {FirstShadowSafeZone}");
                    DebugInfo.AppendLine($"  Danger Zone: {dangerZone}");
                }
                
                if (FourthShadowPosition != null && FourthShadowAttackRight != null && FourthShadowSafeZone != null)
                {
                    DirectionalQuadrant dangerZone = GetOppositeDirection(FourthShadowSafeZone.Value);
                    
                    DebugInfo.AppendLine($"4th Shadow Position: {FourthShadowPosition}");
                    DebugInfo.AppendLine($"  Attack Direction: {(FourthShadowAttackRight.Value ? "Right" : "Left")}");
                    DebugInfo.AppendLine($"  Safe Zone: {FourthShadowSafeZone}");
                    DebugInfo.AppendLine($"  Danger Zone: {dangerZone}");
                }
                
                Vector2 spreadPos = GetCurrentSpreadPosition();
                int rotationDegrees = GetCurrentSpreadRotation();
                Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                    Center.ToVector3(), 
                    rotationDegrees.DegreesToRadians(), 
                    spreadPos.ToVector3());
                
                DebugInfo.AppendLine($"Current Spread Point: {GetCurrentPositionName()}");
                DebugInfo.AppendLine($"  Original Coordinates: {spreadPos.X:F2}, {spreadPos.Y:F2}");
                DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedPos.X:F2}, {rotatedPos.Z:F2}");
            }
            
            if (SafeZone1 != null || SafeZone2 != null)
            {
                DebugInfo.AppendLine("\n=== Stack Point Information ===");
                
                if (SafeZone1 != null)
                {
                    Vector2 stackPos = GetCurrentStackPosition();
                    Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
                        Center.ToVector3(), 
                        Rotations[SafeZone1.Value].DegreesToRadians(), 
                        stackPos.ToVector3());
                    
                    DebugInfo.AppendLine($"Safe Zone 1: {SafeZone1}");
                    DebugInfo.AppendLine($"  Rotation: {Rotations[SafeZone1.Value]}°");
                    DebugInfo.AppendLine($"  Stack Point Coordinates: {rotatedStackPos.X:F2}, {rotatedStackPos.Z:F2}");
                }
                
                if (SafeZone2 != null)
                {
                    Vector2 stackPos = GetCurrentStackPosition();
                    Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
                        Center.ToVector3(), 
                        Rotations[SafeZone2.Value].DegreesToRadians(), 
                        stackPos.ToVector3());
                    
                    DebugInfo.AppendLine($"Safe Zone 2: {SafeZone2}");
                    DebugInfo.AppendLine($"  Rotation: {Rotations[SafeZone2.Value]}°");
                    DebugInfo.AppendLine($"  Stack Point Coordinates: {rotatedStackPos.X:F2}, {rotatedStackPos.Z:F2}");
                }
            }
            
            lastFirstShadowPosition = FirstShadowPosition;
            lastFirstShadowAttackRight = FirstShadowAttackRight;
            lastFourthShadowPosition = FourthShadowPosition;
            lastFourthShadowAttackRight = FourthShadowAttackRight;
            lastSafeZone1 = SafeZone1;
            lastSafeZone2 = SafeZone2;
        }
    }
    
    // Get current spread position (based on reference type)
    private Vector2 GetCurrentSpreadPosition()
    {
        if (C.SpreadBase == SpreadBaseType.SafeZone)
        {
            return SafeZoneSpreadPositions[C.PositionSafeZone];
        }
        else
        {
            return ShadowSpreadPositions[C.PositionShadow];
        }
    }
    
    // Get current stack position (based on reference type)
    private Vector2 GetCurrentStackPosition()
    {
        return C.SpreadBase == SpreadBaseType.SafeZone ? SafeZoneStackPosition : ShadowStackPosition;
    }
    
    // Get current position name (for debug display)
    private string GetCurrentPositionName()
    {
        if (C.SpreadBase == SpreadBaseType.SafeZone)
        {
            return C.PositionSafeZone.ToString();
        }
        else
        {
            return C.PositionShadow.ToString();
        }
    }
    
    // Convert direction to name (shadow positions)
    private string ConvertDirectionToName(DirectionalQuadrant dir)
    {
        return dir switch
        {
            DirectionalQuadrant.North => "North (A)",
            DirectionalQuadrant.South => "South (C)",
            DirectionalQuadrant.East => "East (B)",
            DirectionalQuadrant.West => "West (D)",
            _ => "Unknown Direction"
        };
    }
    
    // Convert quadrant to name (safe zones)
    private string ConvertQuadrantToName(Quadrant quad)
    {
        return quad switch
        {
            Quadrant.NorthWest => "NW (1)",
            Quadrant.NorthEast => "NE (2)",
            Quadrant.SouthEast => "SE (3)",
            Quadrant.SouthWest => "SW (4)",
            _ => "Unknown Area"
        };
    }
    
    private DirectionalQuadrant GetPositionDirection(Vector2 position)
    {
        if (Vector2.Distance(position, new(100, 88)) < 5f) return DirectionalQuadrant.North;
        else if (Vector2.Distance(position, new(100, 112)) < 5f) return DirectionalQuadrant.South;
        else if (Vector2.Distance(position, new(112, 100)) < 5f) return DirectionalQuadrant.East;
        else if (Vector2.Distance(position, new(88, 100)) < 5f) return DirectionalQuadrant.West;
        else return DirectionalQuadrant.North;
    }
    
    private DirectionalQuadrant GetSafeQuadrant(DirectionalQuadrant position, bool isAttackingRight)
    {
        return position switch
        {
            DirectionalQuadrant.North => isAttackingRight ? DirectionalQuadrant.East : DirectionalQuadrant.West,
            DirectionalQuadrant.South => isAttackingRight ? DirectionalQuadrant.West : DirectionalQuadrant.East,
            DirectionalQuadrant.East => isAttackingRight ? DirectionalQuadrant.South : DirectionalQuadrant.North,
            DirectionalQuadrant.West => isAttackingRight ? DirectionalQuadrant.North : DirectionalQuadrant.South,
            _ => DirectionalQuadrant.North
        };
    }
    
    private DirectionalQuadrant GetOppositeDirection(DirectionalQuadrant quadrant)
    {
        return quadrant switch
        {
            DirectionalQuadrant.North => DirectionalQuadrant.South,
            DirectionalQuadrant.South => DirectionalQuadrant.North,
            DirectionalQuadrant.East => DirectionalQuadrant.West,
            DirectionalQuadrant.West => DirectionalQuadrant.East,
            _ => DirectionalQuadrant.North
        };
    }
    
    private int GetCurrentSpreadRotation()
    {
        // Select spread reference based on configuration
        DirectionalQuadrant? baseDirection = null;
        
        if (NumActions < 2)
        {
            // First spread
            if (C.SpreadBase == SpreadBaseType.Shadow)
            {
                baseDirection = FirstShadowPosition;
            }
            else
            {
                baseDirection = FirstShadowSafeZone;
            }
        }
        else
        {
            // Fourth spread
            if (C.SpreadBase == SpreadBaseType.Shadow)
            {
                baseDirection = FourthShadowPosition;
            }
            else
            {
                baseDirection = FourthShadowSafeZone;
            }
        }
        
        if (baseDirection != null && DirectionalRotations.ContainsKey(baseDirection.Value))
        {
            return DirectionalRotations[baseDirection.Value];
        }
        return 0;
    }

    private void DrawSpreadPoints()
    {
        if (FirstShadowPosition == null && FourthShadowPosition == null) 
            return;
        
        int rotationDegrees = GetCurrentSpreadRotation();
        Vector2 spreadPos = GetCurrentSpreadPosition();
        
        string elementName = "";
        if (C.SpreadBase == SpreadBaseType.SafeZone)
        {
            elementName = C.PositionSafeZone.ToString();
        }
        else
        {
            elementName = C.PositionShadow.ToString();
        }
        
        if(!EzThrottler.Check("BeckonSpread") && 
           Controller.TryGetElementByName($"{elementName}", out var spread))
        {
            spread.Enabled = true;
            Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                rotationDegrees.DegreesToRadians(), 
                spreadPos.ToVector3());
            spread.SetRefPosition(rotatedPos);
        }
    }
        
    private void DrawStackPoint(Quadrant quadrant)
    {
        if(!EzThrottler.Check("BeckonStack") && 
           Controller.TryGetElementByName("Stack", out var stack))
        {
            stack.Enabled = true;
            Vector2 stackPos = GetCurrentStackPosition();
            Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                Rotations[quadrant].DegreesToRadians(),
                stackPos.ToVector3());
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
        
        string elementName = "";
        if (C.SpreadBase == SpreadBaseType.SafeZone)
        {
            elementName = C.PositionSafeZone.ToString();
        }
        else
        {
            elementName = C.PositionShadow.ToString();
        }
        
        if(Controller.TryGetElementByName($"{elementName}", out var spread))
        {
            spread.Enabled = true;
            Vector2 spreadPos = GetCurrentSpreadPosition();
            Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                Rotations[quadrant].DegreesToRadians(), 
                spreadPos.ToVector3());
            spread.SetRefPosition(rotatedPos);
            
            DebugInfo.AppendLine($"Spread Point: {elementName}");
            DebugInfo.AppendLine($"  Original Coordinates: {spreadPos.X:F2}, {spreadPos.Y:F2}");
            DebugInfo.AppendLine($"  Rotated Coordinates: {rotatedPos.X:F2}, {rotatedPos.Z:F2}");
        }
        
        if(Controller.TryGetElementByName("Stack", out var stack))
        {
            stack.Enabled = true;
            Vector2 stackPos = GetCurrentStackPosition();
            Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                Rotations[quadrant].DegreesToRadians(), 
                stackPos.ToVector3());
            stack.SetRefPosition(rotatedStackPos);
            
            DebugInfo.AppendLine($"Stack Point:");
            DebugInfo.AppendLine($"  Original Coordinates: {stackPos.X:F2}, {stackPos.Y:F2}");
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
        
        string elementName = "";
        if (C.SpreadBase == SpreadBaseType.SafeZone)
        {
            elementName = C.PositionSafeZone.ToString();
        }
        else
        {
            elementName = C.PositionShadow.ToString();
        }
        
        if(Controller.TryGetElementByName($"{elementName}", out var spread))
        {
            spread.Enabled = true;
            Vector2 spreadPos = GetCurrentSpreadPosition();
            Vector3 rotatedPos = MathHelper.RotateWorldPoint(
                Center.ToVector3(), 
                rotationDegrees.DegreesToRadians(), 
                spreadPos.ToVector3());
            spread.SetRefPosition(rotatedPos);
            
            DebugInfo.AppendLine($"Spread Point: {elementName}");
            DebugInfo.AppendLine($"  Original Coordinates: {spreadPos.X:F2}, {spreadPos.Y:F2}");
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
        
        Vector2 stackPos = GetCurrentStackPosition();
        Vector3 rotatedStackPos = MathHelper.RotateWorldPoint(
            Center.ToVector3(), 
            Rotations[quadrant].DegreesToRadians(), 
            stackPos.ToVector3());
        
        DebugInfo.AppendLine($"Safe Quadrant: {quadrant}");
        DebugInfo.AppendLine($"  Rotation: {Rotations[quadrant]}°");
        DebugInfo.AppendLine($"  Original Coordinates: {stackPos.X:F2}, {stackPos.Y:F2}");
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
        ImGui.Text("Spread Reference Type:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f.Scale());
        SpreadBaseType spreadBase = C.SpreadBase;
        ImGuiEx.EnumCombo("##ReferenceType", ref spreadBase);
        C.SpreadBase = spreadBase;
        ImGui.SameLine();
        ImGuiEx.HelpMarker("Provides support for both \"Game 8\" and raidplan \"Quad\" strategies");

        
        if (C.SpreadBase == SpreadBaseType.Shadow)
        {
            ImGui.Text("Shadow Position:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f.Scale());
            PositionShadow positionShadow = C.PositionShadow;
            ImGuiEx.EnumCombo("##ShadowPosition", ref positionShadow);
            C.PositionShadow = positionShadow;
            // Add hover tip for shadow positions
            ImGui.SameLine();
            ImGuiEx.HelpMarker("Based on Japanese strategy \"Game 8\"\nLink: https://game8.jp/ff14/681843");
        }
        else
        {
            ImGui.Text("Safe Zone Position:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f.Scale());
            PositionSafeZone positionSafeZone = C.PositionSafeZone;
            ImGuiEx.EnumCombo("##SafeZonePosition", ref positionSafeZone);
            C.PositionSafeZone = positionSafeZone;
            // Add hover tip for safe zone positions
            ImGui.SameLine();
            ImGuiEx.HelpMarker("Based on raidplan \"Quad\"\nLink: https://raidplan.io/plan/WFsLBku1C9Iyxneu");
            
        }
        
        ImGui.Text("Output Channel:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f.Scale());
        OutputChannel outputChannel = C.OutputChannel;
        ImGuiEx.EnumCombo("##OutputChannel", ref outputChannel);
        C.OutputChannel = outputChannel;
        
        if(FirstShadowPosition != null && FirstShadowAttackRight != null && FirstShadowSafeZone != null)
        {
            DirectionalQuadrant dangerZone = GetOppositeDirection(FirstShadowSafeZone.Value);
            string positionName = ConvertDirectionToName(FirstShadowPosition.Value);
            string safeName = ConvertDirectionToName(FirstShadowSafeZone.Value);
            string dangerName = ConvertDirectionToName(dangerZone);
            ImGuiEx.Text($"1st Shadow: {positionName} | Attack:{(FirstShadowAttackRight.Value ? "Right" : "Left")} | Safe:{safeName} | Danger:{dangerName}");
        }
        else
        {
            ImGuiEx.Text($"1st Shadow Position: Undetermined");
        }
        
        if(FourthShadowPosition != null && FourthShadowAttackRight != null && FourthShadowSafeZone != null)
        {
            DirectionalQuadrant dangerZone = GetOppositeDirection(FourthShadowSafeZone.Value);
            string positionName = ConvertDirectionToName(FourthShadowPosition.Value);
            string safeName = ConvertDirectionToName(FourthShadowSafeZone.Value);
            string dangerName = ConvertDirectionToName(dangerZone);
            ImGuiEx.Text($"4th Shadow: {positionName} | Attack:{(FourthShadowAttackRight.Value ? "Right" : "Left")} | Safe:{safeName} | Danger:{dangerName}");
        }
        else
        {
            ImGuiEx.Text($"4th Shadow Position: Undetermined");
        }
        
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
        FirstShadowPosition = null;
        FirstShadowAttackRight = null;
        FourthShadowPosition = null;
        FourthShadowAttackRight = null;
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
        lastFirstShadowPosition = null;
        lastFirstShadowAttackRight = null;
        lastFourthShadowPosition = null;
        lastFourthShadowAttackRight = null;
        lastSafeZone1 = null;
        lastSafeZone2 = null;
        
        // Reset output flags
        firstShadowInfoPrinted = false;
        fourthShadowInfoPrinted = false;
        safeZone1Printed = false;
        safeZone2Printed = false;
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

    // New enum types
    public enum OutputChannel { None, Echo, Debug, Party, Alliance }
    public enum SpreadBaseType { Shadow, SafeZone }
    
    public class Config : IEzConfig
    {
        public SpreadBaseType SpreadBase = SpreadBaseType.Shadow; // Default shadow reference
        
        // Shadow position options
        public PositionShadow PositionShadow = PositionShadow.Disabled;
        
        // Safe zone position options
        public PositionSafeZone PositionSafeZone = PositionSafeZone.Disabled;
        
        // Other configs
        public OutputChannel OutputChannel = OutputChannel.Echo; // Default Echo channel
        public Quadrant DebugQuadrant = Quadrant.SouthWest;
        public DirectionalQuadrant DebugDirectionalQuadrant = DirectionalQuadrant.South;
        public Quadrant DebugStackQuadrant = Quadrant.SouthWest;
    }
}