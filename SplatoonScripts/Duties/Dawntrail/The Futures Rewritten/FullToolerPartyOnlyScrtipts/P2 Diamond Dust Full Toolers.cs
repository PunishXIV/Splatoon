using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal unsafe class P2_Diamond_Dust_Full_Toolers :SplatoonScript
{
    #region enums
    public enum Direction
    {
        None = 0,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public enum LR
    {
        Left = 0,
        Right
    }

    private enum State
    {
        None = 0,
        CastedDD,
        DataIsOk,
        DynamoChariotCasting,
        AOESet,
        KnockbackEnded,
        HitHoly,
        HitHolyEnded,
        PetriEnded,
        Slice1Casting,
        Slice1Casted
    }

    private enum AoeType
    {
        None = 0,
        Circle,
        Donut
    }
    #endregion

    #region class
    public class Config :IEzConfig
    {
        public bool LockFace = false;
        public bool Debug = false;
    }

    private class PartyData
    {
        public int index;
        public bool Mine;
        public uint EntityId;
        public bool HasAoe;
        public Direction AvoidDrection;
        public Direction KnockBackDirection;
        public Direction MoveTargetDirection;

        public PartyData(uint entityId)
        {
            Mine = false;
            EntityId = entityId;
            HasAoe = false;
            AvoidDrection = Direction.None;
            KnockBackDirection = Direction.None;
            MoveTargetDirection = Direction.None;
        }
    }

    public struct DirectionalVector
    {
        public Direction Direction { get; }
        public Vector3 Position { get; }

        public DirectionalVector(Direction direction, Vector3 position)
        {
            Direction = direction;
            Position = position;
        }
    }
    #endregion

    #region const
    private const uint DonutCastId = 40203;
    private const uint CircleCastId = 40202;
    private IReadOnlyList<DirectionalVector> directionalVectors = new List<DirectionalVector>
    {
        new DirectionalVector(Direction.North, new Vector3(100, 0, 84)),
        new DirectionalVector(Direction.NorthEast, new Vector3(111.314f, 0, 88.686f)),
        new DirectionalVector(Direction.East, new Vector3(116, 0, 100)),
        new DirectionalVector(Direction.SouthEast, new Vector3(111.314f, 0, 111.314f)),
        new DirectionalVector(Direction.South, new Vector3(100, 0, 116)),
        new DirectionalVector(Direction.SouthWest, new Vector3(88.686f, 0, 111.314f)),
        new DirectionalVector(Direction.West, new Vector3(84, 0, 100)),
        new DirectionalVector(Direction.NorthWest, new Vector3(88.686f, 0, 88.686f))
    };
    #endregion

    #region public properties
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(4, "redmoon");
    #endregion

    #region private properties
    private static ActionManager* ActionManager => FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
    private AoeType _aoeType = AoeType.None;
    private Direction _firstIcicleImpactDirection = Direction.None;
    private bool _deg45Rotated = false;
    private State _state = State.None;
    private bool _startLockFace = false;
    private List<PartyData> _partyDataList = new();
    private uint _sliceEnemyEntityId = 0;
    private int _holyNum = 0;
    private uint _sliceCastId = 0;
    private uint _dressUpId1 = 0;
    private uint _dressUpId2 = 0;
    private Config C => Controller.GetConfig<Config>();
    #endregion

    #region public methods
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0)
        {
            tether = true,
            radius = 3f,
            thicc = 6f
        });

        Controller.RegisterElement("Bait2", new Element(0)
        {
            tether = true,
            radius = 3f,
            thicc = 6f
        });

        Controller.RegisterElement("SlideRange", new Element(1)
        {
            Filled = false,
            refActorComparisonType = 2,
            radius = 32f,
            thicc = 3f
        });

        Controller.RegisterElement("StackRange", new Element(1)
        {
            color = 0xC800FF00,
            Filled = false,
            refActorComparisonType = 2,
            radius = 6f,
            thicc = 3f
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == CircleCastId) _aoeType = AoeType.Circle;
        else if (castId == DonutCastId) _aoeType = AoeType.Donut;
        else if (castId == 40198u && _firstIcicleImpactDirection == Direction.None)
        {
            Direction direction = DividePoint(source.GetObject().Position);

            // 北,北東, 東, 南東以外の方向は対角の方向に変換
            if (direction == Direction.NorthWest)
            {
                direction = Direction.SouthEast;
            }
            else if (direction == Direction.West)
            {
                direction = Direction.East;
            }
            else if (direction == Direction.SouthWest)
            {
                direction = Direction.NorthEast;
            }
            else if (direction == Direction.South)
            {
                direction = Direction.North;
            }

            _firstIcicleImpactDirection = direction;

            // もし、方角が斜めの場合は45度回転させる
            if (direction == Direction.NorthEast || direction == Direction.SouthEast)
            {
                _deg45Rotated = true;
            }
        }
        else if (castId == 40208u)
        {
            _sliceEnemyEntityId = source;
        }
        else if (castId is 40193u or 40194u) // 40193: Slice1Front, 40194: Slice1Back
        {
            _state = State.Slice1Casting;
            _sliceCastId = castId;
            HideAllElements();
            ShowAvoidSliceFirst();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!set.Action.HasValue) return;
        if (set.Action.Value.RowId == 40197u)
        {
            // キャスト開始時にリストを初期化
            // この時点でリストにはエンティティIDが入っている
            // このリストはジョブによってエンティティIDを入れ替える
            SetListEntityIdByJob();

            if (_partyDataList.Count == 0) return;
            else _state = State.CastedDD;
        }
        if (set.Action.Value.RowId == 40203)
        {
            HideAllElements();
            ShowSetIciclePosWhenDynamo();
        }
        if (set.Action.Value.RowId == 40202)
        {
            HideAllElements();
            ShowSetIciclePosWhenChariot();
        }
        if (set.Action.Value.RowId == 40207 && _state != State.KnockbackEnded)
        {
            _state = State.KnockbackEnded;
            HideAllElements();
            ShowSetAoeStartEnd();
            _startLockFace = true;
        }
        if (set.Action.Value.RowId == 40209)
        {
            _state = State.HitHoly;
            ++_holyNum;
            if (_holyNum == 1)
            {
                _state = State.HitHoly;
                HideAllElements();
                ShowSetAoeEnd();
            }
            else if (_holyNum == 8)
            {
                _state = State.HitHolyEnded;
                HideAllElements();
            }
        }
        else if (set.Action.Value.RowId is 40184 or 40185)
        {
            _state = State.PetriEnded;
            HideAllElements();
            _startLockFace = false;
        }
        else if (set.Action.Value.RowId is 40193u or 40194u)
        {

            _state = State.Slice1Casted;
            HideAllElements();
            ShowAvoidSliceSecond();
        }
        if (set.Action.Value.RowId is 40195u or 40196u)
        {
            this.OnReset();
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (_state < State.KnockbackEnded) return;

        if (vfxPath == "vfx/common/eff/dk02ht_zan0m.avfx" && (_dressUpId1 == 0 || _dressUpId2 == 0))
        {
            if (_dressUpId1 == 0)
            {
                _dressUpId1 = target;
            }
            else if (_dressUpId1 != target)
            {
                _dressUpId2 = target;
            }
        }
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, ulong targetId, byte replaying)
    {
        if (command == 34 && p1 == 345 && sourceId == p2)
        {
            var pcdata = _partyDataList.Find(x => x.EntityId == p2);
            if (pcdata == null) return;
            pcdata.HasAoe = true;
        }
    }

    public override void OnUpdate()
    {
        if (_state == State.None) return;
        if (_state == State.CastedDD)
        {
            if (!IsCompleteData()) return;
            _state = State.DataIsOk;
            ParseData();
            ShowAvoidKick();
            _state = State.DynamoChariotCasting;
        }
        if (_state == State.DynamoChariotCasting)
        {
            var obj = Svc.Objects.First(x => x.DataId == 0x45A0);
            if (Vector3.Distance(obj.Position, new(100, 0, 100)) > 9.8f)
            {
                ShowKnockBack();
                _state = State.AOESet;
            }
        }
        if (_state == State.KnockbackEnded || _state == State.HitHoly || _state == State.HitHolyEnded)
        {
            var el = Controller.GetElementByName("Bait");
            if (el != null)
            {
                el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
            }
        }
        else
        {
            Controller.GetRegisteredElements()
                .Where(x => x.Value.Enabled == true)
                .Each(x => x.Value.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint());
        }

        if (_startLockFace && _dressUpId1 != 0 && _dressUpId2 != 0)
        {
            LockFace();
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Lock Face", ref C.LockFace);
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Checkbox("Debug", ref C.Debug);
            ImGui.Text($"State: {_state}");
            ImGui.Text($"AOE Type: {_aoeType}");
            ImGui.Text($"First Icicle Impact Direction: {_firstIcicleImpactDirection}");
            ImGui.Text($"Deg45Rotated: {_deg45Rotated}");
            ImGui.Text($"SliceEnemyEntityId: {_sliceEnemyEntityId}");
            ImGui.Text($"HolyNum: {_holyNum}");
            ImGui.Text($"SliceCastId: {_sliceCastId}");
            ImGui.Text($"StartLockFace: {_startLockFace}");
            ImGui.Text($"PartyDataList: {_partyDataList.Count}");
            ImGui.Text($"DressUpId1: {_dressUpId1}");
            ImGui.Text($"DressUpId2: {_dressUpId2}");
            var obj = Svc.Objects.First(x => x.DataId == 0x45A0);
            ImGui.Text($"MOB: {obj.Name} {obj.Position} {obj.EntityId}");
            if (Vector3.Distance(obj.Position, new(100, 0, 100)) > 9.8f)
            {
                ImGui.Text($"Direction1: {NearDividePoint(obj.Position)}");
                ImGui.Text($"Direction2: {DividePoint(Svc.Objects.First(x => x.DataId == 0x459F).Position)}");
                ImGui.Text($"MyDirection: {DividePoint(Player.Object.Position)}");
            }
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.EntityId.GetObject().Name.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("HasAoe", true, () => ImGui.Text(x.HasAoe.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("AvoidDirection", true, () => ImGui.Text(x.AvoidDrection.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("KnockBackDirection", true, () => ImGui.Text(x.KnockBackDirection.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("MoveTargetDirection", true, () => ImGui.Text(x.MoveTargetDirection.ToString())));
            }

            ImGuiEx.EzTable(Entries);

        }
    }

    public override void OnReset()
    {
        _aoeType = AoeType.None;
        _firstIcicleImpactDirection = Direction.None;
        _deg45Rotated = false;
        _state = State.None;
        _startLockFace = false;
        _partyDataList.Clear();
        _sliceEnemyEntityId = 0;
        _holyNum = 0;
        _sliceCastId = 0;
        _dressUpId1 = 0;
        _dressUpId2 = 0;
        HideAllElements();
    }
    #endregion

    #region private methods
    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();

        // リストに８人分の初期インスタンス生成
        for (var i = 0; i < 8; i++)
        {
            _partyDataList.Add(new PartyData(0));
            _partyDataList[i].index = i;
        }

        foreach (var pc in FakeParty.Get())
        {
            var job = pc.GetJob();
            switch (job)
            {
                case Job.SCH:
                case Job.SGE:
                _partyDataList[0].EntityId = pc.EntityId;
                break;

                case Job.WAR:
                case Job.DRK:
                case Job.GNB:
                _partyDataList[1].EntityId = pc.EntityId;
                break;

                case Job.PLD:
                _partyDataList[2].EntityId = pc.EntityId;
                break;

                case Job.PCT:
                _partyDataList[3].EntityId = pc.EntityId;
                break;

                case Job.SAM:
                case Job.MNK:
                case Job.DRG:
                case Job.RPR:
                _partyDataList[4].EntityId = pc.EntityId;
                break;

                case Job.NIN:
                case Job.VPR:
                case Job.RDM:
                case Job.BLM:
                case Job.SMN:
                _partyDataList[5].EntityId = pc.EntityId;
                break;

                case Job.BRD:
                case Job.MCH:
                case Job.DNC:
                _partyDataList[6].EntityId = pc.EntityId;
                break;

                case Job.WHM:
                case Job.AST:
                _partyDataList[7].EntityId = pc.EntityId;
                break;
            }
        }

        PartyData mine = _partyDataList.Find(x => x.EntityId == Player.Object.EntityId);
        if (mine != null) mine.Mine = true;
    }

    private IReadOnlyList<DirectionalVector> nearDirectionalVectors = new List<DirectionalVector>
    {
        new DirectionalVector(Direction.North, new Vector3(100, 0, 90)),
        new DirectionalVector(Direction.NorthEast, new Vector3(107.08789f, 0, 92.91211f)),
        new DirectionalVector(Direction.East, new Vector3(110, 0, 100)),
        new DirectionalVector(Direction.SouthEast, new Vector3(107.08789f, 0, 107.04199f)),
        new DirectionalVector(Direction.South, new Vector3(100f, 0, 110f)),
        new DirectionalVector(Direction.SouthWest, new Vector3(92.91211f, 0, 107.04199f)),
        new DirectionalVector(Direction.West, new Vector3(90f, 0, 100f)),
        new DirectionalVector(Direction.NorthWest, new Vector3(107.04199f, 0, 92.91211f))
    };

    private Direction NearDividePoint(Vector3 Position)
    {
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


    private Direction DividePoint(Vector3 Position)
    {
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

    // 2つのDirectionを比較して、角度を返す。角度は正しい値ではなく0, 45, 90, 135, 180の値を返す
    private int GetTwoPointAngle(Direction direction1, Direction direction2)
    {
        // enumの値を数値に変換
        int angle1 = (int)direction1;
        int angle2 = (int)direction2;

        // 環状の差分を計算
        int diff = (angle2 - angle1 + 8) % 8; // 環状に補正して差分を取得

        // 差分に応じた角度を計算（時計回りで正、反時計回りで負）
        int angle = diff switch
        {
            0 => 0,
            1 => 45,
            2 => 90,
            3 => 135,
            4 => 180,
            5 => -135,
            6 => -90,
            7 => -45,
            _ => 0 // このケースは通常発生しない
        };

        return angle;
    }

    // 2つのDirectionを比較して、左右どちらかを返す。左なら-1、右なら1、同じまたは逆なら0を返す
    private LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
    {
        if (direction1 == Direction.North && direction2 == Direction.NorthEast) return LR.Right;
        if (direction1 == Direction.NorthEast && direction2 == Direction.East) return LR.Right;
        if (direction1 == Direction.East && direction2 == Direction.SouthEast) return LR.Right;
        if (direction1 == Direction.SouthEast && direction2 == Direction.South) return LR.Right;
        if (direction1 == Direction.South && direction2 == Direction.SouthWest) return LR.Right;
        if (direction1 == Direction.SouthWest && direction2 == Direction.West) return LR.Right;
        if (direction1 == Direction.West && direction2 == Direction.NorthWest) return LR.Right;
        if (direction1 == Direction.NorthWest && direction2 == Direction.North) return LR.Right;

        if (direction1 == Direction.North && direction2 == Direction.West) return LR.Left;
        if (direction1 == Direction.West && direction2 == Direction.South) return LR.Left;
        if (direction1 == Direction.South && direction2 == Direction.East) return LR.Left;
        if (direction1 == Direction.East && direction2 == Direction.North) return LR.Left;

        if (direction1 == Direction.North && direction2 == Direction.SouthEast) return LR.Right;
        if (direction1 == Direction.NorthEast && direction2 == Direction.South) return LR.Right;
        if (direction1 == Direction.East && direction2 == Direction.SouthWest) return LR.Right;
        if (direction1 == Direction.SouthEast && direction2 == Direction.West) return LR.Right;
        if (direction1 == Direction.South && direction2 == Direction.NorthWest) return LR.Right;
        if (direction1 == Direction.SouthWest && direction2 == Direction.North) return LR.Right;
        if (direction1 == Direction.West && direction2 == Direction.NorthEast) return LR.Right;
        if (direction1 == Direction.NorthWest && direction2 == Direction.East) return LR.Right;

        return LR.Left;
    }

    private bool IsCompleteData()
    {
        if (_partyDataList.Count != 8) return false;
        if (_partyDataList.Where(x => x.Mine).ToList().Count != 1) return false;
        if (_partyDataList.Where(x => x.HasAoe).ToList().Count != 4) return false;
        if (_aoeType == AoeType.None) return false;
        if (_firstIcicleImpactDirection == Direction.None) return false;

        for (var i = 0; i < 8; i++)
        {
            if (i < 4)
            {
                _partyDataList[i].KnockBackDirection = _firstIcicleImpactDirection switch
                {
                    Direction.North => Direction.North,
                    Direction.NorthEast => Direction.NorthEast,
                    Direction.East => Direction.East,
                    Direction.SouthEast => Direction.SouthEast,
                    _ => Direction.None
                };
            }
            else
            {
                _partyDataList[i].KnockBackDirection = _firstIcicleImpactDirection switch
                {
                    Direction.North => Direction.South,
                    Direction.NorthEast => Direction.SouthWest,
                    Direction.East => Direction.West,
                    Direction.SouthEast => Direction.NorthWest,
                };
            }

        }

        return true;
    }

    void ParseData()
    {
        var hasAoePcs = _partyDataList.Where(x => x.HasAoe).ToList();
        var noAoePcs = _partyDataList.Where(x => !x.HasAoe).ToList();
        if (_deg45Rotated == false)
        {
            hasAoePcs[0].AvoidDrection = Direction.NorthEast;
            hasAoePcs[1].AvoidDrection = Direction.SouthEast;
            hasAoePcs[2].AvoidDrection = Direction.SouthWest;
            hasAoePcs[3].AvoidDrection = Direction.NorthWest;
            noAoePcs[0].AvoidDrection = Direction.North;
            noAoePcs[1].AvoidDrection = Direction.East;
            noAoePcs[2].AvoidDrection = Direction.South;
            noAoePcs[3].AvoidDrection = Direction.West;
        }
        else if (_deg45Rotated == true)
        {
            hasAoePcs[0].AvoidDrection = Direction.North;
            hasAoePcs[1].AvoidDrection = Direction.East;
            hasAoePcs[2].AvoidDrection = Direction.South;
            hasAoePcs[3].AvoidDrection = Direction.West;
            noAoePcs[0].AvoidDrection = Direction.NorthEast;
            noAoePcs[1].AvoidDrection = Direction.SouthEast;
            noAoePcs[2].AvoidDrection = Direction.SouthWest;
            noAoePcs[3].AvoidDrection = Direction.NorthWest;
        }
    }

    private void ShowAvoidKick()
    {
        var pc = _partyDataList.Find(x => x.Mine);
        if (pc == null) return;

        if (_aoeType == AoeType.Circle)
        {
            if (pc.HasAoe) ApplyElement("Bait", pc.AvoidDrection, 20f - 1.5f);
            else ApplyElement("Bait", pc.AvoidDrection, 20f - 3.5f);

        }
        else if (_aoeType == AoeType.Donut)
        {
            if (pc.HasAoe) ApplyElement("Bait", pc.AvoidDrection, 3.5f, 0.5f);
            else ApplyElement("Bait", pc.AvoidDrection, 1.5f, 0.5f);
        }
    }

    private void ShowSetIciclePosWhenDynamo()
    {
        var pc = _partyDataList.Find(x => x.Mine);
        if (pc == null) return;
        if (pc.HasAoe) ApplyElement("Bait", pc.AvoidDrection, 8.0f, 0.5f);
        else ApplyElement("Bait", pc.AvoidDrection, 0.0f, 0.5f);
    }

    private void ShowSetIciclePosWhenChariot()
    {
        var pc = _partyDataList.Find(x => x.Mine);
        if (pc == null) return;
        if (pc.HasAoe) ApplyElement("Bait", pc.AvoidDrection, 8.4f, 0.5f);
        else ApplyElement("Bait", pc.AvoidDrection, 0.0f, 0.5f);
    }

    private void ShowKnockBack()
    {
        var pc = _partyDataList.Find(x => x.Mine);
        if (pc == null) return;

        var obj = Svc.Objects.First(x => x.DataId == 0x45A0);

        // ノックバック位置最終確定
        float angle = GetTwoPointAngle(pc.KnockBackDirection, NearDividePoint(obj.Position));
        DuoLog.Information($"Angle: {angle}");

        // 135度の場合は、90度になるようにDirectionを変更
        if (angle == 135)
        {
            pc.KnockBackDirection = pc.KnockBackDirection switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.East => Direction.SouthEast,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }
        // 180度の場合は、135度になるようにDirectionを変更
        else if (angle == 180)
        {
            pc.KnockBackDirection = pc.KnockBackDirection switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }
        // -135度の場合は、-90度になるようにDirectionを変更
        else if (angle == -135)
        {
            pc.KnockBackDirection = pc.KnockBackDirection switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        // 0度の場合は、-45度になるようにDirectionを変更
        else if (angle == 0)
        {
            pc.KnockBackDirection = pc.KnockBackDirection switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        // 45度の場合は、90度になるようにDirectionを変更
        else if (angle == 45)
        {
            pc.KnockBackDirection = pc.KnockBackDirection switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        // -45度の場合は、-90度になるようにDirectionを変更
        else if (angle == -45)
        {
            pc.KnockBackDirection = pc.KnockBackDirection switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.East => Direction.SouthEast,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }

        ApplyElement("Bait", pc.KnockBackDirection, 3.5f, 0.5f);
    }

    private void ShowSetAoeStartEnd()
    {
        PluginLog.Information($"SliceEnemyEntityId: {_sliceEnemyEntityId} Direction: {DividePoint(_sliceEnemyEntityId.GetObject().Position)}");

        var pc = _partyDataList.Find(x => x.Mine);
        if (pc == null) return;

        var direction = DividePoint(_sliceEnemyEntityId.GetObject().Position);
        PluginLog.Information($"Direction: {direction}");
        PluginLog.Information($"KnockBackDirection: {pc.KnockBackDirection}");
        var angle = GetTwoPointAngle(pc.KnockBackDirection, direction);

        PluginLog.Information($"Angle: {angle}");

        // StartDirectionの計算
        var startDirection = GetStartDirection(pc.KnockBackDirection, angle);

        // EndDirectionの計算（StartDirectionを考慮）
        var endDirection = GetEndDirection(direction, pc.KnockBackDirection, angle, startDirection);

        pc.MoveTargetDirection = endDirection;

        ApplyElement("Bait", startDirection, 19.0f, 0.5f);
        ApplyElement("Bait2", endDirection, 19.0f, 0.5f);

        if (pc.index >= 3) Controller.GetElementByName("StackRange").refActorObjectID = _partyDataList[0].EntityId;
        else Controller.GetElementByName("StackRange").refActorObjectID = _partyDataList[7].EntityId;

        Controller.GetElementByName("StackRange").Enabled = true;

        // 結果をログ出力（必要なら追加処理）
        PluginLog.Information($"StartDirection: {startDirection}, EndDirection: {endDirection}");
    }

    private void ShowSetAoeEnd()
    {
        var pc = _partyDataList.Find(x => x.Mine);
        if (pc == null) return;
        ApplyElement("Bait", pc.MoveTargetDirection, 19.0f, 0.5f);

        //if (pc.index >= 3) Controller.GetElementByName("StackRange").refActorObjectID = _partyDataList[0].EntityId;
        //else Controller.GetElementByName("StackRange").refActorObjectID = _partyDataList[7].EntityId;

        //Controller.GetElementByName("StackRange").Enabled = true;
    }

    private void ShowAvoidSliceFirst()
    {
        if (_sliceEnemyEntityId == 0) return;
        // SliceFrontの場合はSliceEnemyの方向を取得, SliceBackの場合は逆方向を取得
        var direction = _sliceCastId == 40193u ? DividePoint(_sliceEnemyEntityId.GetObject().Position) : GetOppositeDirection(DividePoint(_sliceEnemyEntityId.GetObject().Position));
        ApplyElement("Bait", direction, 18.0f, 0.5f);

        //Controller.GetElementByName("SlideRange").refActorObjectID = Player.Object.EntityId;
        //Controller.GetElementByName("SlideRange").Enabled = true;
    }

    private void ShowAvoidSliceSecond()
    {
        if (_sliceEnemyEntityId == 0) return;
        // Firstの逆を表示
        var direction = _sliceCastId == 40193u ? GetOppositeDirection(DividePoint(_sliceEnemyEntityId.GetObject().Position)) : DividePoint(_sliceEnemyEntityId.GetObject().Position);
        ApplyElement("Bait", direction, 18.0f, 0.5f);
        //Controller.GetElementByName("SlideRange").refActorObjectID = Player.Object.EntityId;
        //Controller.GetElementByName("SlideRange").Enabled = true;
    }

    private void LockFace()
    {
        if (!C.LockFace) return;
        var obj = Svc.Objects.First(x => x.DataId == 0x45A0);
        Direction dressUp1drection = DividePoint(obj.Position);
        Direction dressUp2drection = DividePoint(Svc.Objects.First(x => x.DataId == 0x459F).Position);

        // 自身の方向を取得
        var myDirection = DividePoint(Player.Object.Position);

        // ドレスアップの方向が自身の方向と同じ場合はそのまま
        if (myDirection == dressUp1drection)
        {
            // もう１体がその方角から180度なら90度回転
            if (GetTwoPointAngle(dressUp1drection, dressUp2drection) == 180)
            {
                myDirection = GetDirectionFromAngle(myDirection, 90);
            }
            // もう１体がその方角から45 ~ 135度なら-90度回転
            else if (GetTwoPointAngle(dressUp1drection, dressUp2drection) >= 45 && GetTwoPointAngle(dressUp1drection, dressUp2drection) <= 135)
            {
                myDirection = GetDirectionFromAngle(myDirection, -90);
            }
            // もう１体がその方角から-45 ~ -135度なら90度回転
            else if (GetTwoPointAngle(dressUp1drection, dressUp2drection) <= -45 && GetTwoPointAngle(dressUp1drection, dressUp2drection) >= -135)
            {
                myDirection = GetDirectionFromAngle(myDirection, 90);
            }
        }
        else if (myDirection == dressUp2drection)
        {
            // もう１体がその方角から180度なら90度回転
            if (GetTwoPointAngle(dressUp2drection, dressUp1drection) == 180)
            {
                myDirection = GetDirectionFromAngle(myDirection, 90);
            }
            // もう１体がその方角から45 ~ 135度なら-90度回転
            else if (GetTwoPointAngle(dressUp2drection, dressUp1drection) >= 45 && GetTwoPointAngle(dressUp2drection, dressUp1drection) <= 135)
            {
                myDirection = GetDirectionFromAngle(myDirection, -90);
            }
            // もう１体がその方角から-45 ~ -135度なら90度回転
            else if (GetTwoPointAngle(dressUp2drection, dressUp1drection) <= -45 && GetTwoPointAngle(dressUp2drection, dressUp1drection) >= -135)
            {
                myDirection = GetDirectionFromAngle(myDirection, 90);
            }
        }
        // 方向からrotationを取得
        var rot = myDirection switch
        {
            Direction.North => 0,
            Direction.NorthEast => 45,
            Direction.East => 90,
            Direction.SouthEast => 135,
            Direction.South => 180,
            Direction.SouthWest => 225,
            Direction.West => 270,
            Direction.NorthWest => 315,
            _ => 0
        };

        FaceTarget((float)rot);
    }

    // Directionと45倍数の角度から角度を算出してDirectionを返す
    private Direction GetDirectionFromAngle(Direction direction, int angle)
    {
        if (angle == 45)
        {
            return direction switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.East => Direction.SouthEast,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }
        else if (angle == 90)
        {
            return direction switch
            {
                Direction.North => Direction.East,
                Direction.NorthEast => Direction.SouthEast,
                Direction.East => Direction.South,
                Direction.SouthEast => Direction.SouthWest,
                Direction.South => Direction.West,
                Direction.SouthWest => Direction.NorthWest,
                Direction.West => Direction.North,
                Direction.NorthWest => Direction.NorthEast,
                _ => Direction.None
            };
        }
        else if (angle == 135)
        {
            return direction switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        else if (angle == 180)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.NorthEast => Direction.SouthWest,
                Direction.East => Direction.West,
                Direction.SouthEast => Direction.NorthWest,
                Direction.South => Direction.North,
                Direction.SouthWest => Direction.NorthEast,
                Direction.West => Direction.East,
                Direction.NorthWest => Direction.SouthEast,
                _ => Direction.None
            };
        }
        else if (angle == -45)
        {
            return direction switch
            {
                Direction.North => Direction.NorthWest,
                Direction.NorthWest => Direction.West,
                Direction.West => Direction.SouthWest,
                Direction.SouthWest => Direction.South,
                Direction.South => Direction.SouthEast,
                Direction.SouthEast => Direction.East,
                Direction.East => Direction.NorthEast,
                Direction.NorthEast => Direction.North,
                _ => Direction.None
            };
        }
        else if (angle == -90)
        {
            return direction switch
            {
                Direction.North => Direction.West,
                Direction.NorthWest => Direction.SouthWest,
                Direction.West => Direction.South,
                Direction.SouthWest => Direction.SouthEast,
                Direction.South => Direction.East,
                Direction.SouthEast => Direction.NorthEast,
                Direction.East => Direction.North,
                Direction.NorthEast => Direction.NorthWest,
                _ => Direction.None
            };
        }
        else if (angle == -135)
        {
            return direction switch
            {
                Direction.North => Direction.NorthEast,
                Direction.NorthEast => Direction.East,
                Direction.East => Direction.SouthEast,
                Direction.SouthEast => Direction.South,
                Direction.South => Direction.SouthWest,
                Direction.SouthWest => Direction.West,
                Direction.West => Direction.NorthWest,
                Direction.NorthWest => Direction.North,
                _ => Direction.None
            };
        }
        else if (angle == -180)
        {
            return direction switch
            {
                Direction.North => Direction.South,
                Direction.NorthEast => Direction.SouthWest,
                Direction.East => Direction.West,
                Direction.SouthEast => Direction.NorthWest,
                Direction.South => Direction.North,
                Direction.SouthWest => Direction.NorthEast,
                Direction.West => Direction.East,
                Direction.NorthWest => Direction.SouthEast,
                _ => Direction.None
            };
        }
        return Direction.None;
    }

    private Direction GetStartDirection(Direction knockBackDirection, int angle)
    {
        // 角度が0または180度の場合に、時計回りに45度回転
        if (angle == 0 || angle == 180 || angle == -180)
        {
            return GetDirectionFromAngle(knockBackDirection, 45);
        }
        return knockBackDirection;
    }

    private Direction GetEndDirection(Direction direction, Direction knockBackDirection, int angle, Direction startDirection)
    {
        var oppositeDirection = GetOppositeDirection(direction);

        if (angle == 90 || angle == -90 || angle == 135 || angle == -135 || angle == 180 || angle == -180)
        {
            return direction;
        }
        else
        {
            return oppositeDirection;
        }
    }

    private Direction GetOppositeDirection(Direction direction) => GetDirectionFromAngle(direction, 180);

    private void ApplyElement(string elementName, Direction direction, float radius, float elementRadius = 0.3f)
    {
        var position = new Vector3(100, 0, 100);
        var angle = GetAngle(direction);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if (Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.SetOffPosition(position);
        }
    }

    private float GetAngle(Direction direction)
    {
        return direction switch
        {
            Direction.North => 270,
            Direction.NorthEast => 315,
            Direction.East => 0,
            Direction.SouthEast => 45,
            Direction.South => 90,
            Direction.SouthWest => 135,
            Direction.West => 180,
            Direction.NorthWest => 225,
            _ => 0
        };
    }

    private void FaceTarget(float rotation, ulong unkObjId = 0xE0000000)
    {
        if (Svc.Condition[ConditionFlag.DutyRecorderPlayback] && EzThrottler.Throttle("FaceTarget", 10000))
        {
            if (false) PluginLog.Information($"FaceTarget {rotation}");
            EzThrottler.Throttle("FaceTarget", 1000, true);
        }

        var adjustedRotation = (rotation + 270) % 360;
        var direction = new Vector2(
            MathF.Cos(adjustedRotation * MathF.PI / 180),
            MathF.Sin(adjustedRotation * MathF.PI / 180)
        );

        var player = Player.Object;
        var normalized = Vector2.Normalize(direction);

        if (player == null)
        {
            PluginLog.LogError("Player is null");
            return;
        }

        var position = player.Position + normalized.ToVector3();

        ActionManager->AutoFaceTargetPosition(&position, unkObjId);
    }
    #endregion
}
