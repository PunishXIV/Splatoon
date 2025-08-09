using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using Dalamud.Bindings.ImGui;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten.FullToolerPartyOnlyScrtipts;
internal class P5_Paradise_Regained_Full_Toolers : SplatoonScript
{
    #region types
    /********************************************************************/
    /* types                                                            */
    /********************************************************************/
    private enum State
    {
        None = 0,
        GimmickStart,
        Tower1,
        Tower2,
        Tower3,
        Cone1,
    }

    private delegate void MineRoleAction();
    #endregion

    #region class
    /********************************************************************/
    /* class                                                            */
    /********************************************************************/
    public class Config : IEzConfig { }

    private class RemoveBuff
    {
        public Vector3 Position = Vector3.Zero;
        public uint AssignEntityId = 0;
    }

    private class PartyData
    {
        public int Index = 0;
        public bool Mine = false;
        public uint EntityId;
        public IPlayerCharacter? Object => (IPlayerCharacter)EntityId.GetObject()! ?? null;
        public DirectionCalculator.Direction AssignDirection = DirectionCalculator.Direction.None;

        public bool IsTank => TankJobs.Contains(Object?.GetJob() ?? Job.WHM);
        public bool IsHealer => HealerJobs.Contains(Object?.GetJob() ?? Job.PLD);
        public bool IsTH => IsTank || IsHealer;
        public bool IsMeleeDps => MeleeDpsJobs.Contains(Object?.GetJob() ?? Job.MCH);
        public bool IsRangedDps => RangedDpsJobs.Contains(Object?.GetJob() ?? Job.MNK);
        public bool IsDps => IsMeleeDps || IsRangedDps;

        public PartyData(uint entityId, int index)
        {
            EntityId = entityId;
            Index = index;
            Mine = EntityId == Player.Object.EntityId;
        }
    }
    #endregion

    #region const
    /********************************************************************/
    /* const                                                            */
    /********************************************************************/
    private readonly Dictionary<DirectionCalculator.Direction, Vector3> TowerPos = new()
    {
        {DirectionCalculator.Direction.South, new Vector3(100f, 0, 107f)},
        {DirectionCalculator.Direction.NorthWest, new Vector3(93.93782f, 0, 96.5f)},
        {DirectionCalculator.Direction.NorthEast, new Vector3(106.0622f, 0, 96.5f)},
    };

    private readonly Dictionary<DirectionCalculator.Direction, Vector3> TowerOppsitePos = new()
        {
        {DirectionCalculator.Direction.South, new Vector3(100f, 0, 93f)},
        {DirectionCalculator.Direction.NorthWest, new Vector3(106.0622f, 0, 103.5f)},
        {DirectionCalculator.Direction.NorthEast, new Vector3(93.93782f, 0, 103.5f)},
    };

    private readonly Dictionary<DirectionCalculator.Direction, Vector3> TowerOppsitePos3f = new()
    {
        {DirectionCalculator.Direction.South, new Vector3(100f, 0, 97f)},
        {DirectionCalculator.Direction.NorthWest, new Vector3(102.12132f, 0, 101.5f)},
        {DirectionCalculator.Direction.NorthEast, new Vector3(97.87868f, 0, 101.5f)},
    };

    private readonly Dictionary<DirectionCalculator.Direction, Vector3> TowerOppsitePos13f = new()
    {
        {DirectionCalculator.Direction.South, new Vector3(100f, 0, 87f)},
        {DirectionCalculator.Direction.NorthWest, new Vector3(109.19239f, 0, 106.5f)},
        {DirectionCalculator.Direction.NorthEast, new Vector3(90.80761f, 0, 106.5f)},
    };
    #endregion

    #region public properties
    /********************************************************************/
    /* public properties                                                */
    /********************************************************************/
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(6, "redmoon");
    #endregion

    #region private properties
    /********************************************************************/
    /* private properties                                               */
    /********************************************************************/
    private State _state = State.None;
    private List<PartyData> _partyDataList = [];
    private Config C => Controller.GetConfig<Config>();
    private List<DirectionCalculator.Direction> towers = [];
    private MineRoleAction? _mineRoleAction = null;
    private string _firstBladeIs = "";
    private bool _StateProcEnded = false;
    #endregion

