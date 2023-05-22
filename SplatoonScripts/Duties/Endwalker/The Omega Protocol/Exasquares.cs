using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Logging;
using System.Numerics;
using ImGuiNET;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommons.Configuration;
using Dalamud.Interface.Colors;
using ECommons.Schedulers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Throttlers;
using ECommons.MathHelpers;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Exasquares : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };
        public override Metadata? Metadata => new(4, "NightmareXIV");
        TickScheduler? sch;
        TickScheduler? doTask;
        bool mechanicResolved = false;

        public override void OnSetup()
        {
            var exasquaresIn = "~Lv2~{\"Name\":\"P6 - 1\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":107.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":107.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":92.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":92.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":92.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":92.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":107.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":107.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":8.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6 - 2\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":110.0,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":110.0,\"offY\":120.0,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":90.0,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":90.0,\"offY\":120.0,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":110.0,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":110.0,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":90.0,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":90.0,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":10.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6 - 3\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":100.0,\"refZ\":-5.456968E-12,\"offX\":120.0,\"offY\":100.0,\"offZ\":-5.456968E-12,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":100.0,\"refY\":80.0,\"refZ\":-5.456968E-12,\"offX\":100.0,\"offY\":120.0,\"offZ\":-5.456968E-12,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":82.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":82.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":82.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":82.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":117.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":117.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":117.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":117.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":12.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6 - 4\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":100.0,\"refZ\":-5.456968E-12,\"offX\":120.0,\"offY\":100.0,\"offZ\":-5.456968E-12,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":100.0,\"refY\":80.0,\"refZ\":-5.456968E-12,\"offX\":100.0,\"offY\":120.0,\"offZ\":-5.456968E-12,\"radius\":5.0,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":14.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6 - 5\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":107.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":107.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":92.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":92.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":92.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":92.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":107.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":107.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":16.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6 - 6\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":112.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":112.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":87.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":87.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":87.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":87.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":112.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":112.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":18.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6 - 7\",\"Group\":\"TOP\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"\",\"type\":2,\"refX\":82.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":82.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":82.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":82.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":80.0,\"refY\":117.5,\"refZ\":9.5366886E-07,\"offX\":120.0,\"offY\":117.5,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1},{\"Name\":\"\",\"type\":2,\"refX\":117.5,\"refY\":80.0,\"refZ\":9.5366886E-07,\"offX\":117.5,\"offY\":120.0,\"radius\":2.5,\"color\":1677721855,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":20.2,\"FireOnce\":true}]}".Split("\n");

            var exasquaresOut = "~Lv2~{\"Name\":\"P6  1\",\"Group\":\"\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":120.23772,\"refY\":92.5,\"refZ\":-5.456968E-12,\"offX\":79.745346,\"offY\":92.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":107.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":107.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":92.41275,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":92.4778,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":107.498146,\"refY\":120.37131,\"refZ\":3.8146918E-06,\"offX\":107.49135,\"offY\":79.92043,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":8.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6  2\",\"Group\":\"\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":120.0,\"refY\":97.5,\"refZ\":-5.456968E-12,\"offX\":80.0,\"offY\":97.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":102.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":102.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":97.5,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":97.5,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":102.5,\"refY\":120.37131,\"refZ\":3.8146918E-06,\"offX\":102.5,\"offY\":79.92043,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"1\",\"type\":2,\"refX\":120.23772,\"refY\":92.5,\"refZ\":-5.456968E-12,\"offX\":79.745346,\"offY\":92.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":107.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":107.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":92.41275,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":92.4778,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":107.498146,\"refY\":120.37131,\"refZ\":3.8146918E-06,\"offX\":107.49135,\"offY\":79.92043,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":10.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6   3\",\"Group\":\"\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":120.23772,\"refY\":87.5,\"refZ\":-5.456968E-12,\"offX\":79.745346,\"offY\":87.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":112.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":112.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":87.5,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":87.5,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":112.5,\"refY\":120.20232,\"refZ\":-3.8146973E-06,\"offX\":112.5,\"offY\":79.734604,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":102.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":102.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"1\",\"type\":2,\"refX\":120.0,\"refY\":97.5,\"refZ\":-5.456968E-12,\"offX\":80.0,\"offY\":97.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":97.5,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":97.5,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":102.5,\"refY\":120.37131,\"refZ\":3.8146918E-06,\"offX\":102.5,\"offY\":79.92043,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":12.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6   4\",\"Group\":\"\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":120.0,\"refY\":82.5,\"refZ\":-3.8146973E-06,\"offX\":80.002525,\"offY\":82.5,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":117.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":117.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":82.5,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":82.5,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":117.5,\"refY\":120.0,\"refZ\":-3.8146973E-06,\"offX\":117.5,\"offY\":80.0,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"1\",\"type\":2,\"refX\":120.23772,\"refY\":92.5,\"refZ\":-5.456968E-12,\"offX\":79.745346,\"offY\":92.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":107.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":107.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":92.41275,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":92.4778,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":107.498146,\"refY\":120.37131,\"refZ\":3.8146918E-06,\"offX\":107.49135,\"offY\":79.92043,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":14.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6   5\",\"Group\":\"\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":120.23772,\"refY\":87.5,\"refZ\":-5.456968E-12,\"offX\":79.745346,\"offY\":87.5,\"offZ\":-5.456968E-12,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":112.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":112.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":87.5,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":87.5,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":112.5,\"refY\":120.20232,\"refZ\":-3.8146973E-06,\"offX\":112.5,\"offY\":79.734604,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":16.2,\"FireOnce\":true}]}\n~Lv2~{\"Name\":\"P6  6\",\"Group\":\"\",\"ZoneLockH\":[1122],\"DCond\":5,\"ElementsL\":[{\"Name\":\"1\",\"type\":2,\"refX\":120.0,\"refY\":82.5,\"refZ\":-3.8146973E-06,\"offX\":80.002525,\"offY\":82.5,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"2\",\"type\":2,\"refX\":120.0,\"refY\":117.5,\"refZ\":-3.8147027E-06,\"offX\":80.0,\"offY\":117.5,\"offZ\":3.8146918E-06,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"3\",\"type\":2,\"refX\":82.5,\"refY\":120.50941,\"refZ\":7.629389E-06,\"offX\":82.5,\"offY\":79.78934,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1},{\"Name\":\"4\",\"type\":2,\"refX\":117.5,\"refY\":120.0,\"refZ\":-3.8146973E-06,\"offX\":117.5,\"offY\":80.0,\"radius\":2.5,\"color\":838861055,\"FillStep\":0.1}],\"UseTriggers\":true,\"Triggers\":[{\"Type\":2,\"Duration\":2.0,\"Match\":\"(12256>31651)\",\"MatchDelay\":18.2,\"FireOnce\":true}]}".Split("\n");

            for (int i = 0; i < exasquaresIn.Length; i++)
            {
                if (!Controller.TryRegisterLayoutFromCode($"In{i}", exasquaresIn[i], out _)) PluginLog.Error("Error");
            }

            for (int i = 0; i < exasquaresOut.Length; i++)
            {
                if (!Controller.TryRegisterLayoutFromCode($"Out{i}", exasquaresOut[i], out _)) PluginLog.Error("Error");
            }
            Off();
        }

        void Reset()
        {
            mechanicResolved = false;
            Controller.ClearRegisteredLayouts();
            this.OnSetup();
            Controller.ApplyOverrides();
        }

        public override void OnMessage(string Message)
        {
            if (Controller.Scene == 7)
            {
                if (Message.Contains("(12256>31651)"))
                {
                    var npc = Svc.Objects.Where(x => x is BattleChara b && b.CastActionId == 31651);
                    if (npc.Any() && !mechanicResolved)
                    {
                        sch?.Dispose();
                        sch = new TickScheduler(Reset, 40000);
                        mechanicResolved = true;
                        if (!npc.Any(x => x.Position.X.InRange(99f, 101f) || x.Position.Y.InRange(99f, 101f)))
                        {
                            DisplayOUT();
                        }
                        else
                        {
                            DisplayIN();
                        }
                    }
                }
            }
            else
            {
                mechanicResolved = false;
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Wipe, DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence))
            {
                sch?.Dispose();
            }
        }

        void Off()
        {
            Controller.GetRegisteredLayouts().Each(x => x.Value.Enabled = false);
        }

        void DisplayIN()
        {
            Off();
            Controller.GetRegisteredLayouts().Each(x => x.Value.ElementsL.Each((z) => { z.color = Controller.GetConfig<Config>().Col.ToUint(); z.FillStep = Controller.GetConfig<Config>().FillStep; }));
            Controller.GetRegisteredLayouts().Where(x => x.Key.Contains("In")).Each(x => x.Value.Enabled = true);
        }

        void DisplayOUT()
        {
            Off();
            Controller.GetRegisteredLayouts().Each(x => x.Value.ElementsL.Each((z) => { z.color = Controller.GetConfig<Config>().Col.ToUint(); z.FillStep = Controller.GetConfig<Config>().FillStep; }));
            Controller.GetRegisteredLayouts().Where(x => x.Key.Contains("Out")).Each(x => x.Value.Enabled = true);
        }

        public override void OnSettingsDraw()
        {
            var c = Controller.GetConfig<Config>();
            ImGui.SetNextItemWidth(400f);
            ImGui.ColorEdit4("Color", ref c.Col);
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("Fill step", ref c.FillStep, 0.01f, 0.01f, 10);
        }

        public class Config : IEzConfig
        {
            public Vector4 Col = ImGuiColors.DalamudRed;
            public float FillStep = 0.5f;
        }
    }
}
