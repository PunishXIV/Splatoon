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
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P4_Darklit__Full_Toolers :SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    private enum State
    {
        None = 0,
        AkhRhai,
        AvoidAkhRhai,
        DarklitReady,
        tower,
        HalfCutStack,
        MTAttack
    }
    #endregion

    #region class
    /********************************************************************/
    /* class                                                            */
    /********************************************************************/
    public class Config :IEzConfig
    {
        public bool NorthSwap = false;
        public PriorityData Priority = new();
    }

    private class PartyData
    {
        public int Index { get; set; }
        public bool Mine => this.EntityId == Player.Object.EntityId;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)this.EntityId.GetObject()! ?? null;
        public uint TetherPairId1 = 0;
        public uint TetherPairId2 = 0;
        public DirectionCalculator.Direction TowerDirection = DirectionCalculator.Direction.None;
        public int ConeIndex = 0;
        public bool IsStack = false;

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
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private State _state = State.None;
    private List<PartyData> _partyDataList = new();
    private int _akhRhaiCount = 0;
    private string _wing = "";
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                   */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("BaitObject", new Element(1)
        {
            tether = true,
            refActorComparisonType = 2,
            radius = 0.5f,
            thicc = 6f
        });

        for (var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Circle{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 6f, fillIntensity = 0.5f });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 40300)
        {
            SetListEntityIdByJob();
            HideAllElements();
            ShowAkhRhaiReadyGuide(source);
            _state = State.AkhRhai;
        }

        if (_state == State.None) return;

        if (castId == 40237 && _akhRhaiCount < 8 && _state == State.AkhRhai)
        {
            if (_akhRhaiCount == 0)
            {
                HideAllElements();
                ShowAvoidAkhRhaiGuide();
            }
            ShowAkhRhai(source);
            _akhRhaiCount++;

            if (_akhRhaiCount == 8)
            {
                _state = State.AvoidAkhRhai;
            }
        }

        if (castId == 40227)
        {
            _wing = "Left"; // 左翼攻撃
            HideAllElements();
            ShowHalfCutStack();
            _state = State.HalfCutStack;
        }

        if (castId == 40228)
        {
            _wing = "Right"; // 右翼攻撃
            HideAllElements();
            ShowHalfCutStack();
            _state = State.HalfCutStack;
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if (castId is 40237 or 40187)
        {
            HideAllElements();
        }

        if (castId is 40227 or 40228)
        {
            HideAllElements();
            ShowMTAttack();
            _state = State.MTAttack;
        }

        if (castId == 40285)
        {
            this.OnReset();
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.None) return;

        if (Controller.TryGetElementByName("Bait", out var el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if (Controller.TryGetElementByName("BaitObject", out el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _akhRhaiCount = 0;
        _wing = "";
        HideAllElements();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (data2 == 0 && data3 == 110 && data5 == 15)
        {
            var partyData = _partyDataList.Find(x => x.EntityId == source);
            if (partyData == null) return;
            partyData.TetherPairId1 = target;

            if (_partyDataList.Where(x => x.TetherPairId1 != 0).Count() == 4)
            {
                HideAllElements();
                if (ParseTether())
                {
                    ShowTowerStateGuide();
                    _state = State.tower;
                }
                else
                {
                    _state = State.None;
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.EntityId.GetObject().Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TetherPairId1", true, () => ImGui.Text(x.TetherPairId1.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TetherPairId2", true, () => ImGui.Text(x.TetherPairId2.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("TowerDirection", true, () => ImGui.Text(x.TowerDirection.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("ConeIndex", true, () => ImGui.Text(x.ConeIndex.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsStack", true, () => ImGui.Text(x.IsStack.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsTank", true, () => ImGui.Text(x.IsTank.ToString())));

            }
            ImGuiEx.EzTable(Entries);
        }
    }
    #endregion

    #region private methods
    /********************************************************************/
    /* private methods                                                  */
    /********************************************************************/
    private void ShowAkhRhaiReadyGuide(uint entityId)
    {
        var pc = GetMinedata();
        if (pc == null) return;
        if (!entityId.TryGetObject(out var obj)) return;

        DirectionCalculator.Direction direction = DirectionCalculator.DividePoint(obj.Position, 10);

        var angle = DirectionCalculator.GetAngle(direction) + ((pc.Index == 0) ? -30 : 30);

        if (pc.Index == 0) ApplyElement("Bait", angle, 12);
        else ApplyElement("Bait", angle, 12);
    }

    private void ShowAvoidAkhRhaiGuide() => ApplyElement("Bait", DirectionCalculator.Direction.North, 0);

    private void ShowAkhRhai(uint entityID)
    {
        for (var i = 0; i < 8; i++)
        {
            if (Controller.TryGetElementByName($"Circle{i}", out var el))
            {
                if (el.Enabled) continue;
                el.Enabled = true;
                el.refActorObjectID = entityID;
                el.color = 0xC80000FF;
                el.radius = 4.0f;
                el.thicc = 2f;
                el.Filled = true;
                el.fillIntensity = 0.5f;
                break;
            }
        }
    }

    private void ShowTowerStateGuide()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        // 塔担当
        if (pc.TetherPairId1 != 0)
        {
            ApplyElement("Bait", pc.TowerDirection, 8f, 4f);
        }
        // 扇担当
        else
        {
            var myDirection = pc.ConeIndex switch
            {
                1 => DirectionCalculator.Direction.NorthEast,
                2 => DirectionCalculator.Direction.SouthEast,
                3 => DirectionCalculator.Direction.SouthWest,
                4 => DirectionCalculator.Direction.NorthWest,
                _ => DirectionCalculator.Direction.None
            };

            var correctionAngle = pc.ConeIndex switch
            {
                1 => 18,
                2 => -18,
                3 => 18,
                4 => -18,
                _ => 0
            };

            ApplyElement("Bait", DirectionCalculator.GetAngle(myDirection) + correctionAngle, 5f);
        }
    }

    private void ShowHalfCutStack()
    {
        if (_wing == "") return;

        var pc = GetMinedata();
        if (pc == null) return;

        float northCorrectionAngle = (_wing == "Left") ? 35 : -35;
        float southCorrectionAngle = (_wing == "Left") ? -35 : 35;

        ApplyElement(
            "Bait", DirectionCalculator.GetAngle(pc.TowerDirection) +
                ((pc.TowerDirection == DirectionCalculator.Direction.North) ? northCorrectionAngle : southCorrectionAngle), 5f);
    }

    private void ShowMTAttack()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        ApplyElement("Bait", DirectionCalculator.Direction.East, 19f);
    }


    private bool ParseTether()
    {
        foreach (var pc in _partyDataList)
        {
            if (pc.TetherPairId1 == 0) continue;

            var pair2 = _partyDataList.Find(x => x.TetherPairId1 == pc.EntityId);
            if (pair2 == null) continue;
            pc.TetherPairId2 = pair2.EntityId;
        }

        foreach (var pc in FakeParty.Get().Where(x => x.StatusList.Any(y => y.StatusId == 2461)))
        {
            var partyData = _partyDataList.Find(x => x.EntityId == pc.EntityId);
            if (partyData == null) return false;
            partyData.IsStack = true;
        }

        if (_partyDataList.Where(x => x.IsStack).Count() != 2) return false;
        if (_partyDataList.Where(x => x.TetherPairId1 != 0 && x.TetherPairId2 != 0).Count() != 4) return false;

        // 線付きヒラを取得
        var healer = _partyDataList.Find(x => x.IsHealer && x.TetherPairId1 != 0);
        if (healer == null) return false;

        // 線付きヒラは北確定
        healer.TowerDirection = DirectionCalculator.Direction.North;

        // 線付きヒラとつながっているDPSを取得
        var dps = _partyDataList
            .Find(x => x.TetherPairId1 == healer.EntityId || (x.TetherPairId2 == healer.EntityId && x.IsDps));
        if (dps == null) return false;

        // そのDPSはそのヒラと同じ塔に入れないので南
        dps.TowerDirection = DirectionCalculator.Direction.South;

        // もう一人のタンヒラを取得
        var tankHealer = _partyDataList.Find(x => x.EntityId != healer.EntityId && x.TetherPairId1 != 0 && x.IsTH);
        if (tankHealer == null) return false;

        // タンヒラは線付きヒラと同じ塔に入れないので南
        tankHealer.TowerDirection = DirectionCalculator.Direction.South;

        // 残りの1人は北
        var dps2 = _partyDataList.Find(x => !new[] { healer, dps, tankHealer }.Contains(x) && x.TetherPairId1 != 0);
        if (dps2 == null) return false;

        dps2.TowerDirection = DirectionCalculator.Direction.North;

        // 線のついていない4人を取得
        var noneTether = _partyDataList.Where(x => x.TetherPairId1 == 0 && x.TetherPairId2 == 0).ToList();
        if (noneTether.Count != 4) return false;

        // 上からConeIndexを割り振る
        for (var i = 0; i < noneTether.Count; i++)
        {
            noneTether[i].ConeIndex = i + 1;
        }

        // 頭割り調整 (わからないので全てのパターンを書く) TODO: あとで不要なものを削除
        // 線付きに頭割り対象がいる場合
        if (healer.IsStack && tankHealer.IsStack)
        {
            DuoLog.Information("Healer and TankHealer are stacker");
            // 両方の場合は調整のしようがないのでそのまま
            // 線無し4人を割り振る
            for (var i = 0; i < noneTether.Count; i++)
            {
                if (i >= 2) noneTether[i].TowerDirection = DirectionCalculator.Direction.North;
                else noneTether[i].TowerDirection = DirectionCalculator.Direction.South;
            }
        }
        else if (healer.IsStack)
        {
            DuoLog.Information("Healer is stacker");
            // 線無しに1人頭割り対象がいるので逆に配置
            var noneTetherStacker = noneTether.Find(x => x.IsStack);
            if (noneTetherStacker == null) return false;

            DuoLog.Information($"noneTetherStacker: {noneTetherStacker.Object?.Name}");

            if (healer.TowerDirection == DirectionCalculator.Direction.North)
            {
                noneTetherStacker.TowerDirection = DirectionCalculator.Direction.South;
            }
            else
            {
                noneTetherStacker.TowerDirection = DirectionCalculator.Direction.North;
            }

            // 線無し3人を割り振る
            noneTether = noneTether.Where(x => !x.IsStack).ToList();
            if (noneTether.Count != 3) return false;

            if (noneTetherStacker.TowerDirection != DirectionCalculator.Direction.North)
            {
                for (var i = 0; i < noneTether.Count; i++)
                {
                    if (i >= 1) noneTether[i].TowerDirection = DirectionCalculator.Direction.North;
                    else noneTether[i].TowerDirection = DirectionCalculator.Direction.South;
                }
            }
            else
            {
                for (var i = 0; i < noneTether.Count; i++)
                {
                    if (i >= 1) noneTether[i].TowerDirection = DirectionCalculator.Direction.South;
                    else noneTether[i].TowerDirection = DirectionCalculator.Direction.North;
                }
            }

        }
        else if (tankHealer.IsTank)
        {
            DuoLog.Information("tankHealer.IsTank");
            // 線無しに1人頭割り対象がいるので逆に配置
            var noneTetherStacker = noneTether.Find(x => x.IsStack);
            if (noneTetherStacker == null) return false;

            if (tankHealer.TowerDirection == DirectionCalculator.Direction.North)
            {
                noneTetherStacker.TowerDirection = DirectionCalculator.Direction.South;
            }
            else
            {
                noneTetherStacker.TowerDirection = DirectionCalculator.Direction.North;
            }

            // 線無し3人を割り振る
            noneTether = noneTether.Where(x => !x.IsStack).ToList();
            if (noneTether.Count != 3) return false;

            if (noneTetherStacker.TowerDirection != DirectionCalculator.Direction.North)
            {
                for (var i = 0; i < noneTether.Count; i++)
                {
                    if (i >= 1) noneTether[i].TowerDirection = DirectionCalculator.Direction.North;
                    else noneTether[i].TowerDirection = DirectionCalculator.Direction.South;
                }
            }
            else
            {
                for (var i = 0; i < noneTether.Count; i++)
                {
                    if (i >= 1) noneTether[i].TowerDirection = DirectionCalculator.Direction.South;
                    else noneTether[i].TowerDirection = DirectionCalculator.Direction.North;
                }
            }
        }
        // どちらもいない場合
        else
        {
            DuoLog.Information("No stacker");
            // 線無し頭割り対象を取得
            var noneTetherStacker = noneTether.Where(x => x.IsStack).ToList();
            if (noneTetherStacker.Count() != 2) return false;

            noneTetherStacker[0].TowerDirection = DirectionCalculator.Direction.North;
            noneTetherStacker[1].TowerDirection = DirectionCalculator.Direction.South;

            // 残りの2人を割り振る
            var noneTetherNormal = noneTether.Where(x => !x.IsStack).ToList();
            if (noneTetherNormal.Count() != 2) return false;

            noneTetherNormal[0].TowerDirection = DirectionCalculator.Direction.North;
            noneTetherNormal[1].TowerDirection = DirectionCalculator.Direction.South;
        }

        DuoLog.Information("ParseTether: Success");

        // 上記の全てが正しく代入されたかを確認
        if (_partyDataList.All(x => x.TowerDirection == DirectionCalculator.Direction.None)) return false;
        if (_partyDataList.Where(x => x.ConeIndex == 0).Count() != 4) return false;

        return true;
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

    private void ApplyElement(
        string elementName,
        DirectionCalculator.Direction direction,
        float radius = 0f,
        float elementRadius = 0.3f,
        bool tether = true)
    {
        var position = new Vector3(100, 0, 100);
        var angle = DirectionCalculator.GetAngle(direction);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.tether = tether;
            element.SetRefPosition(position);
        }
    }

    private void ApplyElement(
        string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool tether = true)
    {
        var position = new Vector3(100, 0, 100);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.tether = tether;
            element.SetRefPosition(position);
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
