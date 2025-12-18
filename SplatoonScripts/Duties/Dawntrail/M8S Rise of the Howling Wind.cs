using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons.Logging;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using ECommons.GameHelpers.LegacyPlayer;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
internal class M8S_Rise_of_the_Howling_Wind : SplatoonScript
{
    /*
     * Constants and Types
     */
    #region Constants and Types
    private enum State
    {
        Inactive = 0,
        Active,
        Confirmed,
        Phase1,
        Phase2Ready,
        Phase2,
        Phase3Ready,
        Phase3,
        Phase4Ready,
        Phase4,
        Phase5,
    }

    private class TowerPos
    {
        public int LandNumber { get; } = 0;
        public Vector2 Position { get; }
        public TowerPos(int landNumber, Vector2 position)
        {
            LandNumber = landNumber;
            Position = position;
        }
    }

    private class AssignedPlayer
    {
        public uint Id { get; } = 0;
        public int LandNumber = 0;
        public Job Job { get; } = 0;
        public AssignedPlayer(uint id, int landNumber, Job job = 0)
        {
            Id = id;
            LandNumber = landNumber;
            Job = job;
        }
    }

    private enum WindState
    {
        Nop = 0,
        Close,
        GetTether,
        PassTether,
    }

    private const uint kProwlingGale = 42094;               // 風狼陣
    private const uint kProwlingGaleTower = 42095;          // 風狼陣(塔)
    private const uint kRiseOfTheHowlingWind = 43050;       // 魔狼戦型・天嵐の相
    private const uint kTwofoldTempest = 42098;             // 双牙暴風撃
    private const uint kBareFangs = 42101;                  // 光牙招来

    // State Map [LandNumber][Phase]
    private readonly WindState[,] kWindState1stStart =
    {
        //        Phase1               Phase2                Phase3                Phase4
        /* 1 */ { WindState.GetTether, WindState.PassTether, WindState.Close,      WindState.Nop },
        /* 2 */ { WindState.Nop,       WindState.GetTether,  WindState.PassTether, WindState.Close },
        /* 3 */ { WindState.Close,     WindState.Nop,        WindState.GetTether,  WindState.PassTether },
        /* 4 */ { WindState.Nop,       WindState.Close,      WindState.Nop,        WindState.GetTether }
    };

    private readonly WindState[,] kWindState4thStart =
    {
        //        Phase1               Phase2                Phase3                Phase4
        /* 1 */ { WindState.Nop,       WindState.Close,      WindState.Nop,        WindState.GetTether },
        /* 2 */ { WindState.Close,     WindState.Nop,        WindState.GetTether,  WindState.PassTether },
        /* 3 */ { WindState.Nop,       WindState.GetTether,  WindState.PassTether, WindState.Close },
        /* 4 */ { WindState.GetTether, WindState.PassTether, WindState.Close,      WindState.Nop }
    };

    private readonly IReadOnlyList<TowerPos> kTowerPos = new List<TowerPos>
    {
        new(1, RoundVector2(new Vector2(83.35651f, 105.4078f))), // 1st
        new(2, RoundVector2(new Vector2(89.71376f, 85.8422f))), // 2nd
        new(3, RoundVector2(new Vector2(110.2862f, 85.8422f))), // 3rd
        new(4, RoundVector2(new Vector2(116.6435f, 105.4078f))), // 4th
    };
    #endregion

    /*
     * Public Fields
     */
    #region Public Fields
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(6, "Redmoon, NightmareXIV");
    #endregion

    /*
     * Private Fields
     */
    #region Private Fields
    private State _state = State.Inactive;
    private List<AssignedPlayer> _assignedPlayers = [];
    private WindState[,]? _currentWindState = null;
    private AssignedPlayer? _hasTetherPlayer = null;
    private bool _start1stLand = false;
    private bool _showIsDone = false;
    private bool _isDelay = false;
    #endregion

