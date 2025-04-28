using ECommons;
using ECommons.GameHelpers;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Splatoon;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Duties.Shadowbringers.The_Epic_Of_Alexander;

public class TEA_P4_Fate_Projection_β : SplatoonScript
{
    public enum FutureActionType : byte
    {
        EastUpperLeft,
        EastCenterLeft,
        EastLowerLeft,
        EastCenter,
        North,
        Spread,
        Stack,
        None
    }

    private readonly List<uint> _futurePlayers = [];

    private bool _canAddFuturePlayer = true;

    private bool _isStartFateProjectionCasting;

    private bool _myDefuffIsYellow;
    private uint? _myFuturePlayer;

    public override Metadata? Metadata => new(2, "Garume");
    public override HashSet<uint>? ValidTerritories => [887];

    public override void OnStartingCast(uint source, uint castId)
    {
        if(castId == 19219)
        {
            _isStartFateProjectionCasting = true;
            PluginLog.Warning("Start Fate Projection Casting");

            Controller.Schedule(() =>
            {
                if(Controller.TryGetElementByName("FirstBait", out var firstElement))
                    firstElement.Enabled = false;
                if(Controller.TryGetElementByName("FirstText", out var firstTextElement))
                    firstTextElement.Enabled = false;
                if(Controller.TryGetElementByName("SecondText", out var secondElement))
                    secondElement.overlayTextColor = EColor.Red.ToUint();
            }, 1000 * 55);
            Controller.Schedule(() =>
            {
                if(Controller.TryGetElementByName("SecondText", out var secondTextElement))
                {
                    secondTextElement.overlayTextColor = EColor.White.ToUint();
                    secondTextElement.Enabled = false;
                }

                if(Controller.TryGetElementByName("SecondBait", out var secondElement))
                {
                    secondElement.Enabled = true;
                    secondElement.tether = true;
                }
            }, 1000 * 63);
            Controller.Schedule(() =>
            {
                _isStartFateProjectionCasting = false;
                if(Controller.TryGetElementByName("SecondBait", out var secondElement)) secondElement.tether = false;
            }, 1000 * 70);
        }
    }

