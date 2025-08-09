using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Schedulers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Bindings.ImGui;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;
internal unsafe class P6_MultiScript : SplatoonScript
{
    #region privateTypes
    private class DistanceCheck
    {
        public float Distance { get; set; }
        public IPlayerCharacter Player { get; set; }
    }

    private class BuffID
    {
        public const uint MagicNumber = 3532;
    }

    private class CastID
    {
        public const uint BlindFaith = 31623;
        public const uint CosmoMemory = 31649;
        public const uint LimiterCut = 31660;
        public const uint RunDunamis = 31648;
        public const uint CosmoArrow = 31650;
        public const uint CosmoDive = 31654;
        public const uint CosmoDiveT = 31655;
        public const uint CosmoDiveHD = 31656;
        public const uint WaveCannonStack = 31657; // and 31658
        public const uint WaveCannonSpread = 31659;
        public const uint LimiterCutWaveCannon = 31663;
        public const uint CosmoMeteor = 31664;
        public const uint MagicNumber = 31670;
        public const uint FlashWind = 32223;
        public const uint CosmoMeteorSpread = 32699;
    }

    private enum Gimmick
    {
        None = 0,
        CosmoDive,
        LimiterCut,
        FlashWind,
        WaveCannonSpread1,
        WaveCannonSpread2,
        WaveCannonStack,
        CosmoMeteor,
        MagicNumber
    }
    #endregion

    #region privateStructs
    public enum SpreadMarker
    {
        NotUse = 0,
        North,
        NorthEast,
        East,
        EastSouth,
        South,
        SouthWest,
        West,
        NorthWest
    }
    #endregion

    #region publicDefine
    public override Metadata Metadata => new(5, "Redmoon");
    public override HashSet<uint>? ValidTerritories => [1122];
    #endregion

    #region privateDefine
    private readonly Job[] tank = { Job.PLD, Job.WAR, Job.DRK, Job.GNB };
    private readonly Job[] healer = { Job.WHM, Job.SCH, Job.AST };

    private bool _isP6Started = false;
    private Gimmick _currentGimmick = Gimmick.None;
    private Gimmick _prevGimmick = Gimmick.None;
    private IBattleNpc? _targetableNpc = null;
    private bool _isSecondHalf = false;
    private bool _showElement = false;
    private int _cosmoMeteorCount = 0;
    private int _limiterCutCount = 0;
    private int _deBuffCount = 0;
    private SpreadMarker _prevSpreadMarker = SpreadMarker.NotUse;
    private SpreadMarker _prevCosmoSpreadMarker = SpreadMarker.NotUse;
    private GameObjectManager* _gom = GameObjectManager.Instance();
    private Config C => Controller.GetConfig<Config>();
    #endregion

