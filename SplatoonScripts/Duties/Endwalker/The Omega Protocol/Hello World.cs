using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Dalamud.Bindings.ImGui;
using Lumina.Data;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using ECommons.DalamudServices.Legacy;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public class Hello_World : SplatoonScript
    {
        public override Metadata? Metadata => new(10, "NightmareXIV, mirage");
        public override HashSet<uint> ValidTerritories => [1122];
        private bool RotPicker = false;
        private int counter = 0;
        private List<Element> AvoidAlerts = [];
        private Config Conf => Controller.GetConfig<Config>();

        private bool IsHelloWorldRunning => FakeParty.Get().Any(x => x.StatusList.Any(z => z.StatusId.EqualsAny(Effects.RedRot, Effects.BlueRot)));

        public class Effects
        {

            //  Underflow Debugger (3432), Remains = 0.0, Param = 0, Count = 0
            public const uint NoRedRot = 3432;

            //  _rsv_3433_-1_1_0_0_S74CFC3B0_E74CFC3B0 (3433), Remains = 0.0, Param = 0, Count = 0
            public const uint NoBlueRot = 3433;

            /// <summary>
            /// _rsv_3429_-1_1_0_0_S74CFC3B0_E74CFC3B0 (3429), Remains = 15.2, Param = 0, Count = 0
            /// </summary>
            public const uint BlueRot = 3429;

            public const uint RedRot = 3526;

            /// <summary>
            /// Critical Overflow Bug (3525), Remains = 9.2, Param = 0, Count = 0
            /// </summary>
            public const uint Defamation = 3525;

            public const uint StackTwoPeople = 3524;

            /// <summary>
            /// Remote Code Smell (3441), Remains = 8.0, Param = 0, Count = 0<br></br>
            /// tethers that break when far, must be <10 seconds remaining
            /// </summary>
            public const uint UpcomingFarTether = 3441;

            /// <summary>
            ///   Local Code Smell (3503), Remains = 50.0, Param = 0, Count = 0<br></br>
            ///   tethers that break when close, must be <10 seconds remaining
            /// </summary>
            public const uint UpcomingCloseTether = 3503;

            /// <summary>
            /// Remote Regression (3530), Remains = 9.6, Param = 0, Count = 0<br></br>
            /// must be broken first, picks up corresponding rot first
            /// </summary>
            public const uint FarTether = 3530;

            /// <summary>
            ///   Local Regression (3529), Remains = 9.6, Param = 0, Count = 0
            ///   must be broken second
            /// </summary>
            public const uint CloseTether = 3529;


        }

        public Vector4 ColorBlueTower = 0xFFFF0000.ToVector4();
        public Vector4 ColorRedTower = 0xFF0000FF.ToVector4();

        public override void OnSetup()
        {
            Controller.RegisterElementFromCode("RedTower", "{\"Name\":\"Red\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"thicc\":7.0,\"refActorName\":\"*\",\"refActorRequireCast\":true,\"refActorCastId\":[31583],\"includeRotation\":false,\"tether\":true}");
            Controller.RegisterElementFromCode("BlueTower", "{\"Name\":\"Blue\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"color\":3372220160,\"thicc\":7.0,\"refActorName\":\"*\",\"refActorRequireCast\":true,\"refActorCastId\":[31584],\"includeRotation\":false,\"tether\":true}");
            Controller.RegisterElementFromCode("RedTowerSolid", "{\"Name\":\"Red\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"thicc\":4.0,\"refActorName\":\"*\",\"refActorRequireCast\":true,\"refActorCastId\":[31583],\"includeRotation\":false,\"tether\":false}");
            Controller.RegisterElementFromCode("BlueTowerSolid", "{\"Name\":\"Blue\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"color\":3372220160,\"thicc\":4.0,\"refActorName\":\"*\",\"refActorRequireCast\":true,\"refActorCastId\":[31584],\"includeRotation\":false,\"tether\":false}");
            Controller.RegisterElementFromCode("Reminder", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"offZ\":3.5,\"overlayBGColor\":4278190335,\"overlayTextColor\":4294967295,\"overlayFScale\":2.0,\"overlayText\":\"REMINDER\",\"refActorType\":1}");

            Controller.RegisterElementFromCode("DefaPartner", "{\"Name\":\"\",\"type\":1,\"Enabled\":false,\"radius\":0.5,\"color\":4294902015,\"overlayBGColor\":4294902015,\"overlayTextColor\":4294967295,\"overlayPlaceholders\":true,\"overlayText\":\"Defamation\\\\nTaker\",\"overlayTextIntl\":{\"Jp\":\"ディフェメ\\\\n取得\"},\"refActorObjectID\":11111,\"FillStep\":0.2,\"refActorComparisonType\":2,\"includeRotation\":true,\"Filled\":true}");

            Controller.TryRegisterLayoutFromCode("ReminderMessages", """
                ~Lv2~{"Enabled":false,"Name":"ReminderMessages","ZoneLockH":[1122],"ElementsL":[
                {"Name":"RedTowerDefamation","overlayText":"Defamation: inside red tower","overlayTextIntl":{"Jp":"赤塔 - 巨大円範囲"}},
                {"Name":"RedTowerStack","overlayText":"Inside red tower >>stack<<","overlayTextIntl":{"Jp":"赤塔 - 頭割り"}},
                {"Name":"BlueTowerDefamation","overlayText":"Defamation: inside blue tower","overlayTextIntl":{"Jp":"青塔 - 巨大円範囲"}},
                {"Name":"BlueTowerStack","overlayText":"Inside blue tower >>stack<<","overlayTextIntl":{"Jp":"青塔 - 頭割り"}},
                {"Name":"FarOutRedTower","overlayText":"Far out of red tower - pick up DEFAMATION","overlayTextIntl":{"Jp":"赤塔の外 - 線を伸ばす"}},
                {"Name":"FarOutBlueTower","overlayText":"Far out of blue tower - pick up DEFAMATION","overlayTextIntl":{"Jp":"青塔の外 - 線を伸ばす"}},
                {"Name":"BetweenRedTowersFinalStackOrAvoid","overlayText":"Between red towers - final stack or avoid","overlayTextIntl":{"Jp":"赤塔の間 - 最終頭割り"}},
                {"Name":"BetweenBlueTowersFinalStackOrAvoid","overlayText":"Between blue towers - final stack or avoid","overlayTextIntl":{"Jp":"青塔の間 - 最終頭割り"}},
                {"Name":"BetweenBlueTowers","overlayText":"Between blue towers - {0}","overlayTextIntl":{"Jp":"青塔の間 - {0}"}},
                {"Name":"BetweenRedTowers","overlayText":"Between red towers - {0}","overlayTextIntl":{"Jp":"赤塔の間 - {0}"}},
                {"Name":"Stack","overlayText":"STACK","overlayTextIntl":{"Jp":"頭割り"}},
                {"Name":"FinalStack","overlayText":"final STACK","overlayTextIntl":{"Jp":"最終頭割り"}},
                {"Name":"PickUpRot","overlayText":"Pick up rot","overlayTextIntl":{"Jp":"デバフを拾う"}},
                {"Name":"BreakTetherFar","overlayText":"Break tethers - go FAR","overlayTextIntl":{"Jp":"線切り - 離れる"}},
                {"Name":"BreakTetherClose","overlayText":"Break tethers - go CLOSE","overlayTextIntl":{"Jp":"線切り - 近づく"}}
                ]}
                """, out _, overwrite: true);
        }

        public override void OnMessage(string Message)
        {
            if(Message.Contains(">31599)"))
            {
                counter++;
                PluginLog.Debug("Counter: " + counter);
            }
        }

        public override void OnUpdate()
        {
            Controller.GetElementByName("DefaPartner").Enabled = false;
            AvoidAlerts.Each(x => x.Enabled = false);

            try
            {
                if(Conf.EnableAvoiders && IsHelloWorldRunning)
                {
                    var otherPlayers = FakeParty.Get().Where(x => x.Address != Svc.ClientState.LocalPlayer.Address);
                    if(HasEffect(Effects.BlueRot))
                    {
                        foreach(var x in otherPlayers)
                        {
                            if(!x.StatusList.Any(z => z.StatusId.EqualsAny(Effects.BlueRot, Effects.NoBlueRot)))
                            {
                                GetAvoidElementByOID(x.EntityId).Enabled = true;
                            }
                        }
                    }
                    else
                    {
                        if(!HasEffect(Effects.NoBlueRot))
                        {
                            foreach(var x in otherPlayers)
                            {
                                if(x.StatusList.Any(z => z.StatusId.EqualsAny(Effects.BlueRot)))
                                {
                                    GetAvoidElementByOID(x.EntityId).Enabled = true;
                                }
                            }
                        }
                    }
                    if(HasEffect(Effects.RedRot))
                    {
                        foreach(var x in otherPlayers)
                        {
                            if(!x.StatusList.Any(z => z.StatusId.EqualsAny(Effects.RedRot, Effects.NoRedRot)))
                            {
                                GetAvoidElementByOID(x.EntityId).Enabled = true;
                            }
                        }
                    }
                    else
                    {
                        if(!HasEffect(Effects.NoRedRot))
                        {
                            foreach(var x in otherPlayers)
                            {
                                if(x.StatusList.Any(z => z.StatusId.EqualsAny(Effects.RedRot)))
                                {
                                    GetAvoidElementByOID(x.EntityId).Enabled = true;
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                e.Log();
            }

            if((HasEffect(Effects.NoBlueRot) && HasEffect(Effects.NoRedRot)))
            {
                RotPicker = false;
            }
            if(Svc.Objects.Any(x => x is IBattleChara b && b.CastActionId == 31599))
            {
                var isDefamationRed = Svc.Objects.Any(x => x is IPlayerCharacter pc && HasEffect(Effects.Defamation, null, pc) && HasEffect(Effects.RedRot, null, pc));
                if(HasEffect(Effects.RedRot))
                {
                    if(Conf.EnableVisualElementsTowers) TowerRed(false);
                    if(HasEffect(Effects.Defamation))
                    {
                        if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("RedTowerDefamation"), ImGuiColors.DalamudRed);
                    }
                    else
                    {
                        if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("RedTowerStack"), ImGuiColors.DalamudRed);
                    }
                }
                else if(HasEffect(Effects.BlueRot))
                {
                    if(Conf.EnableVisualElementsTowers) TowerBlue(false);
                    if(HasEffect(Effects.Defamation))
                    {
                        if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("BlueTowerDefamation"), ImGuiColors.TankBlue);
                    }
                    else
                    {
                        if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("BlueTowerStack"), ImGuiColors.TankBlue);
                    }
                }
                else if(HasEffect(Effects.UpcomingCloseTether, 10f))
                {
                    if(counter != 4 && !(HasEffect(Effects.NoBlueRot) && HasEffect(Effects.NoRedRot))) RotPicker = true;
                    if(counter != 4)
                    {
                        var partner = FakeParty.Get().FirstOrDefault(x => x.Address != Svc.ClientState.LocalPlayer.Address && HasEffect(Effects.UpcomingCloseTether, 10f, x));
                        if(isDefamationRed)
                        {
                            if(Conf.EnableVisualElementsTowers) TowerRed(true);
                            if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("FarOutRedTower"), ImGuiColors.DalamudRed);
                        }
                        else
                        {
                            if(Conf.EnableVisualElementsTowers) TowerBlue(true);
                            if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("FarOutBlueTower"), ImGuiColors.TankBlue);
                        }
                        if(partner != null)
                        {
                            if(Conf.EnableDefamationPartner)
                            {
                                Controller.GetElementByName("DefaPartner").Enabled = true;
                                Controller.GetElementByName("DefaPartner").tether = Conf.EnableDefamationPartnerTether;
                                Controller.GetElementByName("DefaPartner").refActorObjectID = partner.EntityId;
                            }
                        }
                    }
                    else
                    {
                        if(isDefamationRed)
                        {
                            if(Conf.EnableVisualElementsTowers) TowerBlue(true);
                            if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("BetweenBlueTowersFinalStackOrAvoid"), ImGuiColors.TankBlue);
                        }
                        else
                        {
                            if(Conf.EnableVisualElementsTowers) TowerRed(true);
                            if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("BetweenRedTowersFinalStackOrAvoid"), ImGuiColors.DalamudRed);
                        }
                    }
                }
                else if(HasEffect(Effects.UpcomingFarTether, 10))
                {
                    if(counter != 4 && !(HasEffect(Effects.NoBlueRot) && HasEffect(Effects.NoRedRot))) RotPicker = true;
                    var stackLabel = GetReminderText(counter == 4 ? "FinalStack" : "Stack");
                    if(isDefamationRed)
                    {
                        if(Conf.EnableVisualElementsTowers) TowerBlue(true);
                        if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("BetweenBlueTowers", stackLabel), ImGuiColors.TankBlue);
                    }
                    else
                    {
                        if(Conf.EnableVisualElementsTowers) TowerRed(true);
                        if(Conf.EnableOverheadHintsGeneric) Reminder(GetReminderText("BetweenRedTowers", stackLabel), ImGuiColors.DalamudRed);
                    }
                }
            }
            else
            {
                TowerOff();
                Reminder(null);
                if(RotPicker)
                {
                    if(Conf.EnableRotPickerReminding) Reminder(GetReminderText("PickUpRot"), 0xFF000000.ToVector4());
                    if(HasEffect(Effects.BlueRot) || HasEffect(Effects.RedRot))
                    {
                        RotPicker = false;
                    }
                }
                else
                {
                    if(HasEffect(Effects.FarTether))
                    {
                        if(Conf.EnableOverheadHintsTether) Reminder(GetReminderText("BreakTetherFar"), ImGuiColors.HealerGreen);
                    }
                    if(HasEffect(Effects.CloseTether) && !Svc.Objects.Any(x => x is IPlayerCharacter pc && HasEffect(Effects.FarTether)))
                    {
                        if(Conf.EnableOverheadHintsTether) Reminder(GetReminderText("BreakTetherClose"), ImGuiColors.ParsedBlue);
                    }
                }
            }
        }

        public override void OnEnable()
        {
            TowerOff();
            Reminder(null);
            counter = 0;
            RotPicker = false;
            Controller.GetElementByName("DefaPartner").Enabled = false;
            SetupElements();
            PluginLog.Information("Counter: " + counter);
        }

        private void SetupElements()
        {
            var list = FakeParty.Get().ToArray();
            PluginLog.Debug($"Party count is {list}");
            AvoidAlerts.Clear();
            for(var i = 0; i < list.Length; i++)
            {
                AvoidAlerts.Add(Controller.RegisterElementFromCode($"Avoid{i}", "{\"Name\":\"\",\"type\":3,\"Enabled\":false,\"refZ\":1.2,\"radius\":0.0,\"color\":3355473407,\"overlayFScale\":5.0,\"thicc\":25.0,\"refActorObjectID\":12345678,\"refActorComparisonType\":2}".Replace("12345678", $"{list[i].EntityId}")));
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            if(category.EqualsAny(DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe, DirectorUpdateCategory.Commence))
            {
                OnEnable();
                if(category != DirectorUpdateCategory.Wipe)
                {
                    SetupElements();
                }
            }
        }

        private void TowerRed(bool filled)
        {
            Controller.GetElementByName("RedTower").Enabled = true;
            Controller.GetElementByName("BlueTower").Enabled = false;
            Controller.GetElementByName("RedTower").color = GradientColor.Get(ColorRedTower, ColorRedTower with { W = 0.5f }, 333).ToUint();
            Controller.GetElementByName("RedTowerSolid").Enabled = filled;
            if(filled)
            {
                Controller.GetElementByName("RedTowerSolid").Filled = true;
                Controller.GetElementByName("RedTowerSolid").color = (ColorRedTower with { W = 0.3f }).ToUint();
            }
        }

        private void TowerBlue(bool filled)
        {
            Controller.GetElementByName("RedTower").Enabled = false;
            Controller.GetElementByName("BlueTower").Enabled = true;
            Controller.GetElementByName("BlueTower").color = GradientColor.Get(ColorBlueTower, ColorBlueTower with { W = 0.5f }, 333).ToUint();
            Controller.GetElementByName("BlueTowerSolid").Enabled = filled;
            if(filled)
            {
                Controller.GetElementByName("BlueTowerSolid").Filled = true;
                Controller.GetElementByName("BlueTowerSolid").color = (ColorBlueTower with { W = 0.3f }).ToUint();
            }
        }

        private void TowerOff()
        {
            Controller.GetElementByName("RedTower").Enabled = false;
            Controller.GetElementByName("BlueTower").Enabled = false;
        }

        private static bool HasEffect(uint effect, float? remainingTile = null, IBattleChara? obj = null)
        {
            return (obj ?? Svc.ClientState.LocalPlayer).StatusList.Any(x => x.StatusId == effect && (remainingTile == null || x.RemainingTime < remainingTile));
        }

        // Resolve localized ReminderMessages text; supports string.Format placeholders.
        private string GetReminderText(string messageName, params object[] args)
        {
            var e = Controller.GetRegisteredLayouts().SafeSelect("ReminderMessages")?.GetElement(messageName);
            if(e == null) return messageName;
            return string.Format(e.overlayTextIntl.Get(e.overlayText), args);
        }

        private void Reminder(string? text, Vector4? color = null)
        {
            if(Controller.TryGetElementByName("Reminder", out var e))
            {
                if(text == null)
                {
                    e.Enabled = false;
                }
                else
                {
                    e.Enabled = true;
                    e.overlayText = text;
                }
                if(color != null)
                {
                    e.overlayBGColor = color.Value.ToUint();
                }
            }
        }

        private Element? GetAvoidElementByOID(uint oid)
        {
            foreach(var e in AvoidAlerts)
            {
                if(e.refActorObjectID == oid)
                {
                    return e;
                }
            }
            return null;
        }

        public override void OnSettingsDraw()
        {
            ImGui.Checkbox($"Highlight players contacting with which will result debuff to spread", ref Conf.EnableAvoiders);
            ImGui.Checkbox($"Display your defamation partner", ref Conf.EnableDefamationPartner);
            ImGui.SameLine();
            ImGui.Checkbox($"Tether to partner", ref Conf.EnableDefamationPartnerTether);
            ImGui.Checkbox($"Enable visual elements for towers", ref Conf.EnableVisualElementsTowers);
            ImGui.Checkbox($"Enable general mechanic overhead hints", ref Conf.EnableOverheadHintsGeneric);
            ImGui.Checkbox($"Enable rot picking overhead reminder", ref Conf.EnableRotPickerReminding);
            ImGui.Checkbox($"Enable tether break overhead reminder", ref Conf.EnableOverheadHintsTether);
        }
    }

    public class Config : IEzConfig
    {
        public bool EnableAvoiders = true;
        public bool EnableDefamationPartner = true;
        public bool EnableDefamationPartnerTether = false;
        public bool EnableVisualElementsTowers = true;
        public bool EnableOverheadHintsGeneric = true;
        public bool EnableOverheadHintsTether = true;
        public bool EnableRotPickerReminding = true;
    }
}