    public override void OnReset()
    {
        _isStartFateProjectionCasting = false;
        _futurePlayers.Clear();
        _myFuturePlayer = null;
        _myDefuffIsYellow = false;
        _canAddFuturePlayer = true;
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    public override void OnSetup()
    {
        var element = new Element(0)
        {
            color = EColor.Blue.ToUint(),
            thicc = 5f
        };

        Controller.RegisterElement("FirstBait", element, true);

        var secondElement = new Element(0)
        {
            radius = 1f,
            color = EColor.Blue.ToUint(),
            overlayText = Loc(en: "Move beneath the enemy", jp: "足元へ！"),
            overlayFScale = 2f,
            overlayVOffset = 2f,
            thicc = 5f
        };

        Controller.RegisterElement("SecondBait", secondElement, true);

        var firstTextElement = new Element(0)
        {
            overlayText = "",
            overlayVOffset = 8f,
            overlayFScale = 5f,
            Filled = false,
            radius = 0f
        };
        firstTextElement.SetOffPosition(new Vector3(100f, 0, 100f));
        Controller.RegisterElement("FirstText", firstTextElement, true);

        var secondTextElement = new Element(0)
        {
            overlayText = "",
            overlayVOffset = 5f,
            overlayFScale = 5f,
            Filled = false,
            radius = 0f
        };
        secondTextElement.SetOffPosition(new Vector3(100f, 0, 100f));
        Controller.RegisterElement("SecondText", secondTextElement, true);
    }

    public override void OnUpdate()
    {
        if(!_isStartFateProjectionCasting) Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
    }

    private string GetFutureActionText(FutureActionType type)
    {
        var stackDirection = _myDefuffIsYellow ? Loc(en: "Go North", jp: "北に") : Loc(en: "Go South", jp: "南に");
        return type switch
        {
            FutureActionType.EastCenter => Loc(en: "Afterwards, move to the outer edge", jp: "終了後、外周に行け"),
            FutureActionType.EastCenterLeft => Loc(en: "Afterwards, move to the outer edge", jp: "終了後、外周に行け"),
            FutureActionType.EastLowerLeft => Loc(en: "Afterwards, move to the outer edge", jp: "終了後、外周に行け"),
            FutureActionType.EastUpperLeft => Loc(en: "Afterwards, move to the outer edge", jp: "終了後、外周に行け"),
            FutureActionType.Spread => Loc(en: "Spread out", jp: "散開"),
            FutureActionType.Stack => Loc(en: $"Stack {stackDirection}", jp: $"頭割り {stackDirection}"),
            _ => Loc(en: "", jp: "")
        };
    }

    private Vector2 GetFutureActionPosition(FutureActionType type)
    {
        return type switch
        {
            FutureActionType.EastUpperLeft => new Vector2(112f, 98.5f),
            FutureActionType.EastCenterLeft => new Vector2(112f, 100f),
            FutureActionType.EastLowerLeft => new Vector2(112f, 101.5f),
            FutureActionType.EastCenter => new Vector2(115, 100),
            FutureActionType.North => new Vector2(92, 84f),
            FutureActionType.None => Vector2.Zero,
            _ => Vector2.Zero
        };
    }

    public override void OnActionEffectEvent(ActionEffectSet set)
    {
        if(!_isStartFateProjectionCasting) return;
        if(set.Source is not { DataId: 0x2C55 }) return;
        switch(set.Action)
        {
            case { RowId: 18592 }:
                {
                    var text = GetFutureActionText(FutureActionType.Spread);
                    if(Controller.TryGetElementByName("SecondText", out var textElement))
                    {
                        textElement.overlayText = text;
                        textElement.Enabled = true;
                    }

                    break;
                }
            case { RowId: 18593 }:
                {
                    var text = GetFutureActionText(FutureActionType.Stack);
                    if(Controller.TryGetElementByName("SecondText", out var textElement))
                    {
                        textElement.overlayText = text;
                        textElement.Enabled = true;
                    }

                    break;
                }
        }

        if(set.Action is { RowId: 18590 })
            if(Controller.TryGetElementByName("SecondBait", out var element))
            {
                element.SetOffPosition(set.Source.Position);
                element.Enabled = true;
            }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if(!_isStartFateProjectionCasting) return;

        if(_canAddFuturePlayer) _futurePlayers.Add(target);


        if(source == Player.Object.EntityId)
        {
            _myFuturePlayer = target;
            Controller.Schedule(() =>
            {
                _canAddFuturePlayer = false;
                var reversed = _futurePlayers.ToArray().Reverse().ToArray();
                var futureAction = FutureActionType.None;
                for(var i = 0; i < reversed.Length; i++)
                    if(_myFuturePlayer == reversed[i])
                    {
                        futureAction = i switch
                        {
                            0 => FutureActionType.EastCenter,
                            1 => FutureActionType.North,
                            2 => FutureActionType.EastCenterLeft,
                            3 => FutureActionType.EastUpperLeft,
                            4 => FutureActionType.EastUpperLeft,
                            5 => FutureActionType.EastUpperLeft,
                            6 => FutureActionType.EastLowerLeft,
                            7 => FutureActionType.EastUpperLeft,
                            _ => FutureActionType.None
                        };

                        if(i is 1 or 3 or 5 or 7)
                            _myDefuffIsYellow = true;
                    }

                if(Controller.TryGetElementByName("FirstBait", out var element))
                {
                    var position = GetFutureActionPosition(futureAction);
                    element.SetOffPosition(new Vector3(position.X, 0, position.Y));
                    element.tether = true;
                    element.Enabled = true;
                }

                if(Controller.TryGetElementByName("FirstText", out var textElement))
                {
                    var text = GetFutureActionText(futureAction);
                    textElement.overlayText = text;
                    textElement.Enabled = true;
                }
            }, 2000);
        }
    }
}