    #region public methods
    /********************************************************************/
    /* public methods                                                   */
    /********************************************************************/
    public override void OnSetup()
    {
        Controller.RegisterElement("Bait", new Element(0) { tether = true, radius = 3f, thicc = 6f });
        Controller.RegisterElement("BaitStnby", new Element(0) { tether = true, radius = 3f, thicc = 6f, color = 0xC800FF00 });
        Controller.RegisterElement("BaitObject", new Element(1)
        {
            tether = true,
            refActorComparisonType = 2,
            radius = 0.5f,
            thicc = 6f
        });

        for(var i = 0; i < 3; i++)
        {
            Controller.RegisterElement($"CircleFixed{i}", new Element(0) { radius = 5.0f, thicc = 2f, fillIntensity = 0.5f });
        }

        Controller.RegisterElement($"Line", new Element(2) { radius = 0f, thicc = 6f, fillIntensity = 0.5f });
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 40319)
        {
            SetListEntityIdByJob();
            // DEBUG
            //_partyDataList.Each(x => x.Mine = false);
            //_partyDataList[1].Mine = true;

            var npc = source.GetObject() as IBattleNpc;
            if(npc == null) return;

            var ttEntityId = npc.TargetObject?.EntityId ?? 0;
            if(ttEntityId == 0) return;

            var pc = GetMinedata();
            if(pc == null) return;

            if(pc.EntityId == ttEntityId)
            {
                _mineRoleAction = ActionMT;
            }
            else if(pc.IsTank)
            {
                _mineRoleAction = ActionST;
            }
            else if(pc.Index is 2 or 3)
            {
                _mineRoleAction = ActionHealer;
            }
            else if(pc.Index is 4 or 5)
            {
                _mineRoleAction = ActionMelee;
            }
            else if(pc.Index is 6 or 7)
            {
                _mineRoleAction = ActionRange;
            }

            if(_mineRoleAction == null) return;

            SetState(State.GimmickStart);
        }

        if(_state == State.None) return;

