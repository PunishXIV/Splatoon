using ECommons;
using ECommons.GameHelpers;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using Splatoon;
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

    public override Metadata? Metadata => new(3, "Redmoon");

    private const uint PoisonResistanceDownDebuffID = 3935;
    private bool IsShow = false;
    private bool IsAdd = false;
    private PairSplit LatchNextPairSplit = PairSplit.None;

    public override void OnSetup()
    {
        Controller.RegisterElementFromCode("Pair", "{\"Name\":\"Pair\",\"type\":1,\"radius\":6.0,\"color\":4278255389,\"Filled\":false,\"fillIntensity\":0.14,\"originFillColor\":587267869,\"endFillColor\":587267869,\"overlayTextColor\":3943235328,\"overlayVOffset\":1.0,\"overlayText\":\"Pairs\",\"refActorPlaceholder\":[\"<d1>\",\"<d2>\",\"<d3>\",\"<d4>\"],\"refActorComparisonType\":5,\"includeRotation\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("split", "{\"Name\":\"split\",\"type\":1,\"Enabled\":false,\"radius\":6.0,\"color\":4278190335,\"Filled\":false,\"fillIntensity\":0.14,\"originFillColor\":587202815,\"endFillColor\":587202815,\"overlayTextColor\":4278190335,\"overlayVOffset\":1.0,\"overlayText\":\"<< Spread >>\",\"refActorComparisonType\":1,\"includeRotation\":true,\"FaceMe\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        var sourceObj = source.GetObject();
        if(sourceObj == null)
            return;

        if(sourceObj.DataId == 0 || sourceObj.DataId != 16941)
            return;

        if((castId == 37252) || (castId == 39688))
        {
            LatchNextPairSplit = PairSplit.Split;
        }

        if((castId == 37253) || (castId == 39689))
        {
            LatchNextPairSplit = PairSplit.Pair;
        }

        if((castId == 37254) || (castId == 37255) || (castId == 39692) || (castId == 39693))
        {
            ShowElement();
        }

    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(set.Action == null || (set.Source == null)) return;
        if(set.Source.DataId == 0) return;

        if((set.Action.Value.RowId == 37256) && (set.Source.DataId == 16945))
        {
            HideElement();
        }

        if((set.Action.Value.RowId == 39691) && (set.Source.DataId == 16943))
        {
            HideElement();
        }
    }

    private void ShowElement()
    {
        var element = Controller.GetElementByName("Pair");
        if((LatchNextPairSplit == PairSplit.Pair) && element != null && !element.Enabled)
        {
            Controller.GetElementByName("Pair")!.Enabled = true;
        }

        element = Controller.GetElementByName("split");
        if((LatchNextPairSplit == PairSplit.Split) && element != null && !element.Enabled)
        {
            Controller.GetElementByName("split")!.Enabled = true;
        }
    }

    private void HideElement()
    {
        var element = Controller.GetElementByName("Pair");
        if(element != null && element.Enabled)
        {
            Controller.GetElementByName("Pair")!.Enabled = false;
        }

        element = Controller.GetElementByName("split");
        if(element != null && element.Enabled)
        {
            Controller.GetElementByName("split")!.Enabled = false;
        }
    }

    public override void OnReset()
    {
        HideElement();
        LatchNextPairSplit = PairSplit.None;
        IsShow = false;
        IsAdd = false;
    }
}

