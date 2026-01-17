using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameFunctions;
using Lumina.Excel.Sheets;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

internal class M10S_ExFlameSneaking : SplatoonScript
{
    /*
     * Constants and Types
     */

    #region Constants and Types

    // I borrowed Errer's Scripts ty
    private enum MarkerDirection
    {
        Up, // 左上 ↖
        Middle, // 正左 ←
        Down // 左下 ↙
    }

    public enum Order
    {
        HMR,
        HRM,
        MHR,
        MRH,
        RHM,
        RMH
    }

    // Position to Vector3 coordinate mapping (3x3 grid)
    private static readonly Dictionary<uint, Vector3> PositionToCoord = new()
    {
        { 14, new Vector3(87f, 0f, 87f) }, // Bottom-left corner
        { 15, new Vector3(100f, 0f, 87f) }, // Bottom-center
        { 16, new Vector3(113f, 0f, 87f) }, // Bottom-right corner
        { 17, new Vector3(87f, 0f, 100f) }, // Middle-left
        { 18, new Vector3(100f, 0f, 100f) }, // Center
        { 19, new Vector3(113f, 0f, 100f) }, // Middle-right
        { 20, new Vector3(87f, 0f, 113f) }, // Top-left corner
        { 21, new Vector3(100f, 0f, 113f) }, // Top-center
        { 22, new Vector3(113f, 0f, 113f) }, // Top-right corner
    };

    // Data2 到方向的映射
    private static readonly Dictionary<ushort, MarkerDirection> Data2ToDirection = new()
    {
        // 水标记
        { 2, MarkerDirection.Down }, // 水-左下
        { 32, MarkerDirection.Middle }, // 水-左
        { 128, MarkerDirection.Up }, // 水-左上
        // 火标记
        { 512, MarkerDirection.Down }, // 火-左下
        { 2048, MarkerDirection.Middle }, // 火-左
        { 8192, MarkerDirection.Up }, // 火-左上
    };

    #endregion

    /*
     * Public Fields
     */

    #region Public Fields

    public override HashSet<uint>? ValidTerritories => [1323];
    public override Metadata? Metadata => new(1, "Redmoon");

    #endregion

    /*
     * Private Fields
     */

    #region Private Fields

    private bool _gimmickActive = false;
    private bool _isShowed = false;
    private int _buffCount = 0;
    private int _showedCount = 0;
    private int _swapCount = 0;
    private List<IPlayerCharacter> _flamers = new();
    private List<IPlayerCharacter> _waters = new();
    private List<(ushort, ushort)> _flameMarkers = new();
    private List<(ushort, ushort)> _waterMarkers = new();

