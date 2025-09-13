using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P1_Fall_of_Faith_Full_Tooler_Party : SplatoonScript
{
    #region Enums
    private enum State
    {
        None,
        Casting,
        Soil1End,
        Soil2End,
        Soil3End,
        End
    }

    private enum LR
    {
        Left,
        Right
    }

    private enum FireThunder
    {
        Fire,
        Thunder
    }
    #endregion

    #region class
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
        public LR LR;
        public int PriorityNum = 0;
        public int TetherNum = 0;
        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
            LR = LR.Left;
            PriorityNum = 0;
            TetherNum = 0;
        }
    }
    #endregion

    #region public Fields
    public override HashSet<uint>? ValidTerritories { get; } = [1238];
    public override Metadata? Metadata => new(8, "Redmoon");
    #endregion

    #region private Fields
    private List<PartyData> _partyDataList = [];
    private List<FireThunder> _fireThunders = [];
    private State _state = State.None;
    private int _soilEndCount = 0;
    private bool _gimmickEnded = false;
    #endregion

    #region Public Methods
    public override void OnSetup()
    {
        Controller.RegisterElement("LeftTether", new Splatoon.Element(0) { refX = 94.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("LeftTetherNext", new Splatoon.Element(0) { refX = 94.0f, refY = 98.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("LeftNone1", new Splatoon.Element(0) { refX = 94.0f, refY = 102.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("LeftNone2", new Splatoon.Element(0) { refX = 92.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightTether", new Splatoon.Element(0) { refX = 106.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightTetherNext", new Splatoon.Element(0) { refX = 106.0f, refY = 98.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightNone1", new Splatoon.Element(0) { refX = 106.0f, refY = 102.0f, thicc = 10.0f, tether = true, radius = 0.5f });
        Controller.RegisterElement("RightNone2", new Splatoon.Element(0) { refX = 108.0f, refY = 100.0f, thicc = 10.0f, tether = true, radius = 0.5f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!_gimmickEnded && _state == State.None && castId is 40170) // Fall of Faith Cast Too Late
        {
            SetListEntityIdByJob();
            _state = State.Casting;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        if(_state == State.None) return;
        if(set.Action.Value.RowId is 40156 or 40142)
        {
            ++_state;
            if(_state == State.End)
            {
                _state = State.None;
                _gimmickEnded = true;
                HideAllElements();
            }
            ShowElements();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!(_state == State.Casting)) return;

        var pc = _partyDataList.Find(x => x.EntityId == target);
        if(pc == null) return;

        if(data2 == 0 && data3 == 249 && data5 == 15) // fire
        {
            _fireThunders.Add(FireThunder.Fire);
        }
        else if(data2 == 0 && data3 == 287 && data5 == 15) // thunder
        {
            _fireThunders.Add(FireThunder.Thunder);
        }

        if(_fireThunders.Count is 1 or 3)
        {
            pc.LR = LR.Left;
        }
        else
        {
            pc.LR = LR.Right;
        }

        pc.TetherNum = _fireThunders.Count;

        if(target == Player.Object.EntityId)
        {
            if(_fireThunders.Count == 1)
            {
                if(Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
            }
            else if(_fireThunders.Count == 2)
            {
                if(Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
            }
            else if(_fireThunders.Count == 3)
            {
                if(_fireThunders[0] == FireThunder.Thunder)
                {
                    if(Controller.TryGetElementByName("LeftTetherNext", out var el)) el.Enabled = true;
                }
                else
                {
                    if(Controller.TryGetElementByName("LeftNone2", out var el)) el.Enabled = true;
                }
            }
            else
            {
                if(_fireThunders[1] == FireThunder.Thunder)
                {
                    if(Controller.TryGetElementByName("RightTetherNext", out var el)) el.Enabled = true;
                }
                else
                {
                    if(Controller.TryGetElementByName("RightNone2", out var el)) el.Enabled = true;
                }
            }
        }

        if(_fireThunders.Count == 4)
        {
            ParseData();
            ShowElements();
        }
    }

    public override void OnUpdate()
    {
        if(_state == State.None || _gimmickEnded) return;

        var el = Controller.GetRegisteredElements().Where(Element => Element.Value.Enabled).FirstOrDefault().Value;
        if(el == null) return;
        el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
    }

    public override void OnReset()
    {
        _partyDataList.Clear();
        _fireThunders.Clear();
        _state = State.None;
        _soilEndCount = 0;
        _gimmickEnded = false;
        HideAllElements();
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Tether Count: {_fireThunders.Count}");
            ImGui.Text($"Soil End Count: {_soilEndCount}");
            ImGui.Text($"Gimmick Ended: {_gimmickEnded}");
            if(_fireThunders.Count > 0)
            {
                ImGui.Text("Fire Thunders:");
                foreach(var x in _fireThunders)
                {
                    ImGui.Text(x.ToString());
                }
            }
            else
            {
                ImGui.Text($"Fire Thunders: None");
            }

            ImGui.NewLine();
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach(var x in _partyDataList)
            {
                if(!x.EntityId.TryGetObject(out var pc)) continue;
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(pc.Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("ObjectId", () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("LR", () => ImGui.Text(x.LR.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("PriorityNum", () => ImGui.Text(x.PriorityNum.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TetherNum", () => ImGui.Text(x.TetherNum.ToString())));
            }

            ImGuiEx.EzTable(Entries);
        }
    }
    #endregion

    #region Private Methods
    private void ParseData()
    {
        var NoBuffers = _partyDataList.Where(x => x.Object?.StatusList.All(z => z.StatusId != 1051) == true);

        var i = 0;
        foreach(var NoBuffer in NoBuffers)
        {
            NoBuffer.LR = (i < 2) ? LR.Left : LR.Right;
            ++i;
        }

        var leftNoBuffPcs = _partyDataList.Where(x => x.LR == LR.Left && x.EntityId.GetObject() is IPlayerCharacter pc && pc.StatusList.All(z => z.StatusId != 1051)).ToList();
        var rightNoBuffPcs = _partyDataList.Where(x => x.LR == LR.Right && x.EntityId.GetObject() is IPlayerCharacter pc && pc.StatusList.All(z => z.StatusId != 1051)).ToList();

        if(leftNoBuffPcs.Count != 2 || rightNoBuffPcs.Count != 2)
        {
            DuoLog.Error("No Buffer Priority List is not 2");
            _state = State.End;
            return;
        }

        leftNoBuffPcs[0].PriorityNum = 1;
        leftNoBuffPcs[1].PriorityNum = 2;
        rightNoBuffPcs[0].PriorityNum = 1;
        rightNoBuffPcs[1].PriorityNum = 2;
    }

    private void ShowElements()
    {
        HideAllElements();
        if(_state == State.Casting)
        {
            foreach(var pc in _partyDataList)
            {
                if(pc.EntityId != Player.Object.EntityId) continue;
                if(pc.LR == LR.Left)
                {
                    if(_fireThunders[0] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 1 && Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("LeftNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 1 && Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 3 && Controller.TryGetElementByName("LeftTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("LeftNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("LeftNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
                else
                {
                    if(_fireThunders[1] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 2 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 2 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 4 && Controller.TryGetElementByName("RightTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("RightNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
            }
        }
        else if(_state == State.Soil1End)
        {
            foreach(var pc in _partyDataList)
            {
                if(pc.EntityId != Player.Object.EntityId) continue;
                if(pc.LR == LR.Left)
                {
                    if(_fireThunders[2] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 3 && Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("LeftNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 3 && Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 1 && Controller.TryGetElementByName("LeftTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("LeftNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("LeftNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
                else
                {
                    if(_fireThunders[1] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 2 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 2 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 4 && Controller.TryGetElementByName("RightTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("RightNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
            }
        }
        else if(_state == State.Soil2End)
        {
            foreach(var pc in _partyDataList)
            {
                if(pc.EntityId != Player.Object.EntityId) continue;
                if(pc.LR == LR.Left)
                {
                    if(_fireThunders[2] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 3 && Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("LeftNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 3 && Controller.TryGetElementByName("LeftTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 1 && Controller.TryGetElementByName("LeftTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("LeftNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("LeftNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
                else
                {
                    if(_fireThunders[3] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 4 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 4 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 2 && Controller.TryGetElementByName("RightTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("RightNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
            }
        }
        else if(_state == State.Soil3End)
        {
            foreach(var pc in _partyDataList)
            {
                if(pc.EntityId != Player.Object.EntityId) continue;
                if(pc.LR == LR.Left)
                {
                    if(Controller.TryGetElementByName("LeftNone2", out var el)) el.Enabled = true;
                    break;
                }
                else
                {
                    if(_fireThunders[3] == FireThunder.Fire)
                    {
                        if(pc.TetherNum == 4 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                    else
                    {
                        if(pc.TetherNum == 4 && Controller.TryGetElementByName("RightTether", out var el)) el.Enabled = true;
                        else if(pc.TetherNum == 2 && Controller.TryGetElementByName("RightTetherNext", out el)) el.Enabled = true;
                        else if(pc.PriorityNum == 1 && Controller.TryGetElementByName("RightNone1", out el)) el.Enabled = true;
                        else if(Controller.TryGetElementByName("RightNone2", out el)) el.Enabled = true;
                        break;
                    }
                }
            }
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
        Job.BRD,
        Job.MCH,
        Job.DNC,
        Job.RDM,
        Job.SMN,
        Job.PCT,
        Job.BLM,
        Job.DRG,
        Job.VPR,
        Job.SAM,
        Job.MNK,
        Job.RPR,
        Job.NIN,
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
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と角度指定
    public void ApplyElement(string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromAngle(angle, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と方向指定
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
    #endregion
}
