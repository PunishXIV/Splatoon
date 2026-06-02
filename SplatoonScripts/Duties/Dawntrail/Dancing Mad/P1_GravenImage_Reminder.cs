using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using Splatoon.SplatoonScripting;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P1_GravenImage_Reminder : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(2, "mirage");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneIntemperateWill = 4;

    private const uint DataIdTetherSource = 0x4C31;

    private const uint DataIdLeftHalf = 2015164;
    private const uint DataIdRightHalf = 2015165;

    private const uint DataIdDontLook = 2015166;
    private const uint DataIdLook = 2015167;

    private const uint ObjectEffectEnableData1 = 64;
    private const uint ObjectEffectEnableData2 = 128;
    private const uint ObjectEffectDisableData1 = 256;
    private const uint ObjectEffectDisableData2 = 512;

    private static readonly uint[] AnimationMarkerDataIds =
    [
        DataIdLeftHalf,
        DataIdRightHalf,
        DataIdDontLook,
        DataIdLook,
    ];

    private const float PositionMatchEpsilon = 3f;

    private const string ElTetherReminder = "TetherReminder";
    private const string ElAnimationReminder = "AnimationReminder";
    private const string ElLeftHalf = "Lefthalf";
    private const string ElRightHalf = "Righthalf";

    private static readonly Vector3 PosKnockback = new(100f, 18.5f, 56f);

    private static readonly Vector3 PosGravitas = new(102.5f, 22.5f, 27f);
    private static readonly Vector3 PosVitrophyre = new(126f, 7f, 41.5f);

    private static readonly Vector3 PosConfused = new(95f, 27f, 25f);
    private static readonly Vector3 PosSleep = new(107f, 8.5f, 43f);

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    // Active tether reminders keyed by 0x4C31 source EntityId.
    private readonly Dictionary<uint, string> _tetherRemindersBySource = [];

    // Animation-check reminders keyed by marker DataId (cleared on ObjectEffect 256/512).
    private readonly HashSet<uint> _activeAnimationMarkerDataIds = [];

    private bool _leftHalfEnabled;
    private bool _rightHalfEnabled;

    #endregion

    #region Private Class

    private sealed class Config : IEzConfig
    {
        public string Text1Knockback = "Knockback";
        public string Text2Vitrophyre = "Vitrophyre";
        public string Text2Gravitas = "Gravitas";
        public string Text2LeftHalf = "Avoid Lefthalf";
        public string Text2RightHalf = "Avoid Righthalf";
        public string Text3Confused = "Confused";
        public string Text3Sleep = "Sleep";
        public string Text3DontLook = "Dont Look";
        public string Text3Look = "Look";
        public bool ShowImage2HalfAoe;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode(ElTetherReminder,
            """{"Name":"","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"","refActorType":1}""",
            overwrite: true);
        Controller.RegisterElementFromCode(ElAnimationReminder,
            """{"Name":"","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":3.2,"overlayFScale":3.0,"thicc":0.0,"overlayText":"","refActorType":1}""",
            overwrite: true);
        Controller.RegisterElementFromCode(ElLeftHalf,
            """{"Name":"Lefthalf","type":5,"Enabled":false,"refX":100.0,"refY":100.0,"radius":20.0,"coneAngleMax":180,"includeRotation":true}""",
            overwrite: true);
        Controller.RegisterElementFromCode(ElRightHalf,
            """{"Name":"Righthalf","type":5,"Enabled":false,"refX":100.0,"refY":100.0,"radius":20.0,"coneAngleMin":180,"coneAngleMax":360,"includeRotation":true}""",
            overwrite: true);
    }

    public override void OnUpdate()
    {
        ApplyPlayerReminder(ElTetherReminder, BuildTetherReminderTexts());
        ApplyPlayerReminder(ElAnimationReminder, BuildAnimationReminderTexts());

        var showCones = C.ShowImage2HalfAoe && Controller.Scene == SceneIntemperateWill;
        if (Controller.TryGetElementByName(ElLeftHalf, out var leftHalf))
            leftHalf.Enabled = showCones && _leftHalfEnabled;
        if (Controller.TryGetElementByName(ElRightHalf, out var rightHalf))
            rightHalf.Enabled = showCones && _rightHalfEnabled;
    }

    public override void OnObjectEffect(uint target, uint data1, uint data2)
    {
        if (!target.TryGetObject(out var obj) || !IsAnimationMarkerDataId(obj.DataId)) return;

        if (data1 == ObjectEffectEnableData1 && data2 == ObjectEffectEnableData2)
        {
            _activeAnimationMarkerDataIds.Add(obj.DataId);
            if (obj.DataId == DataIdLeftHalf)
                _leftHalfEnabled = true;
            else if (obj.DataId == DataIdRightHalf)
                _rightHalfEnabled = true;
        }
        else if (data1 == ObjectEffectDisableData1 && data2 == ObjectEffectDisableData2)
        {
            _activeAnimationMarkerDataIds.Remove(obj.DataId);
            if (obj.DataId == DataIdLeftHalf)
                _leftHalfEnabled = false;
            else if (obj.DataId == DataIdRightHalf)
                _rightHalfEnabled = false;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (target != BasePlayer.EntityId) return;
        if (!source.TryGetObject(out var sourceObj) || sourceObj.DataId != DataIdTetherSource) return;
        if (!TryResolveTetherReminderText(sourceObj.Position, out var text)) return;

        _tetherRemindersBySource[source] = text;
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        _tetherRemindersBySource.Remove(source);
    }

    public override void OnReset()
    {
        _tetherRemindersBySource.Clear();
        _activeAnimationMarkerDataIds.Clear();
        _leftHalfEnabled = false;
        _rightHalfEnabled = false;
        Controller.Hide();
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Remind Graven Image Actions");
        ImGui.Spacing();
        
        ImGui.Text("Image1 Tether Remind");
        DrawTextInput("Knockback", ref C.Text1Knockback);

        ImGui.Separator();
        ImGui.Text("Image2 Tether Remind");
        DrawTextInput("Vitrophyre", ref C.Text2Vitrophyre);
        DrawTextInput("Gravitas", ref C.Text2Gravitas);

        ImGui.Separator();
        ImGui.Text("Image2 Avoid Half-AOE Area Remind");
        DrawTextInput("Lefthalf AOE", ref C.Text2LeftHalf);
        DrawTextInput("Righthalf AOE", ref C.Text2RightHalf);
        ImGui.Checkbox("Show Image2 Half-AOE", ref C.ShowImage2HalfAoe);

        ImGui.Separator();
        ImGui.Text("Image3 Tether Remind");
        DrawTextInput("Confused", ref C.Text3Confused);
        DrawTextInput("Sleep", ref C.Text3Sleep);

        ImGui.Separator();
        ImGui.Text("Image3 Gaze Remind");
        DrawTextInput("Dont Look", ref C.Text3DontLook);
        DrawTextInput("Look", ref C.Text3Look);
    }

    #endregion

    #region Private Method

    // Draws a labeled ImGui text input bound to config.
    private static void DrawTextInput(string label, ref string value)
    {
        ImGui.SetNextItemWidth(200f);
        ImGui.InputText(label, ref value, 128);
    }

    // Adds text once when non-empty.
    private static void AddUniqueText(List<string> texts, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || texts.Contains(text)) return;
        texts.Add(text);
    }

    // Builds overlay text for active tether reminders.
    private List<string> BuildTetherReminderTexts()
    {
        var texts = new List<string>();
        foreach (var text in _tetherRemindersBySource.Values)
            AddUniqueText(texts, text);
        return texts;
    }

    // Builds overlay text for active animation-check reminders.
    private List<string> BuildAnimationReminderTexts()
    {
        var texts = new List<string>();

        if (_activeAnimationMarkerDataIds.Contains(DataIdLeftHalf))
            AddUniqueText(texts, C.Text2LeftHalf);

        if (_activeAnimationMarkerDataIds.Contains(DataIdRightHalf))
            AddUniqueText(texts, C.Text2RightHalf);

        if (_activeAnimationMarkerDataIds.Contains(DataIdDontLook))
            AddUniqueText(texts, C.Text3DontLook);

        if (_activeAnimationMarkerDataIds.Contains(DataIdLook))
            AddUniqueText(texts, C.Text3Look);

        return texts;
    }

    // Enables or disables a player-attached reminder element with combined overlay text.
    private void ApplyPlayerReminder(string elementName, List<string> activeTexts)
    {
        if (!Controller.TryGetElementByName(elementName, out var reminder))
            return;

        if (activeTexts.Count == 0)
        {
            reminder.Enabled = false;
            return;
        }

        reminder.Enabled = true;
        reminder.refActorObjectID = BasePlayer.EntityId;
        reminder.overlayText = string.Join(" ", activeTexts);
    }

    private static bool IsAnimationMarkerDataId(uint dataId)
        => AnimationMarkerDataIds.Contains(dataId);

    private bool TryResolveTetherReminderText(Vector3 position, out string text)
    {
        if (IsNear(position, PosKnockback))
        {
            text = C.Text1Knockback;
            return !string.IsNullOrWhiteSpace(text);
        }

        if (IsNear(position, PosVitrophyre))
        {
            text = C.Text2Vitrophyre;
            return !string.IsNullOrWhiteSpace(text);
        }

        if (IsNear(position, PosGravitas))
        {
            text = C.Text2Gravitas;
            return !string.IsNullOrWhiteSpace(text);
        }

        if (IsNear(position, PosConfused))
        {
            text = C.Text3Confused;
            return !string.IsNullOrWhiteSpace(text);
        }

        if (IsNear(position, PosSleep))
        {
            text = C.Text3Sleep;
            return !string.IsNullOrWhiteSpace(text);
        }

        text = "";
        return false;
    }

    // Compares game positions within tolerance.
    private static bool IsNear(Vector3 a, Vector3 b)
        => Vector3.Distance(a, b) <= PositionMatchEpsilon;

    #endregion
}
