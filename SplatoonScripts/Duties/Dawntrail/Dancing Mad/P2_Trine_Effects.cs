using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ObjectLifeTracker;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.Dancing_Mad;

internal class P2_Trine_Effects : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(2, "mirage, Poneglyph");
    public override HashSet<uint>? ValidTerritories => [TerritoryDmad];

    #endregion

    #region Constant

    private const uint TerritoryDmad = 1363;
    private const int SceneP3Trine = 6;

    private const uint TelegraphDataIdA = 0x001EBFB2;
    private const uint TelegraphDataIdB = 0x001EBFB3;
    private const uint ObjectEffectData1 = 16;
    private const uint ObjectEffectData2 = 32;

    private const int MaxTelegraphCount = 7;
    private const int VertexCount = 3;

    // plan.md: fillIntensity 0→0.8 while object lifetime is 5–13s
    private const float FillLifeMin = 5f;
    private const float FillLifeMax = 13f;
    private const float FillLifeRange = FillLifeMax - FillLifeMin;
    private const float FillIntensityMax = 0.8f;

    private const float VortexCheckX = 88.5f;
    private const float VortexCheckY = 90.0f;
    private const float VortexProximityTolerance = 1.0f;
    private const float VortexRotationAngle = 60.0f; // degrees

    private static readonly (float X, float Y)[] OffsetsA =
    [
        (5.75f, 0f),
        (-3.0f, 5.0f),
        (-3.0f, -5.0f),
    ];

    private static readonly (float X, float Y)[] OffsetsB =
    [
        (-5.75f, 0f),
        (3.0f, 5.0f),
        (3.0f, -5.0f),
    ];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    private readonly List<TelegraphRecord> _records = [];
    private readonly HashSet<uint> _capturedEntityIds = [];
    private int _activeScene = -1;

    #endregion

    #region Private Class

    private sealed class Config : IEzConfig
    {
        public uint Wave1Color = Colors.Yellow;
        public uint Wave2Color = Colors.Red;
        public uint Wave3Color = Colors.DarkRed;
        public float ElementOffZ = 0f;
        public bool NWTrineFix = true;
    }

    private sealed class TelegraphRecord(int order, uint dataId, uint entityId)
    {
        public int Order { get; } = order;
        public uint DataId { get; } = dataId;
        public uint EntityId { get; } = entityId;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        for (var order = 1; order <= MaxTelegraphCount; order++)
        {
            for (var vertex = 0; vertex < VertexCount; vertex++)
            {
                Controller.RegisterElement(TelegraphElementName(order, vertex), new Element(0)
                {
                    Enabled = false,
                    radius = 6.0f,
                    offZ = 0f,
                    fillIntensity = 0f,
                    color = GetWaveColor(order),
                });
            }
        }
    }

    public override void OnCombatStart() => ResetState();

    public override void OnReset() => ResetState();

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence,
                DirectorUpdateCategory.Wipe))
            ResetState();
    }

    public override void OnUpdate()
    {
        if (!IsPhaseActive())
        {
            if (_activeScene == SceneP3Trine)
                ResetState();
            else
                DisableAllElements();

            _activeScene = Controller.Scene;
            return;
        }

        _activeScene = Controller.Scene;

        DisableAllElements();

        foreach (var record in _records)
        {
            var obj = record.EntityId.GetObject();
            if (obj == null)
                continue;

            var position = NormalizeY(obj.Position);
            var fill = ComputeFillIntensity(obj.GetLifeTimeSeconds());
            var offsets = GetVertexOffsets(record.DataId);
            
            if (C.NWTrineFix && IsNearVortexCoords(position))
                offsets = RotateOffsets(offsets, VortexRotationAngle);

            for (var vertex = 0; vertex < VertexCount; vertex++)
            {
                if (!Controller.TryGetElementByName(TelegraphElementName(record.Order, vertex), out var element))
                    continue;

                element.Enabled = true;
                element.color = GetWaveColor(record.Order);
                element.offX = offsets[vertex].X;
                element.offY = offsets[vertex].Y;
                element.offZ = C.ElementOffZ;
                element.SetRefPosition(position);
                element.fillIntensity = fill;
            }
        }
    }

    public override void OnObjectEffect(uint target, uint data1, uint data2)
    {
        if (!IsPhaseActive())
            return;

        TryCaptureTelegraph(target, data1, data2);
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("Trine wave colors");
        ImGui.Spacing();

        DrawWaveColorPicker("Wave 1 (spawns 1-3)", ref C.Wave1Color);
        DrawWaveColorPicker("Wave 2 (spawn 4)", ref C.Wave2Color);
        DrawWaveColorPicker("Wave 3 (spawns 5-7)", ref C.Wave3Color);
        
        ImGui.Spacing();
        ImGui.Text("Element Settings");
        ImGui.DragFloat("Element Offset Z", ref C.ElementOffZ, 0.1f, 0f, 5f);
        ImGui.Checkbox("NW Trine Fix (60° rotation)", ref C.NWTrineFix);
    }

    #endregion

    #region Private Method

    // Records a trine telegraph spawn (plan.md: 7 spawns, ObjectEffect 16/32).
    private void TryCaptureTelegraph(uint target, uint data1, uint data2)
    {
        if (_records.Count >= MaxTelegraphCount)
        {
            if (!AllRecordsExpired())
                return;

            ResetState();
        }

        if (data1 != ObjectEffectData1 || data2 != ObjectEffectData2)
            return;

        var obj = target.GetObject();
        if (obj == null || !IsTrineTelegraphObject(obj))
            return;

        if (!_capturedEntityIds.Add(target))
            return;

        var order = _records.Count + 1;
        _records.Add(new TelegraphRecord(order, obj.DataId, target));
    }

    // plan.md: fillIntensity ramps from 0 to 0.8 between 5s and 13s object lifetime.
    private static float ComputeFillIntensity(float lifeSeconds)
    {
        return Math.Clamp((lifeSeconds - FillLifeMin) / FillLifeRange, 0f, 1f) * FillIntensityMax;
    }

    // Disables every registered telegraph vertex element.
    private void DisableAllElements()
    {
        for (var order = 1; order <= MaxTelegraphCount; order++)
        {
            for (var vertex = 0; vertex < VertexCount; vertex++)
            {
                if (Controller.TryGetElementByName(TelegraphElementName(order, vertex), out var element))
                    element.Enabled = false;
            }
        }
    }

    // True while scene 6 trine mechanic is active.
    private bool IsPhaseActive() => Controller.Scene == SceneP3Trine;

    // True when every captured telegraph object has despawned.
    private bool AllRecordsExpired()
    {
        return _records.Count > 0 && _records.All(record => record.EntityId.GetObject() == null);
    }

    // Clears capture state and hides all elements.
    private void ResetState()
    {
        _records.Clear();
        _capturedEntityIds.Clear();
        DisableAllElements();
    }

    // True for trine telegraph event objects (2015154 / 2015155).
    private static bool IsTrineTelegraphObject(IGameObject obj)
    {
        return obj.DataId is TelegraphDataIdA or TelegraphDataIdB;
    }

    // Flatten Y so circles sit on the arena floor.
    private static Vector3 NormalizeY(Vector3 position) => new(position.X, 0f, position.Z);

    // plan.md: wave 1 / wave 2 / wave 3 colors (configurable in settings).
    private uint GetWaveColor(int order) => order switch
    {
        >= 1 and <= 3 => C.Wave1Color,
        4 => C.Wave2Color,
        >= 5 and <= 7 => C.Wave3Color,
        _ => C.Wave1Color,
    };

    // Edits an ABGR wave color through ImGui and writes back to config as uint.
    private static void DrawWaveColorPicker(string label, ref uint color)
    {
        var pickerColor = Vector4FromAbgr(color);
        if (ImGui.ColorEdit4(label, ref pickerColor, ImGuiColorEditFlags.NoInputs))
            color = pickerColor.ToUint();
    }

    // Converts Splatoon ABGR element color to ImGui RGBA Vector4.
    private static unsafe Vector4 Vector4FromAbgr(uint color)
    {
        var bytes = (byte*)&color;
        return new Vector4(bytes[0] / 255f, bytes[1] / 255f, bytes[2] / 255f, bytes[3] / 255f);
    }

    // Returns vertex offsets for the telegraph triangle orientation.
    private static (float X, float Y)[] GetVertexOffsets(uint dataId)
    {
        return dataId == TelegraphDataIdA ? OffsetsA : OffsetsB;
    }

    private static bool IsNearVortexCoords(Vector3 position)
    {
        var distX = Math.Abs(position.X - VortexCheckX);
        var distZ = Math.Abs(position.Z - VortexCheckY);
        return distX <= VortexProximityTolerance && distZ <= VortexProximityTolerance;
    }

    private static (float X, float Y)[] RotateOffsets((float X, float Y)[] offsets, float angleDegrees)
    {
        var angleRadians = angleDegrees * (float)Math.PI / 180f;
        var cos = (float)Math.Cos(angleRadians);
        var sin = (float)Math.Sin(angleRadians);

        var rotated = new (float X, float Y)[offsets.Length];
        for (var i = 0; i < offsets.Length; i++)
        {
            var x = offsets[i].X;
            var y = offsets[i].Y;
            rotated[i] = (
                x * cos - y * sin,
                x * sin + y * cos
            );
        }

        return rotated;
    }

    // Element name shared by OnSetup registration and OnUpdate lookup.
    private static string TelegraphElementName(int order, int vertex) => $"T{order}V{vertex}";

    #endregion
}
