using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public unsafe class R1S_Raining_Cats : SplatoonScript
{
    private const uint BlackCatNameId = 12686;
    private readonly uint[] RainingCatsCastActionIds = [39611, 39612, 39613];
    private readonly List<IPlayerCharacter> TetheredPlayers = new(2);

    public override HashSet<uint>? ValidTerritories { get; } = new() { 1226 };
    public override Metadata Metadata => new(2, "Garume");

    private IBattleNpc? BlackCat => Svc.Objects.OfType<IBattleNpc>()
        .FirstOrDefault(x => x is { IsTargetable: true, NameId: BlackCatNameId });

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Cone1",
            "{\"Name\":\"Cone1\",\"type\":4,\"radius\":30.0,\"coneAngleMin\":-23,\"coneAngleMax\":23,\"refActorNPCNameID\":12686,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyTargetable\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"faceplayer\":\"<3>\"}");
        Controller.RegisterElementFromCode("Cone2",
            "{\"Name\":\"Cone2\",\"type\":4,\"radius\":30.0,\"coneAngleMin\":-23,\"coneAngleMax\":23,\"refActorNPCNameID\":12686,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyTargetable\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0,\"faceplayer\":\"<3>\"}");

        Controller.RegisterElementFromCode("Nearest",
            "{\"Name\":\"Near Circle\",\"type\":1,\"radius\":4.2,\"overlayVOffset\":2.0,\"overlayFScale\":2.0,\"overlayText\":\"Near Pair\",\"refActorPlaceholder\":[\"<1>\"],\"refActorComparisonType\":5,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("Farthest",
            "{\"Name\":\"Far Circle\",\"type\":1,\"radius\":4.2,\"overlayVOffset\":2.0,\"overlayFScale\":2.0,\"overlayText\":\"Far Pair\",\"refActorPlaceholder\":[\"<1>\"],\"refActorComparisonType\":5,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (source.GetObject() is IPlayerCharacter player) TetheredPlayers.Add(player);
    }

    public override void OnTetherRemoval(uint source, uint data2, uint data3, uint data5)
    {
        if (source.GetObject() is IPlayerCharacter player) TetheredPlayers.Remove(player);
    }


    public override void OnUpdate()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);

        if (BlackCat == null) return;

        if (!BlackCat.IsCasting || !BlackCat.CastActionId.EqualsAny(RainingCatsCastActionIds)) return;

        var players = Svc.Objects
            .OfType<IPlayerCharacter>()
            .Where(x => !x.IsDead)
            .OrderBy(x => Vector3.Distance(x.Position, BlackCat.Position))
            .ToList();

        if (players.Count > 0)
        {
            var nearestElement = Controller.GetElementByName("Nearest");
            nearestElement!.Enabled = true;
            nearestElement!.refActorPlaceholder = [$"<{GetPlayerOrder(players[0])}>"];

            var farthestElement = Controller.GetElementByName("Farthest");
            farthestElement!.Enabled = true;
            farthestElement!.refActorPlaceholder = [$"<{GetPlayerOrder(players[^1])}>"];
        }

        for (var i = 0; i < TetheredPlayers.Count; i++)
        {
            var element = Controller.GetElementByName($"Cone{i + 1}");
            element!.Enabled = true;
            element!.faceplayer = $"<{GetPlayerOrder(TetheredPlayers[i])}>";
        }
    }

    public override void OnReset()
    {
        TetheredPlayers.Clear();
    }

    private int GetPlayerOrder(IGameObject c)
    {
        for (var i = 1; i <= 8; i++)
            if ((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                return i;

        return 0;
    }
}