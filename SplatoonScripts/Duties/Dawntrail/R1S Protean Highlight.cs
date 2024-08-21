using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
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
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe class R1S_Protean_Highlight : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = [1226];
    public override Metadata? Metadata => new(1, "NightmareXIV");

    IBattleNpc? BlackCat => Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.IsTargetable && x.NameId == 12686);
    int CrossingStage = 0;
    Vector3 EntityPos = Vector3.Zero;

    public override void OnSetup()
    {
        for(int i = 0; i < 4; i++)
        {
            Controller.RegisterElementFromCode($"Cone{i}", "{\"Name\":\"\",\"type\":5,\"radius\":30.0,\"coneAngleMin\":-23,\"coneAngleMax\":23,\"fillIntensity\":0.25,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"refActorNPCNameID\":12686,\"refActorComparisonType\":2,\"includeRotation\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        }

        Controller.RegisterElementFromCode("Avoid", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"overlayBGColor\":2868903936,\"overlayTextColor\":4278190335,\"overlayVOffset\":1.0,\"thicc\":0.0,\"overlayText\":\"! AVOID ! Stay far !\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("Bait", "{\"Name\":\"\",\"type\":1,\"radius\":0.0,\"Filled\":false,\"fillIntensity\":0.5,\"originFillColor\":1677721855,\"endFillColor\":1677721855,\"overlayBGColor\":2868903936,\"overlayTextColor\":4280811264,\"overlayVOffset\":1.0,\"thicc\":0.0,\"overlayText\":\">> Bait protean <<\",\"refActorType\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        if(BlackCat == null) return;
        if(BlackCat.IsCasting)
        {
            if(BlackCat.CastActionId.EqualsAny(37948u))
            {
                CrossingStage = 1;
                EntityPos = BlackCat.Position;
            }
            if(BlackCat.CastActionId.EqualsAny(37975u, 38959u))
            {
                CrossingStage = 1;
                EntityPos = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.Struct()->ModelCharaId == 4223)?.Position ?? Vector3.Zero;
            }
            {
                if(Svc.Objects.OfType<IBattleNpc>().TryGetFirst(x => x.IsCasting && x.CastActionId.EqualsAny(38009u), out var caster))
                {
                    CrossingStage = 1;
                    EntityPos = caster.Position + new Vector3(-10,0,0) * (caster.Rotation.RadToDeg().InRange(170,190)?1:-1);
                }
            }
            {
                if(Svc.Objects.OfType<IBattleNpc>().TryGetFirst(x => x.IsCasting && x.CastActionId.EqualsAny(38995u), out var caster))
                {
                    CrossingStage = 1;
                    EntityPos = caster.Position + new Vector3(10, 0, 0) * (caster.Rotation.RadToDeg().InRange(170, 190) ? 1 : -1);
                }
            }
        }

        if(CrossingStage == 1)
        {
            DrawCrossings(false);
        }

        if(CrossingStage == 2)
        {
            DrawCrossings(true);
        }
    }

    Dictionary<uint, uint[]> MirroredStatus = [];
    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        PluginLog.Information($"Tether created \non {source.GetObject()}\nto{target.GetObject()}\n{data2}, {data3}, {data5}");
        if(source.GetObject() is IBattleNpc shade && shade.NameId == 13072 && data3 == 102)
        {
            MirroredStatus[shade.EntityId] = BlackCat!.StatusList.Select(x => x.StatusId).ToArray();
            PluginLog.Information($"Status list recorded for {shade}");
        }
    }

    void DrawCrossings(bool inverted)
    {
        var players = FakeParty.Get().OrderBy(x => Vector3.Distance(this.EntityPos, x.Position)).ToList();
        var baits = C.IsBaitingFirst;
        if(inverted) baits = !baits;

        foreach(var x in Controller.GetPartyMembers())
        {
            if(AttachedInfo.TryGetSpecificVfxInfo(x, "vfx/lockon/eff/lockon8_t0w.avfx", out var info) && info.AgeF < 15)
            {
                baits = !(AttachedInfo.TryGetSpecificVfxInfo(Player.Object, "vfx/lockon/eff/lockon8_t0w.avfx", out var localInfo) && localInfo.AgeF < 8);
            }
        }

        if(baits)
        {
            Controller.GetElementByName("Bait")!.Enabled = true;
        }
        else
        {
            Controller.GetElementByName("Avoid")!.Enabled = true;
        }
        for(int i = 0; i < 4; i++)
        {
            var e = Controller.GetElementByName($"Cone{i}")!;
            e.Enabled = true;
            e.SetRefPosition(EntityPos);
            var order = GetPlayerOrder(players[i]);
            e.faceplayer = $"<{order}>";
            
            if(order == 1)
            {
                if(baits)
                {
                    e.color = C.GreenColor.ToUint();
                }
                else
                {
                    e.color = C.RedColor.ToUint();
                }
            }
            else
            {
                e.color = C.YellowColor.ToUint();
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!FakeParty.Get().Select(x => x.Address).Contains(set.Source?.Address ?? -1)) PluginLog.Information($"Cast: {ExcelActionHelper.GetActionName(set.Action!.RowId, true)}");
        if(set.Action?.RowId.EqualsAny(37948u, 37976u) == true)
        {
            Controller.Schedule(() =>
            {
                PluginLog.Information("Switch to stage 2");
                CrossingStage = 2;
                this.EntityPos = BlackCat!.Position;
                Controller.ScheduleReset(5000);
            }, 1000);
        }
        if(set.Action?.RowId.EqualsAny(38010u) == true)
        {
            Controller.Schedule(() =>
            {
                PluginLog.Information("Switch to stage 2");
                CrossingStage = 2;
                Controller.ScheduleReset(5000);
            }, 1000);
        }
        if(set.Action?.RowId.EqualsAny(37949u, 37977u, 38011u) == true)
        {
            Controller.Schedule(() =>
            {
                PluginLog.Information("Switch to stage 0");
                CrossingStage = 0;
                Controller.ScheduleReset(5000);
            }, 1000);
        }
    }

    public override void OnReset()
    {
        CrossingStage = 0;
        EntityPos = Vector3.Zero;
    }

    int GetPlayerOrder(IPlayerCharacter c)
    {
        for(int i = 1; i <=8; i++)
        {
            if((nint)FakePronoun.Resolve($"<{i}>") == c.Address) return i;
        }
        return 0;
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.RadioButtonBool("Baiting first proteans", "Baiting second proteans", ref C.IsBaitingFirst);
        if(ImGui.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"EntityPos: {EntityPos}");
        }
    }


    Config C => Controller.GetConfig<Config>();
    public class Config : IEzConfig
    {
        public bool IsBaitingFirst = true;
        public Vector4 RedColor = ImGuiEx.Vector4FromRGBA(0xFF0000C8);
        public Vector4 YellowColor = ImGuiEx.Vector4FromRGBA(0xFFFF00C8);
        public Vector4 GreenColor = ImGuiEx.Vector4FromRGBA(0x00FF00C8);
    }
}
