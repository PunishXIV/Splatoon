using System.Collections.Generic;
using System.Numerics;
using ECommons;
using ECommons.GameHelpers;
using ECommons.Logging;
using Splatoon;
using Splatoon.SplatoonScripting;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers;

public class TEA_P2_The_Final_Word : SplatoonScript
{
    private readonly uint[] _debuffIds = { 3056, 3057, 3058, 3059 };
    private readonly Vector3 OtherPosition = new(112, 0, 100);
    private readonly uint PurpleChildDebuffId = 3059;
    private readonly uint PurpleParentDebuffId = 3058;
    private readonly Vector3 PurpleParentPosition = new(115, 0, 100);
    private readonly uint YellowChildDebuffId = 3057;
    private readonly uint YellowParentDebuffId = 3056;

    private readonly Vector3 YellowParentPosition = new(86, 0, 100);
    private bool _isStartCastingTheFinalWord;
    private bool _isStartTheFinalWord;

    public override HashSet<uint>? ValidTerritories => new() { 887 };
    public override Metadata? Metadata => new(1, "Garume");

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            tether = true
        };
        Controller.RegisterElement("bait", element, true);
    }

    public override void OnStartingCast(uint source, uint castId)
    {
        if (castId == 18557) _isStartCastingTheFinalWord = true;
    }

    public override void OnUpdate()
    {
        if (!_isStartCastingTheFinalWord)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        var statuses = Player.Status;

        /*if (statuses.Any(x => _debuffIds.Contains(x.StatusId)))
        {
            _isStartTheFinalWord = true;
        }
        else
        {
            if (_isStartTheFinalWord) _isStartTheFinalWord = false;
            _isStartTheFinalWord = false;
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        }*/

        if (Controller.TryGetElementByName("bait", out var element))
        {
            foreach (var status in statuses)
            {
                PluginLog.Warning("StatusId: " + status.StatusId);
                if (status.StatusId == YellowParentDebuffId)
                    element.SetOffPosition(YellowParentPosition);
                else if (status.StatusId == PurpleParentDebuffId)
                    element.SetOffPosition(PurpleParentPosition);
                else if (status.StatusId == YellowChildDebuffId || status.StatusId == PurpleChildDebuffId)
                    element.SetOffPosition(OtherPosition);
            }

            element.Enabled = true;
        }
    }
}