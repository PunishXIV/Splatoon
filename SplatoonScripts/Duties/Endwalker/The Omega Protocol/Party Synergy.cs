using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Schedulers;
using ECommons.Throttlers;
using ImGuiNET;
using Lumina.Excel;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol;

public class Party_Synergy :SplatoonScript
{
    // public
    public override HashSet<uint> ValidTerritories => new() { 1122 };
    public override Metadata? Metadata => new(3, "NightmareXIV");

    public enum State
    {
        None = 0,
        PartySynergyCasting,
        PartySynergyCasted,
        OpticalLaserCasting,
        OpticalLaserCasted,
        DisChargeCasting
    }

    public class PartyListData
    {
        public string Name = "";
        public ulong ObjectId = 0u;
        public string PlayStationMarker = "";
        public string LeftRight = "";
        public string FarClose = "";
        public bool IsStacker = false;
    }

    public class BuffList
    {
        public const uint MidGlitch = 3427u;
        public const uint FarGlitch = 3428u;
    }

    public class CastID
    {
        public const uint PartySynergy = 31551u;
        public const uint OpticalLaser = 31521u;
        public const uint DisCharge = 31534u;
    }

    public class VfxID
    {
        public const string StackVFX = "vfx/lockon/eff/com_share2i.avfx";
        public const string ChainVFX = "vfx/lockon/eff/z3oz_firechain_0";
        public const string Circle = "vfx/lockon/eff/z3oz_firechain_01c.avfx";
        public const string Cross = "vfx/lockon/eff/z3oz_firechain_04c.avfx";
        public const string Triangle = "vfx/lockon/eff/z3oz_firechain_02c.avfx";
        public const string Square = "vfx/lockon/eff/z3oz_firechain_03c.avfx";
    }

