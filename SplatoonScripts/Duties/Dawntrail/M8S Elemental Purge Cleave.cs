using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks.ActionEffectTypes;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class M8S_Elemental_Purge_Cleave : SplatoonScript
{
    private bool _isActive;
    public override HashSet<uint>? ValidTerritories => [1263];
    public override Metadata? Metadata => new(2, "Garume");

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Cone",
            "{\"Name\":\"\",\"type\":4,\"radius\":30.0,\"coneAngleMin\":-105,\"coneAngleMax\":105,\"color\":1677787135,\"fillIntensity\":0.5,\"thicc\":1.0,\"refActorDataID\":18222,\"refActorComparisonType\":3,\"includeRotation\":true,\"tether\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"faceplayer\":\"<t1>\"}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 42087 && !_isActive && source.GetObject() is IBattleNpc { TargetObject: not null } npc)
        {
            _isActive = true;
            if (Controller.TryGetElementByName("Cone", out var e))
            {
                e.Enabled = true;
                e.faceplayer = $"<{GetPlayerOrder(npc.TargetObject)}>";
            }
        }
    }

    public override void OnUpdate()
    {
        if (_isActive)
        {
            var npc = Svc.Objects.OfType<IBattleNpc>().FirstOrDefault(x => x.CastActionId == 42087);
            if (npc != null && Controller.TryGetElementByName("Cone", out var e))
            {
                e.Enabled = true;
                if (npc.TargetObject != null) e.faceplayer = $"<{GetPlayerOrder(npc.TargetObject)}>";
            }
        }
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if (set.Action is { RowId: 42093 })
        {
            _isActive = false;
            if (Controller.TryGetElementByName("Cone", out var e)) e.Enabled = false;
        }
    }

    public override void OnReset()
    {
        _isActive = false;
        if (Controller.TryGetElementByName("Cone", out var e)) e.Enabled = false;
    }

    private static unsafe int GetPlayerOrder(IGameObject c)
    {
        for (var i = 1; i <= 8; i++)
            if ((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                return i;

        return 0;
    }
}