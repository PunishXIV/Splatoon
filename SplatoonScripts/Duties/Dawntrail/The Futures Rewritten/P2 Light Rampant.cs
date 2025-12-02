
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;
internal class P2_Light_Rampant : SplatoonScript
{

    #region public properties
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(7, "redmoon & Smoothtalk, NightmareXIV");
    #endregion
    #region enums
    private enum State
    {
        None = 0,
        LightRampantCasting,
        TetherSpawned,
        TowerSpawned,
        AoeSET1,
        AoeSET2,
        AoeSET3,
        AoeSET4,
        AoeSET5,
        WaitStack,
        AvoidAOE,
        TowerIn,
        Banish,
    }
    #endregion

    #region class
    public class Config : IEzConfig
    {
        public bool NorthSwap = false;
        public PriorityData Priority = new();
        public Prio4 AoePriority = new();
        public bool IsDefaultNorth = false;
        public bool GuessDefault = false;
        public bool IsPuddleCW = true;
        public bool PsychopathMode = false;
    }

    public class Prio4 : PriorityData
    {
        public override int GetNumPlayers()
        {
            return 4;
        }
    }


    private class PartyData
    {
        public int index = 0;
        public bool Mine => EntityId == Player.Object.EntityId;
        public uint EntityId = 0;
        public IPlayerCharacter? Object => (IPlayerCharacter)EntityId.GetObject() ?? null;
        public uint TetherPairId1 = 0;
        public uint TetherPairId2 = 0;
        public Direction TowerDirection = Direction.None;

        public PartyData(uint entityId)
        {
            EntityId = entityId;
        }
    }
    #endregion

    #region const
    private readonly List<(Direction, Vector3)> TowerPos =
    [
        new (Direction.NorthWest, new Vector3(86, 0, 92)),
        new (Direction.North, new Vector3(100, 0, 84)),
        new (Direction.NorthEast, new Vector3(114, 0, 92)),
        new (Direction.SouthEast, new Vector3(114, 0, 108)),
        new (Direction.South, new Vector3(100, 0, 116)),
        new (Direction.SouthWest, new Vector3(86, 0, 108)),
    ];
    #endregion

    #region private properties
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private int _onTetherCount = 0;
    private bool? _isClockwiseRotation = null;
    private Config C => Controller.GetConfig<Config>();
    private string NewPlayer = "";
    #endregion

    #region public methods
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("BaitObject", new Element(1) { tether = true, refActorComparisonType = 2, radius = 0.5f, thicc = 6f });
        for(var i = 0; i < 8; i++)
        {
            Controller.RegisterElement($"Circle{i}", new Element(1) { radius = 5.0f, refActorComparisonType = 2, thicc = 2f, fillIntensity = 0.2f });
        }
        Controller.RegisterElement("DropSpotN1", new Element(0) { radius = 0.3f, thicc = 2f, Filled = true, fillIntensity = 1f });
        Controller.RegisterElement("DropSpotS1", new Element(0) { radius = 0.3f, thicc = 2f, Filled = true, fillIntensity = 1f });

