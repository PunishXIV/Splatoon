using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.GameFunctions;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using System.Collections.Generic;
using System.Linq;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P1_Fall_of_Faith_Guide : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [1238];

    #endregion

    #region Constants

    private const uint CastFire = 40137;
    private const uint CastLightning = 40140;

    private const uint TetherFire = 249;
    private const uint TetherLightning = 287;

    private const uint StatusMarking = 1051;
    private const int TetherCount = 4;

    private const string LayoutPositions = "Positions";
    private const string LayoutStyle = "Visualizer.Style";
    private const string LayoutOrderHint = "Visualizer.TetherColorOrderHint";
    private const string LayoutTetherHint = "Visualizer.TetherColorHint";
    private const string LayoutPlayerPillar = "Visualizer.PlayerPillar";
    private const string LayoutCone = "Visualizer.Cone";

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();
    private IPlayerCharacter BasePlayer => Controller.BasePlayer;

    #endregion

    #region State

    private bool _active;
    private int _removals;
    private readonly List<CastSlot> _slots = [];
    private readonly HashSet<uint> _removedIds = [];

    #endregion

    #region Types

    private sealed class Config : IEzConfig
    {
        public PriorityData PriorityData = new();
        public bool EnableVisualizer;
        public bool ShowTetherColorOrderHint = true;
        public bool ShowTetherColorHint = true;
        public bool ShowPlayerPillar = true;
        public bool ShowCone;
    }

    private enum TetherColor
    {
        Red,
        Blue,
    }

    private sealed class CastSlot
    {
        public uint CasterId;
        public uint PlayerId;
        public TetherColor Color;
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        Controller.TryRegisterLayoutFromCode(LayoutPositions, """
            ~Lv2~{"Name":"Positions","ZoneLockH":[1238],"ElementsL":[{"Name":"Tether 1 Bait","type":1,"Enabled":false,"offX":-6.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorDataID":17819,"refActorComparisonType":3},{"Name":"Tether 2 Bait","type":1,"Enabled":false,"offX":6.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorDataID":17819,"refActorComparisonType":3},{"Name":"Tether 3 Wait","type":1,"Enabled":false,"offX":-9.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorDataID":17819,"refActorComparisonType":3},{"Name":"Tether 4 Wait","type":1,"Enabled":false,"offX":9.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorDataID":17819,"refActorComparisonType":3},{"Name":"Tether 3 Bait","type":1,"Enabled":false,"offX":-6.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorDataID":17819,"refActorComparisonType":3},{"Name":"Tether 4 Bait","type":1,"Enabled":false,"offX":6.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorDataID":17819,"refActorComparisonType":3},{"Name":"Target 1 Side 1","type":1,"Enabled":false,"offX":0.0,"offY":-2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 1 Side 2","type":1,"Enabled":false,"offX":0.0,"offY":2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 1 Behind","type":1,"Enabled":false,"offX":-2.0,"offY":0.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 2 Side 1","type":1,"Enabled":false,"offX":0.0,"offY":-2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 2 Side 2","type":1,"Enabled":false,"offX":0.0,"offY":2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 2 Behind","type":1,"Enabled":false,"offX":2.0,"offY":0.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 3 Side 1","type":1,"Enabled":false,"offX":0.0,"offY":-2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 3 Side 2","type":1,"Enabled":false,"offX":0.0,"offY":2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 3 Behind","type":1,"Enabled":false,"offX":-2.0,"offY":0.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 4 Side 1","type":1,"Enabled":false,"offX":0.0,"offY":-2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 4 Side 2","type":1,"Enabled":false,"offX":0.0,"offY":2.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2},{"Name":"Target 4 Behind","type":1,"Enabled":false,"offX":2.0,"offY":0.0,"radius":0.35,"Donut":0.15,"thicc":3.0,"refActorComparisonType":2}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutStyle, """
            ~Lv2~{"Name":"Visualizer.Style","ZoneLockH":[1238],"ElementsL":[{"Name":"Blue Style","Enabled":false,"refX":100.0,"refY":100.0,"radius":0.0,"color":1895759872,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1895759872,"overlayFScale":2.0,"thicc":0.0,"overlayText":"Blue","overlayTextIntl":{"Jp":"青"}},{"Name":"Red Style","Enabled":false,"refX":100.0,"refY":100.0,"radius":0.0,"color":1879048447,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048447,"overlayFScale":2.0,"thicc":0.0,"overlayText":"Red","overlayTextIntl":{"Jp":"赤"}}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutOrderHint, """
            ~Lv2~{"Name":"Visualizer.TetherColorOrderHint","ZoneLockH":[1238],"ElementsL":[{"Name":"Order Hint","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"? → ? → ? → ?","refActorDataID":17819,"refActorComparisonType":3,"includeRotation":true}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutTetherHint, """
            ~Lv2~{"Name":"Visualizer.TetherColorHint","ZoneLockH":[1238],"ElementsL":[{"Name":"Tether 1 Hint","type":1,"Enabled":false,"radius":0.0,"Donut":0.15,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"?","refActorComparisonType":2},{"Name":"Tether 2 Hint","type":1,"Enabled":false,"radius":0.0,"Donut":0.15,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"?","refActorComparisonType":2},{"Name":"Tether 3 Hint","type":1,"Enabled":false,"radius":0.0,"Donut":0.15,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"?","refActorComparisonType":2},{"Name":"Tether 4 Hint","type":1,"Enabled":false,"radius":0.0,"Donut":0.15,"Filled":false,"fillIntensity":0.5,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"?","refActorComparisonType":2}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutPlayerPillar, """
            ~Lv2~{"Name":"Visualizer.PlayerPillar","ZoneLockH":[1238],"ElementsL":[{"Name":"No Tether","type":3,"Enabled":false,"offZ":1.54,"radius":0.0,"color":3355443200,"fillIntensity":0.345,"thicc":30.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorRequireBuffsInvert":true,"refActorComparisonType":5},{"Name":"Tether 1 Mark","type":3,"Enabled":false,"offZ":1.54,"radius":0.0,"color":3355443200,"fillIntensity":0.345,"thicc":30.0,"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":2},{"Name":"Tether 2 Mark","type":3,"Enabled":false,"offZ":1.54,"radius":0.0,"color":3355443200,"fillIntensity":0.345,"thicc":30.0,"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":2},{"Name":"Tether 3 Mark","type":3,"Enabled":false,"offZ":1.54,"radius":0.0,"color":3355443200,"fillIntensity":0.345,"thicc":30.0,"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":2},{"Name":"Tether 4 Mark","type":3,"Enabled":false,"offZ":1.54,"radius":0.0,"color":3355443200,"fillIntensity":0.345,"thicc":30.0,"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":2}]}
            """, out _, overwrite: true);
        Controller.TryRegisterLayoutFromCode(LayoutCone, """
            ~Lv2~{"Name":"Visualizer.Cone","ZoneLockH":[1238],"ElementsL":[{"Name":"Tether 1 Blue Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 1 Blue Closest 2","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1102,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 1 Blue Closest 3","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1103,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 1 Blue Spread 1","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 1 Blue Closest 1>"},{"Name":"Tether 1 Blue Spread 2","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 1 Blue Closest 2>"},{"Name":"Tether 1 Blue Spread 3","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 1 Blue Closest 3>"},{"Name":"Tether 1 Red Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 1 Red Stack","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 1 Red Closest 1>"},{"Name":"Tether 2 Blue Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 2 Blue Closest 2","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1102,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 2 Blue Closest 3","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1103,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 2 Blue Spread 1","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 2 Blue Closest 1>"},{"Name":"Tether 2 Blue Spread 2","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 2 Blue Closest 2>"},{"Name":"Tether 2 Blue Spread 3","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 2 Blue Closest 3>"},{"Name":"Tether 2 Red Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 2 Red Stack","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 2 Red Closest 1>"},{"Name":"Tether 3 Blue Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 3 Blue Closest 2","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1102,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 3 Blue Closest 3","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1103,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 3 Blue Spread 1","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 3 Blue Closest 1>"},{"Name":"Tether 3 Blue Spread 2","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 3 Blue Closest 2>"},{"Name":"Tether 3 Blue Spread 3","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 3 Blue Closest 3>"},{"Name":"Tether 3 Red Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 3 Red Stack","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 3 Red Closest 1>"},{"Name":"Tether 4 Blue Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 4 Blue Closest 2","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1102,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 4 Blue Closest 3","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1103,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 4 Blue Spread 1","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 4 Blue Closest 1>"},{"Name":"Tether 4 Blue Spread 2","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 4 Blue Closest 2>"},{"Name":"Tether 4 Blue Spread 3","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-60,"coneAngleMax":60,"color":3372220160,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 4 Blue Closest 3>"},{"Name":"Tether 4 Red Closest 1","type":1,"Enabled":false,"refActorRequireBuff":true,"refActorBuffId":[2304],"TargetAlteration":1101,"refActorComparisonType":2,"IsCapturing":true,"Nodraw":true},{"Name":"Tether 4 Red Stack","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-45,"coneAngleMax":45,"fillIntensity":0.205,"thicc":5.0,"refActorComparisonType":2,"includeRotation":true,"FaceMe":true,"faceplayer":"<element:Tether 4 Red Closest 1>"}]}
            """, out _, overwrite: true);
    }

    public override void OnUpdate()
    {
        if(!SceneP1)
            return;

        foreach(var slot in _slots)
            TryFillPlayer(slot);

        HideAll();
        if(!_active || _removals >= TetherCount)
            return;

        ApplyPlayerAnchors();
        ApplyGuide();
        ApplyVisualizer();
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(!SceneP1)
            return;
        if(castId is not (CastFire or CastLightning))
            return;

        if(!_active || _removals >= TetherCount)
        {
            ClearState();
            _active = true;
        }
        else if(_slots.Count >= TetherCount)
            return;

        var slot = new CastSlot
        {
            CasterId = source,
            Color = castId == CastFire ? TetherColor.Red : TetherColor.Blue,
        };
        TryFillPlayer(slot);
        _slots.Add(slot);
    }

    public override void OnRemoveBuffEffect(uint sourceId, Status status)
    {
        if(!SceneP1)
            return;
        if(!_active || _slots.Count < TetherCount || _removals >= TetherCount)
            return;
        if(status.StatusId != StatusMarking)
            return;
        if(_removedIds.Contains(sourceId))
            return;
        if(!_slots.Any(x => x.PlayerId == sourceId))
            return;

        _removedIds.Add(sourceId);
        _removals++;
    }

    public override void OnReset()
    {
        ClearState();
        HideAll();
    }

    public override void OnSettingsDraw()
    {
        ImGui.TextDisabled("Priority List:");
        ImGui.Separator();
        C.PriorityData.Draw();
        ImGui.Spacing();
        ImGui.TextDisabled("Visualizer:");
        ImGui.Separator();
        ImGui.Checkbox("Enable Visualizer", ref C.EnableVisualizer);
        ImGui.Indent();
        if(!C.EnableVisualizer)
            ImGui.BeginDisabled();
        ImGui.Checkbox("Show Tether Color Order Hint", ref C.ShowTetherColorOrderHint);
        ImGui.Checkbox("Show Tether Color Hint", ref C.ShowTetherColorHint);
        ImGui.Checkbox("Show Player Pillar", ref C.ShowPlayerPillar);
        ImGui.Checkbox("Show Cone (Spread / Stack)", ref C.ShowCone);
        if(!C.EnableVisualizer)
            ImGui.EndDisabled();
        ImGui.Unindent();
    }

    #endregion

    #region Private Method

    // True while the duty scene is phase 1.
    private bool SceneP1 => Controller.Scene == 1;

    // Clears the mechanic so the next cast can start a new sequence.
    private void ClearState()
    {
        _active = false;
        _slots.Clear();
        _removedIds.Clear();
        _removals = 0;
    }

    // Fills the slot player from the caster's outgoing fire or lightning tether.
    private static void TryFillPlayer(CastSlot slot)
    {
        if(slot.PlayerId != 0)
            return;
        if(!slot.CasterId.TryGetObject(out var obj) || obj is not ICharacter caster)
            return;

        var tether = caster.GetTethers(true).FirstOrDefault(x => x.Id is TetherFire or TetherLightning);
        if(tether == null || tether.Pair is not IPlayerCharacter)
            return;

        slot.PlayerId = tether.PairId;
    }

    // Returns one element from a registered layout.
    private Element? FindElement(string layoutName, string elementName)
        => Controller.GetRegisteredLayouts().SafeSelect(layoutName)?.GetElement(elementName);

    // Layout element name for one tether slot.
    private static string TetherElement(int number, string part) => $"Tether {number} {part}";

    // Layout element name for one target slot.
    private static string TargetElement(int number, string part) => $"Target {number} {part}";

    // Disables every Positions, Style, and visualizer element.
    private void HideAll()
    {
        DisableLayout(LayoutPositions);
        DisableLayout(LayoutStyle);
        DisableLayout(LayoutOrderHint);
        DisableLayout(LayoutTetherHint);
        DisableLayout(LayoutPlayerPillar);
        DisableLayout(LayoutCone);
    }

    // Disables the elements of one layout.
    private void DisableLayout(string name)
    {
        var layout = Controller.GetRegisteredLayouts().SafeSelect(name);
        if(layout == null)
            return;

        foreach(var element in layout.ElementsL)
        {
            element.Enabled = false;
            element.tether = false;
        }
    }

    // Points each tether's Side and Behind at that player.
    private void ApplyPlayerAnchors()
    {
        for(var index = 0; index < TetherCount; index++)
        {
            var number = index + 1;
            SetPlayerAnchor(index, TargetElement(number, "Side 1"));
            SetPlayerAnchor(index, TargetElement(number, "Side 2"));
            SetPlayerAnchor(index, TargetElement(number, "Behind"));
        }
    }

    // Sets one element's actor to the tethered player when that player is known.
    private void SetPlayerAnchor(int index, string name)
    {
        if(index >= _slots.Count || _slots[index].PlayerId == 0)
            return;

        var element = FindElement(LayoutPositions, name);
        if(element == null)
            return;

        element.refActorObjectID = _slots[index].PlayerId;
    }

    // Shows the local player's spot for the current cast count and step.
    private void ApplyGuide()
    {
        if(BasePlayer == null || _slots.Count == 0)
            return;

        var me = BasePlayer.EntityId;
        if(_slots.Count < TetherCount)
        {
            switch(TetherIndex(me))
            {
                case 0: ShowGuide("Tether 1 Bait"); break;
                case 1: ShowGuide("Tether 2 Bait"); break;
                case 2: ShowGuide("Tether 3 Wait"); break;
            }
            return;
        }

        switch(_removals)
        {
            case 0: ApplyStep1(me); break;
            case 1: ApplyStep2(me); break;
            case 2: ApplyStep3(me); break;
            case 3: ApplyStep4(me); break;
        }
    }

    // Step 1. West resolves tether 1, east prepares tether 2.
    private void ApplyStep1(uint me)
    {
        switch(TetherIndex(me))
        {
            case 0: ShowGuide("Tether 1 Bait"); return;
            case 1: ShowGuide("Tether 2 Bait"); return;
            case 2: ShowGuide("Target 1 Behind"); return;
            case 3: ShowGuide("Target 2 Behind"); return;
        }

        switch(GetPriorityIndex())
        {
            case 0: ShowColorPlace(1, _slots[0].Color, 1); break;
            case 1: ShowColorPlace(1, _slots[0].Color, 2); break;
            case 2: ShowColorPlace(2, _slots[1].Color, 1); break;
            case 3: ShowColorPlace(2, _slots[1].Color, 2); break;
        }
    }

    // Step 2. East resolves tether 2, west prepares tether 3.
    private void ApplyStep2(uint me)
    {
        switch(TetherIndex(me))
        {
            case 0: ShowGuide("Target 3 Behind"); return;
            case 1: ShowGuide("Tether 2 Bait"); return;
            case 2: ShowGuide("Tether 3 Bait"); return;
            case 3: ShowGuide("Target 2 Behind"); return;
        }

        switch(GetPriorityIndex())
        {
            case 0: ShowColorPlace(3, _slots[2].Color, 1); break;
            case 1: ShowColorPlace(3, _slots[2].Color, 2); break;
            case 2: ShowColorPlace(2, _slots[1].Color, 1); break;
            case 3: ShowColorPlace(2, _slots[1].Color, 2); break;
        }
    }

    // Step 3. West resolves tether 3, east prepares tether 4.
    private void ApplyStep3(uint me)
    {
        switch(TetherIndex(me))
        {
            case 0: ShowGuide("Target 3 Behind"); return;
            case 1: ShowGuide("Target 4 Behind"); return;
            case 2: ShowGuide("Tether 3 Bait"); return;
            case 3: ShowGuide("Tether 4 Bait"); return;
        }

        switch(GetPriorityIndex())
        {
            case 0: ShowColorPlace(3, _slots[2].Color, 1); break;
            case 1: ShowColorPlace(3, _slots[2].Color, 2); break;
            case 2: ShowColorPlace(4, _slots[3].Color, 1); break;
            case 3: ShowColorPlace(4, _slots[3].Color, 2); break;
        }
    }

    // Step 4. East resolves tether 4.
    private void ApplyStep4(uint me)
    {
        switch(TetherIndex(me))
        {
            case 1: ShowGuide("Target 4 Behind"); return;
            case 3: ShowGuide("Tether 4 Bait"); return;
        }

        switch(GetPriorityIndex())
        {
            case 2: ShowColorPlace(4, _slots[3].Color, 1); break;
            case 3: ShowColorPlace(4, _slots[3].Color, 2); break;
        }
    }

    // Index among non-tether players, or -1.
    private int GetPriorityIndex()
        => C.PriorityData.GetOwnIndex(member =>
            !_slots.Any(slot => slot.PlayerId != 0 && slot.PlayerId == member.IGameObject.EntityId));

    // Returns the tether slot of a player, or -1.
    private int TetherIndex(uint entityId)
        => _slots.FindIndex(slot => slot.PlayerId == entityId);

    // Shows Behind for red, or the numbered side for blue.
    private void ShowColorPlace(int target, TetherColor color, int side)
    {
        if(color == TetherColor.Red)
            ShowGuide($"Target {target} Behind");
        else
            ShowGuide($"Target {target} Side {side}");
    }

    // Enables a Positions element and tethers the local player to it.
    private void ShowGuide(string name)
    {
        var element = FindElement(LayoutPositions, name);
        if(element == null)
            return;

        element.Enabled = true;
        element.tether = true;
        element.color = Controller.AttentionColor;
    }

    // Draws the configured visualizer pieces for the current cast slots.
    private void ApplyVisualizer()
    {
        if(!C.EnableVisualizer)
            return;

        var red = FindElement(LayoutStyle, "Red Style");
        var blue = FindElement(LayoutStyle, "Blue Style");

        if(C.ShowTetherColorOrderHint)
        {
            var order = FindElement(LayoutOrderHint, "Order Hint");
            if(order != null)
            {
                order.overlayText = $"{HintText(0, red, blue)} → {HintText(1, red, blue)} → {HintText(2, red, blue)} → {HintText(3, red, blue)}";
                order.Enabled = true;
            }
        }

        if(C.ShowTetherColorHint)
        {
            for(var index = 0; index < TetherCount; index++)
                ApplyTetherHint(index, red, blue);
        }

        if(C.ShowPlayerPillar)
        {
            var noTether = FindElement(LayoutPlayerPillar, "No Tether");
            if(noTether != null)
                noTether.Enabled = true;

            for(var index = 0; index < TetherCount; index++)
                ApplyTetherMark(index, red, blue);
        }

        if(C.ShowCone)
        {
            for(var index = 0; index < TetherCount; index++)
                EnableCone(index);
        }
    }

    // Red or blue style element for a tether color.
    private static Element? StyleFor(TetherColor color, Element? red, Element? blue)
        => color == TetherColor.Red ? red : blue;

    // Places one tether hint on that player and copies the matching style.
    private void ApplyTetherHint(int index, Element? red, Element? blue)
    {
        if(index >= _slots.Count || _slots[index].PlayerId == 0 || _removals > index)
            return;

        var hint = FindElement(LayoutTetherHint, TetherElement(index + 1, "Hint"));
        if(hint == null)
            return;

        var style = StyleFor(_slots[index].Color, red, blue);
        if(style != null)
            CopyHintStyle(hint, style);

        hint.refActorObjectID = _slots[index].PlayerId;
        hint.Enabled = true;
    }

    // Returns the style label for a cast slot, or ? when that slot is empty.
    private string HintText(int index, Element? red, Element? blue)
    {
        if(index >= _slots.Count)
            return "?";

        var style = StyleFor(_slots[index].Color, red, blue);
        if(style == null)
            return _slots[index].Color == TetherColor.Red ? "Red" : "Blue";

        return style.overlayTextIntl.Get(style.overlayText);
    }

    // Copies the resolved style label and colors onto a tether hint.
    private static void CopyHintStyle(Element target, Element style)
    {
        target.overlayText = style.overlayTextIntl.Get(style.overlayText);
        target.overlayBGColor = style.overlayBGColor;
        target.overlayTextColor = style.overlayTextColor;
    }

    // Places the tether mark on that player and copies the style color.
    private void ApplyTetherMark(int index, Element? red, Element? blue)
    {
        if(index >= _slots.Count || _slots[index].PlayerId == 0)
            return;

        var mark = FindElement(LayoutPlayerPillar, TetherElement(index + 1, "Mark"));
        if(mark == null)
            return;

        mark.refActorObjectID = _slots[index].PlayerId;
        mark.Enabled = true;

        var style = StyleFor(_slots[index].Color, red, blue);
        if(style != null)
            mark.color = style.color;
    }

    // Enables the spread or stack cone for one tether.
    private void EnableCone(int index)
    {
        if(index >= _slots.Count || _slots[index].PlayerId == 0)
            return;

        var playerId = _slots[index].PlayerId;
        var number = index + 1;
        if(_slots[index].Color == TetherColor.Red)
        {
            EnableAnchored(TetherElement(number, "Red Closest 1"), playerId);
            EnableAnchored(TetherElement(number, "Red Stack"), playerId);
            return;
        }

        EnableAnchored(TetherElement(number, "Blue Closest 1"), playerId);
        EnableAnchored(TetherElement(number, "Blue Closest 2"), playerId);
        EnableAnchored(TetherElement(number, "Blue Closest 3"), playerId);
        EnableAnchored(TetherElement(number, "Blue Spread 1"), playerId);
        EnableAnchored(TetherElement(number, "Blue Spread 2"), playerId);
        EnableAnchored(TetherElement(number, "Blue Spread 3"), playerId);
    }

    // Enables one cone element and points it at a player.
    private void EnableAnchored(string name, uint objectId)
    {
        var element = FindElement(LayoutCone, name);
        if(element == null)
            return;

        element.refActorObjectID = objectId;
        element.Enabled = true;
    }

    #endregion
}
