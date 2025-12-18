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
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using ECommons.GameHelpers.LegacyPlayer;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class M8S_Quad_Beckon_Moonlight : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1263];

    public override Metadata? Metadata => new(3, "NightmareXIV");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("SafeCone", """
            {"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":12.0,"coneAngleMax":90,"color":4278255376,"Filled":false,"fillIntensity":0.5,"thicc":6.0,"includeRotation":true,"FillStep":99.0}
            """);
        Controller.RegisterElementFromCode("UnsafeCone", """
            {"Name":"","type":5,"refX":100.0,"refY":100.0,"radius":12.0,"coneAngleMax":90,"color":4278225151,"Filled":false,"fillIntensity":0.5,"thicc":3.0,"includeRotation":true,"FillStep":99.0}
            """);
        Controller.RegisterElementFromCode($"{Position.Ranged_Left}", """
            {"Name":"ranged left","refX":88.5,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{Position.Ranged_Right}", """
            {"Name":"ranged right","refX":99.5,"refY":111.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{Position.Melee_Left}", """
            {"Name":"melee left","refX":95.0,"refY":100.5,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode($"{Position.Melee_Right}", """
            {"Name":"melee right","refX":99.5,"refY":105.0,"radius":0.3,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
        Controller.RegisterElementFromCode("Stack", """
            {"Name":"stack","refX":92.0,"refY":108.0,"radius":0.6,"color":3355505151,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true}
            """);
    }

    private Dictionary<Position, Vector2> SpreadPositions = new()
    {
        [Position.Ranged_Left] = new(88.5f, 100.5f),
        [Position.Ranged_Right] = new(99.5f, 111.5f),
        [Position.Melee_Left] = new(95.0f, 100.5f),
        [Position.Melee_Right] = new(99.5f, 105.0f),
    };

    private Vector2 StackPosition = new(92f, 108f);

    private Dictionary<Quadrant, int> Rotations = new()
    {
        [Quadrant.SouthWest] = 0,
        [Quadrant.NorthWest] = 90,
        [Quadrant.NorthEast] = 180,
        [Quadrant.SouthEast] = 270,
    };

    public enum Position { Disabled, Ranged_Right, Ranged_Left, Melee_Right, Melee_Left };

    private Quadrant? SafeZone1 = null;
    private Quadrant? SafeZone2 = null;
    private IBattleNpc[] Shadows => GetShadows().ToArray().OrderBy(x => Order.IndexOf(x.EntityId)).ToArray();
    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Shadows.Length == 0) return;
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
        if(NumActions < 4)
        {
            if(NumActions < 2)
            {
                {
                    if(SafeZone1 != null && Controller.TryGetElementByName($"SafeCone", out var e))
                    {
                        e.Enabled = true;
                        e.AdditionalRotation = Rotations[SafeZone1.Value].DegreesToRadians();
                        DrawStackSpread(SafeZone1.Value);
                    }
                }
                {
                    if(SafeZone2 != null && Controller.TryGetElementByName($"UnsafeCone", out var e))
                    {
                        e.Enabled = true;
                        e.AdditionalRotation = Rotations[SafeZone2.Value].DegreesToRadians();
                    }
                }
            }
            else
            {
                if(SafeZone2 != null && Controller.TryGetElementByName($"SafeCone", out var e))
                {
                    e.AdditionalRotation = Rotations[SafeZone2.Value].DegreesToRadians();
                    e.Enabled = true;
                    DrawStackSpread(SafeZone2.Value);
                }
            }
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

    private void DrawStackSpread(Quadrant quadrant)
    {
        {
            if(!EzThrottler.Check("BeckonStack") && Controller.TryGetElementByName("Stack", out var e))
            {
                e.Enabled = true;
                e.SetRefPosition(MathHelper.RotateWorldPoint(Center.ToVector3(), Rotations[quadrant].DegreesToRadians(), StackPosition.ToVector3()));
            }
        }
        {
            if(!EzThrottler.Check("BeckonSpread") && Controller.TryGetElementByName($"{C.Position}", out var e) && SpreadPositions.TryGetValue(C.Position, out var pos))
            {
                e.Enabled = true;
                e.SetRefPosition(MathHelper.RotateWorldPoint(Center.ToVector3(), Rotations[quadrant].DegreesToRadians(), pos.ToVector3()));
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
    }

    private Config C => Controller.GetConfig<Config>();
    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(200f.Scale());
        ImGuiEx.EnumCombo("Spread position", ref C.Position);
        if(ImGui.CollapsingHeader("Debug"))
        {
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
        Casted.Clear();
        Order.Clear();
        SafeZone1 = null;
        SafeZone2 = null;
    }

    private static readonly Vector2 Center = new(100, 100);

    private enum Quadrant
    { NorthWest, NorthEast, SouthEast, SouthWest }

    private Quadrant[] FindSafeQuadrants(Vector2 pos1, bool right1, Vector2 pos2, bool right2)
    {
        return Enum.GetValues<Quadrant>().Where(x => !((Quadrant[])[.. GetUnsafeQuadrants(pos1, right1), .. GetUnsafeQuadrants(pos2, right2)]).Contains(x)).ToArray();
    }

    private List<Quadrant> GetUnsafeQuadrants(Vector2 pos, bool isAttackingRight)
    {
        List<Quadrant> ret = [];
        if(Vector2.Distance(pos, new(100, 88)) < 5f)
        {
            //north
            ret = [Quadrant.NorthEast, Quadrant.SouthEast];
        }
        else if(Vector2.Distance(pos, new(100, 112)) < 5f)
        {
            //south
            ret = [Quadrant.NorthWest, Quadrant.SouthWest];
        }
        else if(Vector2.Distance(pos, new(88, 100)) < 5f)
        {
            //west
            ret = [Quadrant.NorthWest, Quadrant.NorthEast];
        }
        else if(Vector2.Distance(pos, new(112, 100)) < 5f)
        {
            //east
            ret = [Quadrant.SouthWest, Quadrant.SouthEast];
        }
        else
        {
            return [];
        }
        if(isAttackingRight)
        {
            return Enum.GetValues<Quadrant>().Where(x => !ret.Contains(x)).ToList();
        }
        return ret;
    }

    public class Config : IEzConfig
    {
        public Position Position = Position.Disabled;
    }
}
