using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Stormblood
{
    public class UCOB_Tethers : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { Raids.the_Unending_Coil_of_Bahamut_Ultimate };
        HashSet<uint> TetheredPlayers = new();
        public override Metadata? Metadata => new(2, "NightmareXIV");

        public override void OnSetup()
        {
            Controller.RegisterElement("Tether1", new(2) { thicc = 5f, radius = 0f });
            Controller.RegisterElement("Tether2", new(2) { thicc = 5f, radius = 0f });
        }

        public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
        {
            if (IsBahamut(target, out _) && data3 == 4 && data5 == 15)
            {
                //DuoLog.Information($"Tether: {data2}, {data3}, {data5}");
                TetheredPlayers.Add(source);
                //UpdateTethers();
            }
        }

        public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
        {
            //DuoLog.Information($"Tether rem: {data2}, {data3}, {data5}");
            TetheredPlayers.Remove(source);
            //UpdateTethers();
        }

        public override void OnUpdate()
        {
            UpdateTethers();
        }

        bool IsBahamut(uint oid, [NotNullWhen(true)] out BattleChara? bahamut)
        {
            if (oid.TryGetObject(out var obj) && obj is BattleChara o && o.NameId == 3210)
            {
                bahamut = o;
                return true;
            }
            bahamut = null;
            return false;
        }

        void Reset()
        {
            TetheredPlayers.Clear();
        }

        BattleChara? GetBahamut()
        {
            return Svc.Objects.FirstOrDefault(x => x is BattleChara o && o.NameId == 3210 && o.IsCharacterVisible()) as BattleChara;
        }

        void UpdateTethers()
        {
            if (TetheredPlayers.Count == 2)
            {
                var tetheredPlayers = TetheredPlayers.ToArray();
                var omega = GetBahamut();
                {
                    if (Controller.TryGetElementByName("Tether1", out var e))
                    {
                        e.Enabled = true;
                        e.SetRefPosition(omega.Position);
                        e.SetOffPosition(tetheredPlayers[0].GetObject().Position);
                        var correct = (tetheredPlayers[0].GetObject() as PlayerCharacter).GetRole() == CombatRole.Tank;
                        e.color = (correct ? Conf.ValidTetherColor : GradientColor.Get(Conf.InvalidTetherColor1, Conf.InvalidTetherColor2, 500)).ToUint();
                    }
                }
                {
                    if (Controller.TryGetElementByName("Tether2", out var e))
                    {
                        e.Enabled = true;
                        e.SetRefPosition(omega.Position);
                        e.SetOffPosition(tetheredPlayers[1].GetObject().Position);
                        var correct = (tetheredPlayers[1].GetObject() as PlayerCharacter).GetRole() == CombatRole.Tank;
                        e.color = (correct ? Conf.ValidTetherColor : GradientColor.Get(Conf.InvalidTetherColor1, Conf.InvalidTetherColor2, 500)).ToUint();
                    }
                }
            }
            else
            {
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            }
        }

        Config Conf => Controller.GetConfig<Config>();
        public override void OnSettingsDraw()
        {
            ImGui.ColorEdit4("Valid tether color", ref Conf.ValidTetherColor, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("##Invalid1", ref Conf.InvalidTetherColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Invalid tether colors", ref Conf.InvalidTetherColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.SetNextItemWidth(100f);
            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGuiEx.Text($"Tethers: {TetheredPlayers.Select(x => x.GetObject()?.ToString() ?? $"unk{x}").Join("\n")}");
            }
        }

        public class Config : IEzConfig
        {
            public Vector4 ValidTetherColor = ImGuiColors.ParsedGreen;
            public Vector4 InvalidTetherColor1 = ImGuiColors.DalamudOrange;
            public Vector4 InvalidTetherColor2 = ImGuiColors.DalamudRed;
        }
    }
}
