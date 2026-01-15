using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.Data;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M12S_P2_Idyllic_Dream_Tired : SplatoonScript
{
    public override Metadata Metadata { get; } = new(2, "NightmareXIV");
    public override HashSet<uint>? ValidTerritories { get; } = [1327];
    int Phase = 0;

    public override void OnSetup()
    {
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"Defamation{i+1}", """
                {"Name":"","refX":112.58201,"refY":113.83432,"refZ":-6.1035156E-05,"radius":19.0,"Donut":1.0,"color":3372155112,"fillIntensity":0.592}
                """);
            Controller.RegisterElementFromCode($"Stack{i + 1}", """
                {"Name":"","refX":106.86787,"refY":99.954346,"radius":4.5,"Donut":0.5,"color":3358850816,"fillIntensity":0.319}
                """);
        }
        Controller.RegisterElementFromCode("PickTether", """
                {"Name":"","type":1,"offY":2.0,"radius":2.5,"Donut":0.5,"color":3356425984,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4278255376,"thicc":6.0,"overlayText":"Pick this tether","refActorComparisonType":2,"includeRotation":true}
                """);

        Controller.RegisterElementFromCode("PortalConeNS1", """
            {"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true}
            """);

        Controller.RegisterElementFromCode("PortalConeNS2", """
            {"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":3.1415927}
            """);

        Controller.RegisterElementFromCode("PortalConeEW1", """
            {"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":4.712389}
            """);

        Controller.RegisterElementFromCode("PortalConeEW2", """
            {"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}
            """);

        Controller.RegisterElementFromCode("Cone1", """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");

        Controller.RegisterElementFromCode("Cone2", """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");
        Controller.RegisterElementFromCode("Cone3", """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");

        Controller.RegisterElementFromCode("Cone4", """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");
        Controller.RegisterElementFromCode("Circle", """{"Name":"","Enabled":false,"refX":86.0,"refY":100.0,"radius":10.0,"tether":false}""");
    }

    public enum DataIds
    {
        PlayerClone = 19210,
    }

    public enum Towers
    {
        WindLight = 2015013,
        DoomLight = 2015014,
        Fire = 2015016,
        Earth = 2015015,
    }

    public enum Direction { N, NE, E, SE, S, SW, W, NW }
    public enum TetherKind
    {
        Stack = 369,
        Defamation = 368,
    }
    int PlayerPosition = -1;
    Vector3? NextAOE = null;
    bool? NextCleavesNorthSouth = null;
    bool? IsCardinalFirst = null;
    HashSet<(Vector3 Pos, float Rot)> NextCleavesList = [];
    Dictionary<uint, Vector3> ClonePositions = [];
    Dictionary<uint, bool> DefamationPlayers = [];
    Dictionary<uint, int> PlayerOrder = [];
    public int DefamationAttack = 0;

    public override void OnReset()
    {
        PlayerPosition = -1;
        Phase = 0;
        NextAOE = null;
        NextCleavesNorthSouth = null;
        IsCardinalFirst = null;
        NextCleavesList.Clear();
        ClonePositions.Clear();
        DefamationPlayers.Clear();
        PlayerOrder.Clear();
        DefamationAttack = 0;
    }

    /// <summary>
    /// Phase list:
    /// 0 - mechanic not yet started
    /// 1 - mechanic just started, initial tethers
    /// 2 - twisted vision 1 cast starts, nothing happens
    /// 3 - twisted vision 1 cast ends
    /// 4 - twisted vision 2 cast starts, first set of aoe previews starts
    /// 5 - twisted vision 2 cast ends, now must pick up your defamations and stacks
    /// 6 - twisted vision 3 cast starts, begin showing preview aoe
    /// 7 - twisted vision 3 cast ends, meteor must be resolved
    /// 8 - twisted vision 4 cast starts
    /// 9 - twisted vision 4 cast ends, must now resolve defamations and stacks
    /// 10 - twisted vision 5 cast starts, must show tower aoes
    /// 11 - twisted vision 5 cast ends, must keep showing tower aoes for a bit and then show position for debuffs
    /// 12 - twisted vision 6 cast starts, must prepare for defamations and stacks, 2x sets, must store aoes
    /// 13 - twisted vision 6 cast ends, must now show defamations and stacks, 1x set
    /// 14 - twisted vision 7 cast starts, must show safe platform and cones
    /// 15 - twisted vision 7 cast ends, must show safe platform and cones
    /// 16 - twisted vision 8 cast starts, must show portal clone. Portal clone always appears static north and does opposite set of cleaves than one on two safe platforms in the air.
    /// 17 - twisted vision 8 cast ends. Must show remaining defamations and stacks. Shortly after must show cleaves. Mechanic resolved.
    /// 
    /// How do clones work?
    /// In the beginning, they seem to appear on either cardinals or intercardinals first. 
    /// This determines which of the clone groups will fire their casts in first rewind, and which in second rewind. Cardinal rewinds first, intercardinal second.
    /// We make pairs out of players in a way that these first 4 clockwise always take stacks, and last 4 clockwise take defamations, clockwise from A. Why are we doing it like that though?
    /// The thing is, order in which they spawned will determine order in which they will respawn. Since we assigned first 4 clockwise players to take stacks, and second 4 players to take defamations, this effectively means that clones will be forced to behave a way that we want: defamations will spawn at west side and stacks and east side, making this mechanic easy to resolve. 
    /// How to programmatically resolve this?
    /// 1) Store which player associates with which clone at tether phase, as well as store clones positions
    /// 2) When players pick tethers up, associate their clone with certain mechanic. 
    /// 3) When clones respawn, draw mechanics on their positions
    /// Because script will remember which clones do what, plugin will draw correct AOEs for any strat.
    /// That is for second clone respawn.
    /// As for first clone respawn: to figure out which player does what, we need to know what tether appears at north. We already store enough information to figure that out. If it's defamation, then order will be defamation, stack, defamation, stack. And if it's stack, then it's stack, defamation, stack, defamation. It's always players 1+5, 2+6, 3+7, 4+8, they do their mechanics in pairs at the same time. 
    /// </summary>
    public override void OnUpdate()
    {
        Controller.Hide();
        if(Phase == 1)
        {
            var allClones = Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsCharacterVisible() && x.DataId == (uint)DataIds.PlayerClone);
            if(allClones.Count() == 4)
            {
                if(allClones.Any(x => Vector2.Distance(x.Position.ToVector2(), new(100,86)) < 2))
                {
                    this.IsCardinalFirst = true;
                }
                else
                {
                    this.IsCardinalFirst = false;
                }
            }


            var clones = MathHelper.EnumerateObjectsClockwise(Svc.Objects.OfType<IBattleNpc>().Where(x => x.Struct()->Vfx.Tethers.ToArray().Any(t => t.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(a => a.ObjectId)))), x => x.Position.ToVector2(), new(100, 100), new(96, 86));
            if(clones.Count == 8)
            {
                for(int i = 0; i < clones.Count; i++)
                {
                    IBattleNpc? x = clones[i];
                    if(x.DataId == (uint)DataIds.PlayerClone)
                    {
                        if(x.Struct()->Vfx.Tethers.ToArray().Any(t => t.TargetId.ObjectId == BasePlayer.ObjectId))
                        {
                            PlayerPosition = i;
                            PluginLog.Information($"Determined player position: {i + 1}");
                        }
                        if(x.Struct()->Vfx.Tethers.ToArray().TryGetFirst(x => x.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(s => s.ObjectId)), out var tetheredPlayer))
                        {
                            this.ClonePositions[tetheredPlayer.TargetId.ObjectId] = x.Position;
                        }
                    }
                }
            }
            NextCleavesList.Clear();
        }
        if(Phase == 2)
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.IsCasting(46354) && x.CurrentCastTime.InRange(1, 2)) //cone aoes
                {
                    NextCleavesList.Add((x.Position, x.Rotation));
                }
                if(x.IsCasting(46353) && x.CurrentCastTime.InRange(1, 2))
                {
                    NextAOE = x.Position;
                }
            }
        }
        if(Phase == 4)
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.IsCasting(46354) && x.CurrentCastTime > 1)
                {
                    this.NextCleavesList.Add((x.Position, x.Rotation));
                }
                if(x.IsCasting(46353))
                {
                    this.NextAOE = x.Position;
                }
            }
        }
        if(Phase == 5 || Phase == 6)
        {
            var clones = MathHelper.EnumerateObjectsClockwise(Svc.Objects.OfType<IBattleNpc>().Where(x => x.Struct()->Vfx.Tethers.ToArray().Any(t => t.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(a => a.ObjectId)))), x => x.Position.ToVector2(), new(100, 100), new(96, 86));
            if(clones.Count == 8)
            {
                var playersSupposedPickup = C.Pickups[PlayerPosition];
                var playerPickupsDefamation = ((int)playersSupposedPickup).InRange(0, 3, true);
                var playerPickupOrder = playerPickupsDefamation ? (int)playersSupposedPickup : (int)playersSupposedPickup - 4;
                var defaClone = 0;
                var stackClone = 0;
                for(int i = 0; i < clones.Count; i++)
                {
                    IBattleNpc? x = clones[i];
                    if(x.Struct()->Vfx.Tethers.ToArray().TryGetFirst(x => x.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(s => s.ObjectId)), out var tether))
                    {
                        if(x.Struct()->Vfx.Tethers.ToArray().TryGetFirst(x => x.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(s => s.ObjectId)), out var tetheredPlayer))
                        {
                            this.PlayerOrder[tetheredPlayer.TargetId.ObjectId] = i;
                        }
                        if(tether.Id == (uint)TetherKind.Defamation)
                        {
                            this.DefamationPlayers[tether.TargetId.ObjectId] = true;
                            if(playerPickupsDefamation && defaClone == playerPickupOrder && Controller.TryGetElementByName("PickTether", out var e))
                            {
                                e.Enabled = true;
                                e.refActorObjectID = x.ObjectId;
                            }
                            defaClone++;
                        }
                        if(tether.Id == (uint)TetherKind.Stack)
                        {
                            this.DefamationPlayers[tether.TargetId.ObjectId] = false;
                            if(!playerPickupsDefamation && stackClone == playerPickupOrder && Controller.TryGetElementByName("PickTether", out var e))
                            {
                                e.Enabled = true;
                                e.refActorObjectID = x.ObjectId;
                            }
                            stackClone++;
                        }
                    }
                }
            }
        }

        if(Phase == 6 || Phase == 7)
        {
            int i = 0;
            foreach(var x in this.NextCleavesList)
            {
                i++;
                if(Controller.TryGetElementByName($"Cone{i}", out var e))
                {
                    e.Enabled = true;
                    e.AdditionalRotation = x.Rot;
                    e.fillIntensity = Phase == 6 ? 0.2f : 0.5f;
                    e.SetRefPosition(x.Pos);
                }
            }
            {
                if(this.NextAOE != null && Controller.TryGetElementByName($"Circle", out var e))
                {
                    e.Enabled = true;
                    e.fillIntensity = Phase == 6 ? 0.2f : 0.5f;
                    e.SetRefPosition(this.NextAOE.Value);
                }
            }
        }

        if(Phase == 9 && DefamationAttack < 4)
        {
            var player1 = this.PlayerOrder.FindKeysByValue(0 + 1 * this.DefamationAttack).FirstOrDefault().GetObject();
            var player2 = this.PlayerOrder.FindKeysByValue(4 + 1 * this.DefamationAttack).FirstOrDefault().GetObject();
            var isDefamationPlayer1 = this.DefamationPlayers[player1.ObjectId];
            var isDefamationPlayer2 = this.DefamationPlayers[player2.ObjectId];
            {
                if(isDefamationPlayer1 && Controller.TryGetElementByName($"Defamation1", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(player1.Position);
                }
            }
            {
                if(isDefamationPlayer2 && Controller.TryGetElementByName($"Defamation2", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(player2.Position);
                }
            }
            {
                if(!isDefamationPlayer1 && Controller.TryGetElementByName($"Stack1", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(player1.Position);
                }
            }
            {
                if(!isDefamationPlayer2 && Controller.TryGetElementByName($"Stack2", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(player2.Position);
                }
            }
        }

        void processStored(int num)
        {
            var player1 = this.PlayerOrder.FindKeysByValue(num).FirstOrDefault().GetObject();
            var isDefamationPlayer1 = this.DefamationPlayers[player1.ObjectId];
            if(Controller.GetRegisteredElements().TryGetFirst(x => !x.Value.Enabled && x.Key.StartsWith(isDefamationPlayer1?"Defamation":"Stack"), out var e))
            {
                e.Value.Enabled = true;
                e.Value.SetRefPosition(this.ClonePositions[player1.ObjectId]);
            }
        }

        if(Phase == 12)
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.IsCasting(46352) && x.CurrentCastTime.InRange(1, 2)) //cone aoes n/s
                {
                    NextCleavesList.Add((x.Position, 0.DegreesToRadians()));
                    NextCleavesList.Add((x.Position, 180.DegreesToRadians()));
                    NextCleavesNorthSouth = false;
                }
                if(x.IsCasting(46351) && x.CurrentCastTime.InRange(1, 2)) //cone aoes e/w
                {
                    NextCleavesList.Add((x.Position, 90.DegreesToRadians()));
                    NextCleavesList.Add((x.Position, 270.DegreesToRadians()));
                    NextCleavesNorthSouth = true;
                }
                if(x.IsCasting(48303) && x.CurrentCastTime.InRange(1, 2))
                {
                    NextAOE = x.Position;
                }
            }
        }

        if(Phase == 13 || Phase == 14)
        {
            if(DefamationAttack < 5)
            {
                if(IsCardinalFirst == true)
                {
                    processStored(0);
                    processStored(1);
                    processStored(4);
                    processStored(5);
                }
                else
                {
                    processStored(2);
                    processStored(3);
                    processStored(6);
                    processStored(7);
                }
            }
        }

        if(Phase == 16 || Phase == 17)
        {
            if(DefamationAttack < 6)
            {
                if(IsCardinalFirst == true)
                {
                    processStored(2);
                    processStored(3);
                    processStored(6);
                    processStored(7);
                }
                else
                {
                    processStored(0);
                    processStored(1);
                    processStored(4);
                    processStored(5);
                }
            }
        }

        if(Phase == 17)
        {
            if(this.NextCleavesNorthSouth == true)
            {
                {
                    if(Controller.TryGetElementByName("PortalConeNS1", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = DefamationAttack < 6 ? 0.2f : 0.5f;
                    }
                }
                {
                    if(Controller.TryGetElementByName("PortalConeNS2", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = DefamationAttack < 6 ? 0.2f : 0.5f;
                    }
                }
            }
            if(this.NextCleavesNorthSouth == false)
            {
                {
                    if(Controller.TryGetElementByName("PortalConeEW1", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = DefamationAttack < 6 ? 0.1f : 0.5f;
                    }
                }
                {
                    if(Controller.TryGetElementByName("PortalConeEW2", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = DefamationAttack < 6 ? 0.2f : 0.5f;
                    }
                }
            }
        }

        if(Phase == 14 || Phase == 15)
        {
            int i = 0;
            foreach(var x in this.NextCleavesList)
            {
                i++;
                if(Controller.TryGetElementByName($"Cone{i}", out var e))
                {
                    e.Enabled = true;
                    e.AdditionalRotation = x.Rot;
                    e.fillIntensity = Phase == 14 ? 0.2f : 0.5f;
                    e.SetRefPosition(x.Pos);
                }
            }
            {
                if(this.NextAOE != null && Controller.TryGetElementByName($"Circle", out var e))
                {
                    e.Enabled = true;
                    e.fillIntensity = Phase == 14 ? 0.2f : 0.5f;
                    e.SetRefPosition(this.NextAOE.Value);
                }
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 46345)
        {
            Phase = 1;
        }
        if(set.Action?.RowId == 48098)
        {
            Phase++;
        }
        if(set.Action?.RowId == 46358 || set.Action?.RowId == 46357)
        {
            this.NextAOE = null;
            this.NextCleavesList.Clear();
        }
        if(set.Action?.RowId == 46362)
        {
            this.NextCleavesNorthSouth = null;
        }
        if(Phase == 9 && set.Action?.RowId.EqualsAny<uint>(46360, 46361) == true)
        {
            if(EzThrottler.Throttle(this.InternalData.FullName + "IncDefCnt"))
            {
                DefamationAttack++;
            }
        }
        if(Phase.EqualsAny(13,14,16,17) && set.Action?.RowId.EqualsAny<uint>(48099) == true)
        {
            if(EzThrottler.Throttle(this.InternalData.FullName + "IncDefCnt"))
            {
                DefamationAttack++;
            }
        }
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionDescriptor == new ActionDescriptor(ActionType.Action, 48098))
        {
            Phase++;
        }
    }

    ImGuiEx.RealtimeDragDrop<PickupOrder> PickupDrag = new("DePiOrd", x => x.ToString());
    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("My tower position, looking at boss", ref C.TowerPosition);
        ImGuiEx.RadioButtonBool("West platform", "East platform", ref C.IsGroup1);
        ImGui.Separator();
        ImGuiEx.Text($"Defamation Pickup order, starting from North clockwise:");
        PickupDrag.Begin();
        for(int i = 0; i < C.Pickups.Count; i++)
        {
            PickupOrder x = C.Pickups[i];
            PickupDrag.DrawButtonDummy(x.ToString(), C.Pickups, i);
            ImGui.SameLine();
            ImGuiEx.Text($"{x}");
        }
        PickupDrag.End();

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputInt("Phase", ref Phase);
            ImGuiEx.Checkbox("NextCleavesNorthSouth", ref this.NextCleavesNorthSouth);
            ImGuiEx.Checkbox("IsCardinalFirst", ref this.IsCardinalFirst);
            ImGui.Separator();
            ImGuiEx.Text($"Next cleaves: \n{NextCleavesList.Select(x => $"{x.Pos} {x.Rot.RadToDeg()}").Print("\n")}");
            ImGui.Separator();
            ImGuiEx.Text($"Next AOE: {NextAOE}");
            ImGui.Separator();
            ImGuiEx.Text($"NextCleavesNorthSouth: {NextCleavesNorthSouth}");
            ImGuiEx.Text($"Order: \n{this.PlayerOrder.Select(x => $"{x.Key.GetObject()}: {x.Value}").Print("\n")}");
            ImGuiEx.Text($"Defa: \n{this.DefamationPlayers.Select(x => $"{x.Key.GetObject()}: {x.Value}").Print("\n")}");
            ImGuiEx.Text($"Clone: \n{this.ClonePositions.Select(x => $"{x.Key.GetObject()}: {x.Value}").Print("\n")}");
        }
    }

    public enum TowerPosition { MeleeLeft, MeleeRight, RangedLeft, RangedRight }
    public enum PickupOrder {Defamation_1, Defamation_2, Defamation_3, Defamation_4, Stack_1, Stack_2, Stack_3, Stack_4}

    public class Config : IEzConfig
    {
        public TowerPosition TowerPosition = default;
        public bool IsGroup1 = true;
        public List<PickupOrder> Pickups = [PickupOrder.Stack_1, PickupOrder.Stack_2, PickupOrder.Stack_3, PickupOrder.Stack_4, PickupOrder.Defamation_1, PickupOrder.Defamation_2, PickupOrder.Defamation_3, PickupOrder.Defamation_4];
    }
    Config C => Controller.GetConfig<Config>();
}
