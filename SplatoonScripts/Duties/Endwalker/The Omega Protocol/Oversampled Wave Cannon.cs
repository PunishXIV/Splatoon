using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Oversampled_Wave_Cannon : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };
        public override Metadata? Metadata => new(3, "NightmareXIV");
        Config Conf => Controller.GetConfig<Config>();

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("West1", "{\"Name\":\"West1\",\"Enabled\":false,\"refX\":101.767784,\"refY\":81.49996,\"refZ\":-5.456968E-12,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"1\",\"tether\":true}");
            Controller.RegisterElementFromCode("West2", "{\"Name\":\"West2\",\"Enabled\":false,\"refX\":108.223785,\"refY\":89.81443,\"refZ\":-5.456968E-12,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"2\",\"tether\":true}");
            Controller.RegisterElementFromCode("West3", "{\"Name\":\"West3\",\"Enabled\":false,\"refX\":118.00965,\"refY\":93.731346,\"refZ\":9.5366886E-07,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"3\",\"tether\":true}");
            Controller.RegisterElementFromCode("West4", "{\"Name\":\"West4\",\"Enabled\":false,\"refX\":117.579094,\"refY\":106.06494,\"refZ\":-5.456968E-12,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"4\",\"tether\":true}");
            Controller.RegisterElementFromCode("West5", "{\"Name\":\"West5\",\"Enabled\":false,\"refX\":101.53348,\"refY\":115.84821,\"refZ\":-9.536798E-07,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"5\",\"tether\":true}");

            Controller.RegisterElementFromCode("East1", "{\"Name\":\"East1\",\"Enabled\":false,\"refX\":97.26763,\"refY\":81.4473,\"refZ\":-5.456968E-12,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"1\",\"tether\":true}");
            Controller.RegisterElementFromCode("East2", "{\"Name\":\"East2\",\"Enabled\":false,\"refX\":92.60179,\"refY\":90.96322,\"refZ\":-3.8147027E-06,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"2\",\"tether\":true}");
            Controller.RegisterElementFromCode("East3", "{\"Name\":\"East3\",\"Enabled\":false,\"refX\":82.71691,\"refY\":94.52687,\"refZ\":-5.456968E-12,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"3\",\"tether\":true}");
            Controller.RegisterElementFromCode("East4", "{\"Name\":\"East4\",\"Enabled\":false,\"refX\":83.29329,\"refY\":105.826805,\"refZ\":9.5366886E-07,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"4\",\"tether\":true}");
            Controller.RegisterElementFromCode("East5", "{\"Name\":\"East5\",\"Enabled\":false,\"refX\":98.28137,\"refY\":116.52846,\"refZ\":9.5366886E-07,\"radius\":1.0,\"thicc\":5.0,\"overlayText\":\"5\",\"tether\":true}");

            Controller.RegisterElementFromCode("EastM1", "{\"Name\":\"EastM1\",\"Enabled\":false,\"refX\":110.13328,\"refY\":90.989174,\"refZ\":-5.456968E-12,\"radius\":1.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":5.0,\"overlayText\":\"1 Inner edge\",\"tether\":true}");
            Controller.RegisterElementFromCode("EastM2", "{\"Name\":\"EastM2\",\"Enabled\":false,\"refX\":110.057434,\"refY\":108.96221,\"refZ\":-5.456968E-12,\"radius\":1.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":5.0,\"overlayText\":\"2 Inner edge\",\"tether\":true}");
            Controller.RegisterElementFromCode("EastM3", "{\"Name\":\"EastM3\",\"Enabled\":false,\"refX\":90.37988,\"refY\":109.85926,\"refZ\":-9.536798E-07,\"radius\":1.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":5.0,\"overlayText\":\"3 IN MARKER\",\"tether\":true}");

            Controller.RegisterElementFromCode("WestM1", "{\"Name\":\"WestM1\",\"Enabled\":false,\"refX\":89.933,\"refY\":90.989174,\"refZ\":-5.456968E-12,\"radius\":1.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":5.0,\"overlayText\":\"1 Inner edge\",\"tether\":true}");
            Controller.RegisterElementFromCode("WestM2", "{\"Name\":\"WestM2\",\"Enabled\":false,\"refX\":89.937,\"refY\":108.96221,\"refZ\":-5.456968E-12,\"radius\":1.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":5.0,\"overlayText\":\"2 Inner edge\",\"tether\":true}");
            Controller.RegisterElementFromCode("WestM3", "{\"Name\":\"WestM3\",\"Enabled\":false,\"refX\":110.04,\"refY\":109.85926,\"refZ\":-9.536798E-07,\"radius\":1.0,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":5.0,\"overlayText\":\"3 IN MARKER\",\"tether\":true}");

            Controller.RegisterElementFromCode("EastM1Point", "{\"Name\":\"EastM1Point\",\"type\":5,\"Enabled\":false,\"refX\":110.13328,\"refY\":90.989174,\"refZ\":-5.456968E-12,\"radius\":4.0,\"coneAngleMin\":90,\"coneAngleMax\":270,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":1.0,\"overlayText\":\"Inner edge\",\"includeRotation\":true,\"tether\":true,\"Filled\":true}");
            Controller.RegisterElementFromCode("EastM2Point", "{\"Name\":\"EastM2Point\",\"type\":5,\"Enabled\":false,\"refX\":110.057434,\"refY\":108.96221,\"refZ\":-5.456968E-12,\"radius\":4.0,\"coneAngleMin\":-90,\"coneAngleMax\":90,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":1.0,\"overlayText\":\"Inner edge\",\"includeRotation\":true,\"tether\":true,\"Filled\":true}");
            Controller.RegisterElementFromCode("EastM3Point", "{\"Name\":\"EastM3Point\",\"type\":5,\"Enabled\":false,\"refX\":90.37988,\"refY\":109.85926,\"refZ\":-9.536798E-07,\"radius\":4.0,\"coneAngleMax\":180,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":1.0,\"overlayText\":\"IN MARKER\",\"includeRotation\":true,\"tether\":true,\"Filled\":true}");

            Controller.RegisterElementFromCode("WestM1Point", "{\"Name\":\"WestM1Point\",\"type\":5,\"Enabled\":false,\"refX\":89.933,\"refY\":90.989174,\"refZ\":-5.456968E-12,\"radius\":4.0,\"coneAngleMin\":90,\"coneAngleMax\":270,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":1.0,\"overlayText\":\"Inner edge\",\"includeRotation\":true,\"tether\":true,\"Filled\":true}");
            Controller.RegisterElementFromCode("WestM2Point", "{\"Name\":\"WestM2Point\",\"type\":5,\"Enabled\":false,\"refX\":89.937,\"refY\":108.96221,\"refZ\":-5.456968E-12,\"radius\":4.0,\"coneAngleMin\":-90,\"coneAngleMax\":90,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":1.0,\"overlayText\":\"Inner edge\",\"includeRotation\":true,\"tether\":true,\"Filled\":true}");
            Controller.RegisterElementFromCode("WestM3Point", "{\"Name\":\"WestM3Point\",\"type\":5,\"Enabled\":false,\"refX\":110.04,\"refY\":109.85926,\"refZ\":-9.536798E-07,\"radius\":4.0,\"coneAngleMin\":-180,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":1.0,\"overlayText\":\"IN MARKER\",\"includeRotation\":true,\"tether\":true,\"Filled\":true}");
        }

        public override void OnUpdate()
        {
            OffAll();
            if (IsMechanicRunning(out var direction))
            {
                var prio = ObtainMyPriority();
                if(prio.Priority != 0)
                {
                    var d = direction == CardinalDirection.West ? "West" : "East";
                    if (prio.IsMonitor)
                    {
                        Controller.GetElementByName($"{d}M{prio.Priority}").Enabled = true;
                        Controller.GetElementByName($"{d}M{prio.Priority}Point").Enabled = true;
                    }
                    else
                    {
                        Controller.GetElementByName($"{d}{prio.Priority}").Enabled = true;
                    }
                }
            }
        }

        void OffAll()
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        public override void OnSettingsDraw()
        {
            ImGuiEx.Text($"Priority list:");
            ImGui.SameLine();
            if (ImGui.SmallButton("Test"))
            {
                if (TryGetPriorityList(out var list))
                {
                    DuoLog.Information($"Success: priority list {list.Print()}");
                }
                else
                {
                    DuoLog.Warning($"Could not get priority list");
                }
            }
            var toRem = -1;
            for (int i = 0; i < Conf.Priorities.Count; i++)
            {
                if (DrawPrioList(i))
                {
                    toRem = i;
                }
            }
            if(toRem != -1)
            {
                Conf.Priorities.RemoveAt(toRem);
            }
            if(ImGui.Button("Create new priority list"))
            {
                Conf.Priorities.Add(new string[] { "", "", "", "", "", "", "", "" });
            }
            if (ImGui.CollapsingHeader("Debug"))
            {
                var pr = ObtainMyPriority();
                ImGuiEx.Text($"My priority: {pr.Priority}, IsMonitor = {pr.IsMonitor}");
                if(IsMechanicRunning(out var dir))
                {
                    ImGuiEx.Text($"Mechanic is running, direction {dir}");
                }
            }
        }

        bool IsMechanicRunning(out CardinalDirection mechanicStep)
        {
            var caster = Svc.Objects.FirstOrDefault(x => x is BattleChara b && b.CastActionId.EqualsAny<uint>(31595, 31596)) as BattleChara;
            if(caster != null)
            {
                mechanicStep = caster.CastActionId == 31595 ? CardinalDirection.East : CardinalDirection.West;
                return true;
            }
            mechanicStep = default;
            return false;
        }

        (int Priority, bool IsMonitor) ObtainMyPriority()
        {
            if(TryGetPriorityList(out var list))
            {
                var isMonitor = Svc.ClientState.LocalPlayer.HasMonitor();
                if (isMonitor)
                {
                    var prio = 1;
                    foreach(var x in list.Where(z => GetPlayer(z)?.HasMonitor() == true))
                    {
                        if(x == Svc.ClientState.LocalPlayer.Name.ToString())
                        {
                            return (prio, true);
                        }
                        else
                        {
                            prio++;
                        }
                    }
                }
                else
                {
                    var prio = 1;
                    foreach (var x in list.Where(z => GetPlayer(z)?.HasMonitor() == false))
                    {
                        if (x == Svc.ClientState.LocalPlayer.Name.ToString())
                        {
                            return (prio, false);
                        }
                        else
                        {
                            prio++;
                        }
                    }
                }
            }
            return (0, false);
        }

        PlayerCharacter? GetPlayer(string name)
        {
            return FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == name);
        }

        bool DrawPrioList(int num)
        {
            var prio = Conf.Priorities[num];
            ImGuiEx.Text($"Priority list {num+1}");
            ImGui.PushID($"prio{num}");
            for (int i = 0; i < prio.Length; i++)
            {
                ImGui.PushID($"prio{num}element{i}");
                ImGui.SetNextItemWidth(200);
                ImGui.InputText($"Player {i + 1}", ref prio[i], 50);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150);
                if (ImGui.BeginCombo("##partysel", "Select from party"))
                {
                    foreach(var x in FakeParty.Get())
                    {
                        if (ImGui.Selectable(x.Name.ToString()))
                        {
                            prio[i] = x.Name.ToString();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.PopID();
            }
            if(ImGui.Button("Delete this list (ctrl+click)") && ImGui.GetIO().KeyCtrl)
            {
                return true;
            }
            ImGui.PopID();
            return false;
        }

        bool TryGetPriorityList([NotNullWhen(true)]out string[]? values)
        {
            foreach(var p in Conf.Priorities)
            {
                var valid = true;
                var l = FakeParty.Get().Select(x => x.Name.ToString()).ToHashSet();
                foreach(var x in p)
                {
                    if (!l.Remove(x))
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    values = p;
                    return true;
                }
            }
            values = default;
            return false;
        }

        public class Config : IEzConfig
        {
            public List<string[]> Priorities = new();
        }
    }

    public static class OWCExtensions
    {
        public static bool HasMonitor(this PlayerCharacter c)
        {
            return c.StatusList.Any(x => x.StatusId.EqualsAny<uint>(3453, 3452));
        }
    }
}
