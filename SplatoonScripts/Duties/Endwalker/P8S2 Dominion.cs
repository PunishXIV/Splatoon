﻿using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.DalamudServices.Legacy;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P8S2_Dominion : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => [1088];
        public override Metadata? Metadata => new(8, "NightmareXIV");
        private int Stage = 0;
        private List<uint> FirstPlayers = [];
        private List<uint> SecondPlayers = [];

        public override void OnSetup()
        {
            Controller.TryRegisterElement("MyTower", new(0) { Enabled = false, thicc = 10, tether = true, radius = 0 });
        }

        public override void OnMessage(string Message)
        {
            if(Message.Contains("(11402>31193)"))
            {
                Stage = 1;
                PluginLog.Information($"Stage 1: Cast");
            }
        }
        public override void OnUpdate()
        {
            //tower cast: 31196
            //debuff:   Earth Resistance Down II (3372)

            if(Svc.ClientState.LocalPlayer == null) return;

            if(Stage == 1)
            {
                var playersSecondTowers = Svc.Objects.Where(x => x is IPlayerCharacter pc && pc.StatusList.Any(x => x.StatusId == 3372 && x.RemainingTime > 6f));
                if(playersSecondTowers.Count() == 4)
                {
                    Stage = 2;
                    PluginLog.Information($"Stage 2: First towers");
                    FirstPlayers = Svc.Objects.Where(x => x is IPlayerCharacter pc && !pc.StatusList.Any(x => x.StatusId == 3372) && IsRoleMatching(pc)).Select(x => x.EntityId).ToList();
                    SecondPlayers = playersSecondTowers.Where(x => x is IPlayerCharacter pc && IsRoleMatching(pc)).Select(x => x.EntityId).ToList();
                    PluginLog.Information($"First towers: {FirstPlayers.Select(x => x.GetObject()?.Name).Print()}\nSecond towers: {SecondPlayers.Select(x => x.GetObject()?.Name).Print()}");
                }
            }
            else if(Stage == 2)
            {
                var towers = GetTowers();
                if(towers.Count() == 4)
                {
                    Stage = 3;
                    PluginLog.Information($"Stage 3: Second towers");
                    if(Controller.TryGetElementByName("MyTower", out var e)) e.Enabled = false;
                    Process(towers.OrderBy(GetAngle).ToArray(), FirstPlayers);
                }
                else if(!towers.Any())
                {
                    Reset();
                }
            }
            else if(Stage == 3)
            {
                var towers = GetTowers();
                if(towers.Count() == 8)
                {
                    Stage = 4;
                    PluginLog.Information($"Stage 4: Final");
                    if(Controller.TryGetElementByName("MyTower", out var e)) e.Enabled = false;
                    towers = GetEarliestTowers();
                    Process(towers.OrderBy(GetAngle).ToArray(), SecondPlayers);
                }
                if(!towers.Any())
                {
                    Reset();
                }
            }
            else if(Stage == 4)
            {
                if(!GetTowers().Any())
                {
                    Reset();
                }
            }
        }

        private void Reset()
        {
            if(Controller.TryGetElementByName("MyTower", out var e)) e.Enabled = false;
            Stage = 0;
            PluginLog.Information($"Reset");
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category == DirectorUpdateCategory.Commence || (category == DirectorUpdateCategory.Recommence && Controller.Phase == 2))
            {
                SelfTest();
            }
        }

        private void Process(IBattleChara[] towers, List<uint> players)
        {
            if(players.Contains(Svc.ClientState.LocalPlayer!.EntityId) && Controller.TryGetElementByName("MyTower", out var e))
            {
                var prio = GetPriority().Where(x => players.Select(z => z.GetObject()!.Name.ToString()).Contains(x)).ToArray();
                if(prio.Length == 2)
                {
                    if(prio[0] == Svc.ClientState.LocalPlayer.Name.ToString())
                    {
                        e.Enabled = true;
                        var pos = ((Svc.ClientState.LocalPlayer.GetRole() == CombatRole.DPS) != Controller.GetConfig<Config>().Reverse) ? 2 : 0;
                        e.refX = towers[pos].Position.X;
                        e.refY = towers[pos].Position.Z;
                        e.refZ = towers[pos].Position.Y;
                        //first prio
                    }
                    else
                    {
                        e.Enabled = true;
                        var pos = ((Svc.ClientState.LocalPlayer.GetRole() == CombatRole.DPS) != Controller.GetConfig<Config>().Reverse) ? 3 : 1;
                        e.refX = towers[pos].Position.X;
                        e.refY = towers[pos].Position.Z;
                        e.refZ = towers[pos].Position.Y;
                        //second prio
                    }
                }
            }
        }

        private bool IsRoleMatching(IPlayerCharacter pc)
        {
            if(Svc.ClientState.LocalPlayer.GetRole() == CombatRole.DPS)
            {
                return pc.GetRole() == CombatRole.DPS;
            }
            else
            {
                return pc.GetRole() != CombatRole.DPS;
            }
        }

        private List<string> GetPriority(bool verbose = false)
        {
            var x = Controller.GetConfig<Config>().Priorities.FirstOrDefault(z => z.All(n => Svc.Objects.Any(e => e is IPlayerCharacter pc && pc.Name.ToString() == n && (pc.GetRole() == CombatRole.DPS) == (Svc.ClientState.LocalPlayer?.GetRole() == CombatRole.DPS))));
            if(x != null)
            {
                var t = $"Got priority list: {x.Print()}";
                PluginLog.Information(t);
                if(verbose)
                {
                    Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground(t, (ushort)UIColor.LightBlue).Build() });
                }
                return x;
            }
            Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground("Could not find valid priority list. You must fix this error for Dominion script to work.", (ushort)UIColor.Red).Build() });
            return [];
        }

        private int GetAngle(IBattleChara x)
        {
            return (int)(MathHelper.GetRelativeAngle(new(100, 0, 100), x.Position) + 180) % 360;
        }

        private IEnumerable<IBattleChara> GetEarliestTowers()
        {
            return GetTowers().OrderBy(x => x.CurrentCastTime).Take(4);
        }

        private IEnumerable<IBattleChara> GetTowers()
        {
            return Svc.Objects.Where(x => x is IBattleChara b && b.IsCasting && b.CastActionId == 31196).Cast<IBattleChara>();
        }

        public override void OnSettingsDraw()
        {
            ImGuiEx.TextWrapped("Priority lists. Fill in priority for your role group (Tanks+Healers or DPS) left to right. You may create few lists, one that contains all players with matching roles in your party will be used.");
            var c = Controller.GetConfig<Config>().Priorities;
            var toRem = -1;
            for(var i = 0; i < c.Count; i++)
            {
                ImGui.PushID("List" + i);
                EditList(c[i]);
                ImGui.SameLine();
                ImGuiEx.Tooltip("Check if current party matches this priority list. All players must be within visible range.");
                ImGui.SameLine();
                if(ImGuiEx.IconButton(Dalamud.Interface.FontAwesomeIcon.Trash) && ImGui.GetIO().KeyCtrl)
                {
                    toRem = i;
                }
                ImGuiEx.Tooltip("Delete list. Hold CTRL+click.");
                ImGui.Separator();
                ImGui.PopID();
            }
            if(toRem > -1)
            {
                c.RemoveAt(toRem);
            }
            ImGui.Checkbox("Reverse (DPS left, H+T right)", ref Controller.GetConfig<Config>().Reverse);
            ImGui.SameLine();
            if(ImGui.Button("Add new priority list"))
            {
                c.Add(["", "", "", ""]);
            }
            ImGui.SameLine();
            if(ImGui.Button("Perform test"))
            {
                SelfTest();
            }
        }

        private void SelfTest()
        {
            Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground("= Dominion self-test =", (ushort)UIColor.LightBlue).Build() });
            var people = GetPriority(true);
            var s = people.Count == 4;
            if(people.ToHashSet().Count == 4)
            {
                foreach(var x in people)
                {
                    if(Svc.Objects.TryGetFirst(z => z is IPlayerCharacter pc && pc.Name.ToString() == x, out var o))
                    {
                        if(!IsRoleMatching((IPlayerCharacter)o))
                        {
                            Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground($"Role mismatch with {o.Name}", (ushort)UIColor.Red).Build() });
                            s = false;
                        }
                    }
                    else
                    {
                        Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground($"Could not find player {x}", (ushort)UIColor.Red).Build() });
                        s = false;
                    }
                }
            }
            else
            {
                Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground("Could not detect enough valid players", (ushort)UIColor.Red).Build() });
                s = false;
            }
            if(s)
            {
                Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground("Test Success!", (ushort)UIColor.Green).Build() });
            }
            else
            {
                Svc.Chat.PrintChat(new() { Message = new SeStringBuilder().AddUiForeground("!!! Test failed !!!", (ushort)UIColor.Red).Build() });
            }
        }

        public void EditList(List<string> s)
        {
            for(var i = 0; i < s.Count; i++)
            {
                var t = s[i];
                ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X / 6f);
                if(ImGui.InputText($"##in{i}", ref t, 100))
                {
                    s[i] = t;
                }
                ImGui.SameLine();
                if(ImGui.Button($"  T  ##{i}") && Svc.Targets.Target is IPlayerCharacter pc)
                {
                    s[i] = pc.Name.ToString();
                }
                ImGuiEx.Tooltip("Fill name from your current target");
                ImGui.SameLine();
            }
            ImGui.Dummy(Vector2.Zero);
        }

        public class Config : IEzConfig
        {
            public bool Reverse = false;
            public List<List<string>> Priorities = [];
        }
    }
}
