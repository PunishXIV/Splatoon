using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.Configuration;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.MathHelpers;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P2_Mirror_Mirror_Guide : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [1238];

    #endregion

    #region Constants

    private const uint CastScythe = 40203;
    private const uint CastMirrorScythe = 40205;
    private const int ConeCount = 8;
    private const int SpotCount = 7;
    private const float TieDistance = 1f;

    private static readonly Vector3 Center = new(100f, 0f, 100f);

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;
    private bool BossGroup => C.FirstGroup == FirstGroup.Boss;

    #endregion

    #region State

    private Step _step = Step.Idle;
    private Direction? _blue;
    private uint _bossId;
    private readonly List<Direction> _reds = [];

    #endregion

    #region Types

    private sealed class Config : IEzConfig
    {
        public FirstGroup FirstGroup = FirstGroup.BlueMirror;
        public ConeBait FirstConeBait = ConeBait.Left;
        public ConeBait SecondConeBait = ConeBait.Left;
        public SecondMirrorRule SecondMirrorRule = SecondMirrorRule.NearClockwise;

        public bool EnableVisualizer = true;
        public bool ShowScytheKick = true;
        public bool ShowCone = true;
        public ConeView ConeView = ConeView.MyGroup;
        public bool ShowAllPosition;
        public bool ShowMyPixel = true;
    }

    private enum FirstGroup
    {
        BlueMirror,
        Boss,
    }

    private enum ConeBait
    {
        Left,
        FrontLeft,
        FrontRight,
        Right,
    }

    private enum SecondMirrorRule
    {
        NearClockwise,
        NearCounterClockwise,
        Clockwise,
        CounterClockwise,
    }

    private enum ConeView
    {
        MyOnly,
        MyGroup,
        All,
    }

    private enum Direction
    {
        North = 1,
        NorthEast = 2,
        East = 3,
        SouthEast = 4,
        South = 5,
        SouthWest = 6,
        West = 7,
        NorthWest = 8,
    }

    private enum Step
    {
        Idle,
        MirrorCast,
        ScytheCast,
        MirrorScythe,
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", GuideElement(true), overwrite: true);
        for(var i = 1; i <= SpotCount; i++)
            Controller.RegisterElement($"Spot{i}", GuideElement(false), overwrite: true);

        Controller.RegisterElement("ScytheBoss", new Element(1)
        {
            Enabled = false,
            radius = 4f,
            Donut = 16f,
            color = 4278210815u,
            fillIntensity = 0.203f,
            refActorComparisonType = 2,
        }, overwrite: true);
        Controller.RegisterElement("ScytheBlue", ScytheAtPoint(), overwrite: true);
        Controller.RegisterElement("ScytheRed1", ScytheAtPoint(), overwrite: true);
        Controller.RegisterElement("ScytheRed2", ScytheAtPoint(), overwrite: true);

        for(var i = 1; i <= ConeCount; i++)
        {
            Controller.RegisterElement($"Cone{i}", new Element(5)
            {
                Enabled = false,
                radius = 40f,
                coneAngleMin = -15,
                coneAngleMax = 15,
                color = 3372220160u,
                fillIntensity = 0.205f,
                includeRotation = true,
            }, overwrite: true);
        }

        Controller.RegisterElement("My Pixel", new Element(1)
        {
            Enabled = false,
            radius = 0f,
            thicc = 10f,
            color = 0xc80000ff,
            refActorType = 1,
        }, overwrite: true);
    }

    public override void OnUpdate()
    {
        HideAll();

        switch(_step)
        {
            case Step.MirrorCast:
                ShowGroupPoint();
                break;
            case Step.ScytheCast:
                ShowFirstBait();
                ShowFirstRanges();
                break;
            case Step.MirrorScythe:
                ShowRedBait();
                ShowRedRanges();
                break;
        }

        if(_step != Step.Idle && C.EnableVisualizer && C.ShowMyPixel && Controller.TryGetElementByName("My Pixel", out var pixel))
            pixel.Enabled = true;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40179)
        {
            ClearMechanic();
            _step = Step.MirrorCast;
            return;
        }

        if(_step == Step.MirrorCast && castId == CastScythe)
        {
            _step = Step.ScytheCast;
            _bossId = source;
            return;
        }

        if(_step == Step.ScytheCast && castId == CastMirrorScythe)
            _step = Step.MirrorScythe;
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(_step == Step.Idle || position is < 1 or > 8)
            return;

        var direction = (Direction)position;
        if(data1 == 1 && data2 == 2)
            _blue = direction;
        else if(data1 == 256 && data2 == 512 && _reds.Count < 2 && !_reds.Contains(direction))
            _reds.Add(direction);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null)
            return;

        var id = set.Action.Value.RowId;
        if(_step == Step.ScytheCast && id == CastScythe)
            _step = Step.MirrorScythe;
        else if(_step == Step.MirrorScythe && id == CastMirrorScythe)
            ClearMechanic();
    }

    public override void OnReset()
    {
        ClearMechanic();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextDisabled("General");
        ImGui.Separator();
        ImGui.Text("1st Spread (△ : Boss, 〇 : Blue Mirror)");
        DrawBaitGrid("Boss", "△", FirstGroup.Boss, ref C.FirstConeBait);
        DrawBaitGrid("Mirror", "〇", FirstGroup.BlueMirror, ref C.FirstConeBait);
        ImGui.Text("2nd Spread (〇 : Red Mirror)");
        DrawBaitGrid("Second", "〇", null, ref C.SecondConeBait);
        ImGui.Spacing();
        ImGui.Text("2nd Mirror Rule (North is the side opposite the blue mirror.)");
        DrawMirrorRule(SecondMirrorRule.NearClockwise, "Boss takes the closer red mirror. On a tie, the first clockwise is Boss and the second is Blue Mirror.");
        DrawMirrorRule(SecondMirrorRule.NearCounterClockwise, "Boss takes the closer red mirror. On a tie, the first counter-clockwise is Boss and the second is Blue Mirror.");
        DrawMirrorRule(SecondMirrorRule.Clockwise, "The first clockwise is Boss, and the second is Blue Mirror.");
        DrawMirrorRule(SecondMirrorRule.CounterClockwise, "The first counter-clockwise is Boss, and the second is Blue Mirror.");

        ImGui.Spacing();
        ImGui.TextDisabled("Visualizer");
        ImGui.Separator();
        ImGui.Checkbox("Enable Visualizer", ref C.EnableVisualizer);
        ImGui.Indent();
        if(!C.EnableVisualizer)
            ImGui.BeginDisabled();
        ImGui.Checkbox("Show Scythe Kick AOE", ref C.ShowScytheKick);
        ImGui.Checkbox("Show Cone", ref C.ShowCone);
        ImGui.Indent();
        if(!C.EnableVisualizer || !C.ShowCone)
            ImGui.BeginDisabled();
        if(ImGui.RadioButton("My Cone Only", C.ConeView == ConeView.MyOnly))
            C.ConeView = ConeView.MyOnly;
        if(ImGui.RadioButton("My Group Cone Only", C.ConeView == ConeView.MyGroup))
            C.ConeView = ConeView.MyGroup;
        if(ImGui.RadioButton("All Cone", C.ConeView == ConeView.All))
            C.ConeView = ConeView.All;
        if(!C.EnableVisualizer || !C.ShowCone)
            ImGui.EndDisabled();
        ImGui.Unindent();
        ImGui.Checkbox("Show All Position", ref C.ShowAllPosition);
        ImGui.Checkbox("Show My Pixel", ref C.ShowMyPixel);
        if(!C.EnableVisualizer)
            ImGui.EndDisabled();
        ImGui.Unindent();
    }

    #endregion

    #region Private Method

    // Drops the mechanic back to the initial step.
    private void ClearMechanic()
    {
        _step = Step.Idle;
        _blue = null;
        _bossId = 0;
        _reds.Clear();
        HideAll();
    }

    // Hides every element this script registered.
    private void HideAll()
    {
        foreach(var element in Controller.GetRegisteredElements().Values)
            element.Enabled = false;
    }

    // Assigns the two reds by the selected mirror rule.
    private (Direction Boss, Direction Blue)? AssignReds()
    {
        if(_blue == null || _reds.Count < 2)
            return null;

        var blue = _blue.Value;
        var redA = _reds[0];
        var redB = _reds[1];
        return C.SecondMirrorRule switch
        {
            SecondMirrorRule.NearClockwise => RuleNearClockwise(blue, redA, redB),
            SecondMirrorRule.NearCounterClockwise => RuleNearCounterClockwise(blue, redA, redB),
            SecondMirrorRule.Clockwise => RuleClockwise(blue, redA, redB),
            SecondMirrorRule.CounterClockwise => RuleCounterClockwise(blue, redA, redB),
        };
    }

    // Boss takes the closer red. On a tie, the first clockwise from north is Boss.
    private static (Direction Boss, Direction Blue) RuleNearClockwise(Direction blue, Direction redA, Direction redB)
    {
        var north = Opposite(blue);
        var northPos = MirrorPosition(north);
        var distanceA = DistanceXz(northPos, MirrorPosition(redA));
        var distanceB = DistanceXz(northPos, MirrorPosition(redB));
        if(MathF.Abs(distanceA - distanceB) <= TieDistance)
        {
            var ordered = OrderClockwise([redA, redB], MirrorAngle(north));
            return (ordered[0], ordered[1]);
        }

        return distanceA < distanceB ? (redA, redB) : (redB, redA);
    }

    // Boss takes the closer red. On a tie, the first counter-clockwise from north is Boss.
    private static (Direction Boss, Direction Blue) RuleNearCounterClockwise(Direction blue, Direction redA, Direction redB)
    {
        var north = Opposite(blue);
        var northPos = MirrorPosition(north);
        var distanceA = DistanceXz(northPos, MirrorPosition(redA));
        var distanceB = DistanceXz(northPos, MirrorPosition(redB));
        if(MathF.Abs(distanceA - distanceB) <= TieDistance)
        {
            var ordered = OrderClockwise([redA, redB], MirrorAngle(north));
            return (ordered[1], ordered[0]);
        }

        return distanceA < distanceB ? (redA, redB) : (redB, redA);
    }

    // The first clockwise from north is Boss.
    private static (Direction Boss, Direction Blue) RuleClockwise(Direction blue, Direction redA, Direction redB)
    {
        var ordered = OrderClockwise([redA, redB], MirrorAngle(Opposite(blue)));
        return (ordered[0], ordered[1]);
    }

    // The first counter-clockwise from north is Boss.
    private static (Direction Boss, Direction Blue) RuleCounterClockwise(Direction blue, Direction redA, Direction redB)
    {
        var ordered = OrderClockwise([redA, redB], MirrorAngle(Opposite(blue)));
        return (ordered[1], ordered[0]);
    }

    // Orders red mirrors clockwise from a compass degree.
    private static List<Direction> OrderClockwise(IEnumerable<Direction> reds, float origin)
        => reds
            .OrderBy(direction => NormalizeAngle(MirrorAngle(direction) - origin))
            .ToList();

    // Returns the compass point opposite a mirror.
    private static Direction Opposite(Direction direction)
        => (Direction)(((int)direction + 3) % 8 + 1);

    // Orders the red mirrors clockwise from the gap between them.
    private List<Direction> ClockwiseFromGap()
    {
        var first = MirrorAngle(_reds[0]);
        var second = MirrorAngle(_reds[1]);
        var turn = NormalizeAngle(second - first);
        float gap;
        if(turn < 180f)
            gap = NormalizeAngle(first + turn * 0.5f);
        else if(turn > 180f)
            gap = NormalizeAngle(second + (360f - turn) * 0.5f);
        else
        {
            var bossSide = MirrorAngle(Opposite(_blue!.Value));
            var east = NormalizeAngle(first + 90f);
            var west = NormalizeAngle(first - 90f);
            gap = AngleDelta(east, bossSide) <= AngleDelta(west, bossSide) ? east : west;
        }

        return OrderClockwise(_reds, gap);
    }

    // Compass degrees of a mirror. North is 0, and positive degrees go clockwise.
    private static float MirrorAngle(Direction direction)
        => ((int)direction - 1) * 45f;

    // Shortest gap between two compass degrees.
    private static float AngleDelta(float left, float right)
    {
        var delta = MathF.Abs(NormalizeAngle(left - right));
        return MathF.Min(delta, 360f - delta);
    }

    // Returns the bait angle. Red clocks use the gap order, and other clocks use the shared angles.
    private static float SlotOffset(ConeBait slot, Direction? red, List<Direction>? clockwise)
    {
        var frontLeft = -40f;
        var frontRight = 40f;
        if(clockwise != null && clockwise.Count >= 2 && red != null)
        {
            var first = red == clockwise[0];
            frontLeft = first ? -45f : 0f;
            frontRight = first ? 0f : 45f;
        }

        return slot switch
        {
            ConeBait.Left => -75f,
            ConeBait.FrontLeft => frontLeft,
            ConeBait.FrontRight => frontRight,
            ConeBait.Right => 75f,
        };
    }

    // Places one compass direction on the arena ring.
    private static Vector3 MirrorPosition(Direction direction)
        => CalculatePointCircle(Center, 20f, MirrorAngle(direction));

    // Degrees clockwise from north, looking from origin toward target.
    private static float LookDegree(Vector3 origin, Vector3 target)
    {
        var flat = target - origin;
        flat.Y = 0f;
        return MathF.Atan2(flat.X, -flat.Z).RadToDeg();
    }

    // Horizontal distance.
    private static float DistanceXz(Vector3 left, Vector3 right)
    {
        var dx = left.X - right.X;
        var dz = left.Z - right.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    // Places a point on a circle. Degree 0 is north, and positive degrees go clockwise.
    private static Vector3 CalculatePointCircle(Vector3 center, float radius, float degree)
    {
        var radian = degree.DegToRad();
        var sin = MathF.Sin(radian);
        var cos = MathF.Cos(radian);
        return new Vector3(
            center.X + sin * radius,
            center.Y,
            center.Z - cos * radius);
    }

    // Wraps a degree into the range 0 inclusive to 360 exclusive.
    private static float NormalizeAngle(float degree)
        => (degree % 360f + 360f) % 360f;

    // Shows the local group's stack as a donut.
    private void ShowGroupPoint()
    {
        if(_blue == null)
            return;

        var blue = MirrorPosition(_blue.Value);
        if(!BossGroup)
        {
            ShowOwn(blue);
            return;
        }

        var towardCenter = Center - blue;
        ShowOwn(blue + Vector3.Normalize(towardCenter) * 30f);
    }

    // Shows all eight clock spots around the boss and the blue mirror.
    private void ShowFirstBait()
    {
        var spots = new List<(Vector3 Position, bool Mine)>();
        if(_blue != null && _bossId.TryGetObject(out var boss))
        {
            var opposite = MirrorPosition(Opposite(_blue.Value));
            AddClock(spots, boss.Position, LookDegree(boss.Position, opposite), C.FirstConeBait, BossGroup, null, null);
        }

        if(_blue != null)
        {
            var blue = MirrorPosition(_blue.Value);
            AddClock(spots, blue, LookDegree(blue, Center), C.FirstConeBait, !BossGroup, null, null);
        }

        ShowSpots(spots);
    }

    // Shows all eight clock spots around the two red mirrors.
    private void ShowRedBait()
    {
        var assigned = AssignReds();
        if(assigned == null)
            return;

        var spots = new List<(Vector3 Position, bool Mine)>();
        var boss = MirrorPosition(assigned.Value.Boss);
        var blue = MirrorPosition(assigned.Value.Blue);
        var clockwise = ClockwiseFromGap();
        AddClock(spots, boss, LookDegree(boss, Center), C.SecondConeBait, BossGroup, assigned.Value.Boss, clockwise);
        AddClock(spots, blue, LookDegree(blue, Center), C.SecondConeBait, !BossGroup, assigned.Value.Blue, clockwise);
        ShowSpots(spots);
    }

    // Adds the four cone-bait slots around one origin.
    private void AddClock(List<(Vector3 Position, bool Mine)> spots, Vector3 origin, float zero, ConeBait bait, bool mineSide, Direction? red, List<Direction>? clockwise)
    {
        foreach(var slot in Enum.GetValues<ConeBait>())
        {
            var mine = mineSide && bait == slot;
            spots.Add((CalculatePointCircle(origin, 3.5f, zero + SlotOffset(slot, red, clockwise)), mine));
        }
    }

    // Draws the local donut and the other seven spots.
    private void ShowSpots(List<(Vector3 Position, bool Mine)> spots)
    {
        var other = 1;
        foreach(var spot in spots)
        {
            if(spot.Mine)
            {
                ShowOwn(spot.Position);
                continue;
            }

            if(!C.EnableVisualizer || !C.ShowAllPosition || other > SpotCount)
                continue;
            if(Controller.TryGetElementByName($"Spot{other}", out var element))
            {
                element.Enabled = true;
                element.tether = false;
                element.SetRefPosition(spot.Position);
            }

            other++;
        }
    }

    // Draws the boss and blue scythes, and cones on the closest players.
    private void ShowFirstRanges()
    {
        if(!C.EnableVisualizer)
            return;

        Vector3? bossPos = null;
        if(_bossId.TryGetObject(out var boss))
        {
            bossPos = boss.Position;
            if(C.ShowScytheKick && Controller.TryGetElementByName("ScytheBoss", out var element))
            {
                element.refActorObjectID = _bossId;
                element.Enabled = true;
            }
        }

        Vector3? bluePos = _blue == null ? null : MirrorPosition(_blue.Value);
        if(bluePos != null && C.ShowScytheKick)
            ShowFixedScythe("ScytheBlue", bluePos.Value);

        if(!C.ShowCone)
            return;

        var group = BossGroup ? bossPos : bluePos;
        if(bossPos != null)
            ShowSourceCones(bossPos.Value, bluePos, group);
        else if(bluePos != null)
            ShowSourceCones(bluePos.Value, null, group);
    }

    // Draws both red scythes, and cones on the closest players.
    private void ShowRedRanges()
    {
        if(!C.EnableVisualizer || _reds.Count < 2)
            return;

        var ordered = _blue != null ? OrderClockwise(_reds, MirrorAngle(Opposite(_blue.Value))) : _reds;
        var first = MirrorPosition(ordered[0]);
        var second = MirrorPosition(ordered[1]);
        if(C.ShowScytheKick)
        {
            ShowFixedScythe("ScytheRed1", first);
            ShowFixedScythe("ScytheRed2", second);
        }

        if(!C.ShowCone)
            return;

        var assigned = AssignReds();
        Vector3? group = assigned == null
            ? null
            : MirrorPosition(BossGroup ? assigned.Value.Boss : assigned.Value.Blue);
        ShowSourceCones(first, second, group);
    }

    // Shows the local donut and tethers the player to it.
    private void ShowOwn(Vector3 position)
    {
        if(!Controller.TryGetElementByName("Bait", out var element))
            return;

        element.Enabled = true;
        element.tether = true;
        element.color = Controller.AttentionColor;
        element.SetRefPosition(position);
    }

    // Enables the scythe donut at a world position.
    private void ShowFixedScythe(string name, Vector3 position)
    {
        if(!Controller.TryGetElementByName(name, out var element))
            return;

        element.SetRefPosition(position);
        element.Enabled = true;
    }

    // Splits players by the nearer source and shows up to four cones on each side.
    private void ShowSourceCones(Vector3 sourceA, Vector3? sourceB, Vector3? groupSource)
    {
        var nearA = new List<(IPlayerCharacter Player, float Distance)>();
        var nearB = new List<(IPlayerCharacter Player, float Distance)>();
        foreach(var player in Controller.GetPartyMembers().Where(player => player.CurrentHp > 0))
        {
            var distanceA = DistanceXz(player.Position, sourceA);
            var distanceB = sourceB == null ? float.MaxValue : DistanceXz(player.Position, sourceB.Value);
            if(MathF.Min(distanceA, distanceB) > 5f)
                continue;
            if(sourceB == null || distanceA <= distanceB)
                nearA.Add((player, distanceA));
            else
                nearB.Add((player, distanceB));
        }

        var selected = nearA.OrderBy(x => x.Distance).Take(4).Select(x => (sourceA, x.Player));
        if(sourceB != null)
            selected = selected.Concat(nearB.OrderBy(x => x.Distance).Take(4).Select(x => (sourceB.Value, x.Player)));
        ShowCones(selected, groupSource);
    }

    // Aims one cone from each source at that player.
    private void ShowCones(IEnumerable<(Vector3 Source, IPlayerCharacter Player)> cones, Vector3? groupSource)
    {
        if(C.ConeView == ConeView.MyOnly)
        {
            if(BasePlayer == null)
                return;
            var me = BasePlayer.EntityId;
            cones = cones.Where(cone => cone.Player.EntityId == me);
        }
        else if(C.ConeView == ConeView.MyGroup)
        {
            if(groupSource == null)
                return;
            cones = cones.Where(cone => cone.Source == groupSource.Value);
        }

        var index = 1;
        foreach(var cone in cones)
        {
            if(index > ConeCount)
                break;
            if(!Controller.TryGetElementByName($"Cone{index}", out var element))
                break;

            element.SetRefPosition(cone.Source);
            element.AdditionalRotation = (180f + MathHelper.GetRelativeAngle(cone.Source, cone.Player.Position)).DegToRad();
            element.Enabled = true;
            index++;
        }
    }

    // Builds the local donut or one of the other spots.
    private static Element GuideElement(bool mine)
        => new(0)
        {
            Enabled = false,
            tether = mine,
            radius = mine ? 0.35f : 0.3f,
            Donut = mine ? 0.25f : 0f,
            Filled = !mine,
            thicc = 2f,
            color = mine ? 0u : 3355508496u,
        };

    // Builds one fixed-position scythe donut.
    private static Element ScytheAtPoint()
        => new(0)
        {
            Enabled = false,
            radius = 4f,
            Donut = 16f,
            color = 4278210815u,
            fillIntensity = 0.203f,
        };

    // Draws the bait slots with the facing direction at the top. One slot stays selected.
    private void DrawBaitGrid(string group, string mark, FirstGroup? spread, ref ConeBait bait)
    {
        var box = ImGui.GetFrameHeight();
        var spacing = ImGui.GetStyle().ItemSpacing;
        var step = box + spacing.X;
        var row2 = box + spacing.Y;
        var origin = ImGui.GetCursorScreenPos();

        DrawBaitBox(origin + new Vector2(step * 0.5f, 0f), group, ConeBait.FrontLeft, spread, ref bait);
        DrawBaitBox(origin + new Vector2(step * 1.5f, 0f), group, ConeBait.FrontRight, spread, ref bait);
        DrawBaitBox(origin + new Vector2(0f, row2), group, ConeBait.Left, spread, ref bait);
        var markSize = ImGui.CalcTextSize(mark);
        ImGui.SetCursorScreenPos(origin + new Vector2(step + (box - markSize.X) * 0.5f, row2 + (box - markSize.Y) * 0.5f));
        ImGui.Text(mark);
        DrawBaitBox(origin + new Vector2(step * 2f, row2), group, ConeBait.Right, spread, ref bait);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(step * 2f + box, row2 + box));
    }

    // Draws one bait slot. Checking it selects that slot, and the selected slot stays on.
    private void DrawBaitBox(Vector2 pos, string group, ConeBait slot, FirstGroup? spread, ref ConeBait bait)
    {
        ImGui.SetCursorScreenPos(pos);
        var on = (spread == null || C.FirstGroup == spread) && bait == slot;
        if(ImGui.Checkbox($"##{group}{slot}", ref on) && on)
        {
            bait = slot;
            if(spread != null)
                C.FirstGroup = spread.Value;
        }
    }

    // Draws one mirror rule as a radio.
    private void DrawMirrorRule(SecondMirrorRule rule, string text)
    {
        if(ImGui.RadioButton($"##{rule}", C.SecondMirrorRule == rule))
            C.SecondMirrorRule = rule;
        ImGui.SameLine();
        ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + ImGui.GetContentRegionAvail().X);
        ImGui.Text(text);
        ImGui.PopTextWrapPos();
    }

    #endregion
}