    #region publicMethods
    public override void OnSetup()
    {
        // Register Elements
        // Wave Cannon 8 Directions
        Controller.RegisterElementFromCode("North", "{\"Name\":\"\",\"refX\":100.0,\"refY\":87.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NorthEast", "{\"Name\":\"\",\"refX\":109.0,\"refY\":91.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("East", "{\"Name\":\"\",\"refX\":113.0,\"refY\":100.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("EastSouth", "{\"Name\":\"\",\"refX\":109.0,\"refY\":109.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("South", "{\"Name\":\"\",\"refX\":100.0,\"refY\":112.5,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("SouthWest", "{\"Name\":\"\",\"refX\":91.0,\"refY\":109.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("West", "{\"Name\":\"\",\"refX\":87.0,\"refY\":100.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NorthWest", "{\"Name\":\"\",\"refX\":91.0,\"refY\":91.0,\"refZ\":-5.456968E-12,\"radius\":0.3,\"color\":4278255432,\"Filled\":false,\"fillIntensity\":0.5,\"thicc\":3.0,\"refActorTargetingYou\":1,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");


        Controller.RegisterElementFromCode("CosmoDiveTank1", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":8.0,\"fillIntensity\":0.5,\"refActorObjectID\":271911582,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoDiveTank2", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":8.0,\"fillIntensity\":0.5,\"refActorObjectID\":271911582,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoDiveShare", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"color\":3355508503,\"fillIntensity\":0.5,\"refActorObjectID\":271413298,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlashWind1", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.4,\"refActorObjectID\":271911582,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FlashWind2", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.4,\"refActorObjectID\":271911582,\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack1", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack2", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack3", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack4", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack5", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack6", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack7", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("WaveCannonSpreadStack8", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"offX\":109.0152,\"offY\":99.52286,\"radius\":4.0,\"fillIntensity\":0.5,\"refActorObjectID\":1073798629,\"refActorTargetingYou\":1,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange1", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange2", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange3", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange4", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange5", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange6", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange7", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CosmoMeteorRange8", "{\"Name\":\"\",\"type\":1,\"radius\":5.0,\"fillIntensity\":0.15,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CountReminder", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":3388997632,\"overlayTextColor\":4278255413,\"overlayVOffset\":2.5,\"overlayFScale\":3.0,\"thicc\":1.0,\"overlayText\":\"0\",\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"FillStep\":0.501}");
        Controller.RegisterElementFromCode("LBReminder", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":3388997632,\"overlayTextColor\":4278255413,\"overlayVOffset\":2.5,\"overlayFScale\":5.0,\"thicc\":1.0,\"overlayText\":\"0\",\"refActorComparisonType\":2,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"FillStep\":0.501}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == CastID.CosmoArrow)
        {
            if(_targetableNpc == null)
            {
                if(source.GetObject() is not IBattleNpc npc || npc == null)
                {
                    return;
                }

                _targetableNpc = npc;
            }

            if(_isSecondHalf)
            {
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                Controller.GetRegisteredElements().Each(x => x.Value.tether = false);
                _ = new TickScheduler(() =>
                {
                    ChangeGimmick(Gimmick.WaveCannonSpread1);
                }, 14000);
            }
        }
        else if(castId == CastID.CosmoDive)
        {
            ChangeGimmick(Gimmick.CosmoDive);
        }
        else if(castId == CastID.LimiterCut)
        {
            ChangeGimmick(Gimmick.LimiterCut);
        }
        else if(castId == CastID.LimiterCutWaveCannon)
        {
            if(EzThrottler.Throttle("LimiterCutWaveCannon", 500))
            {
                ++_limiterCutCount;
                if(_limiterCutCount >= 7)
                {
                    if(!_isSecondHalf)
                    {
                        ChangeGimmick(Gimmick.WaveCannonSpread1);
                    }
                    else
                    {
                        ChangeGimmick(Gimmick.CosmoDive);
                    }
                }
                else
                {
                    EzThrottler.Throttle("LimiterCutWaveCannon", 500, true);
                }

            }
        }
        else if(castId == CastID.WaveCannonStack)
        {
            if(_currentGimmick != Gimmick.WaveCannonSpread1 && !_isSecondHalf)
            {
                ChangeGimmick(Gimmick.WaveCannonSpread1);
            }
        }
        else if(castId == CastID.CosmoMeteor)
        {
            ChangeGimmick(Gimmick.CosmoMeteor);
        }
        else if(castId == CastID.MagicNumber)
        {
            _deBuffCount = 0;
            ChangeGimmick(Gimmick.MagicNumber);
        }
        else if(castId == CastID.RunDunamis)
        {
            ChangeGimmick(Gimmick.None);
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;

        if(set.Action.Value.RowId == CastID.CosmoDive || set.Action.Value.RowId == CastID.WaveCannonStack)
        {
            ChangeGimmick(Gimmick.FlashWind);
        }
        else if(set.Action.Value.RowId == CastID.WaveCannonSpread)
        {
            if(EzThrottler.Throttle("WaveCannonSpread", 500))
            {
                if(_currentGimmick == Gimmick.WaveCannonSpread1)
                {
                    ChangeGimmick(Gimmick.WaveCannonSpread2);
                }
                else
                {
                    ChangeGimmick(Gimmick.WaveCannonStack);
                }
                EzThrottler.Throttle("WaveCannonSpread", 500, true);
            }
        }
        else if(set.Action.Value.RowId == CastID.BlindFaith)
        {
            _isP6Started = true;
        }
        else if(set.Action.Value.RowId == CastID.CosmoMeteorSpread)
        {
            ++_cosmoMeteorCount;
            if(_cosmoMeteorCount >= 16)
            {
                ChangeGimmick(Gimmick.None);
            }
        }
    }

    public override void OnUpdate()
    {
        if(_currentGimmick == Gimmick.None || _targetableNpc == null || _isP6Started == false)
        {
            return;
        }

        switch(_currentGimmick)
        {
            case Gimmick.CosmoDive:
                ShowCosmoDive();
                break;
            case Gimmick.LimiterCut:
                ShowLimiterCut();
                break;
            case Gimmick.FlashWind:
                ShowFlashWind();
                break;
            case Gimmick.WaveCannonSpread1:
            case Gimmick.WaveCannonSpread2:
                ShowWaveCannonSpread();
                break;
            case Gimmick.WaveCannonStack:
                ShowWaveCannonStack();
                break;
            case Gimmick.CosmoMeteor:
                ShowCosmoMeteor();
                break;
            case Gimmick.MagicNumber:
                ShowMagicNumber();
                break;
            default:
                break;
        }
    }

    public override void OnReset()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Controller.GetRegisteredElements().Each(x => x.Value.tether = false);
        _isP6Started = false;
        _currentGimmick = Gimmick.None;
        _prevGimmick = Gimmick.None;
        _targetableNpc = null;
        _isSecondHalf = false;
        _showElement = false;
        _limiterCutCount = 0;
        _deBuffCount = 0;
        _cosmoMeteorCount = 0;
        _prevSpreadMarker = SpreadMarker.NotUse;
        _prevCosmoSpreadMarker = SpreadMarker.NotUse;
        EzThrottler.Reset("WaveCannonSpread");
        EzThrottler.Reset("LimiterCutWaveCannon");
    }

    public class Config : IEzConfig
    {
        public bool Debug = false;
        public bool isTankFirst = false;
        public bool isHealerFirst = false;
        public SpreadMarker spreadMarker = SpreadMarker.NotUse;
        public SpreadMarker cosmoSpreadMarker = SpreadMarker.NotUse;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("P6 MultiScript Settings");
        ImGui.Text("#Wave Cannon Spread Marker");
        if(ImGui.BeginCombo("##SpreadPos", ConvertSpreadMarker(C.spreadMarker)))
        {
            for(var i = 0; i < 9; i++)
            {
                var marker = (SpreadMarker)i;
                if(ImGui.Selectable(ConvertSpreadMarker(marker), C.spreadMarker == marker))
                {
                    C.spreadMarker = marker;
                }
            }
        }

        ImGui.Text("#Cosmo Dive Spread Marker");
        if(ImGui.BeginCombo("##CosmoSpreadPos", ConvertSpreadMarker(C.cosmoSpreadMarker)))
        {
            for(var i = 0; i < 9; i++)
            {
                var marker = (SpreadMarker)i;
                if(ImGui.Selectable(ConvertSpreadMarker(marker), C.cosmoSpreadMarker == marker))
                {
                    C.cosmoSpreadMarker = marker;
                }
            }
        }

        ImGui.Dummy(new Vector2(0, 20));
        ImGui.Text("# LB Reminder");
        if(tank.Contains(Player.Job))
        {
            ImGui.Text("TankLB Setting");
            if(ImGui.RadioButton("I'm Use TankLB is first", C.isTankFirst))
            {
                C.isTankFirst = true;
            }
            if(ImGui.RadioButton("I'm Use TankLB is Second", !C.isTankFirst))
            {
                C.isTankFirst = false;
            }
        }
        else if(healer.Contains(Player.Job))
        {
            ImGui.Text("HealerLB Setting");
            if(ImGui.RadioButton("I'm Use HealerLB is first", C.isHealerFirst))
            {
                C.isHealerFirst = true;
            }
            if(ImGui.RadioButton("I'm Use HealerLB is Second", !C.isHealerFirst))
            {
                C.isHealerFirst = false;
            }
        }
        else
        {
            ImGui.Text("You are DPS so you can't this setting");
        }

        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text("Is P6 Started: " + _isP6Started);
            ImGui.Text("Current Gimmick: " + _currentGimmick);
            ImGui.Text("Prev Gimmick: " + _prevGimmick);
            ImGui.Text("Is Second Half: " + _isSecondHalf);
            ImGui.Text("Limiter Cut Count: " + _limiterCutCount);
            ImGui.Text("DeBuff Count: " + _deBuffCount);
            ImGui.Text("_showElement: " + _showElement);
            ImGui.Text("Cosmo Meteor Count: " + _cosmoMeteorCount);
        }
    }
    #endregion

    #region privateMethods
    private void ActorEffectCheck()
    {
        if(_currentGimmick == Gimmick.MagicNumber)
        {
            _deBuffCount = FakeParty.Get().Count(x => x.StatusList.Any(y => y.StatusId == BuffID.MagicNumber));
        }
    }

    private void ChangeGimmick(Gimmick gimmick)
    {
        if(_isP6Started == false)
        {
            return;
        }

        if(gimmick == Gimmick.CosmoDive && _currentGimmick == Gimmick.LimiterCut)
        {
            _showElement = false;
            _ = new TickScheduler(() =>
            {
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                _limiterCutCount = 0;
                _prevGimmick = _currentGimmick;
                _currentGimmick = gimmick;
                EzThrottler.Reset("WaveCannonSpread");
                EzThrottler.Reset("LimiterCutWaveCannon");
                EzThrottler.Reset("SpreadShowDelay");
            }, 2500);
        }
        else if(_currentGimmick != Gimmick.CosmoDive)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            _showElement = false;
            _limiterCutCount = 0;
            _prevGimmick = _currentGimmick;
            _currentGimmick = gimmick;
            EzThrottler.Reset("WaveCannonSpread");
            EzThrottler.Reset("LimiterCutWaveCannon");
            EzThrottler.Reset("SpreadShowDelay");
        }
        else
        {
            _ = new TickScheduler(() =>
            {
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                _showElement = false;
                _limiterCutCount = 0;

                _prevGimmick = _currentGimmick;
                _currentGimmick = gimmick;
                EzThrottler.Reset("WaveCannonSpread");
                EzThrottler.Reset("LimiterCutWaveCannon");
                EzThrottler.Reset("SpreadShowDelay");
            }, 3000);
        }

        if(!_isSecondHalf && gimmick == Gimmick.WaveCannonStack)
        {
            _isSecondHalf = true;
        }
    }

    private void ShowCosmoDive()
    {
        // This Gimmick Always Update Element
        if(_targetableNpc == null || _targetableNpc.TargetObject == null)
        {
            return;
        }

        // Most 2 Closest Player from AlphaOmega
        var playerCharacters = FakeParty.Get();
        List<DistanceCheck> distanceCheckList = [];

        foreach(var character in playerCharacters)
        {
            if(character == null)
            {
                continue;
            }

            distanceCheckList.Add(new DistanceCheck
            {
                Distance = DistanceTo(character, _targetableNpc),
                Player = character
            });
        }

        var SortedList = distanceCheckList.OrderBy(x => x.Distance).ToList();

        Controller.GetElementByName("CosmoDiveTank1").refActorObjectID = SortedList[0].Player.EntityId;
        Controller.GetElementByName("CosmoDiveTank1").Enabled = true;

        Controller.GetElementByName("CosmoDiveTank2").refActorObjectID = SortedList[1].Player.EntityId;
        Controller.GetElementByName("CosmoDiveTank2").Enabled = true;

        Controller.GetElementByName("CosmoDiveShare").refActorObjectID = SortedList[2].Player.EntityId;
        Controller.GetElementByName("CosmoDiveShare").Enabled = true;
    }

    private void ShowLimiterCut()
    {
        // This Gimmick Always Update Element
        Controller.GetElementByName("CountReminder").refActorObjectID = Svc.ClientState.LocalPlayer.EntityId;
        Controller.GetElementByName("CountReminder").overlayText = _limiterCutCount.ToString();
        if(!_showElement)
        {
            Controller.GetElementByName("CountReminder").Enabled = true;
        }
        if(_limiterCutCount >= 6 && !_isSecondHalf)
        {
            // Show Spread Position
            if(!_isSecondHalf && (_prevSpreadMarker != C.spreadMarker || !_showElement))
            {
                if(_prevSpreadMarker != SpreadMarker.NotUse)
                {
                    Controller.GetElementByName(_prevSpreadMarker.ToString()).tether = false;
                    Controller.GetElementByName(_prevSpreadMarker.ToString()).Enabled = false;
                }

                Controller.GetElementByName(C.spreadMarker.ToString()).tether = true;
                Controller.GetElementByName(C.spreadMarker.ToString()).Enabled = true;

                _prevSpreadMarker = C.spreadMarker; // for Debug
            }
        }
        _showElement = true;
    }

    private void ShowFlashWind()
    {
        // This Gimmick Always Update Element
        var playerCharacters = FakeParty.Get();
        List<DistanceCheck> distanceCheckList = [];

        foreach(var character in playerCharacters)
        {
            if(character == null)
            {
                continue;
            }

            distanceCheckList.Add(new DistanceCheck
            {
                Distance = DistanceTo(character, _targetableNpc),
                Player = character
            });
        }

        var SortedList = distanceCheckList.OrderByDescending(x => x.Distance).ToList();

        // MT
        Controller.GetElementByName("FlashWind1").refActorObjectID = _targetableNpc.TargetObject.EntityId;
        if(!_showElement)
            Controller.GetElementByName("FlashWind1").Enabled = true;

        // OT (Farthest Player)
        Controller.GetElementByName("FlashWind2").refActorObjectID = SortedList[0].Player.EntityId;
        if(!_showElement)
            Controller.GetElementByName("FlashWind2").Enabled = true;

        _showElement = true;
    }

    private void ShowWaveCannonSpread()
    {
        // This Gimmick Always Update Element
        var playerCharacters = FakeParty.Get();
        var i = 1;
        foreach(var character in playerCharacters)
        {
            if(character == null)
            {
                continue;
            }

            Controller.GetElementByName($"WaveCannonSpreadStack{i}").SetRefPosition(_targetableNpc.Position);
            Controller.GetElementByName($"WaveCannonSpreadStack{i}").SetOffPosition(character.Position);

            if(!_showElement)
            {
                Controller.GetElementByName($"WaveCannonSpreadStack{i}").Enabled = true;
            }

            ++i;
        }

        // Show Spread Position
        if(!_isSecondHalf && (_prevSpreadMarker != C.spreadMarker || !_showElement))
        {
            if(_prevSpreadMarker != SpreadMarker.NotUse)
            {
                Controller.GetElementByName(_prevSpreadMarker.ToString()).tether = false;
                Controller.GetElementByName(_prevSpreadMarker.ToString()).Enabled = false;
            }

            Controller.GetElementByName(C.spreadMarker.ToString()).tether = true;
            Controller.GetElementByName(C.spreadMarker.ToString()).Enabled = true;

            _prevSpreadMarker = C.spreadMarker; // for Debug
        }

        _showElement = true;
    }

    private void ShowWaveCannonStack()
    {
        // This Gimmick Always Update Element
        var playerCharacters = FakeParty.Get();
        List<DistanceCheck> distanceCheckList = [];

        foreach(var character in playerCharacters)
        {
            if(character == null)
            {
                continue;
            }

            distanceCheckList.Add(new DistanceCheck
            {
                Distance = DistanceTo(character, _targetableNpc),
                Player = character
            });
        }

        var sortedList = distanceCheckList.OrderBy(x => x.Distance).ToList();

        var extendPos = GetExtendedAndClampedPosition(_targetableNpc.Position, sortedList[0].Player.Position, 30f, 20f);

        Controller.GetElementByName("WaveCannonSpreadStack1").SetRefPosition(_targetableNpc.Position);
        Controller.GetElementByName("WaveCannonSpreadStack1").SetOffPosition(extendPos);
        if(!_showElement)
        {
            Controller.GetElementByName("WaveCannonSpreadStack1").Enabled = true;
            _showElement = true;
        }
    }

    private void ShowCosmoMeteor()
    {
        var playerCharacters = FakeParty.Get();
        var i = 1;
        foreach(var character in playerCharacters)
        {
            if(character == null)
            {
                continue;
            }

            Controller.GetElementByName($"CosmoMeteorRange{i}").refActorObjectID = character.EntityId;

            if(!_showElement)
            {
                Controller.GetElementByName($"CosmoMeteorRange{i}").Enabled = true;
            }

            ++i;
        }

        // Show Spread Position
        if(C.cosmoSpreadMarker != SpreadMarker.NotUse)
        {
            if(_prevCosmoSpreadMarker != C.cosmoSpreadMarker || !_showElement)
            {
                if(_prevCosmoSpreadMarker != SpreadMarker.NotUse)
                {
                    Controller.GetElementByName(_prevCosmoSpreadMarker.ToString()).tether = false;
                    Controller.GetElementByName(_prevCosmoSpreadMarker.ToString()).Enabled = false;
                }

                Controller.GetElementByName(C.cosmoSpreadMarker.ToString()).tether = true;
                Controller.GetElementByName(C.cosmoSpreadMarker.ToString()).Enabled = true;
            }

            _prevCosmoSpreadMarker = C.cosmoSpreadMarker; // for Debug
        }

        _showElement = true;
    }

    private void ShowMagicNumber()
    {
        var myJob = Player.Job;
        if(tank.Contains(myJob))
        {
            if(_targetableNpc.IsCasting() == true)
            {
                Controller.GetElementByName("LBReminder").overlayText = "LB";
                Controller.GetElementByName("LBReminder").Enabled = true;
            }
            else
            {
                Controller.GetElementByName("LBReminder").Enabled = false;
            }
        }
        else if(healer.Contains(myJob))
        {
            if(_deBuffCount >= 8)
            {
                Controller.GetElementByName("LBReminder").overlayText = "LB";
                Controller.GetElementByName("LBReminder").Enabled = true;
            }
            else
            {
                Controller.GetElementByName("LBReminder").Enabled = false;
            }
        }
    }

    private string ConvertSpreadMarker(SpreadMarker marker)
    {
        switch(marker)
        {
            case SpreadMarker.North:
                return "North";
            case SpreadMarker.NorthEast:
                return "NorthEast";
            case SpreadMarker.East:
                return "East";
            case SpreadMarker.EastSouth:
                return "EastSouth";
            case SpreadMarker.South:
                return "South";
            case SpreadMarker.SouthWest:
                return "SouthWest";
            case SpreadMarker.West:
                return "West";
            case SpreadMarker.NorthWest:
                return "NorthWest";
            default:
                return "NotUse";
        }
    }

    private float DistanceTo(IPlayerCharacter player, IBattleNpc npc) => Vector3.Distance(player.Position, npc.Position);

    /// <summary>
    /// Calculates the vector from the center point to the current position, extends it by the specified distance,
    /// and if a limit is specified, clamps the position within the limit.
    /// </summary>
    /// <param name="center">The coordinates of the center point</param>
    /// <param name="currentPos">The current position coordinates</param>
    /// <param name="extensionLength">The distance to extend the vector</param>
    /// <param name="limit">The maximum allowable distance from the center (if null, no clamping is applied)</param>
    /// <returns>The new extended and optionally clamped position</returns>
    private static Vector3 GetExtendedAndClampedPosition(Vector3 center, Vector3 currentPos, float extensionLength, float? limit)
    {
        // Calculate the normalized direction vector from the center to the current position
        var direction = Vector3.Normalize(currentPos - center);

        // Extend the position by the specified length
        var extendedPos = currentPos + (direction * extensionLength);

        // If limit is null, return the extended position without clamping
        if(!limit.HasValue)
        {
            return extendedPos;
        }

        // Calculate the distance from the center to the extended position
        var distanceFromCenter = Vector3.Distance(center, extendedPos);

        // If the extended position exceeds the limit, clamp it within the limit
        if(distanceFromCenter > limit.Value)
        {
            return center + (direction * limit.Value);
        }

        // If within the limit, return the extended position as is
        return extendedPos;
    }
    #endregion
}
