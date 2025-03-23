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
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
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
        split,
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
        public float FastCheatDefault = 1.0f;
        public float FastCheat = 1.5f;
    }

    private class PartyData
    {
        public int Index { get; set; }
        public bool Mine = false;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)this.EntityId.GetObject()! ?? null;
        public uint TetherPairId1 = 0;
        public uint TetherPairId2 = 0;
        public DirectionCalculator.Direction TowerDirection = DirectionCalculator.Direction.None;
        public int ConeIndex = 0;
        public bool IsStack = false;
        public Vector3 SplitPos = Vector3.Zero;

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
    private readonly List<(DirectionCalculator.Direction, Vector3)> vector3List =
        new List<(DirectionCalculator.Direction, Vector3)>
    {
        (DirectionCalculator.Direction.North, new Vector3(96f, 0f, 95f)),
        (DirectionCalculator.Direction.North, new Vector3(104f, 0f, 95f)),
        (DirectionCalculator.Direction.East, new Vector3(110f, 0f, 102f)),
        (DirectionCalculator.Direction.East, new Vector3(110f, 0f, 110f)),
        (DirectionCalculator.Direction.West, new Vector3(90f, 0f, 102f)),
        (DirectionCalculator.Direction.West, new Vector3(90f, 0f, 110f)),
        (DirectionCalculator.Direction.South, new Vector3(96f, 0f, 115f)),
        (DirectionCalculator.Direction.South, new Vector3(104f, 0f, 115f)),
    };
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(12, "redmoon");
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
        if (castId == 40246)
        {
            SetListEntityIdByJob();
            //_partyDataList.Each(x => x.Mine = false);
            //_partyDataList[6].Mine = true;
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

        if (castId == 40213 && _state == State.tower)
        {
            HideAllElements();
            _state = State.split;
            ShowSplit();
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
        _partyDataList.Clear();
        if (Player.Job == Job.DRK)
        {
            var C = Controller.GetConfig<Config>();
            Chat.Instance.ExecuteCommand($"/pdrspeed {C.FastCheatDefault}");
        }
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
        var C = Controller.GetConfig<Config>();
        ImGui.SliderFloat("FastCheat", ref C.FastCheat, 1.0f, 1.5f);
        ImGui.SliderFloat("FastCheatDefault", ref C.FastCheatDefault, 1.0f, 1.5f);

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
                Entries.Add(new ImGuiEx.EzTableEntry("SplitPos", true, () => ImGui.Text(x.SplitPos.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("IsStack", true, () => ImGui.Text(x.IsStack.ToString())));
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
    private void ShowAkhRhaiReadyGuide(uint entityId)
    {
        var pc = GetMinedata();
        if (pc == null) return;
        if (!entityId.TryGetObject(out var obj)) return;

        DirectionCalculator.Direction direction = DirectionCalculator.DividePoint(obj.Position, 10);

        var angle = DirectionCalculator.GetAngle(direction) + ((pc.Index == 0) ? -45 : 45);

        if (pc.Index == 0) ApplyElement("Bait", angle, 10);
        else ApplyElement("Bait", angle, 10);
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

    private void ShowSplit()
    {
        Vector3 anothernorth = Vector3.Zero;
        Vector3 anothersouth = Vector3.Zero;
        foreach (var pc in _partyDataList)
        {
            if (pc.SplitPos != Vector3.Zero) continue;
            if (pc.TetherPairId1 == 0)
            {
                pc.SplitPos = pc.ConeIndex switch
                {
                    1 => vector3List[2].Item2,
                    2 => vector3List[3].Item2,
                    4 => vector3List[4].Item2,
                    3 => vector3List[5].Item2,
                    _ => Vector3.Zero
                };
            }
        }

        var northes = _partyDataList.Where(x => x.TowerDirection == DirectionCalculator.Direction.North && x.TetherPairId1 != 0).ToList();
        if (northes.Count() != 2) return;

        DuoLog.Information($"northes[0]: {northes[0].Object?.Name}, northes[1]: {northes[1].Object?.Name}");

        if (northes[0].Object.Position.X < northes[1].Object.Position.X)
        {
            northes[0].SplitPos = vector3List[0].Item2;
            northes[1].SplitPos = vector3List[1].Item2;
        }
        else
        {
            northes[1].SplitPos = vector3List[0].Item2;
            northes[0].SplitPos = vector3List[1].Item2;
        }

        var souths = _partyDataList.Where(x => x.TowerDirection == DirectionCalculator.Direction.South && x.TetherPairId1 != 0).ToList();
        if (souths.Count() != 2) return;

        // vector3Listの6,7の内もっとも近い方を取得
        if (souths[0].Object.Position.X < souths[1].Object.Position.X)
        {
            souths[0].SplitPos = vector3List[6].Item2;
            souths[1].SplitPos = vector3List[7].Item2;
        }
        else
        {
            souths[1].SplitPos = vector3List[6].Item2;
            souths[0].SplitPos = vector3List[7].Item2;
        }

        var p = GetMinedata();
        if (p == null) return;

        DuoLog.Information($"SplitPos: {p.SplitPos}");

        ApplyElement("Bait", p.SplitPos);
    }

    private void ShowHalfCutStack()
    {
        if (_wing == "") return;

        var pc = GetMinedata();
        if (pc == null) return;

        if (pc.Index == 0)
        {
            var C = Controller.GetConfig<Config>();
            Chat.Instance.ExecuteCommand($"/pdrspeed {C.FastCheat}");
        }

        float Xoffset = (_wing == "Left") ? 2f : -2f;
        if (pc.TowerDirection == DirectionCalculator.Direction.North)
        {
            ApplyElement("Bait", new Vector3(100f + Xoffset, 0, 95f));
        }
        else
        {
            ApplyElement("Bait", new Vector3(100f + Xoffset, 0, 115f));
        }
    }

    private void ShowMTAttack()
    {
        var pc = GetMinedata();
        if (pc == null) return;

        if (pc.Index == 0)
        {
            var posEast = new Vector3(100, 0, 100) + (19f * new Vector3(
                MathF.Cos(MathF.PI * DirectionCalculator.GetAngle(DirectionCalculator.Direction.East) / 180f), 0, MathF.Sin(MathF.PI * 0 / 180f)));
            var posWest = new Vector3(100, 0, 100) + (19f * new Vector3(
                MathF.Cos(MathF.PI * DirectionCalculator.GetAngle(DirectionCalculator.Direction.West) / 180f), 0, MathF.Sin(MathF.PI * 0 / 180f)));
            var disEast = Vector3.Distance(Player.Object.Position, posEast);
            var disWest = Vector3.Distance(Player.Object.Position, posWest);

            // 短い方に誘導する
            if (disEast < disWest)
            {
                ApplyElement("Bait", DirectionCalculator.Direction.East, 19f);
            }
            else
            {
                ApplyElement("Bait", DirectionCalculator.Direction.West, 19f);
            }
        }
        else
        {
            float Xoffset = (_wing == "Left") ? 2f : -2f;
            if (pc.TowerDirection == DirectionCalculator.Direction.North)
            {
                ApplyElement("Bait", new Vector3(100f + Xoffset, 0, 95f));
            }
            else
            {
                ApplyElement("Bait", new Vector3(100f + Xoffset, 0, 115f));
            }
        }
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

        // 線付きヒラとつながっている2人を南にする
        var t1 = _partyDataList.Find(x => x.TetherPairId1 == healer.EntityId);
        var t2 = _partyDataList.Find(x => x.TetherPairId2 == healer.EntityId);
        if (t1 == null || t2 == null) return false;

        t1.TowerDirection = DirectionCalculator.Direction.South;
        t2.TowerDirection = DirectionCalculator.Direction.South;

        // Directionが未確定でTetherPairId1がある人を取得
        var t3 = _partyDataList.Find(x => x.TowerDirection == DirectionCalculator.Direction.None && x.TetherPairId1 != 0);
        if (t3 == null) return false;

        t3.TowerDirection = DirectionCalculator.Direction.North;

        // 線のついていない4人を取得
        var noneTether = _partyDataList.Where(x => x.TetherPairId1 == 0 && x.TetherPairId2 == 0).ToList();
        if (noneTether.Count != 4) return false;

        var TetherStackers = _partyDataList.Where(x => x.IsStack && x.TetherPairId1 != 0).ToList();

        // 頭割り調整 (わからないので全てのパターンを書く) TODO: あとで不要なものを削除
        // 線付きに頭割り対象がいる場合
        if (TetherStackers.Count() == 2)
        {
            DuoLog.Information("TetherStackers 2");
            // 両方の場合は調整のしようがないのでそのまま
            // 線無し4人を割り振る
            for (var i = 0; i < noneTether.Count; i++)
            {
                if (i >= 2) noneTether[i].TowerDirection = DirectionCalculator.Direction.North;
                else noneTether[i].TowerDirection = DirectionCalculator.Direction.South;
            }
        }
        else if (TetherStackers.Count() == 1)
        {
            DuoLog.Information("TetherStackers 1");
            // 線無しに1人頭割り対象がいるので逆に配置
            var noneTetherStacker = noneTether.Find(x => x.IsStack);
            if (noneTetherStacker == null) return false;

            DuoLog.Information($"noneTetherStacker: {noneTetherStacker.Object?.Name}");

            if (TetherStackers[0].TowerDirection == DirectionCalculator.Direction.North)
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
        else if (TetherStackers.Count() == 0)
        {
            DuoLog.Information("TetherStackers 0");
            // 線無しに1人頭割り対象がいるので逆に配置
            var noneTetherStacker = noneTether.Where(x => x.IsStack).ToList();
            if (noneTetherStacker.Count() != 2) return false;

            noneTetherStacker[0].TowerDirection = DirectionCalculator.Direction.North;
            noneTetherStacker[1].TowerDirection = DirectionCalculator.Direction.South;

            // 線無し2人を割り振る
            var noneTetherNoneStacker = noneTether.Where(x => !x.IsStack).ToList();
            if (noneTetherNoneStacker.Count() != 2) return false;

            noneTetherNoneStacker[0].TowerDirection = DirectionCalculator.Direction.North;
            noneTetherNoneStacker[1].TowerDirection = DirectionCalculator.Direction.South;
        }

        noneTether = _partyDataList.Where(x => x.TetherPairId1 == 0 && x.TetherPairId2 == 0).ToList();
        if (noneTether.Count != 4) return false;

        // 上からConeIndexを割り振る
        int northCount = 0;
        int southCount = 0;
        for (var i = 0; i < noneTether.Count; i++)
        {
            if (noneTether[i].TowerDirection == DirectionCalculator.Direction.North)
            {
                if (northCount == 0)
                {
                    noneTether[i].ConeIndex = 1;
                    northCount++;
                }
                else
                {
                    noneTether[i].ConeIndex = 4;
                }
            }
            else
            {
                if (southCount == 0)
                {
                    noneTether[i].ConeIndex = 2;
                    southCount++;
                }
                else
                {
                    noneTether[i].ConeIndex = 3;
                }
            }
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
        DuoLog.Information($"ApplyElement: {element.Name}, {position}, {elementRadius}, {filled}, {tether}");
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
