using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Splatoon.Data;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class M12S_P2_Idyllic_Dream_Tired : SplatoonScript
{
    public override Metadata Metadata { get; } = new(6, "NightmareXIV, Redmoon");
    public override HashSet<uint>? ValidTerritories { get; } = [1327,];
    private int Phase = 0;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("DefamationGroup1", """
                                                               {"Name":"Change my position!","refX":86.5,"refY":113.5,"radius":1.0,"Donut":0.5,"fillIntensity":0.548,"thicc":4,"tether":true,"overlayText":"Defamation 1"}
                                                               """);
        Controller.RegisterElementFromCode("DefamationGroup2", """
                                                               {"Name":"Change my position!","refX":113.5,"refY":113.5,"radius":1.0,"Donut":0.5,"fillIntensity":0.548,"thicc":4,"tether":true,"overlayText":"Defamation 2"}
                                                               """);
        Controller.RegisterElementFromCode("StackGroup1", """
                                                          {"Name":"Change my position!","refX":92.0,"refY":100.0,"radius":1.0,"Donut":0.5,"fillIntensity":0.548,"thicc":4,"tether":true,"overlayText":"Stack 1"}
                                                          """);
        Controller.RegisterElementFromCode("StackGroup2", """
                                                          {"Name":"Change my position!","refX":108.0,"refY":100.0,"radius":1.0,"Donut":0.5,"fillIntensity":0.548,"thicc":4,"tether":true,"overlayText":"Stack 2"}
                                                          """);
        Controller.RegisterElementFromCode("SafespotGroup1", """
                                                             {"Name":"Change my position!","refX":100.0,"refY":91.0,"radius":1.0,"Donut":0.5,"fillIntensity":0.548,"thicc":4,"tether":true,"overlayText":"Safe 1"}
                                                             """);
        Controller.RegisterElementFromCode("SafespotGroup2", """
                                                             {"Name":"Change my position!","refX":100.0,"refY":91.0,"radius":1.0,"Donut":0.5,"fillIntensity":0.548,"thicc":4,"tether":true,"overlayText":"Safe 2"}
                                                             """);
        Controller.RegisterElementFromCode("Given Far",
            """{"Name":"Change my position!","refX":108.669846,"refY":92.17644,"Donut":0.2}""");
        Controller.RegisterElementFromCode("Given Near",
            """{"Name":"Change my position!","refX":108.56247,"refY":97.35193,"Donut":0.2}""");
        Controller.RegisterElementFromCode("Taken Far",
            """{"Name":"Change my position!","refX":108.244225,"refY":107.46305,"refZ":3.8146973E-06,"Donut":0.2}""");
        Controller.RegisterElementFromCode("Taken Near",
            """{"Name":"Change my position!","refX":110.42621,"refY":97.36201,"refZ":3.8146973E-06,"Donut":0.2}""");

        Controller.RegisterElementFromCode("DefamationOnYou", """
                                                              {"Name":"","type":1,"radius":0.0,"Donut":0,"fillIntensity":0.548,"overlayBGColor":4286382206,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayText":"<<< Defamation>>\\n  <<< on YOU! >>>","refActorType":1}
                                                              """);

        for(var i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"Defamation{i + 1}", """
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

        Controller.RegisterElementFromCode("Cone1",
            """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");

        Controller.RegisterElementFromCode("Cone2",
            """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");
        Controller.RegisterElementFromCode("Cone3",
            """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");

        Controller.RegisterElementFromCode("Cone4",
            """{"Name":"","type":5,"refX":100.0,"refY":92.5,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"includeRotation":true,"AdditionalRotation":1.5707964}""");
        Controller.RegisterElementFromCode("Circle",
            """{"Name":"","Enabled":false,"refX":86.0,"refY":100.0,"radius":10.0,"tether":false}""");

        Controller.RegisterElementFromCode("TowerTether",
            "{\"Name\":\"\",\"refX\":81.732285,\"refY\":95.71492,\"radius\":3.0,\"Donut\":0.4,\"color\":3355508509,\"fillIntensity\":0.5,\"thicc\":5.0,\"tether\":true}");

        Controller.RegisterElementFromCode("P7AOERadius",
            "{\"Name\":\"\",\"type\":1,\"radius\":6.3,\"Donut\":0.2,\"fillIntensity\":0.5,\"thicc\":5.0,\"refActorType\":1,\"DistanceMax\":25.199999}");

        Controller.RegisterElementFromCode("FarCone1",
            "{\"Name\":\"\",\"type\":4,\"radius\":60.0,\"coneAngleMin\":-15,\"coneAngleMax\":15,\"color\":3372155131,\"fillIntensity\":0.5,\"includeRotation\":true,\"FaceMe\":true}");
        Controller.RegisterElementFromCode("FarCone2",
            "{\"Name\":\"\",\"type\":4,\"radius\":60.0,\"coneAngleMin\":-15,\"coneAngleMax\":15,\"color\":3372155131,\"fillIntensity\":0.5,\"includeRotation\":true,\"FaceMe\":true}");
        Controller.RegisterElementFromCode("NearCone1",
            "{\"Name\":\"\",\"type\":4,\"radius\":60.0,\"coneAngleMin\":-15,\"coneAngleMax\":15,\"color\":3372155131,\"fillIntensity\":0.5,\"includeRotation\":true,\"FaceMe\":true}");
        Controller.RegisterElementFromCode("NearCone2",
            "{\"Name\":\"\",\"type\":4,\"radius\":60.0,\"coneAngleMin\":-15,\"coneAngleMax\":15,\"color\":3372155131,\"fillIntensity\":0.5,\"includeRotation\":true,\"FaceMe\":true}");
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

    public enum Direction
    {
        N,
        NE,
        E,
        SE,
        S,
        SW,
        W,
        NW,
    }

    public enum TetherKind
    {
        Stack = 369,
        Defamation = 368,
    }

    private int PlayerPosition = -1;
    private Vector3? NextAOE = null;
    private bool? NextCleavesNorthSouth = null;
    private bool? IsCardinalFirst = null;
    private bool? IsThDecreasingResistance = null;
    private HashSet<(Vector3 Pos, float Rot)> NextCleavesList = [];
    private Dictionary<uint, Vector3> ClonePositions = [];
    private Dictionary<uint, bool> DefamationPlayers = [];
    private Dictionary<uint, int> PlayerOrder = [];
    public int DefamationAttack = 0;
    public int Phase11Sub = 0; // 0 - Taken Tower, 1 - Taken Cone, 2 - End

    // debugs
    private (string, IGameObject)[]? TowersDebug;

    public override void OnReset()
    {
        PlayerPosition = -1;
        Phase = 0;
        NextAOE = null;
        NextCleavesNorthSouth = null;
        IsCardinalFirst = null;
        IsThDecreasingResistance = null;
        NextCleavesList.Clear();
        ClonePositions.Clear();
        DefamationPlayers.Clear();
        PlayerOrder.Clear();
        DefamationAttack = 0;
        Phase11Sub = 0;

        // debugs
        TowersDebug = null;
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
            var allClones = Svc.Objects.OfType<IBattleNpc>()
                .Where(x => x.IsCharacterVisible() && x.DataId == (uint)DataIds.PlayerClone);
            if(allClones.Count() == 4)
            {
                if(allClones.Any(x => Vector2.Distance(x.Position.ToVector2(), new Vector2(100, 86)) < 2))
                    IsCardinalFirst = true;
                else
                    IsCardinalFirst = false;
            }


            var clones = MathHelper.EnumerateObjectsClockwise(
                Svc.Objects.OfType<IBattleNpc>().Where(x =>
                    x.DataId == (uint)DataIds.PlayerClone && x.Struct()->Vfx.Tethers.ToArray().Any(t =>
                        t.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(a => a.ObjectId)))),
                x => x.Position.ToVector2(), new Vector2(100, 100), new Vector2(96, 86));
            if(clones.Count == 8)
            {
                for(var i = 0; i < clones.Count; i++)
                {
                    var x = clones[i];
                    if(x.Struct()->Vfx.Tethers.ToArray().Any(t => t.TargetId.ObjectId == BasePlayer.ObjectId))
                    {
                        PlayerPosition = i;
                        PluginLog.Information($"Determined player position: {i + 1}");
                    }

                    if(x.Struct()->Vfx.Tethers.ToArray()
                        .TryGetFirst(
                            x => x.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(s => s.ObjectId)),
                            out var tetheredPlayer)) ClonePositions[tetheredPlayer.TargetId.ObjectId] = x.Position;
                }
            }

            NextCleavesList.Clear();
        }

        if(Phase == 2)
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.IsCasting(46354) && x.CurrentCastTime.InRange(1, 2)) //cone aoes
                    NextCleavesList.Add((x.Position, x.Rotation));
                if(x.IsCasting(46353) && x.CurrentCastTime.InRange(1, 2)) NextAOE = x.Position;
            }
        }

        if(Phase == 4)
        {
            foreach(var x in Svc.Objects.OfType<IBattleNpc>())
            {
                if(x.IsCasting(46354) && x.CurrentCastTime > 1) NextCleavesList.Add((x.Position, x.Rotation));
                if(x.IsCasting(46353)) NextAOE = x.Position;
            }
        }

        if(Phase == 5 || Phase == 6)
        {
            var clones = GetBossClones();
            if(clones.Count == 8)
            {
                var playersSupposedPickup = C.Pickups[PlayerPosition];
                var playerPickupsDefamation = ((int)playersSupposedPickup).InRange(0, 3, true);
                var playerPickupOrder =
                    playerPickupsDefamation ? (int)playersSupposedPickup : (int)playersSupposedPickup - 4;
                var defaClone = 0;
                var stackClone = 0;
                for(var i = 0; i < clones.Count; i++)
                {
                    var x = clones[i];
                    if(x.Struct()->Vfx.Tethers.ToArray()
                        .TryGetFirst(
                            x => x.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(s => s.ObjectId)),
                            out var tether))
                    {
                        if(x.Struct()->Vfx.Tethers.ToArray().TryGetFirst(
                                x => x.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers()
                                    .Select(s => s.ObjectId)), out var tetheredPlayer))
                            PlayerOrder[tetheredPlayer.TargetId.ObjectId] = i;
                        if(tether.Id == (uint)TetherKind.Defamation)
                        {
                            DefamationPlayers[tether.TargetId.ObjectId] = true;
                            if(playerPickupsDefamation && defaClone == playerPickupOrder &&
                                Controller.TryGetElementByName("PickTether", out var e))
                            {
                                e.Enabled = true;
                                e.refActorObjectID = x.ObjectId;
                            }

                            defaClone++;
                        }

                        if(tether.Id == (uint)TetherKind.Stack)
                        {
                            DefamationPlayers[tether.TargetId.ObjectId] = false;
                            if(!playerPickupsDefamation && stackClone == playerPickupOrder &&
                                Controller.TryGetElementByName("PickTether", out var e))
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
            var i = 0;
            foreach(var x in NextCleavesList)
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
                if(NextAOE != null && Controller.TryGetElementByName($"Circle", out var e))
                {
                    e.Enabled = true;
                    e.fillIntensity = Phase == 6 ? 0.2f : 0.5f;
                    e.SetRefPosition(NextAOE.Value);
                }
            }
        }

        if(Phase == 7)
        {
            var baseMeleePos = new Vector3(90.243f, 0f, 95.757f); // West Melee Left
            var baseRangedPos = new Vector3(81.757f, 0f, 95.757f); // West Ranged Left
            var pos = C.TowerPosition switch
            {
                TowerPosition.MeleeLeft => baseMeleePos,
                TowerPosition.MeleeRight => baseMeleePos with { Z = 200f - baseMeleePos.Z, },
                TowerPosition.RangedLeft => baseRangedPos,
                TowerPosition.RangedRight => baseRangedPos with { Z = 200f - baseRangedPos.Z, },
                _ => throw new Exception("Invalid tower position"),
            };

            pos = C.IsGroup1 ? pos : pos with { X = 200f - pos.X, Z = 200f - pos.Z, };
            if(Controller.TryGetElementByName("TowerTether", out var ep7))
            {
                ep7.Enabled = true;
                ep7.color = GetRainbowColor(1f).ToUint();
                ep7.SetRefPosition(pos);
            }

            if(Controller.TryGetElementByName("P7AOERadius", out var e)) e.Enabled = true;
        }

        if(Phase == 9 && GetAdjustedDefamationNumber() < 4)
        {
            var playerGroup2 = PlayerOrder.FindKeysByValue(0 + 1 * GetAdjustedDefamationNumber()).FirstOrDefault()
                .GetObject();
            var playerGroup1 = PlayerOrder.FindKeysByValue(4 + 1 * GetAdjustedDefamationNumber()).FirstOrDefault()
                .GetObject();
            var isDefamationPlayerGroup2 = DefamationPlayers[playerGroup2.ObjectId];
            var isDefamationPlayerGroup1 = DefamationPlayers[playerGroup1.ObjectId];
            var party = PlayerOrder.OrderBy(x => x.Value).Take(4).Any(x => x.Key == BasePlayer.ObjectId) ? 2 : 1;
            if((playerGroup2.AddressEquals(BasePlayer) && isDefamationPlayerGroup2) ||
                (playerGroup1.ObjectId == BasePlayer.ObjectId && isDefamationPlayerGroup1))
            {
                if(Controller.TryGetElementByName($"DefamationOnYou", out var e))
                    e.Enabled = true;
            }

            if(isDefamationPlayerGroup2 && !Controller.GetElementByName("DefamationOnYou")!.Enabled)
            {
                if(Controller.TryGetElementByName($"SafespotGroup{party}", out var e))
                {
                    e.Enabled = true;
                    e.color = GetRainbowColor(1f).ToUint();
                }
            }

            {
                if(isDefamationPlayerGroup2 && Controller.TryGetElementByName($"Defamation2", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(playerGroup2.Position);
                    if(playerGroup2.AddressEquals(BasePlayer) &&
                        Controller.TryGetElementByName("DefamationGroup2", out var el))
                    {
                        el.Enabled = true;
                        el.color = GetRainbowColor(1f).ToUint();
                    }
                }
            }
            {
                if(isDefamationPlayerGroup1 && Controller.TryGetElementByName($"Defamation1", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(playerGroup1.Position);
                    if(playerGroup1.AddressEquals(BasePlayer) &&
                        Controller.TryGetElementByName("DefamationGroup1", out var el))
                    {
                        el.Enabled = true;
                        el.color = GetRainbowColor(1f).ToUint();
                    }
                }
            }
            {
                if(!isDefamationPlayerGroup2 && Controller.TryGetElementByName($"Stack2", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(playerGroup2.Position);
                    if(party == 2 && Controller.TryGetElementByName("StackGroup2", out var el))
                    {
                        el.Enabled = true;
                        el.color = GetRainbowColor(1f).ToUint();
                    }
                }
            }
            {
                if(!isDefamationPlayerGroup1 && Controller.TryGetElementByName($"Stack1", out var e))
                {
                    e.Enabled = true;
                    e.SetRefPosition(playerGroup1.Position);
                    if(party == 1 && Controller.TryGetElementByName("StackGroup1", out var el))
                    {
                        el.Enabled = true;
                        el.color = GetRainbowColor(1f).ToUint();
                    }
                }
            }
        }

        if(Phase is 10 or 11)
        {
            if(Phase11Sub == 0) // tower goes
            {
                var nonLightTowers =
                    Svc.Objects.Where(x => x.Struct()->GetNameId() is (uint)Towers.Fire or (uint)Towers.Earth);
                var assignedNonLightTowers = C.IsGroup1
                    ? nonLightTowers.Where(x => x.Position.X < 100)
                    : nonLightTowers.Where(x => x.Position.X > 100);
                var lightTowers = Svc.Objects.Where(x =>
                    x.Struct()->GetNameId() is (uint)Towers.WindLight or (uint)Towers.DoomLight);
                var assignedLightTowers = C.IsGroup1
                    ? lightTowers.Where(x => x.Position.X < 100)
                    : lightTowers.Where(x => x.Position.X > 100);

                TowersDebug =
                [
                    ("Assigned Non-Light Towers", assignedNonLightTowers.FirstOrDefault() ?? null),
                    ("Assigned Non-Light Towers", assignedNonLightTowers.Skip(1).FirstOrDefault() ?? null),
                    ("Assigned Light Towers", assignedLightTowers.FirstOrDefault() ?? null),
                    ("Assigned Light Towers", assignedLightTowers.Skip(1).FirstOrDefault() ?? null),
                ];

                if(assignedNonLightTowers.Count() + assignedLightTowers.Count() != 4)
                    throw new Exception("Invalid number of assigned non-light-towers");

                var isDps = BasePlayer.GetRole() == CombatRole.DPS;
                if(!IsThDecreasingResistance.HasValue) throw new Exception("DPS is not set");
                var canTakingLightTowers = isDps == IsThDecreasingResistance;
                var isMelee = C.TowerPosition is TowerPosition.MeleeRight or TowerPosition.MeleeLeft;
                var shouldTakeTower = (isMelee, canTakingLightTowers) switch
                {
                    // Melee(The tower closest to the coordinates 100, 0, 100)
                    // Fire/Earth
                    (true, false) => assignedNonLightTowers
                        .OrderBy(x => Vector2.Distance(x.Position.ToVector2(), new Vector2(100, 100))).FirstOrDefault(),
                    // Wind/Doom
                    (true, true) => assignedLightTowers
                        .OrderBy(x => Vector2.Distance(x.Position.ToVector2(), new Vector2(100, 100))).FirstOrDefault(),
                    // Ranged(The tower farthest from the coordinates 100, 0, 100)
                    // Fire/Earth
                    (false, false) => assignedNonLightTowers
                        .OrderByDescending(x => Vector2.Distance(x.Position.ToVector2(), new Vector2(100, 100)))
                        .FirstOrDefault(),
                    // Wind/Doom
                    (false, true) => assignedLightTowers
                        .OrderByDescending(x => Vector2.Distance(x.Position.ToVector2(), new Vector2(100, 100)))
                        .FirstOrDefault(),
                };

                if(shouldTakeTower == null)
                    throw new Exception("Could not determine which tower to take");

                if(Controller.TryGetElementByName("TowerTether", out var e))
                {
                    e.Enabled = true;
                    e.color = GetRainbowColor(1f).ToUint();
                    e.SetRefPosition(shouldTakeTower.Position);
                }
            }

            if(Phase11Sub == 1) // cone goes
            {
                var pcs = Svc.Objects.OfType<IPlayerCharacter>().ToList();

                var farBuffer = pcs.Where(x => x.StatusList.Any(y => y.StatusId == 4766)).ToList();
                var nearBuffer = pcs.Where(x => x.StatusList.Any(y => y.StatusId == 4767)).ToList();

                if(farBuffer.Count + nearBuffer.Count != 4)
                    throw new Exception($" Invalid number of buffers: {farBuffer.Count} far, {nearBuffer.Count} near");

                for(var i = 0; i < farBuffer.Count; i++)
                {
                    var buffer = farBuffer[i];
                    // Find the farthest object in pcs
                    var farthest = pcs.OrderByDescending(x =>
                        Vector3.DistanceSquared(x.Position, buffer.Position)).FirstOrDefault();
                    if(farthest == null)
                        throw new Exception("Could not determine farthest player for far cone");
                    if(Controller.TryGetElementByName($"FarCone{i + 1}", out var e11))
                    {
                        e11.refActorComparisonType = 2;
                        e11.refActorObjectID = buffer.EntityId;
                        e11.faceplayer = GetPlayerOrder(farthest);
                        e11.Enabled = true;
                    }
                }

                for(var i = 0; i < nearBuffer.Count; i++)
                {
                    var buffer = nearBuffer[i];
                    // Find the nearest object in pcs
                    var nearest = pcs.OrderBy(x =>
                        Vector3.Distance(x.Position, buffer.Position)).Skip(1).FirstOrDefault();
                    if(nearest == null)
                        throw new Exception("Could not determine nearest player for near cone");
                    if(Controller.TryGetElementByName($"NearCone{i + 1}", out var e11))
                    {
                        e11.refActorComparisonType = 2;
                        e11.refActorObjectID = buffer.EntityId;
                        e11.faceplayer = GetPlayerOrder(nearest);
                        e11.Enabled = true;
                    }
                }

                var isMelee = C.TowerPosition is TowerPosition.MeleeRight or TowerPosition.MeleeLeft;
                var elementName = (C.TakenFarIsMelee, isMelee) switch
                {
                    (true, true) => "Taken Far",
                    (true, false) => "Taken Near",
                    (false, true) => "Taken Near",
                    (false, false) => "Taken Far",
                };

                // Has Far
                if(BasePlayer.StatusList.Any(y => y.StatusId == 4766)) elementName = "Given Far";
                else if(BasePlayer.StatusList.Any(y => y.StatusId == 4767)) elementName = "Given Near";

                if(Controller.TryGetElementByName(elementName, out var e11s1))
                {
                    if((BasePlayer.Position.X > 100 && e11s1.refX < 100) ||
                        (BasePlayer.Position.X < 100 && e11s1.refX > 100))
                        e11s1.refX = 200 - e11s1.refX; // mirror
                    e11s1.color = GetRainbowColor(1f).ToUint();
                    e11s1.tether = true;
                    e11s1.thicc = 5f;
                    e11s1.Enabled = true;
                }

                string GetPlayerOrder(IPlayerCharacter c)
                {
                    for(var i = 1; i <= 8; i++)
                        if((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                            return $"<{i}>";
                    throw new Exception("Could not determine player order");
                }
            }
        }

        void processStored(int num)
        {
            var orderedClones = MathHelper.EnumerateObjectsClockwise(ClonePositions, x => x.Value.ToVector2(),
                new Vector2(100, 100), new Vector2(98, 86));
            var player1 = orderedClones[num].Key.GetObject();
            var isDefamationPlayer1 = DefamationPlayers[player1.ObjectId];
            if(Controller.GetRegisteredElements()
                .TryGetFirst(
                    x => !x.Value.Enabled &&
                         x.Key.EqualsAny(isDefamationPlayer1
                             ? ["Defamation1", "Defamation2",]
                             : ["Stack1", "Stack2",]),
                    out var e))
            {
                e.Value.Enabled = true;
                e.Value.SetRefPosition(orderedClones[num].Value);
            }
        }

        if(Phase == 12)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
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

                if(x.IsCasting(48303) && x.CurrentCastTime.InRange(1, 2)) NextAOE = x.Position;
            }
        }

        if(Phase == 13 || Phase == 14)
        {
            if(GetAdjustedDefamationNumber() < 5)
            {
                if(IsCardinalFirst == true)
                {
                    processStored(0);
                    processStored(2);
                    processStored(4);
                    processStored(6);
                }
                else if(IsCardinalFirst == false)
                {
                    processStored(1);
                    processStored(3);
                    processStored(5);
                    processStored(7);
                }
            }
        }

        if(Phase == 16 || Phase == 17)
        {
            if(GetAdjustedDefamationNumber() < 6)
            {
                if(IsCardinalFirst == true)
                {
                    processStored(1);
                    processStored(3);
                    processStored(5);
                    processStored(7);
                }
                else if(IsCardinalFirst == false)
                {
                    processStored(0);
                    processStored(2);
                    processStored(4);
                    processStored(6);
                }
            }
        }

        if(Phase == 17)
        {
            if(NextCleavesNorthSouth == true)
            {
                {
                    if(Controller.TryGetElementByName("PortalConeNS1", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = GetAdjustedDefamationNumber() < 6 ? 0.2f : 0.5f;
                    }
                }
                {
                    if(Controller.TryGetElementByName("PortalConeNS2", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = GetAdjustedDefamationNumber() < 6 ? 0.2f : 0.5f;
                    }
                }
            }

            if(NextCleavesNorthSouth == false)
            {
                {
                    if(Controller.TryGetElementByName("PortalConeEW1", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = GetAdjustedDefamationNumber() < 6 ? 0.1f : 0.5f;
                    }
                }
                {
                    if(Controller.TryGetElementByName("PortalConeEW2", out var e))
                    {
                        e.Enabled = true;
                        e.fillIntensity = GetAdjustedDefamationNumber() < 6 ? 0.2f : 0.5f;
                    }
                }
            }
        }

        if(Phase == 14 || Phase == 15)
        {
            var i = 0;
            foreach(var x in NextCleavesList)
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
                if(NextAOE != null && Controller.TryGetElementByName($"Circle", out var e))
                {
                    e.Enabled = true;
                    e.fillIntensity = Phase == 14 ? 0.2f : 0.5f;
                    e.SetRefPosition(NextAOE.Value);
                }
            }
        }
    }

    private List<IBattleNpc> GetBossClones()
    {
        return MathHelper.EnumerateObjectsClockwise(
            Svc.Objects.OfType<IBattleNpc>().Where(x => x.NameId == 14380 && x.Struct()->Vfx.Tethers.ToArray()
                .Any(t => t.TargetId.ObjectId.EqualsAny(Controller.GetPartyMembers().Select(a => a.ObjectId)))),
            x => x.Position.ToVector2(), new Vector2(100, 100), new Vector2(96, 86));
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action?.RowId == 46345) Phase = 1;
        if(set.Action?.RowId == 48098) Phase++;
        if(set.Action?.RowId == 46358 || set.Action?.RowId == 46357)
        {
            NextAOE = null;
            NextCleavesList.Clear();
        }

        if(set.Action?.RowId == 46362) NextCleavesNorthSouth = null;
        if(Phase == 9 && set.Action?.RowId.EqualsAny<uint>(46360, 46361) == true) DefamationAttack++;
        if(Phase.EqualsAny(13, 14, 16, 17) && set.Action?.RowId.EqualsAny<uint>(48099) == true) DefamationAttack++;
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if(Phase == 7 && Status.StatusId == 4164 && !IsThDecreasingResistance.HasValue) // light tower debuff
        {
            if(sourceId.TryGetPlayer(out var pc))
                IsThDecreasingResistance = pc.GetRole() != CombatRole.DPS;
        }

        if(Phase is 10 or 11 && Phase11Sub == 0 && Status.StatusId is 4766 or 4767) Phase11Sub++;
    }

    private int GetAdjustedDefamationNumber()
    {
        return DefamationAttack / 2;
    }

    public override unsafe void OnStartingCast(uint sourceId, PacketActorCast* packet)
    {
        if(packet->ActionDescriptor == new ActionDescriptor(ActionType.Action, 48098)) Phase++;
    }

    private ImGuiEx.RealtimeDragDrop<PickupOrder> PickupDrag = new("DePiOrd", x => x.ToString());

    public override void OnSettingsDraw()
    {
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("My tower position, looking at boss", ref C.TowerPosition);
        ImGuiEx.RadioButtonBool("West platform", "East platform", ref C.IsGroup1);
        ImGuiEx.RadioButtonBool("Guide far North", "Guide far South", ref C.IsGuideFarNorth);
        ImGuiEx.RadioButtonBool("Taken far is Melee", "Taken far is Ranged", ref C.TakenFarIsMelee);
        ImGui.Separator();
        ImGuiEx.Text($"Defamation Pickup order, starting from North clockwise:");
        PickupDrag.Begin();
        for(var i = 0; i < C.Pickups.Count; i++)
        {
            var x = C.Pickups[i];
            PickupDrag.DrawButtonDummy(x.ToString(), C.Pickups, i);
            ImGui.SameLine();
            ImGuiEx.Text($"{x}");
        }

        PickupDrag.End();

        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGui.InputInt("Phase", ref Phase);
            ImGui.InputInt("Defamation num", ref DefamationAttack);
            ImGui.InputInt("Phase11Sub", ref Phase11Sub);
            ImGuiEx.Text($"Adjusted defamation num {GetAdjustedDefamationNumber()}");
            ImGuiEx.Checkbox("NextCleavesNorthSouth", ref NextCleavesNorthSouth);
            ImGuiEx.Checkbox("IsCardinalFirst", ref IsCardinalFirst);
            ImGuiEx.Checkbox("IsThDecreasingResistance", ref IsThDecreasingResistance);
            ImGui.Separator();
            ImGuiEx.Text($"Next cleaves: \n{NextCleavesList.Select(x => $"{x.Pos} {x.Rot.RadToDeg()}").Print("\n")}");
            ImGui.Separator();
            ImGuiEx.Text($"Next AOE: {NextAOE}");
            ImGui.Separator();
            ImGuiEx.Text($"NextCleavesNorthSouth: {NextCleavesNorthSouth}");
            ImGuiEx.Text($"Order: \n{PlayerOrder.Select(x => $"{x.Key.GetObject()}: {x.Value}").Print("\n")}");
            ImGuiEx.Text($"Defa: \n{DefamationPlayers.Select(x => $"{x.Key.GetObject()}: {x.Value}").Print("\n")}");
            ImGuiEx.Text(
                $"ClonePositions: \n{ClonePositions.Select(x => $"{x.Key.GetObject()}: {x.Value}").Print("\n")}");
            ImGui.Separator();
            ImGuiEx.Text($"GetBossClones: \n{GetBossClones().Print("\n")}");
            ImGui.Separator();
            ImGui.Text("Towers Debug:");
            if(TowersDebug != null)
            {
                foreach(var (name, obj) in TowersDebug)
                    ImGuiEx.Text($"{name}: NPCID: {obj.Struct()->GetNameId()} Pos: {obj?.Position}");
            }

            ImGui.Separator();
            ImGui.Text("Elements:");
            foreach(var e in Controller.GetRegisteredElements())
                ImGuiEx.Text(
                    $"{e.Key}: Enabled={e.Value.Enabled} Pos=({e.Value.refX}, {e.Value.refZ}, {e.Value.refY})");
        }
    }

    public enum TowerPosition
    {
        MeleeLeft,
        MeleeRight,
        RangedLeft,
        RangedRight,
    }

    public enum PickupOrder
    {
        Defamation_1,
        Defamation_2,
        Defamation_3,
        Defamation_4,
        Stack_1,
        Stack_2,
        Stack_3,
        Stack_4,
    }

    public class Config : IEzConfig

    {
        public TowerPosition TowerPosition = default;
        public bool IsGroup1 = true;
        public bool IsGuideFarNorth = true;
        public bool TakenFarIsMelee = true;

        public List<PickupOrder> Pickups =
        [
            PickupOrder.Stack_1, PickupOrder.Stack_2, PickupOrder.Stack_3, PickupOrder.Stack_4,
            PickupOrder.Defamation_1, PickupOrder.Defamation_2, PickupOrder.Defamation_3, PickupOrder.Defamation_4,
        ];
    }

    private Config C => Controller.GetConfig<Config>();

    public static Vector4 GetRainbowColor(double cycleSeconds)
    {
        if(cycleSeconds <= 0d) cycleSeconds = 1d;

        var ms = Environment.TickCount64;
        var t = ms / 1000d / cycleSeconds;
        var hue = t % 1f;
        return HsvToVector4(hue, 1f, 1f);
    }

    public static Vector4 HsvToVector4(double h, double s, double v)
    {
        double r = 0f, g = 0f, b = 0f;
        var i = (int)(h * 6f);
        var f = h * 6f - i;
        var p = v * (1f - s);
        var q = v * (1f - f * s);
        var t = v * (1f - (1f - f) * s);

        switch(i % 6)
        {
            case 0:
                r = v;
                g = t;
                b = p;
                break;
            case 1:
                r = q;
                g = v;
                b = p;
                break;
            case 2:
                r = p;
                g = v;
                b = t;
                break;
            case 3:
                r = p;
                g = q;
                b = v;
                break;
            case 4:
                r = t;
                g = p;
                b = v;
                break;
            case 5:
                r = v;
                g = p;
                b = q;
                break;
        }

        return new Vector4((float)r, (float)g, (float)b, 1f);
    }
}