using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class M8S_Quad_Beckon_Moonlight : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1263];

    public override void OnSetup()
    {

        Controller.RegisterElementFromCode($"BottomLeftUnsafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278190335,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"TopLeftUnsafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278190335,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"TopRightUnsafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278190335,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"BottomRightUnsafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278190335,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"AdditionalRotation\":4.712389,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"BottomLeftSafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278386432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"TopLeftSafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278386432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"AdditionalRotation\":1.5707964,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"TopRightSafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278386432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"AdditionalRotation\":3.1415927,\"FillStep\":99.0}");
        Controller.RegisterElementFromCode($"BottomRightSafe", "{\"Name\":\"\",\"type\":5,\"refX\":100.0,\"refY\":100.0,\"radius\":12.0,\"coneAngleMax\":90,\"color\":4278386432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":6.0,\"includeRotation\":true,\"AdditionalRotation\":4.712389,\"FillStep\":99.0}");
    }

    Quadrant? SafeZone1 = null;
    Quadrant? SafeZone2 = null;
    IBattleNpc[] Shadows => GetShadows().ToArray().OrderBy(x => Order.IndexOf(x.EntityId)).ToArray();
    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if(Shadows.Length == 0) return;
        if(Shadows.Length == 4)
        {
            SafeZone1 ??= FindSafeQuadrant(
                Shadows[0].Position.ToVector2(), Shadows[0].GetTransformationID() == 6,
                Shadows[1].Position.ToVector2(), Shadows[1].GetTransformationID() == 6);
            SafeZone2 ??= FindSafeQuadrant(
                Shadows[2].Position.ToVector2(), Shadows[2].GetTransformationID() == 6,
                Shadows[3].Position.ToVector2(), Shadows[3].GetTransformationID() == 6);
        }
        else if(Shadows.Length == 2)
        {
            SafeZone1 ??= FindSafeQuadrant(
                Shadows[0].Position.ToVector2(), Shadows[0].GetTransformationID() == 6,
                Shadows[1].Position.ToVector2(), Shadows[1].GetTransformationID() == 6);
        }
        if(this.NumActions < 4)
        {
            if(this.NumActions < 2)
            {
                {
                    if(Controller.TryGetElementByName($"{SafeZone1}Safe", out var e))
                    {
                        e.Enabled = true;
                    }
                }
                {
                    if(Controller.TryGetElementByName($"{SafeZone2}Unsafe", out var e))
                    {
                        e.Enabled = true;
                    }
                }
            }
            else
            {
                if(Controller.TryGetElementByName($"{SafeZone2}Safe", out var e))
                {
                    e.Enabled = true;
                }
            }
        }
    }

    HashSet<uint> Casted = [];
    List<uint> Order = [];
    int NumActions = 0;
    IEnumerable<IBattleNpc> GetShadows()
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

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action != null && set.SourceCharacter != null && set.SourceCharacter.Value.ObjectKind != FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Pc)
        {
            if(set.Action.Value.RowId == 41923) NumActions++;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text($"Shadows:\n{Shadows.Select(x => $"{x} - {GetAttackedQuadrants(GetRoundedDirection(x.Position.ToVector2()), x.GetTransformationID() == 6).Print()}").Print("\n")}");
        ImGuiEx.Text($"""
            {SafeZone1}
            {SafeZone2}
            """);
        ImGuiEx.Text($"Order:\n{Order.Print("\n")}");
    }

    public override void OnReset()
    {
        Casted.Clear();
        Order.Clear();
        SafeZone1 = null;
        SafeZone2 = null;
        NumActions = 0;
    }

    static readonly Vector2 Center = new(100, 100);

    public enum Quadrant
    {
        TopRight = 1,
        TopLeft = 2,
        BottomLeft = 3,
        BottomRight = 4
    }

    private enum Direction
    {
        North,
        East,
        South,
        West
    }

    public static Quadrant FindSafeQuadrant(Vector2 a1, bool attacksRight, Vector2 a2, bool attacksRight2)
    {
        var attackedQuadrants = new HashSet<Quadrant>();

        attackedQuadrants.UnionWith(GetAttackedQuadrants(GetRoundedDirection(a1), attacksRight));
        attackedQuadrants.UnionWith(GetAttackedQuadrants(GetRoundedDirection(a2), attacksRight2));

        foreach(Quadrant q in Enum.GetValues<Quadrant>())
        {
            if(!attackedQuadrants.Contains(q))
                return q;
        }

        throw new InvalidOperationException("No safe quadrant found.");
    }

    private static Direction GetRoundedDirection(Vector2 position)
    {
        Vector2 delta = Center - position; // looking toward the center
        double angleRad = Math.Atan2(delta.Y, delta.X);
        double angleDeg = angleRad * (180.0 / Math.PI);
        if(angleDeg < 0) angleDeg += 360;

        if(angleDeg >= 45 && angleDeg < 135) return Direction.North;
        if(angleDeg >= 135 && angleDeg < 225) return Direction.West;
        if(angleDeg >= 225 && angleDeg < 315) return Direction.South;
        return Direction.East;
    }

    private static Quadrant[] GetAttackedQuadrants(Direction dir, bool attacksRight)
    {
        return (dir, attacksRight) switch
        {
            (Direction.North, true) => [Quadrant.TopLeft, Quadrant.BottomLeft],
            (Direction.North, false) => [Quadrant.TopRight, Quadrant.BottomRight],

            (Direction.South, true) => [Quadrant.BottomRight, Quadrant.TopRight],
            (Direction.South, false) => [Quadrant.BottomLeft, Quadrant.TopLeft],

            (Direction.East, true) => [Quadrant.TopLeft, Quadrant.TopRight],
            (Direction.East, false) => [Quadrant.BottomLeft, Quadrant.BottomRight],

            (Direction.West, true) => [Quadrant.BottomLeft, Quadrant.BottomRight],
            (Direction.West, false) => [Quadrant.TopLeft, Quadrant.TopRight],

            _ => throw new ArgumentException("Invalid direction.")
        };
    }
}