    private TickScheduler? Sch = null;
    private Config Conf => Controller.GetConfig<Config>();
    private List<PartyListData> PartyList = new();
    private State state = State.None;
    private bool isLeftRightDecided = false;
    private PartyListData? myData = null;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("FarLeft", "{\"Enabled\":false,\"Name\":\"Left\",\"type\":1,\"offX\":2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"thicc\":5.0,\"overlayText\":\"Left\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
        Controller.RegisterElementFromCode("FarRight", "{\"Enabled\":false,\"Name\":\"Right\",\"type\":1,\"offX\":-2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278255615,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278252031,\"thicc\":5.0,\"overlayText\":\"Right\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");

        Controller.RegisterElementFromCode("CloseLeft", "{\"Enabled\":false,\"Name\":\"Left\",\"type\":1,\"offX\":2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"thicc\":5.0,\"overlayText\":\"Left\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
        Controller.RegisterElementFromCode("CloseRight", "{\"Enabled\":false,\"Name\":\"Bottom\",\"type\":1,\"offY\":15.5,\"radius\":1.0,\"color\":4278255615,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278252031,\"thicc\":5.0,\"overlayText\":\"Right\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
        Controller.RegisterElementFromCode("CloseMidLeftAdj", "{\"Enabled\":false,\"Name\":\"Left\",\"type\":1,\"offX\":2.5,\"offY\":13.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278190335,\"thicc\":5.0,\"overlayText\":\"Left\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");
        Controller.RegisterElementFromCode("CloseMidRightAdj", "{\"Enabled\":false,\"Name\":\"Bottom\",\"type\":1,\"offY\":15.5,\"radius\":1.0,\"color\":4278255615,\"overlayBGColor\":4278190080,\"overlayTextColor\":4278252031,\"thicc\":5.0,\"overlayText\":\"Right\",\"refActorDataID\":15713,\"refActorComparisonType\":3,\"includeRotation\":true,\"onlyVisible\":true,\"tether\":true}");

        // Close Position
        // The lowest numbers are closest to the eye.
        Controller.RegisterElementFromCode("CloseRightCircle", "{\"Name\":\"CloseRightCircle\",\"type\":1,\"offX\":-11.0,\"offY\":30.0,\"radius\":1.0,\"color\":4278190335,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseRightCross", "{\"Name\":\"CloseRightCross\",\"type\":1,\"offX\":-11.0,\"offY\":40.0,\"radius\":1.0,\"color\":4294967040,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseRightTriangle", "{\"Name\":\"CloseRightTriangle\",\"type\":1,\"offX\":-11.0,\"offY\":50.0,\"radius\":1.0,\"color\":4278255360,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseRightSquare", "{\"Name\":\"CloseRightSquare\",\"type\":1,\"offX\":-11.0,\"offY\":60.0,\"radius\":1.0,\"color\":4294902015,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseLeftCircle", "{\"Name\":\"CloseLeftCircle\",\"type\":1,\"offX\":11.0,\"offY\":30.0,\"radius\":1.0,\"color\":4278190335,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseLeftCross", "{\"Name\":\"CloseLeftCross\",\"type\":1,\"offX\":11.0,\"offY\":40.0,\"radius\":1.0,\"color\":4294967040,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseLeftTriangle", "{\"Name\":\"CloseLeftTriangle\",\"type\":1,\"offX\":11.0,\"offY\":50.0,\"radius\":1.0,\"color\":4278255360,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("CloseLeftSquare", "{\"Name\":\"CloseLeftSquare\",\"type\":1,\"offX\":11.0,\"offY\":60.0,\"radius\":1.0,\"color\":4294902015,\"Filled\":false,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        // Far Position
        // The lowest numbers are closest to the eye.
        Controller.RegisterElementFromCode("FarRightCircle", "{\"Name\":\"FarRightCircle\",\"type\":1,\"offX\":-11.0,\"offY\":30.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarRightCross", "{\"Name\":\"FarRightCross\",\"type\":1,\"offX\":-19.0,\"offY\":40.0,\"radius\":1.0,\"color\":4294967040,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarRightTriangle", "{\"Name\":\"FarRightTriangle\",\"type\":1,\"offX\":-19.0,\"offY\":50.0,\"radius\":1.0,\"color\":4278255360,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarRightSquare", "{\"Name\":\"FarRightSquare\",\"type\":1,\"offX\":-11.0,\"offY\":60.0,\"radius\":1.0,\"color\":4294902015,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarLeftSquare", "{\"Name\":\"FarLeftSquare\",\"type\":1,\"offX\":11.0,\"offY\":30.0,\"radius\":1.0,\"color\":4294902015,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarLeftTriangle", "{\"Name\":\"FarLeftTriangle\",\"type\":1,\"offX\":19.0,\"offY\":50.0,\"radius\":1.0,\"color\":4294967040,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarLeftCross", "{\"Name\":\"FarLeftCross\",\"type\":1,\"offX\":19.0,\"offY\":40.0,\"radius\":1.0,\"color\":4278255360,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("FarLeftCircle", "{\"Name\":\"FarLeftCircle\",\"type\":1,\"offX\":11.0,\"offY\":60.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorNPCNameID\":7640,\"refActorComparisonType\":6,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (Conf.DecideLeftRight)
        {
            OnVFXSpawnDesideByPriority(target, vfxPath);
        }
        else
        {
            OnVFXSpawnDesideSwapByPos(target, vfxPath);
        }
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (source.GetObject() is var sourceObj && sourceObj == null)
            return;

        // Party Synergy
        if (castId == CastID.PartySynergy && TryGetPriorityList(out var list))
        {
            PluginLog.Information($"PartySynergy Casting");
            state = State.PartySynergyCasting;
            PartyList.Clear();

            foreach (var (name, index) in list.Select((value, i) => (value, i)))
            {
                var obj = Svc.Objects.FirstOrDefault(o => o is IPlayerCharacter pc && pc.Name.ToString() == name);
                if (obj != null)
                {
                    PartyList.Add(new PartyListData
                    {
                        Name = name,
                        ObjectId = obj.GameObjectId,
                        PlayStationMarker = ""
                    });
                }
            }

            if (PartyList.Count != 8)
            {
                DuoLog.Warning("Could not find all party members.");
                OnReset();
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if ((state == State.None) || set.Action == null || set.Source == null)
            return;

        if ((set.Source.ObjectKind != ObjectKind.BattleNpc) && (set.Source.ObjectKind != ObjectKind.EventNpc))
            return;

        // Party Synergy
        if (set.Action.RowId == CastID.PartySynergy)
        {
            state = State.PartySynergyCasted;
        }
        else if (set.Action.RowId == CastID.OpticalLaser)
        {
            state = State.OpticalLaserCasted;
        }
        else if (set.Action.RowId == CastID.DisCharge)
        {
            state = State.None;
            HideAll();
        }
    }

    public override void OnUpdate()
    {
        if ((state == State.None) || (PartyList.Count(x => x.PlayStationMarker != "") != 8) || Svc.ClientState.LocalPlayer == null)
            return;

        if (!isLeftRightDecided)
        {
            // Set Far/Close
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == BuffList.FarGlitch))
            {
                foreach (var row in PartyList)
                {
                    row.FarClose = "Far";
                }
            }
            else
            {
                foreach (var row in PartyList)
                {
                    row.FarClose = "Close";
                }
            }

            // Set Left/Right
            List<PartyListData> fetchedList = new();
            foreach (var row in PartyList)
            {
                if (fetchedList.Any(x => x.PlayStationMarker == row.PlayStationMarker))
                {
                    row.LeftRight = "Left";
                }
                else
                {
                    row.LeftRight = "Right";
                }

                fetchedList.Add(row);
                isLeftRightDecided = true;
                myData = PartyList.FirstOrDefault(x => x.ObjectId == Svc.ClientState.LocalPlayer.GameObjectId);
            }
            return;
        }

        if (state == State.PartySynergyCasted)
        {
            string[] psStrings = { "Circle", "Cross", "Triangle", "Square" };
            string[] leftRightStrings = { "Left", "Right" };

            HideAll();
            foreach (var ps in psStrings)
            {
                foreach (var lr in leftRightStrings)
                {
                    Controller.GetElementByName($"{myData!.FarClose}{lr}{ps}").Enabled = true;

                    if (myData!.PlayStationMarker == ps && myData!.LeftRight == lr)
                    {
                        Controller.GetElementByName($"{myData!.FarClose}{lr}{ps}").tether = true;
                    }
                }
            }

            state = State.OpticalLaserCasting;
        }
        else if (state == State.OpticalLaserCasted)
        {
            if (PartyList.Count(x => x.IsStacker == true) != 2)
                return;
            if (Svc.Objects.FirstOrDefault(x => x is ICharacter c && c.NameId == 7640) is var opticalUnit && opticalUnit == null)
                return;
            if (AttachedInfo.VFXInfos.Where(
                x => x.Value.Any(z => z.Key == VfxID.StackVFX && z.Value.Age < 1000))
                .Select(x => x.Key)
                .Select(x => Svc.Objects.FirstOrDefault(z => z.Address == x))
                .ToArray() is var stackers && stackers == null)
                return;
            if (stackers.OrderBy(x => Vector3.Distance(opticalUnit.Position, x.Position))
                .ToArray()[Conf.ReverseAdjust ? 0 : 1] is var swapper && swapper == null)
                return;

            // If Stacker's Left and Right are not the same, display them as is.
            PartyListData? SwapStacker = PartyList.Where(x => x.IsStacker == true && x.ObjectId == swapper.GameObjectId).FirstOrDefault();
            if (SwapStacker == null)
                return;
            PartyListData? OtherStacker = PartyList.Where(x => x.IsStacker == true && x.ObjectId != swapper.GameObjectId).FirstOrDefault();
            if (OtherStacker == null)
                return;
            PartyListData? myData = PartyList.FirstOrDefault(x => x.ObjectId == Svc.ClientState.LocalPlayer.GameObjectId);
            if (myData == null)
                return;

            if (SwapStacker.LeftRight != OtherStacker.LeftRight)
            {
                HideAll();
                if (Conf.PrintPreciseResultInChat)
                    DuoLog.Information($"No swap, go {myData.LeftRight.ToLower()}");
            }
            else
            {
                PartyListData? NoneVfxSwaper = PartyList.Where(
                    x => x.IsStacker == false &&
                    x.PlayStationMarker == SwapStacker.PlayStationMarker).FirstOrDefault();
                if (NoneVfxSwaper == null)
                    return;


                String leftRightTmp = SwapStacker.LeftRight;
                SwapStacker.LeftRight = NoneVfxSwaper.LeftRight;
                NoneVfxSwaper.LeftRight = leftRightTmp;

                if (Conf.PrintPreciseResultInChat)
                    DuoLog.Warning($"Swapping! \n{SwapStacker.Name}\n{NoneVfxSwaper.Name}\n============");

                if (Svc.ClientState.LocalPlayer.GameObjectId.EqualsAny(SwapStacker.ObjectId, NoneVfxSwaper.ObjectId))
                {
                    new TimedMiddleOverlayWindow("swaponYOU", 10000, () =>
                    {
                        ImGui.SetWindowFontScale(2f);
                        ImGuiEx.Text(ImGuiColors.DalamudRed, $"Stack swap position!\n\n  {SwapStacker.Name} \n  {NoneVfxSwaper.Name}\n Go {myData.LeftRight}");
                    }, 150);
                }
            }

            HideAll();
            if (Conf.ExplicitTether)
            {
                PluginLog.Information($"FarLeft: {Conf.IsRightAdjustKnokback}");
                if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == BuffList.FarGlitch))
                {
                    Controller.GetElementByName($"FarLeft").Enabled = true;
                    Controller.GetElementByName($"FarRight").Enabled = true;

                    if (myData!.LeftRight == "Left")
                    {
                        Controller.GetElementByName($"FarLeft").tether = true;
                    }
                    else
                    {
                        Controller.GetElementByName($"FarRight").tether = true;
                    }
                }
                else
                {
                    PluginLog.Information($"CloseLeft: {Conf.IsRightAdjustKnokback}");
                    if (Conf.IsRightAdjustKnokback)
                    {
                        Controller.GetElementByName($"CloseLeft").Enabled = true;
                        Controller.GetElementByName($"CloseMidRightAdj").Enabled = true;

                        if (myData!.LeftRight == "Left")
                        {
                            Controller.GetElementByName($"CloseLeft").tether = true;
                        }
                        else
                        {
                            Controller.GetElementByName($"CloseMidRightAdj").tether = true;
                        }
                    }
                    else
                    {
                        Controller.GetElementByName($"CloseRight").Enabled = true;
                        Controller.GetElementByName($"CloseMidLeftAdj").Enabled = true;

                        if (myData!.LeftRight == "Left")
                        {
                            Controller.GetElementByName($"CloseMidLeftAdj").tether = true;
                        }
                        else
                        {
                            Controller.GetElementByName($"CloseRight").tether = true;
                        }
                    }
                }
            }

            state = State.OpticalLaserCasting;
        }

    }

    public override void OnReset()
    {
        if (Sch != null)
        {
            Sch.Dispose();
            Sch = null;
        }
        HideAll();
        PartyList.Clear();
        PartyList = new();
        state = State.None;
        isLeftRightDecided = false;
        myData = null;
    }

    public class Config :IEzConfig
    {
        public bool Debug = false;
        public bool DecideLeftRight = false;
        public List<string[]> LeftRightPriorities = new();
        public bool ReverseAdjust = false;
        public bool PrintPreciseResultInChat = false;
        public bool ExplicitTether = false;
        public bool IsRightAdjustKnokback = false;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("# Use the left/right judgement function and be guided by the tether.");
        ImGui.Text("NOTE: If this function is not turned on, only knockback adjustments will be displayed.");
        ImGui.Indent();
        ImGui.Checkbox("Decide left/right Function", ref Conf.DecideLeftRight);
        ImGui.Unindent();

        ImGui.Text("# Adjustment considering eye distance for biased knockback.");
        ImGui.Indent();
        if (ImGui.RadioButton("Furthest from eye adjusts", !Conf.ReverseAdjust))
            Conf.ReverseAdjust = false;
        if (ImGui.RadioButton("Closest to eye adjusts", Conf.ReverseAdjust))
            Conf.ReverseAdjust = true;
        ImGui.Unindent();

        ImGui.Text("# Adjustment Middle Position for Close knockback.");
        ImGui.Indent();
        if (ImGui.RadioButton("AdjustmentLeft", !Conf.IsRightAdjustKnokback))
            Conf.IsRightAdjustKnokback = false;
        if (ImGui.RadioButton("AdjustmentRight", Conf.IsRightAdjustKnokback))
            Conf.IsRightAdjustKnokback = true;
        ImGui.Unindent();

        ImGui.Dummy(new(0f, 20f));
        ImGui.Checkbox($"Print in chat info about not your adjusts", ref Conf.PrintPreciseResultInChat);
        if (!Conf.DecideLeftRight)
        {
            ImGui.Checkbox($"Explicit position tether (unfinished feature, supports right side adjust only)", ref Conf.ExplicitTether);
        }
        else
        {
            ImGui.Checkbox($"Explicit position tether", ref Conf.ExplicitTether);
        }

        if (Conf.DecideLeftRight)
        {
            ImGui.Text("# How to determine left/right priority : ");
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
            for (int i = 0; i < Conf.LeftRightPriorities.Count; i++)
            {
                if (DrawPrioList(i))
                {
                    toRem = i;
                }
            }
            if (toRem != -1)
            {
                Conf.LeftRightPriorities.RemoveAt(toRem);
            }
            if (ImGui.Button("Create new priority list"))
            {
                Conf.LeftRightPriorities.Add(new string[] { "", "", "", "", "", "", "", "" });
            }
        }

        if (ImGui.CollapsingHeader("Debug"))
        {
            var opticalUnit = Svc.Objects.FirstOrDefault(x => x is ICharacter c && c.NameId == 7640);
            ImGui.Text($"State: {state}");
            if (opticalUnit != null)
            {
                var mid = MathHelper.GetRelativeAngle(new(100, 100), opticalUnit.Position.ToVector2());
                ImGuiEx.Text($"Mid: {mid}");
                foreach (var x in Svc.Objects)
                {
                    if (x is IPlayerCharacter pc)
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
            List<ImGuiEx.EzTableEntry> Entries = [];
            foreach (var x in PartyList)
            {
                Entries.Add(new("Name", true, () => ImGui.Text(x.Name.ToString())));
                Entries.Add(new("ObjectId", () => ImGui.Text(x.ObjectId.ToString())));
                Entries.Add(new("PlayStationMarker", () => ImGui.Text(x.PlayStationMarker.ToString())));
                Entries.Add(new("LeftRight", () => ImGui.Text(x.LeftRight.ToString())));
                Entries.Add(new("FarClose", () => ImGui.Text(x.FarClose.ToString())));
                Entries.Add(new("IsStacker", () => ImGui.Text(x.IsStacker.ToString())));
            }
            ImGuiEx.EzTable(Entries);
        }
    }

    private void OnVFXSpawnDesideSwapByPos(uint target, string vfxPath)
    {
        //Dequeued message: VFX vfx/lockon/eff/com_share2i.avfx
        if (vfxPath == VfxID.StackVFX && Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId.EqualsAny<uint>(3427, 3428)))
        {
            var stackers = AttachedInfo.VFXInfos.Where(x => x.Value.Any(z => z.Key == VfxID.StackVFX && z.Value.Age < 1000)).Select(x => x.Key).Select(x => Svc.Objects.FirstOrDefault(z => z.Address == x)).ToArray();
            var opticalUnit = Svc.Objects.FirstOrDefault(x => x is ICharacter c && c.NameId == 7640);
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
                    if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == BuffList.FarGlitch))
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
                if ((a1 > 180 && a2 > 180) || (a1 < 180 && a2 < 180))
                {
                    //DuoLog.Information($"Swap!");
                    var swapper = stackers.OrderBy(x => Vector3.Distance(opticalUnit.Position, x.Position)).ToArray()[Conf.ReverseAdjust ? 0 : 1];
                    var swappersVfx = AttachedInfo.VFXInfos[swapper.Address].FirstOrDefault(x => x.Key.Contains(VfxID.ChainVFX) && x.Value.AgeF < 60).Key;
                    //DuoLog.Information($"Swapper: {swapper} Swapper's vfx: {swappersVfx}");
                    var secondSwapper = AttachedInfo.VFXInfos.Where(x => x.Key != swapper.Address && x.Value.Any(z => z.Key.Contains(swappersVfx) && z.Value.AgeF < 60)).Select(x => x.Key).Select(x => Svc.Objects.FirstOrDefault(z => z.Address == x)).FirstOrDefault();
                    //DuoLog.Information($"Second swapper: {secondSwapper}");
                    if (Conf.PrintPreciseResultInChat)
                        DuoLog.Warning($"Swapping! \n{swapper.Name}\n{secondSwapper?.Name}\n============");
                    if (Svc.ClientState.LocalPlayer.Address.EqualsAny(swapper.Address, secondSwapper.Address))
                    {
                        HideAll();
                        if (Conf.ExplicitTether)
                        {
                            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == BuffList.FarGlitch))
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
                    if (Conf.PrintPreciseResultInChat)
                        DuoLog.Information($"No swap, go {(myAngle > 180 ? "right" : "left")}");
                }
            }
        }
    }

    private void OnVFXSpawnDesideByPriority(uint target, string vfxPath)
    {
        if (state == State.None)
            return;

        if (target.GetObject() is var targetObj && targetObj == null)
            return;

        if (targetObj is IPlayerCharacter pc && new[] { VfxID.Circle, VfxID.Cross, VfxID.Triangle, VfxID.Square }.Contains(vfxPath))
        {
            var me = PartyList.FirstOrDefault(x => x.ObjectId == target);
            if (me == null)
                return;

            me.PlayStationMarker = vfxPath switch
            {
                VfxID.Circle => "Circle",
                VfxID.Cross => "Cross",
                VfxID.Triangle => "Triangle",
                VfxID.Square => "Square",
                _ => ""
            };
        }
        else
        {
            if (vfxPath != VfxID.StackVFX)
                return;

            if (PartyList.FirstOrDefault(x => x.ObjectId == target) is var rowData && rowData == null)
                return;

            rowData.IsStacker = true;
        }
    }

    private bool TryGetPriorityList([NotNullWhen(true)] out string[]? values)
    {
        foreach (var p in Conf.LeftRightPriorities)
        {
            var valid = true;
            var l = FakeParty.Get().Select(x => x.Name.ToString()).ToHashSet();
            foreach (var x in p)
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

    private bool DrawPrioList(int num)
    {
        var prio = Conf.LeftRightPriorities[num];
        ImGuiEx.Text($"# Priority list {num + 1}");
        ImGui.PushID($"prio{num}");
        ImGuiEx.Text($"    Omega Female");
        for (int i = 0; i < prio.Length; i++)
        {
            ImGui.PushID($"prio{num}element{i}");
            ImGui.SetNextItemWidth(200);
            ImGui.InputText($"Player {i + 1}", ref prio[i], 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo("##partysel", "Select from party"))
            {
                foreach (var x in FakeParty.Get())
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
        ImGuiEx.Text($"    Omega Male");
        if (ImGui.Button("Delete this list (ctrl+click)") && ImGui.GetIO().KeyCtrl)
        {
            return true;
        }
        ImGui.PopID();
        return false;
    }

    private void HideAll()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        Controller.GetRegisteredElements().Each(x => x.Value.tether = false);
    }
}
