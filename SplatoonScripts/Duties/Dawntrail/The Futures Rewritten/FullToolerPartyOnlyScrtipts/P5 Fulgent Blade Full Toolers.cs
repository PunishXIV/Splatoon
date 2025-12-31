using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.MathHelpers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P5_Fulgent_Blade_Full_Toolers : SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    private enum State
    {
        None = 0,
        Stack1,
        Dodge1,
        Stack2,
        Dodge2,
        Stack3,
        Dodge3,
        Stack4,
        Dodge4,
    }
    #endregion

    #region class
    /********************************************************************/
    /* class                                                            */
    /********************************************************************/
    public class Config : IEzConfig
    {
        public float FastCheatDefault = 1.0f;
        public float FastCheat = 1.5f;
    }

    private class RemoveBuff
    {
        public Vector3 Position = Vector3.Zero;
        public uint AssignEntityId = 0;
    }

    private class PartyData
    {
        public int Index = 0;
        public bool Mine => EntityId == Player.Object.EntityId;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)EntityId.GetObject()! ?? null;
        public DirectionCalculator.Direction AssignDirection = DirectionCalculator.Direction.None;

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsTH => IsTank || IsHealer;
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);
        public bool IsDps => IsMeleeDps || IsRangedDps;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
        }
    }
    #endregion

    #region const
    /********************************************************************/
    /* const                                                            */
    /********************************************************************/
    private readonly Dictionary<int, (string, string, string, string)> BaitList = new()
    {
        { 0, ("L", "R", "R", "R") },
        { 1, ("R", "L", "L", "L") },
        { 2, ("L", "L", "L", "L") },
        { 3, ("R", "R", "R", "R") },
        { 4, ("L", "L", "R", "R") },
        { 5, ("R", "R", "L", "L") },
        { 6, ("L", "L", "L", "R") },
        { 7, ("R", "R", "R", "L") },
    };
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(3, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private Config C => Controller.GetConfig<Config>();
    private ClockDirectionCalculator _clockDirectionCalculator = new();
    private int _firstLightPcIndex = 0;
    private int _firstDarkPcIndex = 0;
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                   */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40316)
        {
            SetListEntityIdByJob();
            if(!source.TryGetObject(out var obj)) return;
            var angle = ConvertRotationRadiansToDegrees(obj.Rotation);
            var dir = DirectionCalculator.GetDirectionFromAngle(DirectionCalculator.Direction.North, (int)angle);
            _clockDirectionCalculator._12ClockDirection = dir;
            _firstLightPcIndex = 0;
            _firstDarkPcIndex = 1;
            var pc = GetMinedata();
            if(pc == null) return;
            // BaitListの中から、自分のIndexに対応するものを取得
            var bait = BaitList[pc?.Index ?? 0];
            var bait1 = bait.Item1;
            var range = (pc.Index == _firstLightPcIndex || pc.Index == _firstDarkPcIndex) ? 5f : 10f;
            DuoLog.Information($"bait1: {bait1} range: {range}");
            if(bait1 == "L") ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(7)) - 15, range);
            if(bait1 == "R") ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(4)) + 15, range);
            _state = State.Stack1;

            Chat.Instance.ExecuteCommand($"/pdrspeed {C.FastCheat}");
        }

        if(_state == State.None) return;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(_state == State.None) return;
        if(set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if(castId is 40317) // Light
        {
            var pc = _partyDataList[_firstLightPcIndex].Object;
            //if (pc != null && Controller.TryGetElementByName("Line", out var el))
            //{
            //    el.SetRefPosition(new Vector3(100, 0, 100));
            //    var pos = GetExtendedAndClampedPosition(new Vector3(100, 0, 100), pc.Position, 25f);
            //    el.SetOffPosition(pos);
            //    el.radius = 3f;
            //    el.thicc = 2f;
            //    el.Enabled = true;
            //}

            ApplyElement("Bait", _clockDirectionCalculator.GetDirectionFromClock(6), 8f);

            _state++;
        }

        if(castId is 40318) // Dark
        {
            var pc = _partyDataList[_firstDarkPcIndex].Object;
            //if (pc != null && Controller.TryGetElementByName("Line2", out var el))
            //{
            //    el.SetRefPosition(new Vector3(100, 0, 100));
            //    var pos = GetExtendedAndClampedPosition(new Vector3(100, 0, 100), pc.Position, 25f);
            //    el.SetOffPosition(pos);
            //    el.radius = 3f;
            //    el.thicc = 2f;
            //    el.Enabled = true;
            //}
        }

        if(castId is 40119)
        {
            if(Controller.TryGetElementByName("Line", out var el)) el.Enabled = false;
            if(_state == State.Dodge4)
            {
                OnReset();
                return;
            }
            _state++;
            ChengeStatus();
            Show();
        }
        if(castId is 40120)
        {
            if(Controller.TryGetElementByName("Line2", out var el)) el.Enabled = false;
        }
    }

    public override void OnUpdate()
    {
        if(_state == State.None) return;

        if(Controller.TryGetElementByName("Bait", out var el))
        {
            if(el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _partyDataList.Clear();
        _firstLightPcIndex = 0;
        _firstDarkPcIndex = 0;
        Chat.Instance.ExecuteCommand($"/pdrspeed {C.FastCheatDefault}");
        HideAllElements();
    }

    public override void OnSettingsDraw()
    {
        ImGui.SliderFloat("FastCheat", ref C.FastCheat, 1.0f, 1.5f);
        ImGui.SliderFloat("FastCheatDefault", ref C.FastCheatDefault, 1.0f, 1.5f);
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"FirstLightPcIndex: {_firstLightPcIndex}");
            ImGui.Text($"FirstDarkPcIndex: {_firstDarkPcIndex}");
            ImGui.Text("PartyDataList");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach(var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                if(x.Object != null)
                {
                    Entries.Add(new ImGuiEx.EzTableEntry("Job", true, () => ImGui.Text(x.Object.GetJob().ToString())));
                    Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.Object.Name.ToString())));
                }
                else
                {
                    Entries.Add(new ImGuiEx.EzTableEntry("Job", true, () => ImGui.Text("null")));
                    Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text("null")));
                }
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
            }
            ImGuiEx.EzTable(Entries);
        }
    }
    #endregion

    #region private methods
    /********************************************************************/
    /* private methods                                                  */
    /********************************************************************/
    private void ChengeStatus()
    {
        _firstLightPcIndex = _firstLightPcIndex switch
        {
            0 => 4,
            4 => 6,
            6 => 2,
            _ => 0
        };

        _firstDarkPcIndex = _firstDarkPcIndex switch
        {
            1 => 5,
            5 => 7,
            7 => 3,
            _ => 1
        };
    }

    private void Show()
    {
        var pc = GetMinedata();
        if(pc == null) return;
        // BaitListの中から、自分のIndexに対応するものを取得
        var bait = BaitList[pc?.Index ?? 0];
        var bait1 = _state switch
        {
            State.Stack1 => bait.Item1,
            State.Stack2 => bait.Item2,
            State.Stack3 => bait.Item3,
            State.Stack4 => bait.Item4,
            _ => "L"
        };
        DuoLog.Information($"_firstLightPcIndex: {_firstLightPcIndex} _firstDarkPcIndex: {_firstDarkPcIndex}");
        var range = (pc.Index == _firstLightPcIndex || pc.Index == _firstDarkPcIndex) ? 5f : 10f;
        DuoLog.Information($"bait1: {bait1} range: {range}");
        if(bait1 == "L") ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(7)) - 15, range);
        if(bait1 == "R") ApplyElement("Bait", DirectionCalculator.GetAngle(_clockDirectionCalculator.GetDirectionFromClock(4)) + 15, range);
    }

    private PartyData? GetMinedata() => _partyDataList.Find(x => x.Mine) ?? null;

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();
        var tmpList = new List<PartyData>();

        foreach(var pc in FakeParty.Get())
        {
            tmpList.Add(new PartyData(pc.EntityId, Array.IndexOf(jobOrder, pc.GetJob())));
        }

        // Sort by job order
        tmpList.Sort((a, b) => a.Index.CompareTo(b.Index));
        foreach(var data in tmpList)
        {
            _partyDataList.Add(data);
        }

        // Set index
        for(var i = 0; i < _partyDataList.Count; i++)
        {
            _partyDataList[i].Index = i;
        }
    }
    #endregion


    #region API
    /********************************************************************/
    /* API                                                              */
    /********************************************************************/
    private static readonly Job[] jobOrder =
    {
        Job.DRK,
        Job.WAR,
        Job.GNB,
        Job.PLD,
        Job.WHM,
        Job.AST,
        Job.SCH,
        Job.SGE,
        Job.DRG,
        Job.VPR,
        Job.SAM,
        Job.MNK,
        Job.RPR,
        Job.NIN,
        Job.BRD,
        Job.MCH,
        Job.DNC,
        Job.RDM,
        Job.SMN,
        Job.PCT,
        Job.BLM,
    };

    private static readonly Job[] TankJobs = { Job.DRK, Job.WAR, Job.GNB, Job.PLD };
    private static readonly Job[] HealerJobs = { Job.WHM, Job.AST, Job.SCH, Job.SGE };
    private static readonly Job[] MeleeDpsJobs = { Job.DRG, Job.VPR, Job.SAM, Job.MNK, Job.RPR, Job.NIN };
    private static readonly Job[] RangedDpsJobs = { Job.BRD, Job.MCH, Job.DNC };
    private static readonly Job[] MagicDpsJobs = { Job.RDM, Job.SMN, Job.PCT, Job.BLM };
    private static readonly Job[] DpsJobs = MeleeDpsJobs.Concat(RangedDpsJobs).Concat(MagicDpsJobs).ToArray();
    private enum Role
    {
        Tank,
        Healer,
        MeleeDps,
        RangedDps,
        MagicDps
    }

    public class DirectionCalculator
    {
        public enum Direction : int
        {
            None = -1,
            East = 0,
            SouthEast = 1,
            South = 2,
            SouthWest = 3,
            West = 4,
            NorthWest = 5,
            North = 6,
            NorthEast = 7,
        }

        public enum LR : int
        {
            Left = -1,
            SameOrOpposite = 0,
            Right = 1
        }

        public class DirectionalVector
        {
            public Direction Direction { get; }
            public Vector3 Position { get; }

            public DirectionalVector(Direction direction, Vector3 position)
            {
                Direction = direction;
                Position = position;
            }

            public override string ToString()
            {
                return $"{Direction}: {Position}";
            }
        }

        public static int Round45(int value) => (int)(MathF.Round((float)value / 45) * 45);
        public static Direction GetOppositeDirection(Direction direction) => GetDirectionFromAngle(direction, 180);

        public static Direction DividePoint(Vector3 Position, float Distance, Vector3? Center = null)
        {
            // Distance, Centerの値を用いて、８方向のベクトルを生成
            var directionalVectors = GenerateDirectionalVectors(Distance, Center ?? new Vector3(100, 0, 100));

            // ８方向の内、最も近い方向ベクトルを取得
            var closestDirection = Direction.North;
            var closestDistance = float.MaxValue;
            foreach(var directionalVector in directionalVectors)
            {
                var distance = Vector3.Distance(Position, directionalVector.Position);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDirection = directionalVector.Direction;
                }
            }

            return closestDirection;
        }

        public static Direction GetDirectionFromAngle(Direction direction, int angle)
        {
            if(direction == Direction.None) return Direction.None; // 無効な方向の場合

            // 方向数（8方向: North ~ NorthWest）
            const int directionCount = 8;

            // 角度を45度単位に丸め、-180～180の範囲に正規化
            angle = ((Round45(angle) % 360) + 360) % 360; // 正の値に変換して360で正規化
            if(angle > 180) angle -= 360;

            // 現在の方向のインデックス
            var currentIndex = (int)direction;

            // 45度ごとのステップ計算と新しい方向の計算
            var step = angle / 45;
            var newIndex = (currentIndex + step + directionCount) % directionCount;

            return (Direction)newIndex;
        }

        public static LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
        {
            // 不正な方向の場合（None）
            if(direction1 == Direction.None || direction2 == Direction.None)
                return LR.SameOrOpposite;

            // 方向数（8つ: North ~ NorthWest）
            var directionCount = 8;

            // 差分を循環的に計算
            var difference = ((int)direction2 - (int)direction1 + directionCount) % directionCount;

            // LRを直接返す
            return difference == 0 || difference == directionCount / 2
                ? LR.SameOrOpposite
                : (difference < directionCount / 2 ? LR.Right : LR.Left);
        }

        public static int GetTwoPointAngle(Direction direction1, Direction direction2)
        {
            // 不正な方向を考慮
            if(direction1 == Direction.None || direction2 == Direction.None)
                return 0;

            // enum の値を数値として扱い、環状の差分を計算
            var diff = ((int)direction2 - (int)direction1 + 8) % 8;

            // 差分から角度を計算
            return diff <= 4 ? diff * 45 : (diff - 8) * 45;
        }

        public static float GetAngle(Direction direction)
        {
            if(direction == Direction.None) return 0; // 無効な方向の場合

            // 45度単位で計算し、0度から始まる時計回りの角度を返す
            return (int)direction * 45 % 360;
        }

        private static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
        {
            var directionalVectors = new List<DirectionalVector>();

            // 各方向のオフセット計算
            foreach(Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if(direction == Direction.None) continue; // Noneはスキップ

                var offset = direction switch
                {
                    Direction.North => new Vector3(0, 0, -1),
                    Direction.NorthEast => Vector3.Normalize(new Vector3(1, 0, -1)),
                    Direction.East => new Vector3(1, 0, 0),
                    Direction.SouthEast => Vector3.Normalize(new Vector3(1, 0, 1)),
                    Direction.South => new Vector3(0, 0, 1),
                    Direction.SouthWest => Vector3.Normalize(new Vector3(-1, 0, 1)),
                    Direction.West => new Vector3(-1, 0, 0),
                    Direction.NorthWest => Vector3.Normalize(new Vector3(-1, 0, -1)),
                    _ => Vector3.Zero
                };

                // 距離を適用して座標を計算
                var position = (center ?? new Vector3(100, 0, 100)) + (offset * distance);

                // リストに追加
                directionalVectors.Add(new DirectionalVector(direction, position));
            }

            return directionalVectors;
        }
    }

    public class ClockDirectionCalculator
    {
        public DirectionCalculator.Direction _12ClockDirection = DirectionCalculator.Direction.None;
        public bool isValid => _12ClockDirection != DirectionCalculator.Direction.None;
        public DirectionCalculator.Direction Get12ClockDirection() => _12ClockDirection;

        // _12ClockDirectionを0時方向として、指定時計からの方向を取得
        public DirectionCalculator.Direction GetDirectionFromClock(int clock)
        {
            if(!isValid)
                return DirectionCalculator.Direction.None;

            // 特別ケース: clock = 0 の場合、_12ClockDirection をそのまま返す
            if(clock == 0)
                return _12ClockDirection;

            // 12時計位置を8方向にマッピング
            var clockToDirectionMapping = new Dictionary<int, int>
        {
            { 0, 0 },   // Same as _12ClockDirection
            { 1, 1 }, { 2, 1 },   // Diagonal right up
            { 3, 2 },             // Right
            { 4, 3 }, { 5, 3 },   // Diagonal right down
            { 6, 4 },             // Opposite
            { 7, -3 }, { 8, -3 }, // Diagonal left down
            { 9, -2 },            // Left
            { 10, -1 }, { 11, -1 } // Diagonal left up
        };

            // 現在の12時方向をインデックスとして取得
            var baseIndex = (int)_12ClockDirection;

            // 時計位置に基づくステップを取得
            var step = clockToDirectionMapping[clock];

            // 新しい方向を計算し、範囲を正規化
            var targetIndex = (baseIndex + step + 8) % 8;

            // 対応する方向を返す
            return (DirectionCalculator.Direction)targetIndex;
        }

        public int GetClockFromDirection(DirectionCalculator.Direction direction)
        {
            if(!isValid)
                throw new InvalidOperationException("Invalid state: _12ClockDirection is not set.");

            if(direction == DirectionCalculator.Direction.None)
                throw new ArgumentException("Direction cannot be None.", nameof(direction));

            // 各方向に対応する最小の clock 値を定義
            var directionToClockMapping = new Dictionary<int, int>
            {
                { 0, 0 },   // Same as _12ClockDirection
                { 1, 1 },   // Diagonal right up (SouthEast)
                { 2, 3 },   // Right (South)
                { 3, 4 },   // Diagonal right down (SouthWest)
                { 4, 6 },   // Opposite (West)
                { 5, 7 },   // Diagonal left down (NorthWest)
                { 6, 9 },   // Left (North)
                { 7, 10 }   // Diagonal left up (NorthEast)
            };

            // 現在の12時方向をインデックスとして取得
            var baseIndex = (int)_12ClockDirection;

            // 指定された方向のインデックス
            var targetIndex = (int)direction;

            // 差分を計算し、時計方向に正規化
            var step = (targetIndex - baseIndex + 8) % 8;

            // 該当する clock を取得
            return directionToClockMapping[step];
        }

        public float GetAngle(int clock) => DirectionCalculator.GetAngle(GetDirectionFromClock(clock));
    }

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    // original
    private void ApplyElement(
        [NotNull] Element element,
        Vector3 position,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        element.Enabled = true;
        element.radius = elementRadius;
        element.tether = tether;
        element.Filled = Filled;
        element.SetRefPosition(position);
    }

    // mutable
    private void ApplyElement(
        string elementName, // mutable
        Vector3 position,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            ApplyElement(element, position, elementRadius, Filled, tether);
        }
    }

    private void ApplyElement(
        [NotNull] Element element,
        float angle, // mutable
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        var position = new Vector3(100, 0, 100);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        ApplyElement(element, position, elementRadius, Filled, tether);
    }

    private void ApplyElement(
        [NotNull] Element element, // mutable
        DirectionCalculator.Direction direction, // mutable
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        var angle = DirectionCalculator.GetAngle(direction);
        ApplyElement(element, angle, radius, elementRadius, Filled, tether);
    }


    private void ApplyElement(
        string elementName, // mutable
        DirectionCalculator.Direction direction, // mutable
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            var angle = DirectionCalculator.GetAngle(direction);
            ApplyElement(element, angle, radius, elementRadius, Filled, tether);
        }
    }

    private void ApplyElement(
        string elementName, // mutable
        float angle,
        float radius = 0f,
        float elementRadius = 0.3f,
        bool Filled = true,
        bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            ApplyElement(element, angle, radius, elementRadius, Filled, tether);
        }
    }

    private static float GetCorrectionAngle(Vector3 origin, Vector3 target, float rotation) =>
            GetCorrectionAngle(MathHelper.ToVector2(origin), MathHelper.ToVector2(target), rotation);

    private static float GetCorrectionAngle(Vector2 origin, Vector2 target, float rotation)
    {
        // Calculate the relative angle to the target
        var direction = target - origin;
        var relativeAngle = MathF.Atan2(direction.Y, direction.X) * (180 / MathF.PI);

        // Normalize relative angle to 0-360 range
        relativeAngle = (relativeAngle + 360) % 360;

        // Calculate the correction angle
        var correctionAngle = (relativeAngle - ConvertRotationRadiansToDegrees(rotation) + 360) % 360;

        // Adjust correction angle to range -180 to 180 for shortest rotation
        if(correctionAngle > 180)
            correctionAngle -= 360;

        return correctionAngle;
    }

    private static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        var degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        var radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }

    public static Vector3 GetExtendedAndClampedPosition(
        Vector3 center, Vector3 currentPos, float extensionLength, float? limit = null)
    {
        // Calculate the normalized direction vector from the center to the current position
        var direction = Vector3.Normalize(currentPos - center);

        // Extend the position by the specified length
        var extendedPos = currentPos + (direction * extensionLength);

        // If limit is null, return the extended position without clamping
        if(!limit.HasValue)
        {
            return extendedPos;
        }

        // Calculate the distance from the center to the extended position
        var distanceFromCenter = Vector3.Distance(center, extendedPos);

        // If the extended position exceeds the limit, clamp it within the limit
        if(distanceFromCenter > limit.Value)
        {
            return center + (direction * limit.Value);
        }

        // If within the limit, return the extended position as is
        return extendedPos;
    }

    public static void ExceptionReturn(string message)
    {
        PluginLog.Error(message);
    }
    #endregion
}