        if(_firstBladeIs == "" && castId == 40233) _firstBladeIs = "Dark";
        if(_firstBladeIs == "" && castId == 40313) _firstBladeIs = "Light";
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null) return;
        var castId = set.Action.Value.RowId;

        if(castId is 40314 or 40315)
        {
            if(_state == State.Tower3) SetState(State.Cone1);
            else if(_state == State.Cone1) OnReset();
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(_state == State.None) return;
    }

    public override void OnMapEffect(uint position, ushort data1, ushort data2)
    {
        if(_state == State.None) return;

        if(data1 == 1 && data2 == 2)
        {
            var dir = position switch
            {
                51 => DirectionCalculator.Direction.NorthWest,
                52 => DirectionCalculator.Direction.NorthEast,
                53 => DirectionCalculator.Direction.South,
                _ => DirectionCalculator.Direction.None,
            };
            if(dir == DirectionCalculator.Direction.None) return;

            towers.Add(dir);

            if(towers.Count == 1)
            {
                SetState(State.Tower1);
            }
            else if(towers.Count == 2)
            {
                SetState(State.Tower2);
            }
            else if(towers.Count == 3)
            {
                SetState(State.Tower3);

                if(towers[0] == DirectionCalculator.Direction.South && towers[1] == DirectionCalculator.Direction.NorthWest)
                    towers.Add(DirectionCalculator.Direction.NorthEast);
                else if(towers[0] == DirectionCalculator.Direction.NorthWest && towers[1] == DirectionCalculator.Direction.South)
                    towers.Add(DirectionCalculator.Direction.NorthEast);
                else if(towers[0] == DirectionCalculator.Direction.NorthEast && towers[1] == DirectionCalculator.Direction.South)
                    towers.Add(DirectionCalculator.Direction.NorthWest);
                else if(towers[0] == DirectionCalculator.Direction.South && towers[1] == DirectionCalculator.Direction.NorthEast)
                    towers.Add(DirectionCalculator.Direction.NorthWest);
                else if(towers[0] == DirectionCalculator.Direction.NorthWest && towers[1] == DirectionCalculator.Direction.NorthEast)
                    towers.Add(DirectionCalculator.Direction.South);
                else if(towers[0] == DirectionCalculator.Direction.NorthEast && towers[1] == DirectionCalculator.Direction.NorthWest)
                    towers.Add(DirectionCalculator.Direction.South);
            }
        }
    }

    public override void OnUpdate()
    {
        if(_state == State.None) return;

        if(_mineRoleAction != null)
        {
            _mineRoleAction();
        }

        if(Controller.TryGetElementByName("Bait", out var el))
        {
            if(el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }

        if(Controller.TryGetElementByName("BaitObject", out el))
        {
            if(el.Enabled) el.color = GradientColor.Get(0xFF00FF00.ToVector4(), 0xFF0000FF.ToVector4()).ToUint();
        }
    }

    public override void OnReset()
    {
        _state = State.None;
        _partyDataList.Clear();
        _mineRoleAction = null;
        _firstBladeIs = "";
        _StateProcEnded = false;
        towers.Clear();
        HideAllElements();
    }

    public override void OnSettingsDraw()
    {
        if(ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGui.Text($"State: {_state}");
            ImGui.Text($"FirstBladeIs: {_firstBladeIs}");
            ImGui.Text($"_StateProcEnded: {_StateProcEnded}");
            if(_mineRoleAction != null) ImGui.Text($"_mineRoleAction: {_mineRoleAction.Method.Name}");
            if(_state >= State.Tower3)
            {
                ImGui.Text($"IsClockwise: {IsClockwise(towers[0], towers[1])}");
            }
            else
            {
                ImGui.Text($"IsClockwise: null");
            }
            ImGui.Text("PartyDataList");
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach(var x in _partyDataList)
            {
                Entries.Add(new ImGuiEx.EzTableEntry("Index", true, () => ImGui.Text(x.Index.ToString())));
                Entries.Add(new ImGuiEx.EzTableEntry("EntityId", true, () => ImGui.Text(x.EntityId.ToString())));
                if(x.Object != null)
                {
                    Entries.Add(new ImGuiEx.EzTableEntry("Job", true, () => ImGui.Text(x.Object.GetJob().ToString())));
                    Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text(x.Object.Name.ToString())));
                }
                else
                {
                    Entries.Add(new ImGuiEx.EzTableEntry("Job", true, () => ImGui.Text("null")));
                    Entries.Add(new ImGuiEx.EzTableEntry("Name", true, () => ImGui.Text("null")));
                }
                Entries.Add(new ImGuiEx.EzTableEntry("Mine", true, () => ImGui.Text(x.Mine.ToString())));
            }
            ImGuiEx.EzTable(Entries);

            ImGui.Text("Towers");
            List<ImGuiEx.EzTableEntry> Entries2 = [];
            foreach(var x in towers)
            {
                Entries2.Add(new ImGuiEx.EzTableEntry("Direction", true, () => ImGui.Text(x.ToString())));
            }
            ImGuiEx.EzTable(Entries2);
        }
    }
    #endregion

    #region private methods
    /********************************************************************/
    /* private methods                                                  */
    /********************************************************************/
    private void SetState(State state)
    {
        HideAllElements();
        ResetCircleElement();
        _StateProcEnded = false;
        _state = state;
    }

    private void ActionMT()
    {
        if(_StateProcEnded) return;

        if(_state is State.Tower1)
        {
            var towerDirection = towers[0];
            ApplyElement("Bait", TowerOppsitePos[towerDirection]);

            if(_firstBladeIs == "") return;

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Darkpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ外へ\n**次離れる**");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Lightpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ内へ\n**次近づく**");
                }, 400);
            }

            _StateProcEnded = true;
        }
        if(_state is State.Tower2 or State.Tower3)
        {
            var towerDirection = towers[0];
            ApplyElement("Bait", TowerOppsitePos[towerDirection]);

            _StateProcEnded = true;
        }
        if(_state == State.Cone1)
        {
            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Dark", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "離れる");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Light", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "近づく");
                }, 400);
            }

            _StateProcEnded = true;
        }
    }

    private void ActionST()
    {
        if(_StateProcEnded) return;

        if(_state is State.Tower1) _StateProcEnded = true;
        if(_state is State.Tower3)
        {
            var towerDirection = DirectionCalculator.Direction.None;
            if(_firstBladeIs == "Dark" && !IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[1];
            }
            else if(_firstBladeIs == "Dark" && IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[2];
            }
            else if(_firstBladeIs == "Light" && !IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[2];
            }
            else if(_firstBladeIs == "Light" && IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[1];
            }
            else
            {
                return;
            }

            ApplyElement("Bait", (_firstBladeIs == "Dark") ? TowerOppsitePos3f[towerDirection] : TowerOppsitePos13f[towerDirection]);

            _StateProcEnded = true;
        }
        if(_state == State.Cone1)
        {
            var towerDirection = DirectionCalculator.Direction.None;
            if(_firstBladeIs == "Dark" && !IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[1];
            }
            else if(_firstBladeIs == "Dark" && IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[2];
            }
            else if(_firstBladeIs == "Light" && !IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[2];
            }
            else if(_firstBladeIs == "Light" && IsClockwise(towers[0], towers[1]))
            {
                towerDirection = towers[1];
            }
            else
            {
                return;
            }

            var pos = TowerOppsitePos[towerDirection];
            ApplyElement("Bait", pos);

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Dark", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ内に入る");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Light", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ外まで離れる");
                }, 400);
            }

            _StateProcEnded = true;
        }
    }

    private void ActionHealer()
    {
        if(_StateProcEnded) return;

        if(_state is State.Tower1)
        {
            if(_firstBladeIs == "") return;
            var towerDirection = towers[0];
            ApplyElement("Bait", TowerPos[towerDirection], 3f);

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Darkpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ外まで離れる");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Lightpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ内に入る");
                }, 400);
            }

            _StateProcEnded = true;
        }
        if(_state is State.Tower2 or State.Tower3)
        {
            var towerDirection = towers[0];
            ApplyElement("Bait", TowerPos[towerDirection], 3f);

            _StateProcEnded = true;
        }
        if(_state == State.Cone1)
        {
            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Dark", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ内に入る");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Light", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ外まで離れる");
                }, 400);
            }

            var towerDirection = towers[0];

            ApplyElement("Bait", TowerOppsitePos[towerDirection]);

            _StateProcEnded = true;
        }
    }

    private void ActionMelee()
    {
        if(_StateProcEnded) return;

        if(_state is State.Tower1)
        {
            if(_firstBladeIs == "") return;
            var towerDirection = towers[0];
            //ApplyElement("BaitStnby", TowerPos[towerDirection], 3f, tether: false);

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Darkpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ外まで離れる");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Lightpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ内に入る");
                }, 400);
            }

            _StateProcEnded = true;
        }
        if(_state is State.Tower2)
        {
            DuoLog.Information($"Tower1: {towers[0]}, Tower2: {towers[1]}");
            DuoLog.Information($"GetTwoPointAngle: {DirectionCalculator.GetTwoPointAngle(towers[0], towers[1])}");
            if((_firstBladeIs == "Dark" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is 135 or 90) ||
               (_firstBladeIs == "Light" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is -135 or -90))
            {
                var towerDirection = towers[1];
                ApplyElement("BaitStnby", TowerPos[towerDirection], 3f, tether: false);
            }
            else
            {
                //var towerDirection = towers[0];
                //ApplyElement("Bait", TowerPos[towerDirection], 3f);
            }

            _StateProcEnded = true;
        }
        if(_state is State.Tower3)
        {
            DuoLog.Information($"Tower1: {towers[0]}, Tower2: {towers[1]}, Tower3: {towers[2]}");
            DuoLog.Information($"GetTwoPointAngle1: {DirectionCalculator.GetTwoPointAngle(towers[0], towers[1])}");
            DuoLog.Information($"GetTwoPointAngle2: {DirectionCalculator.GetTwoPointAngle(towers[1], towers[2])}");

            if((_firstBladeIs == "Dark" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is 135 or 90) ||
               (_firstBladeIs == "Light" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is -135 or -90))
            {
                var towerDirection = towers[1];
                ApplyElement("BaitStnby", TowerPos[towerDirection], 3f, tether: false);
            }
            else
            {
                var towerDirection = towers[2];
                ApplyElement("Bait", TowerPos[towerDirection], 3f);
            }

            _StateProcEnded = true;
        }
        if(_state == State.Cone1)
        {
            var towerDirection = towers[2];
            ApplyElement("Bait", TowerPos[towerDirection], 3f);

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Dark", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ内に入る");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Light", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ外まで離れる");
                }, 400);
            }

            _StateProcEnded = true;
        }
    }

    private void ActionRange()
    {
        if(_StateProcEnded) return;

        if(_state is State.Tower1)
        {
            if(_firstBladeIs == "") return;
            //var towerDirection = towers[0];
            //ApplyElement("Bait", TowerPos[towerDirection], 3f);

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Darkpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ外まで離れる");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Lightpre", 7000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, "タゲサ内に入る");
                }, 400);
            }

            _StateProcEnded = true;
        }
        if(_state is State.Tower2)
        {
            DuoLog.Information($"GetTwoPointAngle: {DirectionCalculator.GetTwoPointAngle(towers[0], towers[1])}");
            if((_firstBladeIs == "Dark" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is 135 or 90) ||
               (_firstBladeIs == "Light" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is -135 or -90))
            {
                var towerDirection = towers[1];
                ApplyElement("Bait", TowerPos[towerDirection], 3f);
            }
            else
            {
                //var towerDirection = towers[0];
                //ApplyElement("Bait", TowerPos[towerDirection], 3f);
            }

            _StateProcEnded = true;
        }
        if(_state is State.Tower3)
        {
            if((_firstBladeIs == "Dark" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is 135 or 90) ||
               (_firstBladeIs == "Light" && DirectionCalculator.GetTwoPointAngle(towers[0], towers[1]) is -135 or -90))
            {
                var towerDirection = towers[1];
                ApplyElement("Bait", TowerPos[towerDirection], 3f);
            }
            else
            {
                var towerDirection = towers[2];
                ApplyElement("BaitStnby", TowerPos[towerDirection], 3f, tether: false);
            }

            _StateProcEnded = true;
        }
        if(_state == State.Cone1)
        {
            var towerDirection = towers[1];
            ApplyElement("Bait", TowerPos[towerDirection], 3f);

            if(_firstBladeIs == "Dark")
            {
                _ = new TimedMiddleOverlayWindow("Dark", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ内に入る");
                }, 400);
            }
            else if(_firstBladeIs == "Light")
            {
                _ = new TimedMiddleOverlayWindow("Light", 4000, () =>
                {
                    ImGui.SetWindowFontScale(2f);
                    ImGuiEx.Text(ImGuiColors.DalamudRed, "タゲサ外まで離れる");
                }, 400);
            }

            _StateProcEnded = true;
        }
    }

    private void ResetCircleElement()
    {
        for(var i = 0; i < 3; i++)
        {
            Controller.RegisterElement($"CircleFixed{i}", new Element(0) { radius = 5.0f, thicc = 2f, fillIntensity = 0.5f }, true);
        }
    }

    private PartyData? GetMinedata() => _partyDataList.Find(x => x.Mine) ?? null;

    // 塔の時計反時計判断
    private bool IsClockwise(DirectionCalculator.Direction dir1, DirectionCalculator.Direction dir2)
    {
        if(dir1 == DirectionCalculator.Direction.South && dir2 == DirectionCalculator.Direction.NorthWest) return true;
        if(dir1 == DirectionCalculator.Direction.NorthWest && dir2 == DirectionCalculator.Direction.NorthEast) return true;
        if(dir1 == DirectionCalculator.Direction.NorthEast && dir2 == DirectionCalculator.Direction.South) return true;
        return false;
    }


    private void SetListEntityIdByJob()
    {
        _partyDataList.Clear();
        var tmpList = new List<PartyData>();

        foreach(var pc in FakeParty.Get())
        {
            tmpList.Add(new PartyData(pc.EntityId, Array.IndexOf(jobOrder, pc.GetJob())));
        }

        // Sort by job order
        tmpList.Sort((a, b) => a.Index.CompareTo(b.Index));
        foreach(var data in tmpList)
        {
            _partyDataList.Add(data);
        }

        // Set index
        for(var i = 0; i < _partyDataList.Count; i++)
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
        public enum Direction : int
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

        public enum DirectionRelative : int
        {
            None = -1,
            East = 4,
            SouthEast = 3,
            South = 2,
            SouthWest = 3,
            West = 4,
            NorthWest = 5,
            North = 6,
            NorthEast = 5,
        }

        public enum LR : int
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

        public static Direction GetDirectionFromAngle(Direction direction, int angle)
        {
            if(direction == Direction.None) return Direction.None; // 無効な方向の場合

            // 方向数（8方向: North ~ NorthWest）
            const int directionCount = 8;

            // 角度を45度単位に丸め、-180～180の範囲に正規化
            angle = ((Round45(angle) % 360) + 360) % 360; // 正の値に変換して360で正規化
            if(angle > 180) angle -= 360;

            // 現在の方向のインデックス
            var currentIndex = (int)direction;

            // 45度ごとのステップ計算と新しい方向の計算
            var step = angle / 45;
            var newIndex = (currentIndex + step + directionCount) % directionCount;

            return (Direction)newIndex;
        }

        public static LR GetTwoPointLeftRight(Direction direction1, Direction direction2)
        {
            // 不正な方向の場合（None）
            if(direction1 == Direction.None || direction2 == Direction.None)
                return LR.SameOrOpposite;

            // 方向数（8つ: North ~ NorthWest）
            var directionCount = 8;

            // 差分を循環的に計算
            var difference = ((int)direction2 - (int)direction1 + directionCount) % directionCount;

            // LRを直接返す
            return difference == 0 || difference == directionCount / 2
                ? LR.SameOrOpposite
                : (difference < directionCount / 2 ? LR.Right : LR.Left);
        }

        public static int GetTwoPointAngle(Direction direction1, Direction direction2)
        {
            // 不正な方向を考慮
            if(direction1 == Direction.None || direction2 == Direction.None)
                return 0;

            // enum の値を数値として扱い、環状の差分を計算
            var diff = ((int)direction2 - (int)direction1 + 8) % 8;

            // 差分から角度を計算
            return diff <= 4 ? diff * 45 : (diff - 8) * 45;
        }

        public static float GetAngle(Direction direction)
        {
            if(direction == Direction.None) return 0; // 無効な方向の場合

            // 45度単位で計算し、0度から始まる時計回りの角度を返す
            return (int)direction * 45 % 360;
        }

        private static List<DirectionalVector> GenerateDirectionalVectors(float distance, Vector3? center = null)
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
            if(!isValid)
                return DirectionCalculator.Direction.None;

            // 特別ケース: clock = 0 の場合、_12ClockDirection をそのまま返す
            if(clock == 0)
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
            var baseIndex = (int)_12ClockDirection;

            // 時計位置に基づくステップを取得
            var step = clockToDirectionMapping[clock];

            // 新しい方向を計算し、範囲を正規化
            var targetIndex = (baseIndex + step + 8) % 8;

            // 対応する方向を返す
            return (DirectionCalculator.Direction)targetIndex;
        }

        public int GetClockFromDirection(DirectionCalculator.Direction direction)
        {
            if(!isValid)
                throw new InvalidOperationException("Invalid state: _12ClockDirection is not set.");

            if(direction == DirectionCalculator.Direction.None)
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
            var baseIndex = (int)_12ClockDirection;

            // 指定された方向のインデックス
            var targetIndex = (int)direction;

            // 差分を計算し、時計方向に正規化
            var step = (targetIndex - baseIndex + 8) % 8;

            // 該当する clock を取得
            return directionToClockMapping[step];
        }

        public float GetAngle(int clock) => DirectionCalculator.GetAngle(GetDirectionFromClock(clock));
    }

    private void HideAllElements() => Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

    private Vector3 BasePosition => new(100, 0, 100);

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
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と角度指定
    public void ApplyElement(string elementName, float angle, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
        {
            var position = CalculatePositionFromAngle(angle, radius);
            InternalApplyElement(element, position, elementRadius, filled, tether);
        }
    }

    // Element名と方向指定
    public void ApplyElement(string elementName, DirectionCalculator.Direction direction, float radius = 0f, float elementRadius = 0.3f, bool filled = true, bool tether = true)
    {
        if(Controller.TryGetElementByName(elementName, out var element))
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

    private static float ConvertRotationRadiansToDegrees(float radians)
    {
        // Convert radians to degrees with coordinate system adjustment
        var degrees = ((-radians * (180 / MathF.PI)) + 180) % 360;

        // Ensure the result is within the 0° to 360° range
        return degrees < 0 ? degrees + 360 : degrees;
    }

    private static float ConvertDegreesToRotationRadians(float degrees)
    {
        // Convert degrees to radians with coordinate system adjustment
        var radians = -(degrees - 180) * (MathF.PI / 180);

        // Normalize the result to the range -π to π
        radians = ((radians + MathF.PI) % (2 * MathF.PI)) - MathF.PI;

        return radians;
    }

    public static Vector3 GetExtendedAndClampedPosition(
        Vector3 center, Vector3 currentPos, float extensionLength, float? limit)
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

    public static void ExceptionReturn(string message)
    {
        PluginLog.Error(message);
    }
    #endregion
}
