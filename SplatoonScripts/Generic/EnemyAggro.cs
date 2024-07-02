using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Dalamud.Hooking;
using System.Threading;
using ECommons.ImGuiMethods;

namespace SplatoonScriptsOfficial.Generic;
public class EnemyAggro : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => new() { };
    Config Settings => Controller.GetConfig<Config>();

    public class Config : IEzConfig
    {
        public bool TetherEnable = true;
        public Vector4 TetherColor = new(1, 0, 0, 1);
        public float TetherThicc = 2.0f;
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if (Svc.ClientState.LocalPlayer == null || Svc.Objects == null) return;
        if (Svc.ClientState.LocalPlayer.CurrentHp > 0)
        {
            int i = 0;
            foreach (var x in Svc.Objects)
            {
                if (x is IPlayerCharacter pc && pc.Address != Player.Object.Address && pc.Address.ToInt64() != 0 && pc.CurrentHp > 0 && pc.TargetObjectId == Svc.ClientState.LocalPlayer.EntityId)
                {
                    var element = GetElement(i++);
                    element.refActorObjectID = pc.EntityId;
                    element.Enabled = true;
                }
            }
        }
    }

    public Element GetElement(int i)
    {
        if (Controller.TryGetElementByName($"Player{i}", out var element))
        {
            return element;
        }
        else
        {
            var ret = new Element(1)
            {
                refActorType = 0,
                radius = 0,
                refActorComparisonType = 2,
                tether = Settings.TetherEnable,
                color = ImGui.ColorConvertFloat4ToU32(Settings.TetherColor),
                thicc = Settings.TetherThicc,
                overlayPlaceholders = true,
            };
            Controller.RegisterElement($"Player{i}", ret);
            return ret;
        }
    }

    public override void OnSettingsDraw()
    {
        if (ImGui.Checkbox("Enable Tethers", ref Settings.TetherEnable))
        {
            UpdateElementProperties();
        }
        ImGuiComponents.HelpMarker("Shows tethers to players targeting you.".Loc());
        ImGui.SetNextItemWidth(200);
        if (ImGui.ColorEdit4("Tether Color", ref Settings.TetherColor))
        {
            UpdateElementProperties();
        }
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Tether Thickness", ref Settings.TetherThicc, 0.1f, 5f, "%.1f"))
        {
            UpdateElementProperties();
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Targeting you: ");
            foreach(var x in Svc.Objects)
            {
                if(x.TargetObjectId == Player.Object?.EntityId)
                {
                    ImGuiEx.Text($"{x}");
                    if (ImGuiEx.HoveredAndClicked())
                    {
                        Svc.Commands.ProcessCommand($"/sf {x.Name}");
                    }
                }
            }
        }
    }

    public void UpdateElementProperties()
    {
        bool newTetherEnable = Settings.TetherEnable;
        Vector4 newTetherColor = Settings.TetherColor;
        float newTetherThicc = Settings.TetherThicc;

        foreach (var kvp in Controller.GetRegisteredElements())
        {
            var element = kvp.Value;

            if (element.tether != newTetherEnable)
            {
                element.tether = newTetherEnable;
            }

            var newColorU32 = ImGui.ColorConvertFloat4ToU32(newTetherColor);
            if (element.color != newColorU32)
            {
                element.color = newColorU32;
            }

            if (element.thicc != newTetherThicc)
            {
                element.thicc = newTetherThicc;
            }
        }
    }
}