        Controller.TryRegisterLayoutFromCode("DropNorthCCW", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":100.62637,"refY":91.17723,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":97.08154,"refY":92.43323,"offX":93.63914,"offY":95.08048,"offZ":9.536743E-07,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":91.52648,"refY":99.03643,"refZ":9.536743E-07,"offX":93.63914,"offY":95.08048,"offZ":9.536743E-07,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":92.01154,"refY":104.049995,"refZ":9.536743E-07,"offX":91.54469,"offY":99.03506,"offZ":-1.9073486E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":98.196945,"refY":113.5512,"offX":92.00482,"offY":103.995766,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);
        Controller.TryRegisterLayoutFromCode("DropSouthCCW", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":100.05792,"refY":108.1015,"refZ":1.9073486E-06,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":102.87615,"refY":107.529915,"refZ":-3.8146973E-06,"offX":107.19561,"offY":103.58603,"offZ":-7.6293945E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":109.096306,"refY":98.77759,"offX":107.20059,"offY":103.55709,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":106.55073,"refY":93.23555,"refZ":-3.8146973E-06,"offX":109.07767,"offY":98.877205,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":100.29016,"refY":84.03371,"refZ":3.8146973E-06,"offX":106.52548,"offY":93.19448,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);
        Controller.TryRegisterLayoutFromCode("DropSouthCW", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":100.05792,"refY":108.1015,"refZ":1.9073486E-06,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":96.07363,"refY":107.04102,"refZ":1.9073486E-06,"offX":92.51755,"offY":102.39281,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":93.41478,"refY":94.92555,"refZ":-1.9073486E-06,"offX":92.51698,"offY":102.30562,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":97.42292,"refY":89.76134,"refZ":1.9073486E-06,"offX":93.475876,"offY":95.040054,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":97.42292,"refY":89.76134,"refZ":1.9073486E-06,"offX":100.065575,"offY":84.53303,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1}]}""", out _);
        Controller.TryRegisterLayoutFromCode("DropNorthCW", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":100.62637,"refY":91.17723,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":104.348076,"refY":91.029396,"offX":107.91795,"offY":94.45116,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":108.70579,"refY":99.64915,"refZ":-1.9073486E-06,"offX":107.89801,"offY":94.37764,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":106.07384,"refY":104.78666,"refZ":3.8146973E-06,"offX":108.72344,"offY":99.71201,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1},{"Name":"","type":2,"refX":100.00951,"refY":114.448395,"offX":106.00915,"offY":104.84683,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);

        Controller.TryRegisterLayoutFromCode("DropWestUp", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":80.90873,"refY":99.89605,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":84.01141,"refY":100.00803,"refZ":-3.8146973E-06,"offX":93.686966,"offY":99.75496,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":95.47949,"refY":83.61224,"refZ":3.8146973E-06,"offX":93.686966,"offY":99.75496,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);
        Controller.TryRegisterLayoutFromCode("DropWestDown", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":80.90873,"refY":99.89605,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":84.01141,"refY":100.00803,"refZ":-3.8146973E-06,"offX":93.686966,"offY":99.75496,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":95.71823,"refY":116.053375,"refZ":3.8146973E-06,"offX":93.686966,"offY":99.75496,"offZ":3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);
        Controller.TryRegisterLayoutFromCode("DropEastUp", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":119.29768,"refY":99.952484,"refZ":-3.8146973E-06,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":115.83856,"refY":99.940704,"refZ":-3.8146973E-06,"offX":105.75837,"offY":99.99495,"offZ":-3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":104.254906,"refY":82.546585,"offX":105.764175,"offY":100.02766,"offZ":-3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);
        Controller.TryRegisterLayoutFromCode("DropEastDown", """~Lv2~{"ZoneLockH":[1238],"ElementsL":[{"Name":"","refX":119.29768,"refY":99.952484,"refZ":-3.8146973E-06,"radius":5.0,"color":3355508496,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"tether":true},{"Name":"","type":2,"refX":115.83856,"refY":99.940704,"refZ":-3.8146973E-06,"offX":105.75837,"offY":99.99495,"offZ":-3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndB":1},{"Name":"","type":2,"refX":104.582115,"refY":116.23678,"refZ":7.6293945E-06,"offX":105.80082,"offY":100.04084,"offZ":-3.8146973E-06,"radius":0.0,"fillIntensity":0.5,"thicc":4.0,"LineEndA":1}]}""", out _);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40212)
        {
            SetListEntityId();

            _state = State.LightRampantCasting;
        }
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(_state != State.TetherSpawned) return;
        if(position == 9 && data1 == 1 && data2 == 2)
        {
            var pc = GetMinedata();

            if(pc.TowerDirection != Direction.None)
            {
                Controller.GetElementByName("Bait").SetRefPosition(TowerPos.Find(x => x.Item1 == pc.TowerDirection).Item2);
                Controller.GetElementByName("Bait").tether = true;
                Controller.GetElementByName("Bait").Enabled = true;
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if(castId == 40213)
        {
            Controller.Reset();
        }
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Where(x => x.Value.Enabled).Each(x => x.Value.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint());
        Controller.GetRegisteredLayouts().Each(x => x.Value.Enabled = false);
        if(!Player.Status.Any(s => s.StatusId == 4158 || s.StatusId == 4157))
        {
            if(Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == 4159 && s.RemainingTime >= 4f)))
            {
                bool? isNorthBait = null;
                var prio2 = C.AoePriority.GetFirstValidList();
                if(prio2 != null)
                {
                    var nonTethers = C.AoePriority.GetPlayers(x => !((IPlayerCharacter)x.IGameObject).StatusList.Any(s => s.StatusId == 4157)) ?? [];
                    if(nonTethers.Count == 2)
                    {
                        if(nonTethers[0].IGameObject.AddressEquals(Player.Object))
                        {
                            isNorthBait = true;
                        }
                        if(nonTethers[1].IGameObject.AddressEquals(Player.Object))
                        {
                            isNorthBait = false;
                        }
                    }
                    else if(nonTethers.Count == 1)
                    {
                        if(nonTethers[0].IGameObject.AddressEquals(Player.Object))
                        {
                            if(C.GuessDefault)
                            {
                                var other = Controller.GetPartyMembers().FirstOrDefault(x => !x.AddressEquals(Player.Object) && !x.StatusList.Any(s => s.StatusId == 4157));
                                if(other != null)
                                {
                                    isNorthBait = Player.Position.X < other.Position.X;
                                }
                            }
                            else
                            {
                                isNorthBait = C.IsDefaultNorth;
                            }
                        }
                    }
                }
                if(!C.PsychopathMode)
                {
                    var suffix = C.IsPuddleCW ? "CW" : "CCW";
                    if(isNorthBait == true)
                    {
                        if(Controller.TryGetLayoutByName($"DropNorth{suffix}", out var l))
                        {
                            l.Enabled = true;
                        }
                    }
                    if(isNorthBait == false)
                    {
                        if(Controller.TryGetLayoutByName($"DropSouth{suffix}", out var l))
                        {
                            l.Enabled = true;
                        }
                    }
                }
                else
                {
                    if(isNorthBait == true)
                    {
                        var suffix = C.IsPuddleCW ? "Up" : "Down";
                        if(Controller.TryGetLayoutByName($"DropWest{suffix}", out var l))
                        {
                            l.Enabled = true;
                        }
                    }
                    if(isNorthBait == false)
                    {
                        var suffix = !C.IsPuddleCW ? "Up" : "Down";
                        if(Controller.TryGetLayoutByName($"DropEast{suffix}", out var l))
                        {
                            l.Enabled = true;
                        }
                    }
                }
            }
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _onTetherCount = 0;
        _partyDataList.Clear();
        _isClockwiseRotation = null;
        HideAllElements();
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_state != State.LightRampantCasting) return;
        if(data2 == 0 && data3 == 110 && data5 == 15)
        {
            var partyData = _partyDataList.Find(x => x.EntityId == source);
            if(partyData == null) return;
            partyData.TetherPairId1 = target;
            _onTetherCount++;

            if(_onTetherCount == 6)
            {
                if(C.Priority.GetFirstValidList() == null)
                {
                    PluginLog.Error("PriorityList is null");
                    OnReset();
                }

                if(ParseTether())
                {
                    PluginLog.Information("State: tether spawned");
                    _state = State.TetherSpawned;
                }
                else
                {
                    _state = State.None;
                    PluginLog.Information("State: none");
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("If NorthWest, NorthEast Swap, Check This box");
        ImGui.Checkbox("NorthSwap", ref C.NorthSwap);

        ImGui.Text("Set NorthWest ClockWise (ex. NorthWest -> North -> NorthEast -> SouthEast -> South -> SouthWest)");
        ImGui.Text("Swaper is NorthEast and SouthEast Players");
        C.Priority.Draw();
        ImGui.Separator();
        ImGuiEx.TextWrapped(EColor.GreenBright, "If you would like to resolve puddle baits, fill the following priority as well and configure default positions");
        ImGuiEx.TextV($"Puddle bait mode:");
        ImGui.SameLine();
        ImGuiEx.RadioButtonBool("East/West (NAUR)", "North/South (LPDU/JP)", ref C.PsychopathMode, true);
        ImGuiEx.TextV("Default puddle position:");
        ImGui.SameLine();
        if(!C.PsychopathMode)
        {
            ImGuiEx.RadioButtonBool("North", "South", ref C.IsDefaultNorth, true);
        }
        else
        {
            ImGui.Checkbox("Try to guess", ref C.GuessDefault);
            if(!C.GuessDefault)
            {
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool("West", "East", ref C.IsDefaultNorth, true);
            }
        }
        ImGuiEx.HelpMarker("Where will you go with puddle if no adjustment is required");
        ImGuiEx.TextV("Puddle drop direction:");
        ImGui.SameLine();
        if(!C.PsychopathMode)
        {
            ImGuiEx.RadioButtonBool("Clockwise", "Counter-Clockwise", ref C.IsPuddleCW, true);
        }
        else
        {
            ImGuiEx.RadioButtonBool("West->North, East->South", "West->South, East->North", ref C.IsPuddleCW, true);
        }

        if(!C.PsychopathMode)
        {
            ImGuiEx.Text($"Aoe priority adjustment, north to south:");
        }
        else
        {
            ImGuiEx.Text($"Aoe priority adjustment, west to east:");
        }
        C.AoePriority.Draw();
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"OnTetherCount: {_onTetherCount}");
            ImGui.Text($"PartyDataList.Count: {_partyDataList.Count}");
            if(_isClockwiseRotation == null) ImGui.Text($"ClockwiseRotate: None");
            else ImGui.Text($"ClockwiseRotate: {_isClockwiseRotation}");
            if(_partyDataList.Count <= 0) return;
            foreach(var pc in _partyDataList)
            {
                if(pc.EntityId == 0) continue;
                ImGui.Text($"PartyData: {pc.EntityId.GetObject().Name.ToString()}");
                if(pc.TetherPairId1 == 0) continue;
                ImGui.Text($"TetherPairId1: {pc.TetherPairId1.GetObject().Name.ToString()}");
                if(pc.TetherPairId2 == 0) continue;
                ImGui.Text($"TetherPairId2: {pc.TetherPairId2.GetObject().Name.ToString()}");
                ImGui.Text($"TowerDirection: {pc.TowerDirection}");
            }
        }
    }
    #endregion

    #region private methods
    private readonly CombatRole[] SupportRoles = new CombatRole[] { CombatRole.Tank, CombatRole.Healer };
    private bool ParseTether()
    {
        foreach(var pc in _partyDataList)
        {
            if(pc.TetherPairId1 == 0) continue;

            var pair2 = _partyDataList.Find(x => x.TetherPairId1 == pc.EntityId);
            if(pair2 == null) continue;
            pc.TetherPairId2 = pair2.EntityId;
        }

        if(_partyDataList.Where(x => x.TetherPairId1 != 0 && x.TetherPairId2 != 0).Count() != 6) return false;

        var tetherList = _partyDataList.Where(x => x.TetherPairId1 != 0 && x.TetherPairId2 != 0).ToList();
        List<PartyData> tethersSortedList = [];
        var priority = C.Priority.GetPlayers(_ => true);
        if(priority == null || priority.Count != 8)
        {
            PluginLog.Information("PriorityList is null");
            return false;
        }

        // tetherListをpriority順に並べる
        foreach(var pc in priority)
        {
            var tether = tetherList.Find(x => x.EntityId == pc.IGameObject.EntityId);
            if(tether != null)
            {
                tethersSortedList.Add(tether);
            }
        }

        var Index = 0;
        foreach(var pc in tethersSortedList)
        {
            if(pc == null)
            {
                PluginLog.Information($"EntityId: {pc.EntityId} {Index}");
            }
            Index++;
        }

        if(priority == null || priority.Count == 0) return false;

        _isClockwiseRotation = ClockwiseRotation(tethersSortedList);

        // since both healers have puddles, SW DPS rotates to NW spot, move that player to 0 
        if(_isClockwiseRotation == true)
        {
            //insert last position first
            tethersSortedList.Insert(0, tethersSortedList[5]);

            //delete the last element
            tethersSortedList.RemoveAt(6);
        }

        // NW
        var x = _partyDataList.Find(x => x.EntityId == tethersSortedList[0].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.NorthWest : Direction.NorthEast;

        // N
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[1].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.North : Direction.South;

        // NE
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[2].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.NorthEast : Direction.NorthWest;

        // SE
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[3].EntityId.GetObject().EntityId).TowerDirection = Direction.SouthEast;

        // S
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[4].EntityId.GetObject().EntityId).TowerDirection = (C.NorthSwap != true) ? Direction.South : Direction.North;

        // SW
        x = _partyDataList.Find(x => x.EntityId == tethersSortedList[5].EntityId.GetObject().EntityId).TowerDirection = Direction.SouthWest;


        if(_partyDataList.Where(x => x.TowerDirection != Direction.None).Count() != 6) return false;
        else return true;
    }

    // Handle the odd case where two supports are selected and a dps becomes top left of the hexagon
    private bool? ClockwiseRotation(List<PartyData> tethersSortedList)
    {
        List<CombatRole> tetheredRoles = [];

        foreach(var pc in tethersSortedList)
        {
            if(pc.Object is IPlayerCharacter player)
            {
                var role = CharacterFunctions.GetRole(player);

                tetheredRoles.Add(role);
            }
        }

        // Count how many THJobs appear in the tetheredJobs list
        var thRoleCount = tetheredRoles.Count(role => SupportRoles.Contains(role));

        // Check if count is 2 (4 dps, 2 supports)
        if(thRoleCount == 2)
        {
            return true; // CW Rotation LR case detected
        }

        return false; // No Rotation LR case
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

    private void SetListEntityId()
    {
        _partyDataList.Clear();

        var index = 0;

        foreach(var pc in FakeParty.Get())
        {
            var job = pc.GetJob();
            _partyDataList.Add(new PartyData(0));

            _partyDataList[index].index = index;
            _partyDataList[index].EntityId = pc.EntityId;
            index++;
        }
    }

    private PartyData GetMinedata() => _partyDataList.Find(x => x.Mine);

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    #endregion
}