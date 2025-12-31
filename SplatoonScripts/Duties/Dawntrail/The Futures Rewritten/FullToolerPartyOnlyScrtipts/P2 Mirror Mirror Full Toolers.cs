using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using ECommons.GameHelpers.LegacyPlayer;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P2_Mirror_Mirror_Full_Toolers : SplatoonScript
{
    #region enums
    private enum State
    {
        None = 0,
        SpawnMirror,
        DataIsReady,
        KickCast,
        WaveHit,
        WaveHit2,
        BanishCasting,
    }
    #endregion

    #region class
    private class PartyData
    {
        public int index;
        public bool Mine;
        public uint EntityId;

        public PartyData(uint entityId)
        {
            Mine = false;
            EntityId = entityId;
        }
    }

    private class MirrorData
    {
        public bool isFirstWave;
        public uint EntityId;
        public Direction Direction;

        public MirrorData(uint entityId, Direction direction)
        {
            EntityId = entityId;
            Direction = direction;
            isFirstWave = false;
        }
    }
    #endregion

    #region public properties
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(4, "redmoon");
    #endregion

    #region private properties
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private List<MirrorData> _mirrorDataList = [];
    private List<MirrorData> _meleeSortedList = [];
    private uint _mainBossId = 0;
    private bool _transLock = false;

    private IReadOnlyList<(int, float)> CorrectionAngles = new List<(int, float)>
    {
        (1, -75.88f),
        (2, -45.38f),
        (3, -14.58f),
        (4, 14.58f),
        (5, 45.38f),
        (6, 75.88f),
    };
    #endregion

    #region public methods
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("BaitObject", new Element(1) { tether = true, refActorComparisonType = 2, radius = 0.5f, thicc = 6f });
        Controller.RegisterElement("BaitObject2", new Element(1) { tether = true, refActorComparisonType = 2, radius = 0.5f, thicc = 6f });
        Controller.RegisterElement("MirrorToMirror", new Element(2) { color = 0xC800FF00, radius = 0f, thicc = 11f });
        Controller.RegisterElement($"BaitTether", new Element(2) { thicc = 6.0f, radius = 0f });
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"ConeRange{i}", new Element(5) { radius = 35.0f, coneAngleMin = -15, coneAngleMax = 15, includeRotation = true, fillIntensity = 0.2f });
        }
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Circle{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 2f, fillIntensity = 0.2f });
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40179)
        {
            SetListEntityIdByJob();

            _mainBossId = Svc.Objects.FirstOrDefault(x => x is IBattleNpc npc && npc.IsTargetable == true && npc.SubKind == 5)?.EntityId ?? 0;

            if(_partyDataList.Count == 0) return;
            if(_mainBossId == 0) return;
            _state = State.SpawnMirror;
        }

        if(castId is 40220 or 40221)
        {
            _state = State.BanishCasting;
            HideAllElements();
            ShowBanish(castId);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        if(set.Action.Value.RowId == 40203)
        {
            _state = State.WaveHit;
            _transLock = true;
            _ = new TickScheduler(delegate { _transLock = false; }, 1000);
            HideAllElements();
            ShowSecondMirror();
        }
        if(set.Action.Value.RowId == 40204 && !_transLock)
        {
            _state = State.WaveHit2;
            HideAllElements();
        }
        if(set.Action.Value.RowId is 40220 or 40221)
        {
            OnReset();
        }
    }

    public override void OnUpdate()
    {
        if(_state == State.None) return;
        if(_state == State.DataIsReady)
        {
            //DynamicFirstShowRange();
        }
        if(_state == State.WaveHit)
        {
            //DynamicSecondShowRange();
        }
        if(_state == State.BanishCasting)
        {
            DynamicShowBanish();
        }

        if(Controller.GetElementByName("Bait").Enabled) Controller.GetElementByName("Bait").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("BaitObject").Enabled) Controller.GetElementByName("BaitObject").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName("BaitObject2").Enabled) Controller.GetElementByName("BaitObject2").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        if(Controller.GetElementByName($"BaitTether").Enabled) Controller.GetElementByName("BaitTether").color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5, uint p6, uint p7, uint p8, ulong targetId, byte replaying)
    {
        if(_state == State.None) return;
        if(_state == State.SpawnMirror)
        {
            if(command == 503 && sourceId == Player.Object.EntityId)
            {
                _mirrorDataList.Add(new MirrorData(p2, DividePoint(p2.GetObject().Position, 20)));
            }

            if(_mirrorDataList.Count == 3)
            {
                // 最もEntityIdが小さいミラーが最初に来るミラー
                var firstMirror = _mirrorDataList.OrderBy(x => x.EntityId).First();
                if(firstMirror.EntityId == 0) return;
                PluginLog.Information($"First Mirror: {firstMirror.EntityId}");
                firstMirror.isFirstWave = true;
                _state = State.DataIsReady;
                ShowFirstMirror();
            }
        }
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"Main Boss: {_mainBossId}");
            ImGui.Text($"Party Data Count: {_partyDataList.Count}");
            ImGui.Text($"Mirror Data[0] EntityId: {_mirrorDataList[0].EntityId}");
            ImGui.Text($"Mirror Data[0] Direction: {_mirrorDataList[0].Direction}");
            ImGui.Text($"Mirror Data[0] isFirstWave: {_mirrorDataList[0].isFirstWave}");
            if(_mirrorDataList[0].EntityId.GetObject() != null)
            {
                ImGui.Text($"Mirror Data[0] Position: {_mirrorDataList[0].EntityId.GetObject().Position}");
            }
            ImGui.Text($"Mirror Data[1] EntityId: {_mirrorDataList[1].EntityId}");
            ImGui.Text($"Mirror Data[1] Direction: {_mirrorDataList[1].Direction}");
            ImGui.Text($"Mirror Data[1] isFirstWave: {_mirrorDataList[1].isFirstWave}");
            if(_mirrorDataList[1].EntityId.GetObject() != null)
            {
                ImGui.Text($"Mirror Data[1] Position: {_mirrorDataList[1].EntityId.GetObject().Position}");
            }
            ImGui.Text($"Mirror Data[2] EntityId: {_mirrorDataList[2].EntityId}");
            ImGui.Text($"Mirror Data[2] Direction: {_mirrorDataList[2].Direction}");
            ImGui.Text($"Mirror Data[2] isFirstWave: {_mirrorDataList[2].isFirstWave}");
            if(_mirrorDataList[2].EntityId.GetObject() != null)
            {
                ImGui.Text($"Mirror Data[2] Position: {_mirrorDataList[2].EntityId.GetObject().Position}");
            }
            ImGui.Text($"Melee Sorted Data Count: {_meleeSortedList.Count}");
            foreach(var partyData in _partyDataList)
            {
                var obj = partyData.EntityId.GetObject();
                if(obj == null) continue;
                ImGui.Text($"Party {obj.Name.ToString()}: {partyData.index}");
            }

            ImGui.Text($"My Angle: {MathHelper.GetRelativeAngle(Player.Object.Position, _mirrorDataList.First(x => x.isFirstWave).EntityId.GetObject().Position)}");
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _partyDataList.Clear();
        _mirrorDataList.Clear();
        _meleeSortedList.Clear();
        _mainBossId = 0;
        HideAllElements();
    }
    #endregion

    #region private methods
    private void ShowFirstMirror()
    {
        var firstMirror = _mirrorDataList.Find(x => x.isFirstWave);
        if(firstMirror == null) return;
        // 自分がmeleeかrangeかで分岐
        var pc = _partyDataList.Find(x => x.EntityId == Player.Object.EntityId);
        if(pc == null) return;
        var index = pc.index;

        // 自分がmeleeの場合 0,1,2,3
        if(index <= 3)
        {
            DuoLog.Information("Melee");
            ApplyElement("Bait", GetOppositeDirection(firstMirror.Direction), 13.0f);
        }
        // 自分がrangeの場合 4,5,6,7
        else
        {
            DuoLog.Information("Range");

            Controller.GetElementByName("MirrorToMirror").SetRefPosition(firstMirror.EntityId.GetObject().Position);
            Controller.GetElementByName("MirrorToMirror").SetOffPosition(_mainBossId.GetObject().Position);
            Controller.GetElementByName("MirrorToMirror").Enabled = true;
            Controller.GetElementByName("BaitObject").refActorObjectID = firstMirror.EntityId;
            Controller.GetElementByName("BaitObject").Enabled = true;
        }

        //DynamicFirstShowRange();
    }

    //private void DynamicFirstShowRange()
    //{
    //    var firstMirror = _mirrorDataList.Find(x => x.isFirstWave);
    //    if (firstMirror == null) return;

    //    Controller.GetElementByName("MirrorToMirror").SetOffPosition(_mainBossId.GetObject().Position);

    //    // 扇形を表示
    //    // 0,1,2,3はターゲット可能な敵
    //    for (var i = 0; i < 4; i++)
    //    {
    //        //Controller.GetElementByName($"ConeRange{i}").SetRefPosition(_mainBossId.GetObject().Position);
    //        float angle = MathHelper.GetRelativeAngle(_partyDataList[i].EntityId.GetObject().Position, _mainBossId.GetObject().Position);
    //        if (angle < 0) angle = angle % 360;
    //        //Controller.GetElementByName($"ConeRange{i}").AdditionalRotation = MathHelper.DegToRad(angle);
    //        //Controller.GetElementByName($"ConeRange{i}").Enabled = true;
    //    }

    //    // 4,5,6,7はfirstMirror
    //    for (var i = 4; i < 8; i++)
    //    {
    //        //Controller.GetElementByName($"ConeRange{i}").SetRefPosition(firstMirror.EntityId.GetObject().Position);
    //        float angle = MathHelper.GetRelativeAngle(_partyDataList[i].EntityId.GetObject().Position, firstMirror.EntityId.GetObject().Position);
    //        if (angle < 0) angle = angle % 360;
    //        //Controller.GetElementByName($"ConeRange{i}").AdditionalRotation = MathHelper.DegToRad(angle);

    //        if (Vector3.Distance(Player.Position, firstMirror.EntityId.GetObject().Position) > 5.0f)
    //        {
    //            //Controller.GetElementByName($"ConeRange{i}").Enabled = false;
    //        }
    //        else
    //        {
    //            //Controller.GetElementByName($"ConeRange{i}").Enabled = true;
    //        }
    //    }
    //}

    private void ShowSecondMirror()
    {
        var secondMirrorsList = _mirrorDataList.Where(x => !x.isFirstWave).ToList();

        _meleeSortedList = secondMirrorsList.OrderBy(x => Vector3.Distance(x.EntityId.GetObject().Position, _mainBossId.GetObject().Position)).ToList();

        Controller.GetElementByName("MirrorToMirror").SetRefPosition(_meleeSortedList.First().EntityId.GetObject().Position);
        Controller.GetElementByName("MirrorToMirror").SetOffPosition(_meleeSortedList.Last().EntityId.GetObject().Position);
        Controller.GetElementByName("MirrorToMirror").Enabled = true;

        // 自分がmeleeかrangeかで分岐
        var pc = _partyDataList.Find(x => x.EntityId == Player.Object.EntityId);
        if(pc == null) return;
        var index = pc.index;
        // 自分がmeleeの場合 0,1,2,3
        if(index <= 3)
        {
            Controller.GetElementByName("BaitObject").refActorObjectID = _meleeSortedList.First().EntityId;
            Controller.GetElementByName("BaitObject").Enabled = true;
        }
        else
        {
            Controller.GetElementByName("BaitObject2").refActorObjectID = _meleeSortedList.Last().EntityId;
            Controller.GetElementByName("BaitObject2").Enabled = true;
        }

        //DynamicSecondShowRange();
    }

    //private void DynamicSecondShowRange()
    //{
    //    var firstMirror = _mirrorDataList.Find(x => x.isFirstWave);
    //    if (firstMirror == null) return;

    //    // 扇形を表示
    //    // 0,1,2,3はターゲット可能な敵
    //    bool isShow = false;
    //    for (var i = 0; i < 4; i++)
    //    {
    //        //Controller.GetElementByName($"ConeRange{i}").SetRefPosition(_meleeSortedList[0].EntityId.GetObject().Position);
    //        //Controller.GetElementByName($"ConeRange{i}").AdditionalRotation = MathHelper.DegToRad(MathHelper.GetRelativeAngle(_partyDataList[i].EntityId.GetObject().Position, _meleeSortedList[0].EntityId.GetObject().Position));

    //        if (Vector3.Distance(Player.Position, _meleeSortedList[0].EntityId.GetObject().Position) > 5.0f)
    //        {
    //            //Controller.GetElementByName($"ConeRange{i}").Enabled = false;
    //        }
    //        else
    //        {
    //            //Controller.GetElementByName($"ConeRange{i}").Enabled = true;
    //            if (_partyDataList[i].Mine) isShow = true;
    //        }
    //    }

    //    // 4,5,6,7はfirstMirror
    //    for (var i = 4; i < 8; i++)
    //    {
    //        //Controller.GetElementByName($"ConeRange{i}").SetRefPosition(_meleeSortedList[1].EntityId.GetObject().Position);
    //        //Controller.GetElementByName($"ConeRange{i}").AdditionalRotation = MathHelper.DegToRad(MathHelper.GetRelativeAngle(_partyDataList[i].EntityId.GetObject().Position, _meleeSortedList[1].EntityId.GetObject().Position));

    //        if (Vector3.Distance(Player.Position, _meleeSortedList[1].EntityId.GetObject().Position) > 5.0f)
    //        {
    //            //Controller.GetElementByName($"ConeRange{i}").Enabled = false;
    //        }
    //        else
    //        {
    //            //Controller.GetElementByName($"ConeRange{i}").Enabled = true;
    //            if (_partyDataList[i].Mine) isShow = true;
    //        }
    //    }

    //    for (var i = 0; i < 8; i++)
    //    {
    //        //if (isShow) Controller.GetElementByName($"ConeRange{i}").Enabled = true;
    //        //else Controller.GetElementByName($"ConeRange{i}").Enabled = false;
    //    }
    //}

    public void ShowBanish(uint castId)
    {
        // Stack
        if(castId == 40220)
        {
            var pc = _partyDataList.Find(x => x.EntityId == Player.Object.EntityId);
            if(pc == null) return;
            var pairIndex = pc.index switch
            {
                0 => 2,
                1 => 3,
                2 => 0,
                3 => 1,
                4 => 6,
                5 => 7,
                6 => 4,
                7 => 5,
                _ => 0
            };
            Controller.GetElementByName("BaitTether").SetRefPosition(_partyDataList[pairIndex].EntityId.GetObject().Position);
            Controller.GetElementByName("BaitTether").SetOffPosition(pc.EntityId.GetObject().Position);
            Controller.GetElementByName("BaitTether").Enabled = true;
        }
        // Spread
        else if(castId == 40221)
        {
            for(var i = 0; i < 8; i++)
            {
                //Controller.GetElementByName($"Circle{i}").refActorObjectID = _partyDataList[i].EntityId;
                //Controller.GetElementByName($"Circle{i}").Enabled = true;
            }
        }
    }

    public void DynamicShowBanish()
    {
        if(Controller.GetElementByName("BaitTether").Enabled)
        {
            var pc = _partyDataList.Find(x => x.EntityId == Player.Object.EntityId);
            if(pc == null) return;
            var pairIndex = pc.index switch
            {
                0 => 2,
                1 => 3,
                2 => 0,
                3 => 1,
                4 => 6,
                5 => 7,
                6 => 4,
                7 => 5,
                _ => 0
            };
            Controller.GetElementByName("BaitTether").SetRefPosition(_partyDataList[pairIndex].EntityId.GetObject().Position);
            Controller.GetElementByName("BaitTether").SetOffPosition(pc.EntityId.GetObject().Position);
            Controller.GetElementByName("BaitTether").Enabled = true;
        }
    }
    #endregion

    #region API
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

    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();

        // リストに８人分の初期インスタンス生成
        for(var i = 0; i < 8; i++)
        {
            _partyDataList.Add(new PartyData(0));
            _partyDataList[i].index = i;
        }

        foreach(var pc in FakeParty.Get())
        {
            var job = pc.GetJob();
            switch(job)
            {
                case Job.WAR:
                case Job.DRK:
                case Job.GNB:
                    _partyDataList[0].EntityId = pc.EntityId;
                    break;

                case Job.PLD:
                    _partyDataList[1].EntityId = pc.EntityId;
                    break;

                case Job.SAM:
                case Job.MNK:
                case Job.DRG:
                case Job.RPR:
                    _partyDataList[2].EntityId = pc.EntityId;
                    break;

                case Job.NIN:
                case Job.VPR:
                case Job.RDM:
                case Job.BLM:
                case Job.SMN:
                    _partyDataList[3].EntityId = pc.EntityId;
                    break;

                case Job.BRD:
                case Job.MCH:
                case Job.DNC:
                    _partyDataList[4].EntityId = pc.EntityId;
                    break;

                case Job.PCT:
                    _partyDataList[5].EntityId = pc.EntityId;
                    break;

                case Job.WHM:
                case Job.AST:
                    _partyDataList[6].EntityId = pc.EntityId;
                    break;

                case Job.SCH:
                case Job.SGE:
                    _partyDataList[7].EntityId = pc.EntityId;
                    break;
            }
        }

        var mine = _partyDataList.Find(x => x.EntityId == Player.Object.EntityId);
        if(mine != null) mine.Mine = true;
    }

    private Direction DividePoint(Vector3 Position, float Distance, Vector3? Center = null)
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

    public static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
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

    // 2つのDirectionを比較して、角度を返す。角度は正しい値ではなく0, 45, 90, 135, 180の値を返す
    private int GetTwoPointAngle(Direction direction1, Direction direction2)
    {
        // enumの値を数値に変換
        var angle1 = (int)direction1;
        var angle2 = (int)direction2;

        // 環状の差分を計算
        var diff = (angle2 - angle1 + 8) % 8; // 環状に補正して差分を取得

        // 差分に応じた角度を計算（時計回りで正、反時計回りで負）
        var angle = diff switch
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
        if(direction1 == Direction.North && direction2 == Direction.NorthEast) return LR.Right;
        if(direction1 == Direction.NorthEast && direction2 == Direction.East) return LR.Right;
        if(direction1 == Direction.East && direction2 == Direction.SouthEast) return LR.Right;
        if(direction1 == Direction.SouthEast && direction2 == Direction.South) return LR.Right;
        if(direction1 == Direction.South && direction2 == Direction.SouthWest) return LR.Right;
        if(direction1 == Direction.SouthWest && direction2 == Direction.West) return LR.Right;
        if(direction1 == Direction.West && direction2 == Direction.NorthWest) return LR.Right;
        if(direction1 == Direction.NorthWest && direction2 == Direction.North) return LR.Right;

        if(direction1 == Direction.North && direction2 == Direction.West) return LR.Left;
        if(direction1 == Direction.West && direction2 == Direction.South) return LR.Left;
        if(direction1 == Direction.South && direction2 == Direction.East) return LR.Left;
        if(direction1 == Direction.East && direction2 == Direction.North) return LR.Left;

        if(direction1 == Direction.North && direction2 == Direction.SouthEast) return LR.Right;
        if(direction1 == Direction.NorthEast && direction2 == Direction.South) return LR.Right;
        if(direction1 == Direction.East && direction2 == Direction.SouthWest) return LR.Right;
        if(direction1 == Direction.SouthEast && direction2 == Direction.West) return LR.Right;
        if(direction1 == Direction.South && direction2 == Direction.NorthWest) return LR.Right;
        if(direction1 == Direction.SouthWest && direction2 == Direction.North) return LR.Right;
        if(direction1 == Direction.West && direction2 == Direction.NorthEast) return LR.Right;
        if(direction1 == Direction.NorthWest && direction2 == Direction.East) return LR.Right;

        return LR.Left;
    }
    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private Direction Rotate45Clockwise(Direction direction)
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

    private Direction GetOppositeDirection(Direction direction)
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

    private void ApplyElement(string elementName, Direction direction, float radius, float elementRadius = 0.3f)
    {
        var position = new Vector3(100, 0, 100);
        var angle = GetAngle(direction);
        position += radius * new Vector3(MathF.Cos(MathF.PI * angle / 180f), 0, MathF.Sin(MathF.PI * angle / 180f));
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            element.Enabled = true;
            element.radius = elementRadius;
            element.SetRefPosition(position);
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

    /// <summary>
    /// Calculates the correction angle needed to rotate the object to face the target.
    /// </summary>
    /// <param name="origin">The current position of the object.</param>
    /// <param name="target">The position of the target.</param>
    /// <param name="rotation">The current rotation angle of the object (in radian).</param>
    /// <returns>The correction angle (in degrees) needed to face the target.</returns>
    public static float GetCorrectionAngle(Vector3 origin, Vector3 target, float rotation) => GetCorrectionAngle(MathHelper.ToVector2(origin), MathHelper.ToVector2(target), rotation);

    public static float GetCorrectionAngle(Vector2 origin, Vector2 target, float rotation)
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

    /// <summary>
    /// Converts a rotation angle in radians to degrees in a system where:
    /// - North is 0°
    /// - Angles increase clockwise
    /// - Range is 0° to 360°
    /// </summary>
    /// <param name="radians">The rotation angle in radians.</param>
    /// <returns>The equivalent rotation angle in degrees.</returns>
    public static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        var degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    /// <summary>
    /// Converts a rotation angle in degrees to radians in a system where:
    /// - North is 0°
    /// - Angles increase clockwise
    /// - Range is -π to π
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    /// <returns>The equivalent rotation angle in radians.</returns>
    public static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        var radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }
    #endregion
}