    /*
     * Public Methods
     */
    #region Public Methods
    public override void OnSetup()
    {
        // GetTether
        Controller.RegisterElementFromCode("GetTether1Land", "{\"Name\":\"\",\"refX\":87.33649,\"refY\":99.75424,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether2LandR", "{\"Name\":\"\",\"refX\":89.71051,\"refY\":92.66795,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether2LandL", "{\"Name\":\"\",\"refX\":96.46778,\"refY\":87.43436,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether3LandR", "{\"Name\":\"\",\"refX\":103.565384,\"refY\":87.639145,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether3LandL", "{\"Name\":\"\",\"refX\":110.47798,\"refY\":92.965866,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether4Land", "{\"Name\":\"\",\"refX\":112.93407,\"refY\":99.71784,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        // GetTether After
        Controller.RegisterElementFromCode("GetTether1LandAfter", "{\"Name\":\"\",\"refX\":76.60892,\"refY\":107.700485,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether2LandAfter", "{\"Name\":\"\",\"refX\":85.42989,\"refY\":79.55555,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether3LandAfter", "{\"Name\":\"\",\"refX\":115.055916,\"refY\":79.894295,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("GetTether4LandAfter", "{\"Name\":\"\",\"refX\":123.544495,\"refY\":108.381584,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        // PassTether
        Controller.RegisterElementFromCode("PassTether1Land", "{\"Name\":\"\",\"refX\":87.29962,\"refY\":91.355835,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("PassTether2LandR", "{\"Name\":\"\",\"refX\":84.6142,\"refY\":99.45096,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("PassTether2LandL", "{\"Name\":\"\",\"refX\":104.41058,\"refY\":85.26373,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("PassTether3LandR", "{\"Name\":\"\",\"refX\":95.65186,\"refY\":85.23573,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("PassTether3LandL", "{\"Name\":\"\",\"refX\":115.34537,\"refY\":99.41585,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("PassTether4Land", "{\"Name\":\"\",\"refX\":112.63836,\"refY\":91.22686,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        // Close
        Controller.RegisterElementFromCode("Close1Land", "{\"Name\":\"\",\"refX\":90.027,\"refY\":102.663574,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Close2Land", "{\"Name\":\"\",\"refX\":94.49744,\"refY\":91.60655,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Close3Land", "{\"Name\":\"\",\"refX\":106.541794,\"refY\":91.944725,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Close4Land", "{\"Name\":\"\",\"refX\":109.86473,\"refY\":103.37861,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        // Nop
        Controller.RegisterElementFromCode("Nop1Land", "{\"Name\":\"\",\"refX\":83.41339,\"refY\":105.50039,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Nop2Land", "{\"Name\":\"\",\"refX\":89.54323,\"refY\":85.11416,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Nop3Land", "{\"Name\":\"\",\"refX\":111.726204,\"refY\":85.567665,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Nop4Land", "{\"Name\":\"\",\"refX\":117.59568,\"refY\":105.360825,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        // Phase5
        Controller.RegisterElementFromCode("Phase5", "{\"Name\":\"\",\"refX\":100.134705,\"refY\":118.0584,\"refZ\":-150.0,\"radius\":1.0,\"color\":3355508480,\"fillIntensity\":0.345,\"thicc\":10.0,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == kProwlingGale)
        {
            foreach(var pc in FakeParty.Get())
            {
                _assignedPlayers.Add(new AssignedPlayer(pc.EntityId, 0, pc.GetJob()));
            }

            if(_assignedPlayers.Count != 8) return;
            _state = State.Active;
        }
        if(castId == kBareFangs)
        {
            OnReset();
        }

        if(_state == State.Inactive) return;
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if((_state == State.Inactive) || !set.Action.HasValue) return;
        if(set.Source == null) return;

        if(set.Action.Value.RowId == kProwlingGaleTower)
        {
            // Get Tower Number
            var towerNumber = 0;
            var towerPos = RoundVector2(ConvertVector(set.Source.Position));
            for(var i = 0; i < kTowerPos.Count; i++)
            {
                if(RoundVector2(ConvertVector(set.Source.Position)) == kTowerPos[i].Position)
                {
                    towerNumber = kTowerPos[i].LandNumber;
                    break;
                }
            }
            if(towerNumber == 0) return;


            // 最も近いプレイヤーを取得
            var minDistance = float.MaxValue;
            AssignedPlayer? closestPlayer = null;
            foreach(var player in _assignedPlayers)
            {
                if(player.LandNumber != 0) continue;
                if(!(player.Id.TryGetObject(out var obj) && obj is IPlayerCharacter pc)) continue;
                var playerPos = RoundVector2(ConvertVector(pc.Position));
                var distance = Vector2.DistanceSquared(towerPos, playerPos);
                if(distance < minDistance)
                {
                    minDistance = distance;
                    closestPlayer = player;
                }
            }
            if(closestPlayer == null) return;
            closestPlayer.LandNumber = towerNumber;

            // その次に近いプレイヤーを取得
            minDistance = float.MaxValue;
            AssignedPlayer? secondClosestPlayer = null;
            foreach(var player in _assignedPlayers)
            {
                if(player.LandNumber != 0 || player.Id == closestPlayer.Id) continue;
                if(!(player.Id.TryGetObject(out var obj) && obj is IPlayerCharacter pc)) continue;
                var playerPos = RoundVector2(ConvertVector(pc.Position));
                var distance = Vector2.DistanceSquared(towerPos, playerPos);
                if(distance < minDistance)
                {
                    minDistance = distance;
                    secondClosestPlayer = player;
                }
            }
            if(secondClosestPlayer == null) return;
            secondClosestPlayer.LandNumber = towerNumber;

            if(_assignedPlayers.Where(x => x.LandNumber != 0).Count() == 8)
            {
                _state = State.Confirmed;
            }
        }

        if(set.Action.Value.RowId == kTwofoldTempest)
        {
            switch(_state)
            {
                case State.Phase1:
                    _state = State.Phase2Ready;
                    break;
                case State.Phase2:
                    _state = State.Phase3Ready;
                    break;
                case State.Phase3:
                    _state = State.Phase4Ready;
                    break;
                case State.Phase4:
                    _state = State.Phase5;
                    break;
                default:
                    break;
            }

            _showIsDone = false;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_state == State.Inactive) return;
        if(_isDelay) return;
        if(data2 == 0 && data3 == 84 && data5 == 15)
        {
            // 別島のプレイヤーにTetherが付与された場合のみ有効

            if(_hasTetherPlayer != null)
            {
                if(_hasTetherPlayer.Id == target) return;
                var getTetherPlayer = _assignedPlayers.Where(x => x.Id == target).First();
                if(_hasTetherPlayer.LandNumber == getTetherPlayer.LandNumber) return;
            }

            _hasTetherPlayer = _assignedPlayers.Where(x => x.Id == target).First();

            switch(_state)
            {
                case State.Confirmed:
                    // 1島か４島どっちからか判定
                    var i = _assignedPlayers.Where(x => x.Id == target).First().LandNumber;
                    if(i == 0) return;
                    if(i == 1)
                    {
                        _start1stLand = true;
                        _currentWindState = kWindState1stStart;
                    }
                    else if(i == 4)
                    {
                        _start1stLand = false;
                        _currentWindState = kWindState4thStart;
                    }
                    else return;

                    _state = State.Phase1;
                    break;
                case State.Phase2Ready:
                    _state = State.Phase2;
                    break;
                case State.Phase3Ready:
                    _state = State.Phase3;
                    break;
                case State.Phase4Ready:
                    _state = State.Phase4;
                    break;
                default:
                    break;
            }

            _isDelay = true;
            _ = new TickScheduler(delegate
            {
                _isDelay = false;
            }, 200);

            _showIsDone = false;
        }
    }

    public override void OnUpdate()
    {
        if(_state == State.Inactive) return;
        if(_showIsDone) return;

        var pc = Mine();
        if(pc == null) return;
        var landNumber = _assignedPlayers.Where(x => x.Id == pc.EntityId).First().LandNumber;
        if(landNumber == 0) return;

        var isMyTether = _assignedPlayers.Any(x => x.LandNumber == landNumber && x.Id == _hasTetherPlayer?.Id);

        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        switch(_state)
        {
            case State.Confirmed:
                var mine = _assignedPlayers.Where(x => x.Id == pc.EntityId).First();
                if(mine.LandNumber == 1)
                {
                    if(Controller.TryGetElementByName("Close1Land", out var element))
                    {
                        element.Enabled = true;
                    }
                }
                else if(mine.LandNumber == 2)
                {
                    if(Controller.TryGetElementByName(!isMyTether && C.Is23Origin?"Nop2Land":"Nop1Land", out var element))
                    {
                        element.Enabled = true;
                    }
                }
                else if(mine.LandNumber == 3)
                {
                    if(Controller.TryGetElementByName(!isMyTether && C.Is23Origin ? "Nop3Land" : "Nop4Land", out var element))
                    {
                        element.Enabled = true;
                    }
                }
                else if(mine.LandNumber == 4)
                {
                    if(Controller.TryGetElementByName("Close4Land", out var element))
                    {
                        element.Enabled = true;
                    }
                }
                else return;
                break;
            case State.Phase1:
                PhaseProc(landNumber, 1);
                break;
            case State.Phase2Ready:
                PhaseReadyProc(landNumber, 2);
                break;
            case State.Phase2:
                PhaseProc(landNumber, 2);
                break;
            case State.Phase3Ready:
                PhaseReadyProc(landNumber, 3);
                break;
            case State.Phase3:
                PhaseProc(landNumber, 3);
                break;
            case State.Phase4Ready:
                PhaseReadyProc(landNumber, 4);
                break;
            case State.Phase4:
                PhaseProc(landNumber, 4);
                break;
            case State.Phase5:
                if(Controller.TryGetElementByName("Phase5", out var element2))
                {
                    element2.Enabled = true;
                }
                _showIsDone = true;
                break;
            case State.Inactive:
            case State.Active:
                // Do nothing
                break;
            default:
                // Invalid state
                OnReset();
                break;
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Don't switch off platforms 2 and 3 before tether appears", ref C.Is23Origin);
        ImGui.Indent();
        ImGuiEx.Text("(enable for EU and NA strats)");
        ImGui.Unindent();
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"State: {_state}");
            ImGuiEx.Text($"Assigned Players: {_assignedPlayers.Count}");
            foreach(var player in _assignedPlayers)
            {
                if(player.Id.TryGetObject(out var obj) && obj is IPlayerCharacter pc)
                {
                    ImGuiEx.Text($"LandNum: {player.LandNumber} Player: {player.Id} JOB: {pc.GetJob()} Position: {RoundVector2(ConvertVector(pc.Position))}");
                }
            }
            ImGuiEx.Text($"Start 1st Land: {_start1stLand}");
            ImGuiEx.Text($"Show Is Done: {_showIsDone}");
            ImGuiEx.Text($"Is Delay: {_isDelay}");
            if(_state == State.Phase1 || _state == State.Phase2 || _state == State.Phase3 || _state == State.Phase4)
            {
                ImGui.Text("Now Proc");
                var pc2 = Mine();
                if(pc2 == null) return;
                var landNumber = _assignedPlayers.Where(x => x.Id == pc2.EntityId).First().LandNumber;
                if(landNumber == 0) return;
                var phase = _state == State.Phase1 ? 1 : _state == State.Phase2Ready ? 2 : _state == State.Phase2 ? 2 : _state == State.Phase3Ready ? 3 : _state == State.Phase3 ? 3 : _state == State.Phase4Ready ? 4 : _state == State.Phase4 ? 4 : _state == State.Phase5 ? 5 : -1;
                if(phase == -1) return;
                if(_currentWindState == null) return;
                var windState = _currentWindState[landNumber - 1, phase - 1];
                ImGuiEx.Text($"LandNum: {landNumber} Phase: {phase} WindState: {windState}");
            }
        }
    }

    public override void OnReset()
    {
        _state = State.Inactive;
        _assignedPlayers.Clear();
        _currentWindState = null;
        _showIsDone = false;
        _isDelay = false;
        _start1stLand = false;
        _hasTetherPlayer = null;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }
    #endregion

    /*
     * Private Methods
     */
    #region Private Methods
    private static Vector2 ConvertVector(Vector3 vec) => new(vec.X, vec.Z);
    private static Vector2 RoundVector2(Vector2 vec) => new((float)(int)vec.X, (float)(int)vec.Y);

    private void PhaseProc(int landNumber, int phase)
    {
        if(_currentWindState == null) return;
        // Phaseでの処理を確認
        var windState = _currentWindState[landNumber - 1, phase - 1];
        switch(windState)
        {
            case WindState.GetTether:
                // GetTether
                if(Controller.TryGetElementByName($"GetTether{landNumber}LandAfter", out var element))
                {
                    element.Enabled = true;
                }
                break;
            case WindState.PassTether:
                // PassTether
                var RLprefix = string.Empty;
                if(landNumber == 2 || landNumber == 3)
                {
                    if(_start1stLand)
                    {
                        RLprefix = "L";
                    }
                    else
                    {
                        RLprefix = "R";
                    }
                }

                if(Controller.TryGetElementByName($"PassTether{landNumber}Land{RLprefix}", out var element2))
                {
                    element2.Enabled = true;
                }
                break;
            case WindState.Close:
                // Close
                if(Controller.TryGetElementByName($"Close{landNumber}Land", out var element3))
                {
                    element3.Enabled = true;
                }
                break;
            case WindState.Nop:
                // Nop
                if(Controller.TryGetElementByName($"Nop{landNumber}Land", out var element4))
                {
                    element4.Enabled = true;
                }
                break;
            default:
                // Invalid state
                OnReset();
                break;
        }
    }

    private void PhaseReadyProc(int landNumber, int phase)
    {
        if(_currentWindState == null) return;
        // Phaseでの処理を確認
        var windState = _currentWindState[landNumber - 1, phase - 1];
        switch(windState)
        {
            case WindState.GetTether:
                // GetTether
                var RLprefix = string.Empty;
                if(landNumber == 2 || landNumber == 3)
                {
                    if(_start1stLand)
                    {
                        RLprefix = "R";
                    }
                    else
                    {
                        RLprefix = "L";
                    }
                }
                if(Controller.TryGetElementByName($"GetTether{landNumber}Land{RLprefix}", out var element))
                {
                    element.Enabled = true;
                }
                break;
            case WindState.PassTether:
                // PassTether
                var RLprefix2 = string.Empty;
                if(landNumber == 2 || landNumber == 3)
                {
                    if(_start1stLand)
                    {
                        RLprefix2 = "L";
                    }
                    else
                    {
                        RLprefix2 = "R";
                    }
                }
                if(Controller.TryGetElementByName($"PassTether{landNumber}Land{RLprefix2}", out var element2))
                {
                    element2.Enabled = true;
                }
                break;
            case WindState.Close:
                // Close
                if(Controller.TryGetElementByName($"Close{landNumber}Land", out var element3))
                {
                    element3.Enabled = true;
                }
                break;
            case WindState.Nop:
                // Nop
                if(Controller.TryGetElementByName($"Nop{landNumber}Land", out var element4))
                {
                    element4.Enabled = true;
                }
                break;
            default:
                // Invalid state
                OnReset();
                break;
        }
    }

    private IPlayerCharacter? Mine()
    {
        var id = Player.Object.EntityId;
        //uint id = FakeParty.Get().Where(x => x.GetJob() == Job.PLD).First().EntityId;
        if(_assignedPlayers.Count == 0) return null;
        var player = _assignedPlayers.Where(x => x.Id == id).FirstOrDefault();
        if(player == null) return null;
        if(player.Id.TryGetObject(out var obj) && obj is IPlayerCharacter pc)
        {
            return pc;
        }
        return null;
    }
    #endregion

    public Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool Is23Origin = false;
    }
}
