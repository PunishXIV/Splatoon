using ECommons.GameHelpers;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using static Splatoon.Splatoon;

namespace SplatoonScriptsOfficial.Tests;

public unsafe class GenericTest : SplatoonScript
{
    public override Metadata Metadata => new(1, "NightmareXIV");
    public override HashSet<uint> ValidTerritories => [1234];

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("SomeElementName", """
            ~Lv2~{"Name":"Overdraught - Spread","Group":"EX6 - Hell on Rails","ZoneLockH":[1308],"DCond":5,"UseTriggers":true,"Triggers":[{"Type":2,"Match":"(14284>45663)"},{"Type":3,"Match":"(14284>45670)","MatchDelay":12.0},{"Type":3,"Match":"(14284>45677)","MatchDelay":12.0}],"ElementsL":[{"Name":"Spread Text","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":3355443455,"overlayVOffset":1.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"Spread","overlayTextIntl":{"Jp":"散会"},"refActorNPCNameID":14284,"refActorComparisonType":6,"onlyTargetable":true},{"Name":"Trigger","type":1,"refActorNPCNameID":14284,"refActorRequireCast":true,"refActorCastId":[45670,45677],"refActorUseCastTime":true,"refActorCastTimeMin":6.0,"refActorCastTimeMax":12.0,"refActorUseOvercast":true,"refActorComparisonType":6,"Conditional":true,"Nodraw":true},{"Name":"Player Circles (All)","type":1,"radius":5.0,"fillIntensity":0.2,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"ObjectKinds":[1]}]}
            """);
    }

    public override void OnUpdate()
    {
        var element = Controller.GetElementByName("SomeElementName");
        element.SetRefPosition(BasePlayer.Position);
    }
}
