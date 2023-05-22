using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ImGuiNET;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Party_Synergy : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        public override Metadata? Metadata => new(2, "NightmareXIV");

        const string StackVFX = "vfx/lockon/eff/com_share2i.avfx";
        const string ChainVFX = "vfx/lockon/eff/z3oz_firechain_0";

        const uint FarGlitch = 3428;

        TickScheduler? Sch = null;

        Config Conf => Controller.GetConfig<Config>();

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("FarLeft", "{\"Enabled\":false,\"Name\":\"Left\",\"type\":1,\"offX\":2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"thicc\":5.0,\"overlayText\":\"Left\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
            Controller.RegisterElementFromCode("FarRight", "{\"Enabled\":false,\"Name\":\"Right\",\"type\":1,\"offX\":-2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278255615,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278252031,\"thicc\":5.0,\"overlayText\":\"Right\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");

            Controller.RegisterElementFromCode("CloseLeft", "{\"Enabled\":false,\"Name\":\"Left\",\"type\":1,\"offX\":2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"thicc\":5.0,\"overlayText\":\"Left\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
            Controller.RegisterElementFromCode("CloseRight", "{\"Enabled\":false,\"Name\":\"Bottom\",\"type\":1,\"offY\":15.5,\"radius\":1.0,\"color\":4278255615,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278252031,\"thicc\":5.0,\"overlayText\":\"Right\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            //Dequeued message: VFX vfx/lockon/eff/com_share2i.avfx
            if (vfxPath == StackVFX && Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId.EqualsAny<uint>(3427, 3428)))
            {
                var stackers = AttachedInfo.VFXInfos.Where(x => x.Value.Any(z => z.Key == StackVFX && z.Value.Age < 1000)).Select(x => x.Key).Select(x => Svc.Objects.FirstOrDefault(z => z.Address == x)).ToArray();
                var opticalUnit = Svc.Objects.FirstOrDefault(x => x is Character c && c.NameId == 7640);
                var mid = MathHelper.GetRelativeAngle(new(100, 100), opticalUnit.Position.ToVector2());
                var myAngle = (MathHelper.GetRelativeAngle(Svc.ClientState.LocalPlayer.Position, opticalUnit.Position) - mid + 360) % 360;
                if (stackers.Length == 2 && opticalUnit != null)
                {
                    Sch?.Dispose();
                    Sch = new TickScheduler(HideAll, 9000);
                    HideAll();
                    var dirNormal = myAngle > 180 ? "Right" : "Left";
                    var dirModified = myAngle < 180 ? "Right" : "Left";
                    if (Conf.ExplicitTether)
                    {
                        if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == FarGlitch))
                        {
                            Controller.GetElementByName($"Far{dirNormal}").Enabled = true;
                        }
                        else
                        {
                            Controller.GetElementByName($"Close{dirNormal}").Enabled = true;
                        }
                    }
                    var a1 = (MathHelper.GetRelativeAngle(stackers[0].Position, opticalUnit.Position) - mid + 360) % 360;
                    var a2 = (MathHelper.GetRelativeAngle(stackers[1].Position, opticalUnit.Position) - mid + 360) % 360;
                    //DuoLog.Information($"Angles: {a1}, {a2}");
                    if((a1 > 180 && a2 > 180) || (a1 < 180 && a2 < 180))
                    {
                        //DuoLog.Information($"Swap!");
                        var swapper = stackers.OrderBy(x => Vector3.Distance(opticalUnit.Position, x.Position)).ToArray()[Conf.ReverseAdjust ? 0 : 1];
                        var swappersVfx = AttachedInfo.VFXInfos[swapper.Address].FirstOrDefault(x => x.Key.Contains(ChainVFX) && x.Value.AgeF < 60).Key;
                        //DuoLog.Information($"Swapper: {swapper} Swapper's vfx: {swappersVfx}");
                        var secondSwapper = AttachedInfo.VFXInfos.Where(x => x.Key != swapper.Address && x.Value.Any(z => z.Key.Contains(swappersVfx) && z.Value.AgeF < 60)).Select(x => x.Key).Select(x => Svc.Objects.FirstOrDefault(z => z.Address == x)).FirstOrDefault();
                        //DuoLog.Information($"Second swapper: {secondSwapper}");
                        if (Conf.PrintPreciseResultInChat) DuoLog.Warning($"Swapping! \n{swapper.Name}\n{secondSwapper?.Name}\n============");
                        if (Svc.ClientState.LocalPlayer.Address.EqualsAny(swapper.Address, secondSwapper.Address))
                        {
                            HideAll();
                            if (Conf.ExplicitTether)
                            {
                                if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == FarGlitch))
                                {
                                    Controller.GetElementByName($"Far{dirModified}").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName($"Close{dirModified}").Enabled = true;
                                }
                            }
                            new TimedMiddleOverlayWindow("swaponYOU", 10000, () =>
                            {
                                ImGui.SetWindowFontScale(2f);
                                ImGuiEx.Text(ImGuiColors.DalamudRed, $"Stack swap position!\n\n  {swapper.Name} \n  {secondSwapper?.Name}\n Go {dirModified}");
                            }, 150);
                        }
                    }
                    else
                    {
                        if (Conf.PrintPreciseResultInChat) DuoLog.Information($"No swap, go {(myAngle > 180 ? "right" : "left")}");
                    }
                }
            }
        }

        void HideAll()
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category == DirectorUpdateCategory.Commence || category == DirectorUpdateCategory.Recommence)
            {
                Sch?.Dispose();
                HideAll();
            }
        }

        public override void OnSettingsDraw()
        {
            if (ImGui.RadioButton("Furthest from eye adjusts", !Conf.ReverseAdjust)) Conf.ReverseAdjust = false;
            if (ImGui.RadioButton("Closest to eye adjusts", Conf.ReverseAdjust)) Conf.ReverseAdjust = true;
            ImGui.Checkbox($"Print in chat info about not your adjusts", ref Conf.PrintPreciseResultInChat);
            ImGui.Checkbox($"Explicit position tether (unfinished feature, supports right side adjust only)", ref Conf.ExplicitTether);
            if (ImGui.CollapsingHeader("Debug"))
            {
                var opticalUnit = Svc.Objects.FirstOrDefault(x => x is Character c && c.NameId == 7640);
                if (opticalUnit != null)
                {
                    var mid = MathHelper.GetRelativeAngle(new(100, 100), opticalUnit.Position.ToVector2());
                    ImGuiEx.Text($"Mid: {mid}");
                    foreach (var x in Svc.Objects)
                    {
                        if (x is PlayerCharacter pc)
                        {
                            var pos = (MathHelper.GetRelativeAngle(pc.Position.ToVector2(), opticalUnit.Position.ToVector2()) - mid + 360) % 360;
                            ImGuiEx.Text($"{pc.Name} {pos} {(pos > 180 ? "right" : "left")}");
                        }
                    }
                }
                if (ImGui.Button("test"))
                {
                    new TimedMiddleOverlayWindow("swaponYOU", 5000, () =>
                    {
                        ImGui.SetWindowFontScale(2f);
                        ImGuiEx.Text(ImGuiColors.DalamudRed, $"Stack swap position!\n\n  Player 1 \n  Player 2");
                    }, 150);
                }
            }
        }

        public class Config : IEzConfig
        {
            public bool ReverseAdjust = false;
            public bool PrintPreciseResultInChat = false;
            public bool ExplicitTether = false;
        }
    }
}
