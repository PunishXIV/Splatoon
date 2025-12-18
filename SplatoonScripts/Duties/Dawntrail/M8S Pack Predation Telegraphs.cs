using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Player = ECommons.GameHelpers.LegacyPlayer.Player;
using ECommons.GameHelpers.LegacyPlayer;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public unsafe sealed class M8S_Pack_Predation_Telegraphs : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories => [1263];

    public override Metadata? Metadata => new(2, "NightmareXIV");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("WosCone", """{"Name":"wos cone","type":4,"Enabled":false,"radius":30.0,"coneAngleMin":-45,"coneAngleMax":45,"refActorNPCNameID":13847,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"RotationOverride":true,"RotationOverridePoint":{}}""");
        Controller.RegisterElementFromCode("WowCone", """{"Name":"wow cone","type":4,"Enabled":false,"radius":30.0,"coneAngleMin":-45,"coneAngleMax":45,"refActorNPCNameID":13846,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"RotationOverride":true,"RotationOverridePoint":{}}""");
        Controller.RegisterElementFromCode("WosLineYou", """{"Name":"wos line","type":3,"Enabled":false,"offY":30.0,"radius":6.0,"color":3356032768,"fillIntensity":0.2,"refActorNPCNameID":13847,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"RotationOverride":true,"RotationOverridePoint":{}}""");
        Controller.RegisterElementFromCode("WowLineYou", """{"Name":"wow line","type":3,"Enabled":false,"offY":30.0,"radius":6.0,"color":3356032768,"fillIntensity":0.2,"refActorNPCNameID":13846,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"RotationOverride":true,"RotationOverridePoint":{}}""");
        Controller.RegisterElementFromCode("WosLine", """{"Name":"wos line","type":3,"Enabled":false,"offY":30.0,"radius":6.0,"color":3355508712,"fillIntensity":0.1,"refActorNPCNameID":13847,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"RotationOverride":true,"RotationOverridePoint":{}}""");
        Controller.RegisterElementFromCode("WowLine", """{"Name":"wow line","type":3,"Enabled":false,"offY":30.0,"radius":6.0,"color":3355508725,"fillIntensity":0.1,"refActorNPCNameID":13846,"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"RotationOverride":true,"RotationOverridePoint":{}}""");
    }

    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        var wow = Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsCasting(41932) && x.NameId == 13846).FirstOrDefault();
        var wos = Svc.Objects.OfType<IBattleNpc>().Where(x => x.IsCasting(41932) && x.NameId == 13847).FirstOrDefault();
        if(wow != null)
        {
            if(Controller.TryGetElementByName("WowCone", out var cone) && Controller.TryGetElementByName("WowLine", out var line) && Controller.TryGetElementByName("WowLineYou", out var lineYou))
            {
                var conePlayer = Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.GetJob().IsTank()).OrderBy(x => Vector3.Distance(x.Position, wow.Position)).FirstOrDefault();
                var linePlayer = Svc.Objects.OfType<IPlayerCharacter>().Where(x => AttachedInfo.TryGetSpecificVfxInfo(x, "vfx/lockon/eff/lockon5_t0h.avfx", out var vfx) && vfx.AgeF < 6f).OrderBy(x => Vector3.Distance(x.Position, wow.Position)).FirstOrDefault();
                if(conePlayer != null)
                {
                    cone.Enabled = true;
                    cone.RotationOverridePoint = conePlayer.Position.ToVector2().ToPoint2();
                }
                if(linePlayer != null)
                {
                    (linePlayer.AddressEquals(Player.Object) ? lineYou : line).Enabled = true;
                    (linePlayer.AddressEquals(Player.Object) ? lineYou : line).RotationOverridePoint = linePlayer.Position.ToVector2().ToPoint2();
                }
            }
        }
        if(wos != null)
        {
            if(Controller.TryGetElementByName("WosCone", out var cone) && Controller.TryGetElementByName("WosLine", out var line) && Controller.TryGetElementByName("WosLineYou", out var lineYou))
            {
                var conePlayer = Svc.Objects.OfType<IPlayerCharacter>().Where(x => x.GetJob().IsTank()).OrderBy(x => Vector3.Distance(x.Position, wos.Position)).FirstOrDefault();
                var linePlayer = Svc.Objects.OfType<IPlayerCharacter>().Where(x => AttachedInfo.TryGetSpecificVfxInfo(x, "vfx/lockon/eff/lockon5_t0h.avfx", out var vfx) && vfx.AgeF < 6f).OrderBy(x => Vector3.Distance(x.Position, wos.Position)).FirstOrDefault();
                if(conePlayer != null)
                {
                    cone.Enabled = true;
                    cone.RotationOverridePoint = conePlayer.Position.ToVector2().ToPoint2();
                }
                if(linePlayer != null)
                {
                    (linePlayer.AddressEquals(Player.Object)?lineYou:line).Enabled = true;
                    (linePlayer.AddressEquals(Player.Object) ? lineYou : line).RotationOverridePoint = linePlayer.Position.ToVector2().ToPoint2();
                }
            }
        }
    }
}