using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Schedulers;
using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker
{
    public class P11S_Multiscript : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1152 };
        public override Metadata? Metadata => new(1, "NightmareXIV");

        const string DarkVFX = "vfx/common/eff/m0830_dark_castloopc0k1.avfx";
        const string LightVFX = "vfx/common/eff/m0830_light_castloopc0k1.avfx";
        enum Color { Unknown, Light, Dark };
        BattleNpc? Themis => Svc.Objects.FirstOrDefault(x => x is BattleNpc b && b.DataId == 16114 && b.IsTargetable()) as BattleNpc;
        IEnumerable<BattleNpc> IllusoryThemises => Svc.Objects.Where(x => x is BattleNpc b && b.DataId == 16115).Cast<BattleNpc>();
        TickScheduler? DonutScheduler;

        public override void OnSetup()
        {
            for(var i = 0; i < 8; i++)
            {
                Controller.RegisterElementFromCode($"PairDonut{i}", "{\"Name\":\"\",\"Enabled\":false,\"refX\":93.386154,\"refY\":89.96649,\"radius\":2.0,\"Donut\":7.0,\"color\":4290576590,\"thicc\":3.0,\"refActorPlaceholder\":[],\"FillStep\":0.25,\"refActorComparisonType\":5}");
                Controller.RegisterElementFromCode($"LingerAOE{i}", "{\"Name\":\"\",\"Enabled\":false,\"refX\":89.57288,\"refY\":89.32873,\"refZ\":-9.536743E-07,\"radius\":5.0,\"color\":1358954495,\"Filled\":true}");
            }
        }

        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            if(set.Source != null && set.Source is BattleNpc b)
            {
                //DuoLog.Information($"{set.Action.RowId} - {set.Action.Name} ({b.Name})");
                if(set.Action.RowId.EqualsAny<uint>(33257, 33256)) //protean
                {
                    var col = GetColor(Themis);
                    var name = col == Color.Dark ? "PairDonut" : "LingerAOE";
                    int i = 0;
                    foreach (var x in FakeParty.Get())
                    {
                        if (Controller.TryGetElementByName($"{name}{i}", out var e))
                        {
                            e.Enabled = true;
                            e.SetRefPosition(x.Position);
                        }
                        i++;
                    }
                    DonutScheduler?.Dispose();
                    DonutScheduler = new(() => Controller.GetRegisteredElements().Where(x => x.Key.StartsWithAny("PairDonut", "LingerAOE")).Each(z => z.Value.Enabled = false), 3000);
                }
            }
        }

        Color GetColor(GameObject obj)
        {
            Color col = Color.Unknown;
            long age = long.MaxValue;
            if(AttachedInfo.TryGetVfx(obj, out var info))
            {
                foreach(var x in info)
                {
                    if (x.Value.Age < age)
                    {
                        if (x.Key == LightVFX)
                        {
                            col = Color.Light;
                            age = x.Value.Age;
                        }
                        else if(x.Key == DarkVFX)
                        {
                            col = Color.Dark;
                            age = x.Value.Age;
                        }
                    }
                }
            }
            return col;
        }

        public override void OnSettingsDraw()
        {
            if (ImGui.CollapsingHeader("Debug"))
            {
                var t = Themis;
                if(t != null)
                {
                    ImGuiEx.Text($"Themis color: {GetColor(t)} / {t}");
                }
                foreach(var x in IllusoryThemises)
                {
                    ImGuiEx.Text($"{x} color: {GetColor(x)}");
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                DonutScheduler?.Dispose();
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            }
        }
    }
}
