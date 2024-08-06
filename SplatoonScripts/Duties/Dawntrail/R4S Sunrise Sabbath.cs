using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ImGuiNET;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class R4S_Sunrise_Sabbath : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1232];
    public override Metadata? Metadata => new(3, "NightmareXIV");
    uint DebuffYellow = 4000;
    uint DebuffBlue = 4001;
    string GunYellow = "vfx/common/eff/m0888_stlp04_c0t1.avfx";
    string GunBlue = "vfx/common/eff/m0888_stlp03_c0t1.avfx";
    Vector3 Mid = new(100, 0, 165);
    IBattleNpc? WickedThunder => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == 13057 && x.IsTargetable);
    IBattleNpc? WickedThunder2 => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.NameId == 13058 && x.IsTargetable);

    IBattleNpc[] ClonesTower => Svc.Objects.OfType<IBattleNpc>().Where(b => b.NameId == 13562 && b.GetTransformationID() == 28).ToArray();
    IBattleNpc[] ClonesGun => Svc.Objects.OfType<IBattleNpc>().Where(b => b.NameId == 13562 && b.GetTransformationID() == 57).ToArray();
    bool IsCloneNorthSouth => ClonesTower.Any(x => Vector3.Distance(x.Position, new(100, 0, 150)) < 5);

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("TowerSouth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":180.0,\"refZ\":3.8146973E-06,\"radius\":3.0,\"Donut\":0.56,\"color\":3355467263,\"fillIntensity\":0.2,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("TowerNorth", "{\"Name\":\"\",\"refX\":100.0,\"refY\":150.0,\"refZ\":3.8146973E-06,\"radius\":3.0,\"Donut\":0.56,\"color\":3355467263,\"fillIntensity\":0.2,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("TowerWest", "{\"Name\":\"\",\"refX\":85.0,\"refY\":165.0,\"radius\":3.0,\"Donut\":0.56,\"color\":3355467263,\"fillIntensity\":0.2,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("TowerEast", "{\"Name\":\"\",\"refX\":115.0,\"refY\":165.0,\"refZ\":-9.536743E-07,\"radius\":3.0,\"Donut\":0.56,\"color\":3355467263,\"fillIntensity\":0.2,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        for(int i = 0; i < 2; i++)
        {
            Controller.RegisterElementFromCode($"Blue{i}", "{\"Name\":\"\",\"type\":3,\"refY\":30.0,\"radius\":6.0,\"color\":3371826944,\"fillIntensity\":0.1,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"overlayPlaceholders\":true,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
            Controller.RegisterElementFromCode($"Yellow{i}", "{\"Name\":\"\",\"type\":3,\"refY\":30.0,\"radius\":6.0,\"color\":3355508719,\"fillIntensity\":0.1,\"originFillColor\":1157628159,\"endFillColor\":1157628159,\"overlayPlaceholders\":true,\"refActorComparisonType\":2,\"includeRotation\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }

        Controller.RegisterElementFromCode("T1", "{\"Name\":\"EW safe SW clone\",\"Enabled\":false,\"refX\":88.0,\"refY\":173.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T2", "{\"Name\":\"NS safe SW clone\",\"Enabled\":false,\"refX\":92.0,\"refY\":177.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T3", "{\"Name\":\"EW safe SE clone\",\"Enabled\":false,\"refX\":112.0,\"refY\":173.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T4", "{\"Name\":\"NS safe SE clone\",\"Enabled\":false,\"refX\":108.0,\"refY\":177.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T5", "{\"Name\":\"EW safe NE clone\",\"Enabled\":false,\"refX\":112.0,\"refY\":157.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T6", "{\"Name\":\"NS safe NE clone\",\"Enabled\":false,\"refX\":108.0,\"refY\":153.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T7", "{\"Name\":\"EW safe NW clone\",\"Enabled\":false,\"refX\":88.0,\"refY\":157.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
        Controller.RegisterElementFromCode("T8", "{\"Name\":\"NS safe EW clone\",\"Enabled\":false,\"refX\":92.0,\"refY\":153.0,\"radius\":0.5,\"color\":3356032768,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"thicc\":5.0,\"refActorNPCNameID\":13562,\"refActorComparisonType\":6,\"tether\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"refActorUseTransformation\":true,\"refActorTransformationID\":57}");
    }

    bool IsTakingGun => Player.Object.StatusList.Any(x => x.StatusId.EqualsAny<uint>(this.DebuffBlue, this.DebuffYellow) && x.RemainingTime < 16f && x.RemainingTime > 0.1f);
    bool IsMechanicActive(float remainingTime) => Controller.GetPartyMembers().Any(z => z.StatusList.Any(x => x.StatusId.EqualsAny<uint>(this.DebuffBlue, this.DebuffYellow) && x.RemainingTime < remainingTime && x.RemainingTime > 0.1f)) || Controller.GetPartyMembers().Count(x => x.StatusList.Any(s => s.StatusId == this.DebuffBlue)) == 4;
    bool IsTowerHidden => Controller.GetPartyMembers().Count(z => z.StatusList.Any(x => x.StatusId.EqualsAny<uint>(2940) && x.RemainingTime > 11f)) == 4 || (WickedThunder2 != null && WickedThunder2.CastActionId.EqualsAny(38418u, 38416u));
    bool IsGunHidden => Controller.GetPartyMembers().Count(z => z.StatusList.Any(x => x.StatusId.EqualsAny<uint>(2998) && x.RemainingTime > 15f)) == 4;

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each((x) =>
        {
            x.Value.Enabled = false;
        });
        if(this.Controller.Scene == 2 && Controller.GetPartyMembers().Any(z => z.StatusList.Any(x => x.StatusId == DebuffBlue)))
        {
            var clones = ClonesTower;
            if(clones.Length > 0)
            {
                var northSouth = IsCloneNorthSouth;
                if(clones.Any(x => (x.Rotation.RadiansToDegrees() % 90).InRange(30,60))) northSouth = !northSouth;
                if(!IsTowerHidden)
                {
                    if(northSouth)
                    {
                        var t1 = Controller.GetElementByName("TowerNorth");
                        var t2 = Controller.GetElementByName("TowerSouth");
                        t1.Enabled = true;
                        t2.Enabled = true;
                        t1.tether = false;
                        t2.tether = false;
                        if(!IsTakingGun && IsMechanicActive(11f))
                        {
                            if(C.TetherNorth) t1.tether = true;
                            if(C.TetherSouth) t2.tether = true;
                        }
                    }
                    else
                    {
                        var t1 = Controller.GetElementByName("TowerEast");
                        var t2 = Controller.GetElementByName("TowerWest");
                        t1.Enabled = true;
                        t2.Enabled = true;
                        t1.tether = false;
                        t2.tether = false;
                        if(!IsTakingGun && IsMechanicActive(11f))
                        {
                            if(C.TetherEast) t1.tether = true;
                            if(C.TetherWest) t2.tether = true;
                        }
                    }
                }

                if(IsMechanicActive(9.5f))
                {
                    var guns = ClonesGun.OrderBy(x => MathHelper.GetRelativeAngle(new(100, 0, 165), x.Position)).ToList();
                    if(C.StartWest)
                    {
                        guns = ClonesGun.OrderBy(x => (MathHelper.GetRelativeAngle(new(100, 0, 165), x.Position) + 90 + 360) % 360).ToList();
                    }
                    if(C.IsCcw) guns.Reverse();
                    int blues = 0;
                    int yellows = 0;
                    var showAoe = C.ShowAOETreshold != null && Controller.GetPartyMembers().Any(x => x.StatusList.Any(s => s.StatusId == this.DebuffBlue && s.RemainingTime < C.ShowAOETreshold.Value));
                    for(int i = 0; i < guns.Count; i++)
                    {
                        var gun = guns[i];
                        if(AttachedInfo.TryGetVfx(gun, out var vfx))
                        {
                            if(vfx.Where(x => x.Key.EqualsAny(this.GunBlue, this.GunYellow)).OrderBy(x => x.Value.AgeF).FirstOrDefault().Key == this.GunBlue)
                            {
                                if(showAoe)
                                {
                                    var e = Controller.GetElementByName($"Blue{blues++}")!;
                                    e.Enabled = true;
                                    e.refActorObjectID = gun.EntityId;
                                }
                                if(Player.Object.StatusList.Any(x => x.StatusId == this.DebuffYellow)) EnableTether();
                            }
                            else
                            {
                                if(showAoe)
                                {
                                    var e = Controller.GetElementByName($"Yellow{yellows++}")!;
                                    e.Enabled = true;
                                    e.refActorObjectID = gun.EntityId;
                                }
                                if(Player.Object.StatusList.Any(x => x.StatusId == this.DebuffBlue)) EnableTether();
                            }
                        }

                        void EnableTether()
                        {
                            if(IsTakingGun && !Controller.GetRegisteredElements().Any(x => x.Value.Name.Contains("safe") && x.Value.Enabled && !IsGunHidden))
                            {
                                var e = Controller.GetRegisteredElements().Where(x => x.Value.Name.Contains("safe") && x.Value.Name.Contains(northSouth ? "EW safe" : "NS safe")).OrderBy(x => Vector3.Distance(gun.Position, new(x.Value.refX, x.Value.refZ, x.Value.refY))).First().Value;
                                e.Enabled = true;
                            }
                        }
                    }
                }
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.InputInt(150f, "Show laser AOE when this seconds remains", ref C.ShowAOETreshold);
        ImGuiEx.Text("Select your towers:");
        ImGui.Checkbox($"North", ref C.TetherNorth);
        ImGui.Checkbox($"South", ref C.TetherSouth);
        ImGui.Checkbox($"West", ref C.TetherWest);
        ImGui.Checkbox($"East", ref C.TetherEast);
        ImGui.Separator();
        ImGui.Checkbox("Resolve bait priority", ref C.TetherBait);
        ImGui.Checkbox("Start from West instead of North", ref C.StartWest);
        ImGuiEx.RadioButtonBool("Counter-Clockwise", "Clockwise", ref C.IsCcw);
    }

    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public int? ShowAOETreshold = 7;
        public bool TetherNorth = false;
        public bool TetherSouth = false;
        public bool TetherWest = false;
        public bool TetherEast = false;

        public bool TetherBait = false;
        public bool IsCcw = false;
        public bool StartWest = false;
    }
}
