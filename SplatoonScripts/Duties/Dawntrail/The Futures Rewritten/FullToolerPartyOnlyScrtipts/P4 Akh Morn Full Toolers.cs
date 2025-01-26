using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal unsafe class P4_Akh_Morn_Full_Toolers :SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    #endregion

    #region class
    /********************************************************************/
    /* class                                                            */
    /********************************************************************/
    public class Config :IEzConfig { }

    private class PartyData
    {
        public int Index { get; set; }
        public bool Mine = false;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)this.EntityId.GetObject()! ?? null;

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsTH => IsTank || IsHealer;
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);
        public bool IsMagicDps => MagicDpsJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsDps => IsMeleeDps || IsRangedDps || IsMagicDps;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
            Mine = entityId == Player.Object.EntityId;
        }
    }
    #endregion

    #region const
    /********************************************************************/
    /* const                                                            */
    /********************************************************************/
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
    private bool _gimmick = false;
    private List<PartyData> _partyDataList = new();
    private uint _gaiaEntityId = 0;
    private uint _RinEntityId = 0;
    private int _akhMornCount = 0;
    private bool _mitiBuff = false;
    private ActionManager* actionManager = ActionManager.Instance();
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
        if (castId == 40302)
        {
            _gaiaEntityId = source;
        }
        if (castId == 40247)
        {
            _RinEntityId = source;
        }

        if ((castId == 40302 || castId == 40247) &&
            _gaiaEntityId != 0 && _RinEntityId != 0)
        {
            _gimmick = true;
            SetListEntityIdByJob();

            // DEBUG
            //_partyDataList.Each(x => x.Mine = false);
            //_partyDataList[2].Mine = true;

            var pc = GetMinedata();
            if (pc == null) return;
            if (_akhMornCount == 0) // 1回目
            {
                if (pc.Index == 1)
                {
                    ApplyElement("Bait", DirectionCalculator.Direction.East, 13f);
                }
                else
                {
                    ApplyElement("Bait", DirectionCalculator.Direction.East, 0f);
                }
            }
            else // 2回目
            {
                _mitiBuff = true;
                if (pc.Index == 0)
                {
                    ApplyElement("Bait", DirectionCalculator.Direction.West, 13f);
                }
                else
                {
                    ApplyElement("Bait", DirectionCalculator.Direction.West, 0);
                }
            }

            _akhMornCount++;
        }

        if (castId == 40249)
        {
            ApplyElement("Bait", DirectionCalculator.Direction.East, 0);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if (castId == 40249)
        {
            HotReset();
        }
    }

    public override void OnUpdate()
    {
        if (!_gimmick) return;

        if (Controller.TryGetElementByName("Bait", out var el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if (_mitiBuff && (Player.Job == Job.DRK))
        {
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 7531u))
            {
                actionManager->UseAction(ActionType.Action, 7531u);
            }
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 3634u))
            {
                actionManager->UseAction(ActionType.Action, 3634u);
            }
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 25754u))
            {
                actionManager->UseAction(ActionType.Action, 25754u);
            }
            if (actionManager->AnimationLock == 0 && !actionManager->IsRecastTimerActive(ActionType.Action, 7393u))
            {
                actionManager->UseAction(ActionType.Action, 7393u);
            }
        }
    }

    public override void OnReset()
    {
        _akhMornCount = 0;
        HotReset();
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"Gimmick: {_gimmick}");
            ImGui.Text($"Akh Morn Count: {_akhMornCount}");
            ImGui.Text($"Gaia: {_gaiaEntityId}");
            ImGui.Text($"Rin: {_RinEntityId}");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.EntityId.GetObject().Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsTank", true, () => ImGui.Text(x.IsTank.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsHealer", true, () => ImGui.Text(x.IsHealer.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsTH", true, () => ImGui.Text(x.IsTH.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsMeleeDps", true, () => ImGui.Text(x.IsMeleeDps.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsRangedDps", true, () => ImGui.Text(x.IsRangedDps.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsMagicDps", true, () => ImGui.Text(x.IsMagicDps.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsDps", true, () => ImGui.Text(x.IsDps.ToString())));

            }
            ImGuiEx.EzTable(Entries);
        }
    }
    #endregion

    #region private methods
    /********************************************************************/
    /* private methods                                                  */
    /********************************************************************/

    private void HotReset()
    {
        _gaiaEntityId = 0;
        _RinEntityId = 0;
        _partyDataList.Clear();
        _mitiBuff = false;
        HideAllElements();
    }

    private PartyData? GetMinedata() => _partyDataList.Find(x => x.Mine) ?? null;

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();
        var tmpList = new List<PartyData>();

        foreach (var pc in FakeParty.Get())
        {
            tmpList.Add(new PartyData(pc.EntityId, Array.IndexOf(jobOrder, pc.GetJob())));
        }

        // Sort by job order
        tmpList.Sort((a, b) => a.Index.CompareTo(b.Index));
        foreach (var data in tmpList)
        {
            _partyDataList.Add(data);
        }

        // Set index
        for (var i = 0; i < _partyDataList.Count; i++)
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
        public enum Direction :int
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

        public enum LR :int
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
            foreach (var directionalVector in directionalVectors)
            {
                var distance = Vector3.Distance(Position, directionalVector.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestDirection = directionalVector.Direction;
                }
            }

            return closestDirection;
        }

        public static Direction GetDirectionFromAngle(Direction direction, int angle)
        {
            if (direction == Direction.None) return Direction.None; // 無効な方向の場合

            // 方向数（8方向: North ~ NorthWest）
            const int directionCount = 8;

            // 角度を45度単位に丸め、-180～180の範囲に正規化
            angle = ((Round45(angle) % 360) + 360) % 360; // 正の値に変換して360で正規化
            if (angle > 180) angle -= 360;

            // 現在の方向のインデックス
            int currentIndex = (int)direction;

            // 45度ごとのステップ計算と新しい方向の計算
            int step = angle / 45;
            int newIndex = (currentIndex + step + directionCount) % directionCount;

            return (Direction)newIndex;
        }

        public static LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
        {
            // 不正な方向の場合（None）
            if (direction1 == Direction.None || direction2 == Direction.None)
                return LR.SameOrOpposite;

            // 方向数（8つ: North ~ NorthWest）
            int directionCount = 8;

            // 差分を循環的に計算
            int difference = ((int)direction2 - (int)direction1 + directionCount) % directionCount;

            // LRを直接返す
            return difference == 0 || difference == directionCount / 2
                ? LR.SameOrOpposite
                : (difference < directionCount / 2 ? LR.Right : LR.Left);
        }

        public static int GetTwoPointAngle(Direction direction1, Direction direction2)
        {
            // 不正な方向を考慮
            if (direction1 == Direction.None || direction2 == Direction.None)
                return 0;

            // enum の値を数値として扱い、環状の差分を計算
            int diff = ((int)direction2 - (int)direction1 + 8) % 8;

            // 差分から角度を計算
            return diff <= 4 ? diff * 45 : (diff - 8) * 45;
        }

        public static float GetAngle(Direction direction)
        {
            if (direction == Direction.None) return 0; // 無効な方向の場合

            // 45度単位で計算し、0度から始まる時計回りの角度を返す
            return (int)direction * 45 % 360;
        }

        private static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
        {
            var directionalVectors = new List<DirectionalVector>();

            // 各方向のオフセット計算
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if (direction == Direction.None) continue; // Noneはスキップ

                Vector3 offset = direction switch
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
                Vector3 position = (center ?? new Vector3(100, 0, 100)) + (offset * distance);

                // リストに追加
                directionalVectors.Add(new DirectionalVector(direction, position));
            }

            return directionalVectors;
        }
    }

    public class ClockDirectionCalculator
    {
        private DirectionCalculator.Direction _12ClockDirection = DirectionCalculator.Direction.None;
        public bool isValid => _12ClockDirection != DirectionCalculator.Direction.None;
        public DirectionCalculator.Direction Get12ClockDirection() => _12ClockDirection;

        public ClockDirectionCalculator(DirectionCalculator.Direction direction)
        {
            _12ClockDirection = direction;
        }

        // _12ClockDirectionを0時方向として、指定時計からの方向を取得
        public DirectionCalculator.Direction GetDirectionFromClock(int clock)
        {
            if (!isValid)
                return DirectionCalculator.Direction.None;

            // 特別ケース: clock = 0 の場合、_12ClockDirection をそのまま返す
            if (clock == 0)
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
            int baseIndex = (int)_12ClockDirection;

            // 時計位置に基づくステップを取得
            int step = clockToDirectionMapping[clock];

            // 新しい方向を計算し、範囲を正規化
            int targetIndex = (baseIndex + step + 8) % 8;

            // 対応する方向を返す
            return (DirectionCalculator.Direction)targetIndex;
        }

        public int GetClockFromDirection(DirectionCalculator.Direction direction)
        {
            if (!isValid)
                throw new InvalidOperationException("Invalid state: _12ClockDirection is not set.");

            if (direction == DirectionCalculator.Direction.None)
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
            int baseIndex = (int)_12ClockDirection;

            // 指定された方向のインデックス
            int targetIndex = (int)direction;

            // 差分を計算し、時計方向に正規化
            int step = (targetIndex - baseIndex + 8) % 8;

            // 該当する clock を取得
            return directionToClockMapping[step];
        }

        public float GetAngle(int clock) => DirectionCalculator.GetAngle(GetDirectionFromClock(clock));
    }

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private Vector3 BasePosition => new Vector3(100, 0, 100);

    private Vector3 CalculatePositionFromAngle(float angle, float radius = 0f)
    {
        return BasePosition + (radius * new Vector3(
            MathF.Cos(MathF.PI * angle / 180f),
            0,
            MathF.Sin(MathF.PI * angle / 180f)
        ));
    }

    private Vector3 CalculatePositionFromDirection(DirectionCalculator.Direction direction, float radius = 0f)
    {
        var angle = DirectionCalculator.GetAngle(direction);
        return CalculatePositionFromAngle(angle, radius);
    }

    /// <summary>
    /// Elementへの実適用処理を行う"大元"のメソッド。
    /// </summary>
    private void InternalApplyElement(Element element, Vector3 position, float elementRadius, bool filled, bool tether)
    {
        element.Enabled = true;
        element.radius = elementRadius;
        element.tether = tether;
        element.Filled = filled;
        element.SetRefPosition(position);
    }

    //----------------------- 公開ApplyElementメソッド群 -----------------------

    // Elementインスタンスと直接的な座標指定
    public void ApplyElement(Element element, Vector3 position, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Elementインスタンスと角度指定
    public void ApplyElement(Element element, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        var position = CalculatePositionFromAngle(angle, radius);
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Elementインスタンスと方向指定
    public void ApplyElement(Element element, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        var position = CalculatePositionFromDirection(direction, radius);
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Element名と直接的な座標指定
    public void ApplyElement(string elementName, Vector3 position, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と角度指定
    public void ApplyElement(string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromAngle(angle, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と方向指定
    public void ApplyElement(string elementName, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromDirection(direction, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    private static float GetCorrectionAngle(Vector3 origin, Vector3 target, float rotation) =>
            GetCorrectionAngle(MathHelper.ToVector2(origin), MathHelper.ToVector2(target), rotation);

    private static float GetCorrectionAngle(Vector2 origin, Vector2 target, float rotation)
    {
        // Calculate the relative angle to the target
        Vector2 direction = target - origin;
        float relativeAngle = MathF.Atan2(direction.Y, direction.X) * (180 / MathF.PI);

        // Normalize relative angle to 0-360 range
        relativeAngle = (relativeAngle + 360) % 360;

        // Calculate the correction angle
        float correctionAngle = (relativeAngle - ConvertRotationRadiansToDegrees(rotation) + 360) % 360;

        // Adjust correction angle to range -180 to 180 for shortest rotation
        if (correctionAngle > 180)
            correctionAngle -= 360;

        return correctionAngle;
    }

    private static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        float degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        float radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }

    public static Vector3 GetExtendedAndClampedPosition(
        Vector3 center, Vector3 currentPos, float extensionLength, float? limit)
    {
        // Calculate the normalized direction vector from the center to the current position
        Vector3 direction = Vector3.Normalize(currentPos - center);

        // Extend the position by the specified length
        Vector3 extendedPos = currentPos + (direction * extensionLength);

        // If limit is null, return the extended position without clamping
        if (!limit.HasValue)
        {
            return extendedPos;
        }

        // Calculate the distance from the center to the extended position
        float distanceFromCenter = Vector3.Distance(center, extendedPos);

        // If the extended position exceeds the limit, clamp it within the limit
        if (distanceFromCenter > limit.Value)
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
