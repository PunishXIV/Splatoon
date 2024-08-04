using ECommons;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;

public class R2S_Venom_Love_Pair_Split : SplatoonScript
{
    private enum PairSplit
    {
        None = 0,
        Pair,
        Split
    }
    public override HashSet<uint>? ValidTerritories { get; } = [1228];

    public override Metadata? Metadata => new(0, "Redmoon");

    const uint PoisonResistanceDownDebuffID = 3935;
    bool IsShow = false;
    bool IsAdd = false;
    private PairSplit LatchNextPairSplit = PairSplit.None;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Pair", "{\"Name\":\"Pair\",\"type\":1,\"radius\":6.0,\"color\":4278255389,\"fillIntensity\":0.14,\"thicc\":2.0,\"refActorPlaceholder\":[\"<d1>\",\"<d2>\",\"<d3>\",\"<d4>\"],\"refActorComparisonType\":5,\"includeRotation\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("split", "{\"Name\":\"split\",\"type\":1,\"radius\":6.0,\"color\":4278190335,\"fillIntensity\":0.14,\"refActorComparisonType\":1,\"includeRotation\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnEnable()
    {
        Reset();
    }

    public override void OnDirectorUpdate(DirectorUpdateCategory category)
    {
        switch (category)
        {
            case DirectorUpdateCategory.Commence:
            case DirectorUpdateCategory.Recommence:
            case DirectorUpdateCategory.Complete:
            case DirectorUpdateCategory.Wipe:
                Reset();
                break;
        }
    }
    public override void OnUpdate()
    {
        if (Player.Object.StatusList.Any(x => x.StatusId == PoisonResistanceDownDebuffID) && IsShow && !IsAdd)
        {
            IsAdd = true;
            HideElement();
        }

        if (IsAdd && !(Player.Object.StatusList.Any(x => x.StatusId == PoisonResistanceDownDebuffID)))
        {
            IsAdd = false;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (IsShow && vfxPath.Contains("vfx/common/eff/m0906_stlp_ht4_03k2.avfx"))
        {
            HideElement();
        }
    }

    public override void OnMessage(string Message)
    {
        if (Message.Contains("12685>37252") || (Message.Contains("12685>39688")))
        {
            LatchNextPairSplit = PairSplit.Split;
        }
        else if (Message.Contains("12685>37253") || (Message.Contains("12685>39689")))
        {
            LatchNextPairSplit = PairSplit.Pair;
        }
        else if (Message.Contains("12685>37254") ||
                (Message.Contains("12685>37255")) ||
                (Message.Contains("12685>39696")) ||
                (Message.Contains("12685>39697")))
        {
            ShowElement();
        }
        else
        {
            // NOP
        }
    }

    private void ShowElement()
    {
        if (LatchNextPairSplit == PairSplit.Pair)
        {
            Controller.GetElementByName("Pair")!.Enabled = true;
        }
        else if (LatchNextPairSplit == PairSplit.Split)
        {
            Controller.GetElementByName("split")!.Enabled = true;
        }
        else
        {
        }
        IsShow = true;
    }

    private void HideElement()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        IsShow = false;
    }

    private void Reset()
    {
        HideElement();
        LatchNextPairSplit = PairSplit.None;
        IsShow = false;
        IsAdd = false;
    }
}