    private Config C => Controller.GetConfig<Config>();

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (C.basePlayerOverride == "") return Player.Object;
            else if (!Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.DutyRecorderPlayback])
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(C.basePlayerOverride)) ?? Player.Object;
        }
    }

    #endregion

    /*
     * Public Methods
     */

    #region Public Methods

    public override void OnSetup()
    {
        Controller.RegisterElement($"tether", new Element(0)
        {
            radius = 0.3f,
            tether = true,
            thicc = 10f,
        });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 46510) _gimmickActive = true;
        if (castId == 46512)
        {
            if (Controller.TryGetElementByName("tether", out var el))
            {
                el.Enabled = false;
            }

            _isShowed = false;
            if (_showedCount >= 8) this.OnReset();
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (!_gimmickActive) return;
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        // ギミックがアクティブであることを確認
        // また、水と火のマーカーがそれぞれ4つずつ存在することを確認
        if (!_gimmickActive || _flamers.Count() != 4 || _waters.Count() != 4) return;

        // position が有効か確認（14～22）
        if (position is < 14 or > 22) return;

        // data2 が有効な水／火マーカーか確認
        if (!Data2ToDirection.TryGetValue(data2, out var direction)) return;

        // 水マーカー
        if (data2 is (2 or 32 or 128))
        {
            _waterMarkers.Add(((ushort)(position), data2));
        }

        if (data2 is 512 or 2048 or 8192)
        {
            _flameMarkers.Add(((ushort)(position), data2));
        }
    }

    public override void OnGainBuffEffect(uint sourceId, Status status)
    {
        if (!_gimmickActive) return;
        else if (status.StatusId is (4827 or 4828))
        {
            _buffCount++;
            if (_buffCount < 8) return;
            var order = BuildJobOrderList(C.jobOrder, C.d2AddedJob);
            DuoLog.Information($"Job Order: {string.Join(", ", order)}");
            var orderIndex = BuildJobOrderIndex(order);

            _waters = BuildSortedGroupByStatus(4828, orderIndex);
            _flamers = BuildSortedGroupByStatus(4827, orderIndex);
        }
    }

    public override void OnUpdate()
    {
        if (!_gimmickActive) return;

        // 火/水マーカーが2ずつ追加されるたびに処理を行う
        if ((_showedCount + 2 <= _flameMarkers.Count + _waterMarkers.Count) && !_isShowed)
        {
            var (posF, data2F) = _flameMarkers[_showedCount / 2];
            var (posW, data2W) = _waterMarkers[_showedCount / 2];
            DuoLog.Information(
                $"Processing markers: Flame Pos={posF}, Data2={data2F}; Water Pos={posW}, Data2={data2W}");
            // 左上ならばタンクをスイッチさせる
            if (data2F == 8192 || data2W == 128)
            {
                // flame タンクと water タンクを入れ替え
                (_flamers[3], _waters[3]) = (_waters[3], _flamers[3]);
            }
            // 左、左下ならばタンク以外がorderに従いスイッチ
            else
            {
                (_flamers[_swapCount], _waters[_swapCount]) = (_waters[_swapCount], _flamers[_swapCount]);
                _swapCount++;
            }

            if (BasePlayer.GetRole() != CombatRole.Tank && !C.showWhenTanks && data2F == 8192 && data2W == 128)
            {
                _isShowed = true;
                _showedCount += 2;
                return;
            }

            if (_flamers.Any(x => x.EntityId == BasePlayer.EntityId))
            {
                if (Controller.TryGetElementByName("tether", out var el1))
                {
                    el1.SetRefPosition(PositionToCoord[posF]);
                    el1.Enabled = true;
                    _isShowed = true;
                }
            }
            else if (_waters.Any(x => x.EntityId == BasePlayer.EntityId))
            {
                if (Controller.TryGetElementByName("tether", out var el1))
                {
                    el1.SetRefPosition(PositionToCoord[posW]);
                    el1.Enabled = true;
                    _isShowed = true;
                }
            }

            _showedCount += 2;
        }

        if (Controller.TryGetElementByName("tether", out var el))
        {
            if (el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public override void OnReset()
    {
        _gimmickActive = false;
        _isShowed = false;
        _buffCount = 0;
        _showedCount = 0;
        _swapCount = 0;
        _flamers.Clear();
        _waters.Clear();
        _flameMarkers.Clear();
        _waterMarkers.Clear();
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public class Config : IEzConfig
    {
        public Order jobOrder = Order.HRM;
        public Job d2AddedJob = Job.ADV;
        public bool showWhenTanks = false;
        public string basePlayerOverride;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Switch Job Order:");
        ImGuiEx.HelpMarker("H = Healer, M = Melee DPS, R = Ranged DPS");
        ImGui.Indent();
        ImGuiEx.EnumCombo("##jobOrder", ref C.jobOrder);
        ImGui.Unindent();

        ImGui.Checkbox("If tanks attack timing, show tether other roles", ref C.showWhenTanks);

        ImGui.Text("[Option] D2 Added Job:");
        ImGuiEx.HelpMarker(
            "D2 Role Assignment Added Job. Select the job you want to add to Melee DPS (ADV will be removed from Ranged DPS).");
        ImGui.Indent();
        if (ImGui.BeginCombo("##d2AddedJob", C.d2AddedJob.ToString()))
        {
            foreach (var job in (Job[])Enum.GetValues(typeof(Job)))
            {
                if (ImGui.Selectable(job.ToString(), C.d2AddedJob == job))
                {
                    C.d2AddedJob = job;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.Unindent();
        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref C.basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                        C.basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.Text($"Gimmick Active: {_gimmickActive}");
            ImGui.Text($"Is Showed: {_isShowed}");
            ImGui.Text($"Buff Count: {_buffCount}");
            ImGui.Text($"Showed Count: {_showedCount}");
            ImGui.Text($"Swap Count: {_swapCount}");
            ImGui.Text("Flamers:");
            foreach (var f in _flamers)
                ImGui.Text($"- {f.GetNameWithWorld()} ({(Job)f.ClassJob.RowId})");
            ImGui.Text("Waters:");
            foreach (var w in _waters)
                ImGui.Text($"- {w.GetNameWithWorld()} ({(Job)w.ClassJob.RowId})");
            ImGui.Text("Flame Markers:");
            foreach (var fm in _flameMarkers)
                ImGui.Text($"- Pos: {fm.Item1}, Data2: {fm.Item2}");
            ImGui.Text("Water Markers:");
            foreach (var wm in _waterMarkers)
                ImGui.Text($"- Pos: {wm.Item1}, Data2: {wm.Item2}");
        }
    }

    #endregion

    /*
     * Private Methods
     */

    #region Private Methods

    private static List<Job> BuildJobOrderList(Order order, Job d2AddedJob)
    {
        // ベースカテゴリ
        var healer = new[] { Job.WHM, Job.AST, Job.SCH, Job.SGE }.ToList();
        var melee = new[] { Job.DRG, Job.VPR, Job.SAM, Job.MNK, Job.RPR, Job.NIN }.ToList();
        var ranged = new[] { Job.BRD, Job.MCH, Job.DNC, Job.BLM, Job.PCT, Job.RDM, Job.SMN }.ToList();
        var tank = new[] { Job.PLD, Job.GNB, Job.WAR, Job.DRK }.ToList(); // Tankは必ず最後

        // 例外：D2追加ジョブを melee に寄せる（ADV は無視）
        if (d2AddedJob != Job.ADV)
        {
            ranged.Remove(d2AddedJob); // 無ければ何もしない
            melee.Add(d2AddedJob);
        }

        // 並びだけを決める（中身は上の3カテゴリで固定）
        return order switch
        {
            Order.HMR => Concat(healer, melee, ranged, tank),
            Order.MHR => Concat(melee, healer, ranged, tank),
            Order.MRH => Concat(melee, ranged, healer, tank),
            Order.RHM => Concat(ranged, healer, melee, tank),
            Order.RMH => Concat(ranged, melee, healer, tank),
            Order.HRM or _ => Concat(healer, ranged, melee, tank),
        };

        static List<Job> Concat(List<Job> a, List<Job> b, List<Job> c, List<Job> d)
        {
            var r = new List<Job>(a.Count + b.Count + c.Count + d.Count);
            r.AddRange(a);
            r.AddRange(b);
            r.AddRange(c);
            r.AddRange(d);
            return r;
        }
    }

    private static Dictionary<Job, int> BuildJobOrderIndex(List<Job> order)
    {
        // 同じ Job が重複した場合でも「最初の順番」を採用
        var dict = new Dictionary<Job, int>(order.Count);
        for (var i = 0; i < order.Count; i++)
            if (!dict.ContainsKey(order[i]))
                dict.Add(order[i], i);
        return dict;
    }

    private static List<IPlayerCharacter> BuildSortedGroupByStatus(uint statusId, Dictionary<Job, int> orderIndex)
    {
        return FakeParty.Get()
            .Where(x => x.StatusList.Any(y => y.StatusId == statusId))
            .OrderBy(x => orderIndex.TryGetValue((Job)x.ClassJob.RowId, out var i) ? i : int.MaxValue)
            .ToList();
    }

    #endregion
}