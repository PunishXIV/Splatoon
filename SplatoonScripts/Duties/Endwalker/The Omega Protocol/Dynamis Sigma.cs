using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Newtonsoft.Json;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Endwalker.The_Omega_Protocol
{
    public unsafe class Dynamis_Sigma : SplatoonScript
    {
        public override HashSet<uint> ValidTerritories => new() { 1122 };

        public override Metadata? Metadata => new(6, "NightmareXIV");

        public const uint TowerSingle = 2013245;
        public const uint TowerDual = 2013246;
        public const uint TowerAny = 2013244;

        public const uint GlitchFar = 3428;
        public const uint GlitchClose = 3427;

        public class Headmarkers
        {
            public const string Playstation = "vfx/lockon/eff/z3oz_firechain_";
            public const string BlueCross = "vfx/lockon/eff/z3oz_firechain_04c.avfx";
            public const string PurpleSquare = "vfx/lockon/eff/z3oz_firechain_03c.avfx";
            public const string RedCircle = "vfx/lockon/eff/z3oz_firechain_01c.avfx";
            public const string GreenTriangle = "vfx/lockon/eff/z3oz_firechain_02c.avfx";

            public const string Marker = "vfx/lockon/eff/lockon8_t0w.avfx";
        }

        MarkingController* MKC;
        Vector3 OmegaPos = Vector3.Zero;
        Dictionary<uint, ChainMarker> Chains = new();
        HashSet<uint> Markers = new();
        List<string> State = new();
        string MyMarker = "";
        bool isLeft;
        bool isUp;
        long StopRegisteringAt;
        bool announced = false;

        Config Conf => Controller.GetConfig<Config>();

        GameObject[] GetTowers() => Svc.Objects.Where(x => x.DataId.EqualsAny<uint>(TowerSingle, TowerDual)).ToArray();

        public override void OnSetup()
        {
            MKC = MarkingController.Instance();
            for (int i = 0; i < 6; i++)
            {
                Controller.RegisterElementFromCode($"{i}", "{\"Enabled\":false,\"Name\":\"\",\"radius\":2.5,\"Donut\":0.5,\"color\":4278255615,\"overlayBGColor\":4110417920,\"overlayTextColor\":4278255615,\"overlayFScale\":2.0,\"overlayPlaceholders\":true,\"thicc\":4.0,\"overlayText\":\"L1\\\\nL2\",\"refActorDataID\":2013244,\"refActorComparisonType\":3}");
            }
            Controller.RegisterElementFromCode("BlueCrossR", "{\"Name\":\"cross right\",\"type\":1,\"Enabled\":false,\"offX\":-3.0,\"offY\":1.0,\"radius\":1.0,\"color\":4294967040,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("BlueCrossL", "{\"Name\":\"cross left\",\"type\":1,\"Enabled\":false,\"offX\":3.0,\"offY\":1.0,\"radius\":1.0,\"color\":4294967040,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("PurpleSquareR", "{\"Name\":\"square right\",\"type\":1,\"Enabled\":false,\"offX\":-3.0,\"offY\":3.5,\"radius\":1.0,\"color\":4294902015,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("PurpleSquareL", "{\"Name\":\"square left\",\"type\":1,\"Enabled\":false,\"offX\":3.0,\"offY\":3.5,\"radius\":1.0,\"color\":4294902015,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("RedCircleR", "{\"Name\":\"circle right\",\"type\":1,\"Enabled\":false,\"offX\":-3.0,\"offY\":6.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("RedCircleL", "{\"Name\":\"circle left\",\"type\":1,\"Enabled\":false,\"offX\":3.0,\"offY\":6.0,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("GreenTriangleR", "{\"Name\":\"triangle right\",\"type\":1,\"Enabled\":false,\"offX\":-3.0,\"offY\":8.5,\"radius\":1.0,\"color\":4278255360,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("GreenTriangleL", "{\"Name\":\"triangle left\",\"type\":1,\"Enabled\":false,\"offX\":3.0,\"offY\":8.5,\"radius\":1.0,\"color\":4278255360,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");

            Controller.RegisterElementFromCode("BlueCrossU", "{\"Name\":\"cross up\",\"type\":1,\"Enabled\":false,\"offX\":4.5,\"offY\":3.5,\"radius\":1.0,\"color\":4294967040,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("BlueCrossD", "{\"Name\":\"cross down\",\"type\":1,\"Enabled\":false,\"offX\":4.5,\"offY\":8.5,\"radius\":1.0,\"color\":4294967040,\"overlayBGColor\":0,\"overlayTextColor\":4294967040,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("PurpleSquareU", "{\"Name\":\"square up\",\"type\":1,\"Enabled\":false,\"offX\":1.5,\"offY\":3.5,\"radius\":1.0,\"color\":4294902015,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("PurpleSquareD", "{\"Name\":\"square down\",\"type\":1,\"Enabled\":false,\"offX\":1.5,\"offY\":8.5,\"radius\":1.0,\"color\":4294902015,\"overlayBGColor\":0,\"overlayTextColor\":4294902015,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("RedCircleU", "{\"Name\":\"circle up\",\"type\":1,\"Enabled\":false,\"offX\":-1.5,\"offY\":3.5,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("RedCircleD", "{\"Name\":\"circle down\",\"type\":1,\"Enabled\":false,\"offX\":-1.5,\"offY\":8.5,\"radius\":1.0,\"color\":4278190335,\"overlayBGColor\":0,\"overlayTextColor\":4278190335,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("GreenTriangleU", "{\"Name\":\"triangle up\",\"type\":1,\"Enabled\":false,\"offX\":-4.5,\"offY\":3.5,\"radius\":1.0,\"color\":4278255360,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");
            Controller.RegisterElementFromCode("GreenTriangleD", "{\"Name\":\"triangle down\",\"type\":1,\"Enabled\":false,\"offX\":-4.5,\"offY\":8.5,\"radius\":1.0,\"color\":4278255360,\"overlayBGColor\":0,\"overlayTextColor\":4278255360,\"overlayFScale\":2.0,\"thicc\":5.0,\"overlayText\":\"\",\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true}");

            Controller.RegisterElementFromCode($"Far{Directions.Front}", "{\"Name\":\"\",\"type\":1,\"offY\":1.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.Bottom}", "{\"Name\":\"\",\"type\":1,\"offY\":39.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.Left}", "{\"Name\":\"\",\"type\":1,\"offX\":19.0,\"offY\":20.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.Right}", "{\"Name\":\"\",\"type\":1,\"offX\":-19.0,\"offY\":20.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.FrontLeft}", "{\"Name\":\"\",\"type\":1,\"offX\":13.5,\"offY\":6.5,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.FrontRight}", "{\"Name\":\"\",\"type\":1,\"offX\":-13.5,\"offY\":6.5,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.BottomLeft}", "{\"Name\":\"\",\"type\":1,\"offX\":13.5,\"offY\":33.5,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Far{Directions.BottomRight}", "{\"Name\":\"\",\"type\":1,\"offX\":-13.5,\"offY\":33.5,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");

            Controller.RegisterElementFromCode($"Close{Directions.Front}", "{\"Name\":\"\",\"type\":1,\"offY\":8.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.Bottom}", "{\"Name\":\"\",\"type\":1,\"offY\":31.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.Left}", "{\"Name\":\"\",\"type\":1,\"offX\":11.0,\"offY\":20.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.Right}", "{\"Name\":\"\",\"type\":1,\"offX\":-11.0,\"offY\":20.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.FrontLeft}", "{\"Name\":\"\",\"type\":1,\"offX\":8.0,\"offY\":12.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.FrontRight}", "{\"Name\":\"\",\"type\":1,\"offX\":-8.0,\"offY\":12.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.BottomLeft}", "{\"Name\":\"\",\"type\":1,\"offX\":8.0,\"offY\":28.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");
            Controller.RegisterElementFromCode($"Close{Directions.BottomRight}", "{\"Name\":\"\",\"type\":1,\"offX\":-8.0,\"offY\":28.0,\"radius\":1.0,\"color\":4278255615,\"thicc\":4.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");

            Controller.RegisterElementFromCode("MaleFinder", "{\"Name\":\"OmegaM\",\"type\":1,\"Enabled\":false,\"radius\":0.0,\"color\":4278255615,\"thicc\":5.0,\"refActorDataID\":15720,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true}");

            Controller.RegisterElementFromCode("TetherToCenter", "{\"Name\":\"\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":0.0,\"color\":4278255615,\"thicc\":4.0,\"tether\":true}");
            Controller.RegisterElementFromCode("TowerKB", "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":0.0,\"color\":4278190335,\"thicc\":7.0,\"tether\":true}");

            base.OnSetup();
        }

        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            if(Controller.Scene == 6)
            {
                if (vfxPath.StartsWith(Headmarkers.Playstation))
                {
                    Chains[target] = GetChainMarker(vfxPath);
                }
                else if(vfxPath == Headmarkers.Marker)
                {
                    Markers.Add(target);
                    StopRegisteringAt = Environment.TickCount64 + 1500;
                }
            }
        }

        internal void ApplyMarkerPhaseNorthToSouth(GameObject omega, PlayerCharacter partner, bool first, string glitch) {
            var rota = (MathHelper.GetRelativeAngle(omega.Position, Svc.ClientState.LocalPlayer.Position) + omega.Rotation.RadToDeg()) % 360;
            var rotaPar = (MathHelper.GetRelativeAngle(omega.Position, partner.Position) + omega.Rotation.RadToDeg()) % 360;
            if (Environment.TickCount64 < StopRegisteringAt) isLeft = rota > rotaPar;
            if (isLeft) {
                //left
                if (first) {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerFirstLeft]}").Enabled = true;
                } else {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerSecondLeft]}").Enabled = true;
                }
            } else {
                //right
                if (first) {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerFirstRight]}").Enabled = true;
                } else {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerSecondRight]}").Enabled = true;
                }
            }
            State.Add($"Your rotation: {rota}, partner rotation: {rotaPar}");
        }

        internal void ApplyMarkerPhaseWestToEast(GameObject omega, PlayerCharacter partner, bool first, string glitch) {
            var relativeY = Dynamis_Sigma_Utils.GetRelativePosition(omega.Position, Svc.ClientState.LocalPlayer.Position,
                omega.Rotation).Y;
            var partnerRelativeY =
                Dynamis_Sigma_Utils.GetRelativePosition(omega.Position, partner.Position, omega.Rotation).Y;
            if (Environment.TickCount64 < StopRegisteringAt) isUp = relativeY < partnerRelativeY;
            if (isUp) {
                // left
                if (first) {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerFirstUp]}").Enabled = true;
                } else {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerSecondUp]}").Enabled = true;
                }
            } else {
                // right
                if (first) {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerFirstDown]}").Enabled = true;
                } else {
                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.DoubleMarkerSecondDown]}").Enabled = true;
                }
            }
        }

        public override void OnUpdate()
        {
            Off();
            State.Clear();
            if (Controller.Scene == 6)
            {
                State.Add("In scene 6");
                if (GetTowers().Length.EqualsAny(5, 6))
                {
                    State.Add($"Tower phase, yours is {MyMarker}");
                    if (!announced)
                    {
                        announced = true;
                        if(!Conf.NoChat) DuoLog.Information(IsInverted() ? "Inverted pattern" : "Default pattern");
                    }
                    var towers = GetTowers().OrderBy(x => GetTowerAngle(x, IsInverted())).ToArray();
                    Queue<string> enumeration = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == GlitchFar) ? new(Conf.FarTowers) : new(Conf.CloseTowers);
                    for (int i = 0; i < towers.Length; i++)
                    {
                        if (towers[i].DataId == TowerSingle)
                        {
                            var e = enumeration.Dequeue();
                            SetTowerAs(i, towers[i], MyMarker.EqualsIgnoreCase(e.GetFirstLetter()), e);
                        }
                        else
                        {
                            var e1 = enumeration.Dequeue();
                            var e2 = enumeration.Dequeue();
                            SetTowerAs(i, towers[i], MyMarker.EqualsIgnoreCaseAny(e1.GetFirstLetter(), e2.GetFirstLetter()), e1, e2);
                        }
                    }
                }
                else
                {
                    if (Markers.Count == 6)
                    {
                        var glitch = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == GlitchFar) ? "Far" : "Close";
                        State.Add("Markers phase");
                        State.Add($"Markers: {Markers.Select(x => x.GetObject()).Print()}");
                        var omega = Svc.Objects.FirstOrDefault(x => x.DataId == 15720);
                        State.Add($"Omega-M is {omega}");
                        var partner = GetPartner();
                        if(Markers.Contains(Svc.ClientState.LocalPlayer.ObjectId) && Markers.Contains(partner.ObjectId))
                        {
                            State.Add("You and your partners are markers");
                            //both are markers
                            bool first = true;
                            foreach(var x in Conf.MarkerOrder)
                            {
                                State.Add($"Checking {x}...");
                                if (x == Chains[Svc.ClientState.LocalPlayer.ObjectId]) break;
                                State.Add(Chains.Where(c => c.Value == x).Select(z => $"{z.Key.GetObject()}/{Markers.Contains(z.Key)}").Print());
                                if(Chains.Where(c => c.Value == x).All(z => Markers.Contains(z.Key)))
                                {
                                    State.Add($"Not first because {x} are same");
                                    first = false;
                                }
                            }
                            State.Add($"You are {(first?"first":"second")}");
                            switch (Conf.AlignmentDirection) {
                                case MarkerAlignmentDirection.NorthToSouth:
                                    ApplyMarkerPhaseNorthToSouth(omega, partner, first, glitch);
                                    break;
                                case MarkerAlignmentDirection.WestToEast:
                                    ApplyMarkerPhaseWestToEast(omega, partner, first, glitch);
                                    break;
                            }
                        }
                        else
                        {
                            State.Add($"Only one of you or your partners are markers");
                            //single marker
                            bool first = true;
                            foreach (var x in Conf.MarkerOrder)
                            {
                                State.Add($"Checking {x}...");
                                if (x == Chains[Svc.ClientState.LocalPlayer.ObjectId]) break;
                                if (Chains.Where(c => c.Value == x).Count(z => Markers.Contains(z.Key)) == 1)
                                {
                                    State.Add($"Not first because {x} are same");
                                    first = false;
                                }
                            }
                            State.Add($"You are {(first ? "first" : "second")}");
                            var marked = Markers.Contains(Svc.ClientState.LocalPlayer.ObjectId);
                            State.Add($"You are marked: {marked}");
                            if (marked)
                            {
                                if (first)
                                {
                                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.SingleMarkerFirstMarked]}").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.SingleMarkerSecondMarked]}").Enabled = true;
                                }
                            }
                            else
                            {
                                if (first)
                                {
                                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.SingleMarkerFirstUnmarked]}").Enabled = true;
                                }
                                else
                                {
                                    Controller.GetElementByName($"{glitch}{Conf.DirectionsSpots[Position.SingleMarkerSecondUnmarked]}").Enabled = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if(Chains.Count == 8 && Vector3.Distance(new(100,0,100), Svc.Objects.FirstOrDefault(x => x.DataId == 15720).Position) > 10)
                        {
                            State.Add("Omega-M found");
                            if (Conf.AlignmentDirection == MarkerAlignmentDirection.NorthToSouth) {
                                var i = 1f;
                                foreach (var x in Conf.MarkerOrder) {
                                    if (Controller.TryGetElementByName($"{x}L", out var l) && Controller.TryGetElementByName($"{x}R", out var r)) {
                                        l.offY = i;
                                        r.offY = i;
                                        l.Enabled = true;
                                        r.Enabled = true;
                                        //l.tether = Chains[Svc.ClientState.LocalPlayer.ObjectId] == x;
                                        //r.tether = Chains[Svc.ClientState.LocalPlayer.ObjectId] == x;
                                    }
                                    i += 3f;
                                }
                            }
                            else if (Conf.AlignmentDirection == MarkerAlignmentDirection.WestToEast) {
                                var i = 4.5f;
                                foreach (var x in Conf.MarkerOrder) {
                                    if (Controller.TryGetElementByName($"{x}U", out var u) &&
                                        Controller.TryGetElementByName($"{x}D", out var d)) {
                                        u.offX = i;
                                        d.offX = i;
                                        u.Enabled = true;
                                        d.Enabled = true;
                                    }
                                    i -= 3f;
                                }
                            }
                            Controller.GetElementByName("MaleFinder").Enabled = true;
                        }
                    }
                }
            }
            else
            {
                Markers.Clear();
                Chains.Clear();
                announced = false;
            }
        }

        PlayerCharacter GetPartner()
        {
            return FakeParty.Get().First(x => x.ObjectId != Svc.ClientState.LocalPlayer.ObjectId && Chains[x.ObjectId] == Chains[Svc.ClientState.LocalPlayer.ObjectId]);
        }

        public override void OnActionEffect(uint ActionID, ushort animationID, ActionEffectType type, uint sourceID, ulong targetOID, uint damage)
        {
            if (Controller.Scene == 6)
            {
                if (ActionID == 31603)
                {
                    OmegaPos = Svc.Objects.FirstOrDefault(x => x.DataId == 15720)?.Position ?? Vector3.Zero;
                    if (!Conf.NoChat) DuoLog.Information($"Omega position captured: {OmegaPos}");
                    var distance = float.MaxValue;
                    var marker = "A";
                    for (int i = 0; i < MkNum.Length; i++)
                    {
                        var d = MKC->FieldMarkerArraySpan[i].GetPositon().GetDistanceToWaymark() ;
                        if (d < distance)
                        {
                            marker = MkNum[i];
                            distance = d;
                        }
                    }
                    MyMarker = marker;
                    if (!Conf.NoChat) DuoLog.Information($"You are marker {marker}!");
                    Chains.Clear();
                    Markers.Clear();
                }
                else if(ActionID == 32788 || ActionID == 31492)
                {
                    //DuoLog.Information($"Starting sigma");
                    Chains.Clear();
                    Markers.Clear();
                }
            }
        }

        void Off()
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }

        void SetTowerAs(int tower, GameObject obj, bool tether, params string[] s)
        {
            if (Controller.TryGetElementByName($"{tower}", out var t))
            {
                t.Enabled = true;
                t.SetRefPosition(obj.Position);
                t.overlayText = s.Join("\n") + (Conf.Angle ? $"\n{GetTowerAngle(obj)}/{GetTowerAngle(obj, IsInverted())}" : "");
                t.tether = tether && Conf.TetherDirect && Conf.RememberMarker;
                if(tether && Conf.RememberMarker && !Conf.TetherDirect)
                {
                    Controller.GetElementByName("TetherToCenter").Enabled = true;
                    if(Controller.TryGetElementByName("TowerKB", out var c))
                    {
                        c.SetOffPosition(obj.Position);
                        c.Enabled = true;
                    }
                }
            }
            else
            {
                DuoLog.Error($"Could not obtain element {tower}");
            }
        }

        float GetTowerAngle(GameObject t, bool inverted = false)
        {
            var z = new Vector3(100, 0, 100);
            var angle = (MathHelper.GetRelativeAngle(z, t.Position) + (inverted ? 181 : 1) + 360 - MathHelper.GetRelativeAngle(z, OmegaPos)) % 360;
            return angle;
        }

        bool IsInverted()
        {
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == GlitchFar))
            {
                return !GetTowers().Any(x => GetTowerAngle(x) < 3);
            }
            else
            {
                return !GetTowers().Any(x => GetTowerAngle(x).InRange(90, 90 + 45));
            }
        }

        ChainMarker GetChainMarker(string s)
        {
            if (s == Headmarkers.RedCircle) return ChainMarker.RedCircle;
            if (s == Headmarkers.BlueCross) return ChainMarker.BlueCross;
            if (s == Headmarkers.GreenTriangle) return ChainMarker.GreenTriangle;
            if (s == Headmarkers.PurpleSquare) return ChainMarker.PurpleSquare;
            return default;
        }

        public override void OnSettingsDraw()
        {
            ImGuiEx.Text(ImGuiColors.DalamudYellow, $"Warning, this mechanic is OVERWHELMINGLY difficult. Don't even try to configure and use this script before you fully understand how it works. ");
            if(ImGui.CollapsingHeader("Initial position and spread configuration"))
            {
                ImGuiEx.Text("Marker alignment, ");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                var alignmentDirection = Conf.AlignmentDirection;
                if (ImGuiEx.EnumCombo("(Omega-M is true north)", ref alignmentDirection)) {
                    Conf.AlignmentDirection = alignmentDirection;
                }
                ImGuiEx.Text("Omega-M");
                for (int i = 0; i < Conf.MarkerOrder.Length; i++)
                {
                    ImGui.PushID($"num{i}");
                    if (ImGui.ArrowButton("up", ImGuiDir.Up))
                    {
                        if(i != 0)
                        {
                            (Conf.MarkerOrder[i], Conf.MarkerOrder[i - 1]) = (Conf.MarkerOrder[i - 1], Conf.MarkerOrder[i]);
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.ArrowButton("down", ImGuiDir.Down))
                    {
                        if (i != Conf.MarkerOrder.Length - 1)
                        {
                            (Conf.MarkerOrder[i], Conf.MarkerOrder[i + 1]) = (Conf.MarkerOrder[i + 1], Conf.MarkerOrder[i]);
                        }
                    }
                    ImGui.SameLine();
                    ImGuiEx.Text($"{Conf.MarkerOrder[i]}");
                    ImGui.PopID();
                }
                ImGuiEx.Text($"Hands");
                ImGui.Separator();
                ImGuiEx.Text($"Spread configuration");
                List<Position> directionsSpots = new();
                switch (Conf.AlignmentDirection)
                {
                    case MarkerAlignmentDirection.NorthToSouth:
                        directionsSpots = PositionsNorthToSouthOnly;
                        break;
                    case MarkerAlignmentDirection.WestToEast:
                        directionsSpots = PositionsWestToEastOnly;
                        break;
                }
                foreach (var x in directionsSpots) {
                    var z = Conf.DirectionsSpots[x];
                    ImGui.SetNextItemWidth(200f);
                    if (ImGuiEx.EnumCombo($"{x}", ref z))
                    {
                        Conf.DirectionsSpots[x] = z;
                    }
                }
            }
            if (ImGui.CollapsingHeader("Reset and reconfigure tower markers"))
            {
                if (ImGui.Button("Reset and reconfigure for 1-2 north marker strat (new toolbox) (CTRL+click)") && ImGui.GetIO().KeyCtrl)
                {
                    Conf.FarTowers = new Config().FarTowers;
                    Conf.CloseTowers = new Config().CloseTowers;
                }
                if (ImGui.Button("Reset and reconfigure for 3-4 north marker strat (old toolbox) (CTRL+click)") && ImGui.GetIO().KeyCtrl)
                {
                    Conf.FarTowers = new Config().FarTowersOldToolbox;
                    Conf.CloseTowers = new Config().CloseTowersOldToolbox;
                }
                if (ImGui.Button("Reset and reconfigure for A-D north marker strat (Wingwan) (CTRL+click)") && ImGui.GetIO().KeyCtrl) 
                {
                    Conf.FarTowers = new Config().FarTowersWingwan;
                    Conf.CloseTowers = new Config().CloseTowersWingwan;
                }
                if (ImGui.Button("Reset and reconfigure for UCOB-like strat (L1/R1...) (CTRL+click)") && ImGui.GetIO().KeyCtrl)
                {
                    Conf.FarTowers = new Config().FarTowersUcob;
                    Conf.CloseTowers = new Config().CloseTowersUcob;
                }
            }
            ImGui.PushID("Far towers");
            if (ImGui.CollapsingHeader("Far towers, clockwise"))
            {
                ImGuiEx.Text($"Tower 1 (front)");
                ImGui.InputText($"##0", ref Conf.FarTowers[0], 50);
                ImGui.InputText($"##1", ref Conf.FarTowers[1], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 2 (right)");
                ImGui.InputText($"##2", ref Conf.FarTowers[2], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 3 (bottom-right)");
                ImGui.InputText($"##3", ref Conf.FarTowers[3], 50);
                ImGui.InputText($"##4", ref Conf.FarTowers[4], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 4 (bottom-left)");
                ImGui.InputText($"##5", ref Conf.FarTowers[5], 50);
                ImGui.InputText($"##6", ref Conf.FarTowers[6], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 5 (left)");
                ImGui.InputText($"##7", ref Conf.FarTowers[7], 50);
            }
            ImGui.PopID();
            ImGui.PushID("Near towers");
            if (ImGui.CollapsingHeader("Close towers, clockwise"))
            {

                ImGuiEx.Text($"Tower 1 (front-right)");
                ImGui.InputText($"##0", ref Conf.CloseTowers[0], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 2 (right)");
                ImGui.InputText($"##1", ref Conf.CloseTowers[1], 50);
                ImGui.InputText($"##2", ref Conf.CloseTowers[2], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 3 (bottom-right)");
                ImGui.InputText($"##3", ref Conf.CloseTowers[3], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 4 (bottom-left)");
                ImGui.InputText($"##4", ref Conf.CloseTowers[4], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 5 (left)");
                ImGui.InputText($"##5", ref Conf.CloseTowers[5], 50);
                ImGui.InputText($"##6", ref Conf.CloseTowers[6], 50);
                ImGui.Separator();
                ImGuiEx.Text($"Tower 6 (front left)");
                ImGui.InputText($"##7", ref Conf.CloseTowers[7], 50);
            }
            ImGui.PopID();

            ImGui.Checkbox($"Remember my marker and tether to my tower", ref Conf.RememberMarker);
            if (Conf.RememberMarker)
            {
                ImGuiEx.TextWrapped("Warning, tower label marker MUST START with the waymark label");
                var farYes = Conf.FarTowers.All(x => x.GetFirstLetter().EqualsIgnoreCaseAny("a,b,c,d,1,2,3,4".Split(",")) && Conf.FarTowers.Where(z => z.GetFirstLetter().EqualsIgnoreCase(x.GetFirstLetter())).Count() == 1);
                var closeYes = Conf.CloseTowers.All(x => x.GetFirstLetter().EqualsIgnoreCaseAny("a,b,c,d,1,2,3,4".Split(",")) && Conf.CloseTowers.Where(z => z.GetFirstLetter().EqualsIgnoreCase(x.GetFirstLetter())).Count() == 1);
                ImGuiEx.Text(farYes ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed, $"Far towers: " + (farYes ? "check passed" : "LABEL MISMATCH, FUNCTION WILL NOT WORK"));
                ImGuiEx.Text(closeYes ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed, $"Close towers: " + (closeYes ? "check passed" : "LABEL MISMATCH, FUNCTION WILL NOT WORK"));
                ImGui.Checkbox($"Instead of indicating knockback direction, directly tether to my tower", ref Conf.TetherDirect);
            }

            ImGui.Checkbox($"Disable chat output", ref Conf.NoChat);

            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGui.InputFloat3("Omega pos", ref OmegaPos);
                if (ImGui.Button("Copy"))
                {
                    ImGui.SetClipboardText(JsonConvert.SerializeObject(OmegaPos));
                }
                ImGui.SameLine();
                if (ImGui.Button("Paste"))
                {
                    GenericHelpers.Safe(() => { OmegaPos = JsonConvert.DeserializeObject<Vector3>(ImGui.GetClipboardText()); });
                }
                ImGui.SameLine();
                ImGui.Checkbox("Display tower angle", ref Conf.Angle);
                ImGui.Separator();
                foreach(var x in MkNum)
                {
                    ImGuiEx.Text($"Waymark {x}: {GetMarker(x)} distance {GetMarker(x).GetDistanceToWaymark()}");
                }
                ImGui.Separator();
                ImGuiEx.Text($"State:");
                ImGuiEx.TextWrapped(State.Join("\n"));
            }
        }

        public class Config : IEzConfig
        {
            internal readonly string[] FarTowersOldToolbox = new string[] { "3", "2", "D", "4", "C", "1", "B", "A" };
            internal readonly string[] CloseTowersOldToolbox = new string[] { "4", "D", "2", "C", "B", "1", "A", "3" };
            internal readonly string[] FarTowersWingwan = new string[] { "3", "4", "D", "1", "A", "B", "2", "C" };
            internal readonly string[] CloseTowersWingwan = new string[] { "3", "D", "1", "A", "B", "2", "C", "4" };
            internal readonly string[] FarTowersUcob = new string[] { "R1", "L1", "R2", "R3", "L4", "R4", "L3", "L2" };
            internal readonly string[] CloseTowersUcob = new string[] { "R1", "L4", "R2", "R3", "L3", "R4", "L2", "L1" };

            public bool NoChat = false;
            public ChainMarker[] MarkerOrder = new ChainMarker[] { ChainMarker.BlueCross, ChainMarker.PurpleSquare, ChainMarker.RedCircle, ChainMarker.GreenTriangle };
            public string[] FarTowers = new string[] { "1", "2", "D", "C", "4", "B", "3", "A" };
            public string[] CloseTowers = new string[] { "2", "D", "4", "C", "B", "A", "3", "1" };
            public bool Angle = false;
            public bool RememberMarker = true;
            public bool TetherDirect = false;
            public MarkerAlignmentDirection AlignmentDirection = MarkerAlignmentDirection.NorthToSouth;

            public Dictionary<Position, Directions> DirectionsSpots = new()
            {
                { Position.SingleMarkerFirstUnmarked, Directions.FrontLeft },
                { Position.SingleMarkerFirstMarked, Directions.BottomRight },
                { Position.SingleMarkerSecondUnmarked, Directions.FrontRight },
                { Position.SingleMarkerSecondMarked, Directions.BottomLeft },
                {Position.DoubleMarkerFirstLeft, Directions.Front },
                {Position.DoubleMarkerFirstRight, Directions.Bottom},
                {Position.DoubleMarkerSecondLeft, Directions.Left },
                {Position.DoubleMarkerSecondRight, Directions.Right},
                {Position.DoubleMarkerFirstUp, Directions.Left},
                {Position.DoubleMarkerFirstDown, Directions.Right},
                {Position.DoubleMarkerSecondUp, Directions.Front},
                {Position.DoubleMarkerSecondDown, Directions.Bottom},
            };
        }

        public enum MarkerAlignmentDirection
        {
            NorthToSouth,
            WestToEast,
        }

        public enum Position
        {
            DoubleMarkerFirstLeft,
            DoubleMarkerFirstRight,
            DoubleMarkerSecondLeft,
            DoubleMarkerSecondRight,
            SingleMarkerFirstMarked,
            SingleMarkerFirstUnmarked,
            SingleMarkerSecondMarked,
            SingleMarkerSecondUnmarked,
            DoubleMarkerFirstUp,
            DoubleMarkerFirstDown,
            DoubleMarkerSecondUp,
            DoubleMarkerSecondDown,
        }

        internal readonly List<Position> PositionsNorthToSouthOnly = new() {
            Position.DoubleMarkerFirstLeft,
            Position.DoubleMarkerFirstRight,
            Position.DoubleMarkerSecondLeft,
            Position.DoubleMarkerSecondRight,
            Position.SingleMarkerFirstMarked,
            Position.SingleMarkerFirstUnmarked,
            Position.SingleMarkerSecondMarked,
            Position.SingleMarkerSecondUnmarked,
        };

        internal readonly List<Position> PositionsWestToEastOnly = new() {
            Position.DoubleMarkerFirstUp,
            Position.DoubleMarkerFirstDown,
            Position.DoubleMarkerSecondUp,
            Position.DoubleMarkerSecondDown,
            Position.SingleMarkerFirstMarked,
            Position.SingleMarkerFirstUnmarked,
            Position.SingleMarkerSecondMarked,
            Position.SingleMarkerSecondUnmarked,
        };

        public enum Directions
        {
            Front, Bottom, Left, Right, FrontLeft, FrontRight, BottomLeft, BottomRight
        }

        public enum ChainMarker
        {
            BlueCross, PurpleSquare, RedCircle, GreenTriangle
        }


        static string[] MkNum = new string[] { "A", "B", "C", "D", "1", "2", "3", "4" };
        Vector3 GetMarker(string s)
        {
            var index = 0;
            for (int i = 0; i < MkNum.Length; i++)
            {
                if (MkNum[i].EqualsIgnoreCase(s))
                {
                    index = i;
                    break;
                }
            }
            return new Vector3(MKC->FieldMarkerArraySpan[index].X, MKC->FieldMarkerArraySpan[index].Y, MKC->FieldMarkerArraySpan[index].Z);
        }
    }

    public unsafe static class Dynamis_Sigma_Utils
    {
        public static string GetFirstLetter(this string s)
        {
            return s.Length > 0 ? s[0..1] : "";
        }

        public static float GetDistanceToWaymark(this Vector3 waymarkPos)
        {
            var d1 = Vector3.Distance(new(100, 0, 100), waymarkPos / 1000f);
            var d2 = Vector3.Distance(Svc.ClientState.LocalPlayer?.Position ?? Vector3.Zero, waymarkPos / 1000f);
            return d1 + d2;
        }

        public static Vector3 GetPositon(this FieldMarker marker)
        {
            return new Vector3(marker.X, 0, marker.Z);
        }

        public static Vector2 GetRelativePosition(Vector2 origin, Vector2 target, float rotation) {
            var offset = target - origin;
            var relative = Vector2.Transform(offset, Quaternion.CreateFromYawPitchRoll(0, 0, rotation));
            return relative;
        }

        public static Vector2 GetRelativePosition(Vector3 origin, Vector3 target, float rotation) {
            return GetRelativePosition(origin.ToVector2(), target.ToVector2(), rotation);
        }
    }
}
