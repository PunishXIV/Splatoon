using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Element = Splatoon.Element;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;

public class P1_Burn_Strike_Tower_Tooler_Party : SplatoonScript
{
    private class PartyData
    {
        public int Index = 0;
        public bool Mine = false;
        public uint EntityId;
        public IPlayerCharacter? Object
        {
            get
            {
                return EntityId.GetObject() as IPlayerCharacter;
            }
        }
        public int AssignedTowerIndex = 0;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
            Mine = entityId == Player.Object.EntityId;
        }
    }

    private readonly Dictionary<int, List<uint>> TowerCastIds = new()
    {
        { 1, [0x9CC7, 0x9CC3] },
        { 2, [0x9CBD, 0x9CBA] },
        { 3, [0x9CBE, 0x9CBB] },
        { 4, [0x9CBF, 0x9CBC] }
    };

    private IBattleNpc?[] _currentTowers = new IBattleNpc[3];
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(6, "Garume, Redmoon");

    private int TowerCount(uint castId)
    {
        return TowerCastIds.First(x => x.Value.Contains(castId)).Key;
    }

    public override void OnSettingsDraw()
    {
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("State: " + _state);
            ImGui.Text("Towers: ");
            foreach(var tower in _currentTowers)
                ImGui.Text(tower + " " + tower?.Position);

            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach(var x in _partyDataList)
            {
                if(!x.EntityId.TryGetObject(out var pc)) continue;
                Entries.Add(new ImGuiEx.EzTableEntry("Index", () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(pc.Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("ObjectId", () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("AssignedTowerIndex", () => ImGui.Text(x.AssignedTowerIndex.ToString())));
            }

            ImGuiEx.EzTable(Entries);
        }
    }

    public override void OnScriptUpdated(uint previousVersion)
    {
        if(previousVersion < 3)
            new PopupWindow(() =>
            {
                ImGuiEx.Text($"""
                              Warning: Splatoon Script
                              {InternalData.Name}
                              was updated.
                              If you were using the fixed priority feature,
                              Please make sure to set the fixed roles again.
                              """);
            });
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true,
            radius = 4f,
            thicc = 6f,
            overlayText = "<< Go Here >>",
            overlayVOffset = 3f,
            overlayFScale = 3f
        };
        Controller.RegisterElement("Bait", element);
    }

    public override void OnReset()
    {
        _state = State.None;
        _partyDataList.Clear();
        _currentTowers = new IBattleNpc[3];
    }

    public override void OnUpdate()
    {
        if(_state == State.Split)
        {
            if(Controller.TryGetElementByName("Bait", out var el))
                el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
        else
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(_state != State.Split) return;
        if(TowerCastIds.Values.Any(x => x.Contains(set.Action.Value.RowId)))
            _state = State.End;
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(_state == State.Split) return;

        if((castId is 0x9CC7 or 0x9CC3 or 0x9CBD or 0x9CBA or 0x9CBE or 0x9CBB or 0x9CBF or 0x9CBC) && _partyDataList.Count != 8)
        {
            SetListEntityIdByJob();
            _state = State.Start;
        }

        if(!TowerCastIds.Values.Any(x => x.Contains(castId))) return;
        if(source.GetObject() is IBattleNpc npc)
        {
            switch(npc.Position.Z)
            {
                case < 95:
                    _currentTowers[0] = npc;
                    break;
                case < 105:
                    _currentTowers[1] = npc;
                    break;
                default:
                    _currentTowers[2] = npc;
                    break;
            }
        }

        DuoLog.Information($"[P1 Burn Strike Tower] Any(x => x == null) = {_currentTowers.Any(x => x == null)}");
        DuoLog.Information($"[P1 Burn Strike Tower] Any tower is set: {_partyDataList.Count(x => x != null)}");
        if(_currentTowers.Any(x => x == null) || _partyDataList.Count() != 8) return;

        DuoLog.Information("[P1 Burn Strike Tower] All towers are set");

        _state = State.Split;
        var towers = _currentTowers.Where(x => x != null).Select(x => x!).ToList();

        if(towers.Count != 3)
        {
            DuoLog.Warning("[P1 Burn Strike Tower] Tower is null");
            return;
        }

        var pc = GetMinedata();
        if(pc == null)
        {
            DuoLog.Warning("[P1 Burn Strike Tower] My data is null");
            return;
        }

        var nextAssignIndex = 2;
        for(var i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            var assignNum = TowerCount(tower.CastActionId);
            DuoLog.Information($"[P1 Burn Strike Tower] Tower {i + 1} is assigned to {assignNum} players");
            for(var j = 0; j < assignNum; j++)
            {
                var x = _partyDataList.Find(x => x.Index == nextAssignIndex);
                if(x == null)
                {
                    DuoLog.Warning("[P1 Burn Strike Tower] Assign data is null");
                    return;
                }
                DuoLog.Information($"[P1 Burn Strike Tower] {x.Object?.Name} is assigned to tower {i + 1}");
                x.AssignedTowerIndex = i;
                nextAssignIndex++;
            }
        }

        if(TankJobs.Contains(pc.Object.GetJob())) return;

        if(Controller.TryGetElementByName("Bait", out var bait))
        {
            bait.Enabled = true;
            bait.SetOffPosition(towers[pc.AssignedTowerIndex].Position);
        }
    }

    private enum State
    {
        None,
        Start,
        Split,
        End
    }

    private class PriorityData6 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 6;
        }
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
        Job.SCH,
        Job.SGE,
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

        public enum DirectionRelative : int
        {
            None = -1,
            East = 4,
            SouthEast = 3,
            South = 2,
            SouthWest = 3,
            West = 4,
            NorthWest = 5,
            North = 6,
            NorthEast = 5,
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
            // Distance, Center�̒l��p���āA�W�����̃x�N�g���𐶐�
            var directionalVectors = GenerateDirectionalVectors(Distance, Center ?? new Vector3(100, 0, 100));

            // �W�����̓��A�ł��߂������x�N�g�����擾
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
            if(direction == Direction.None) return Direction.None; // �����ȕ����̏ꍇ

            // �������i8����: North ~ NorthWest�j
            const int directionCount = 8;

            // �p�x��45�x�P�ʂɊۂ߁A-180�`180�͈̔͂ɐ��K��
            angle = ((Round45(angle) % 360) + 360) % 360; // ���̒l�ɕϊ�����360�Ő��K��
            if(angle > 180) angle -= 360;

            // ���݂̕����̃C���f�b�N�X
            var currentIndex = (int)direction;

            // 45�x���Ƃ̃X�e�b�v�v�Z�ƐV���������̌v�Z
            var step = angle / 45;
            var newIndex = (currentIndex + step + directionCount) % directionCount;

            return (Direction)newIndex;
        }

        public static LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
        {
            // �s���ȕ����̏ꍇ�iNone�j
            if(direction1 == Direction.None || direction2 == Direction.None)
                return LR.SameOrOpposite;

            // �������i8��: North ~ NorthWest�j
            var directionCount = 8;

            // �������z�I�Ɍv�Z
            var difference = ((int)direction2 - (int)direction1 + directionCount) % directionCount;

            // LR�𒼐ڕԂ�
            return difference == 0 || difference == directionCount / 2
                ? LR.SameOrOpposite
                : (difference < directionCount / 2 ? LR.Right : LR.Left);
        }

        public static int GetTwoPointAngle(Direction direction1, Direction direction2)
        {
            // �s���ȕ������l��
            if(direction1 == Direction.None || direction2 == Direction.None)
                return 0;

            // enum �̒l�𐔒l�Ƃ��Ĉ����A��̍������v�Z
            var diff = ((int)direction2 - (int)direction1 + 8) % 8;

            // ��������p�x���v�Z
            return diff <= 4 ? diff * 45 : (diff - 8) * 45;
        }

        public static float GetAngle(Direction direction)
        {
            if(direction == Direction.None) return 0; // �����ȕ����̏ꍇ

            // 45�x�P�ʂŌv�Z���A0�x����n�܂鎞�v���̊p�x��Ԃ�
            return (int)direction * 45 % 360;
        }

        private static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
        {
            var directionalVectors = new List<DirectionalVector>();

            // �e�����̃I�t�Z�b�g�v�Z
            foreach(Direction direction in Enum.GetValues(typeof(Direction)))
            {
                if(direction == Direction.None) continue; // None�̓X�L�b�v

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

                // ������K�p���č��W���v�Z
                var position = (center ?? new Vector3(100, 0, 100)) + (offset * distance);

                // ���X�g�ɒǉ�
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

        // _12ClockDirection��0�������Ƃ��āA�w�莞�v����̕������擾
        public DirectionCalculator.Direction GetDirectionFromClock(int clock)
        {
            if(!isValid)
                return DirectionCalculator.Direction.None;

            // ���ʃP�[�X: clock = 0 �̏ꍇ�A_12ClockDirection �����̂܂ܕԂ�
            if(clock == 0)
                return _12ClockDirection;

            // 12���v�ʒu��8�����Ƀ}�b�s���O
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

            // ���݂�12���������C���f�b�N�X�Ƃ��Ď擾
            var baseIndex = (int)_12ClockDirection;

            // ���v�ʒu�Ɋ�Â��X�e�b�v���擾
            var step = clockToDirectionMapping[clock];

            // �V�����������v�Z���A�͈͂𐳋K��
            var targetIndex = (baseIndex + step + 8) % 8;

            // �Ή����������Ԃ�
            return (DirectionCalculator.Direction)targetIndex;
        }

        public int GetClockFromDirection(DirectionCalculator.Direction direction)
        {
            if(!isValid)
                throw new InvalidOperationException("Invalid state: _12ClockDirection is not set.");

            if(direction == DirectionCalculator.Direction.None)
                throw new ArgumentException("Direction cannot be None.", nameof(direction));

            // �e�����ɑΉ�����ŏ��� clock �l���`
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

            // ���݂�12���������C���f�b�N�X�Ƃ��Ď擾
            var baseIndex = (int)_12ClockDirection;

            // �w�肳�ꂽ�����̃C���f�b�N�X
            var targetIndex = (int)direction;

            // �������v�Z���A���v�����ɐ��K��
            var step = (targetIndex - baseIndex + 8) % 8;

            // �Y������ clock ���擾
            return directionToClockMapping[step];
        }

        public float GetAngle(int clock) => DirectionCalculator.GetAngle(GetDirectionFromClock(clock));
    }

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private Vector3 BasePosition => new(100, 0, 100);

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
    /// Element�ւ̎��K�p�������s��"�匳"�̃��\�b�h�B
    /// </summary>
    private void InternalApplyElement(Element element, Vector3 position, float elementRadius, bool filled, bool tether)
    {
        element.Enabled = true;
        element.radius = elementRadius;
        element.tether = tether;
        element.Filled = filled;
        element.SetRefPosition(position);
    }

    //----------------------- ���JApplyElement���\�b�h�Q -----------------------

    // Element�C���X�^���X�ƒ��ړI�ȍ��W�w��
    public void ApplyElement(Element element, Vector3 position, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Element�C���X�^���X�Ɗp�x�w��
    public void ApplyElement(Element element, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        var position = CalculatePositionFromAngle(angle, radius);
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Element�C���X�^���X�ƕ����w��
    public void ApplyElement(Element element, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        var position = CalculatePositionFromDirection(direction, radius);
        InternalApplyElement(element, position, elementRadius, filled, tether);
    }

    // Element���ƒ��ړI�ȍ��W�w��
    public void ApplyElement(string elementName, Vector3 position, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element���Ɗp�x�w��
    public void ApplyElement(string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromAngle(angle, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element���ƕ����w��
    public void ApplyElement(string elementName, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
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

        // Ensure the result is within the 0�� to 360�� range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        var radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -�� to ��
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }

    public static Vector3 GetExtendedAndClampedPosition(
        Vector3 center, Vector3 currentPos, float extensionLength, float? limit)
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
}
#endregion