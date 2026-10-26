using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using ECommons.Configuration;
using ECommons.ImGuiMethods;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;

namespace SplatoonScriptsOfficial.Duties.Stormblood;

public class UCOB_Nael_Quotes_International : SplatoonScript
{
    #region Metadata

    public override Metadata? Metadata => new(1, "mirage");
    public override HashSet<uint>? ValidTerritories => [733];

    #endregion

    #region Config

    private Config C => Controller.GetConfig<Config>();

    #endregion

    #region State

    private Element? _inDonut;
    private Element? _outCircle;
    private Element? _stackMarker;
    private Element? _spreadMarker;

    #endregion

    #region Types

    // NpcYell lines as they appear in chat. Line breaks are spaces.
    private sealed class Config : IEzConfig
    {
        public bool EnableSpreadIn = true;
        public InternationalString QuoteSpreadIn = new(
            en: "From on high I descend, the hallowed moon to call!",
            jp: "我、舞い降りて 月を仰がん！",
            de: "Seht, ich steige herab, vom rotglühenden Monde!",
            fr: "Des cieux je vais descendre et révérer la lune !");

        public bool EnableSpreadOut = true;
        public InternationalString QuoteSpreadOut = new(
            en: "From on high I descend, the iron path to walk!",
            jp: "我、舞い降りて 鉄の覇道を征く！",
            de: "Seht, ich steige herab, um euch zu beherrschen!",
            fr: "Du haut des cieux, je vais descendre pour conquérir !");

        public bool EnableStackIn = true;
        public InternationalString QuoteStackIn = new(
            en: "Take fire, O hallowed moon!",
            jp: "赤熱せし 月の祝福を！",
            de: "Flammender Pfad, geschaffen vom roten Mond!",
            fr: "Baignez dans la bénédiction de la lune incandescente !");

        public bool EnableStackOut = true;
        public InternationalString QuoteStackOut = new(
            en: "Blazing path, lead me to iron rule!",
            jp: "赤熱し、焼かれし道を 鉄の覇道と成す！",
            de: "Umloderter Pfad, führe mich zur Herrschaft!",
            fr: "La voie marquée par l'incandescence mène à la domination !");

        public bool EnableInStack = true;
        public InternationalString QuoteInStack = new(
            en: "O hallowed moon, take fire and scorch my foes!",
            jp: "月よ！ 赤熱し、神敵を焼け！",
            de: "O roter Mond! Umlodere meinen Pfad!",
            fr: "Que l'incandescence de la lune brûle mes ennemis !");

        public bool EnableInOut = true;
        public InternationalString QuoteInOut = new(
            en: "O hallowed moon, shine you the iron path!",
            jp: "月よ！ 鉄の覇道を照らせ！",
            de: "O roter Mond! Führe mich zur Herrschaft!",
            fr: "Ô lune ! Éclaire la voie de la domination !");

        public bool EnableTankStack = true;
        public InternationalString QuoteTankStack = new(
            en: "Fleeting light! 'Neath the red moon, scorch you the earth!",
            jp: "超新星よ、輝きを増せ！ 紅月下の赤熱せし地を照らせ！",
            de: "Neues Gestirn! Glühe herab und umlodere meinen Pfad!",
            fr: "Supernova, brille de tout ton feu et irradie la terre rougie !");

        public bool EnableSpreadTank = true;
        public InternationalString QuoteSpreadTank = new(
            en: "Fleeting light! Amid a rain of stars, exalt you the red moon!",
            jp: "超新星よ、輝きを増せ！ 星降りの夜に、紅月を称えよ！",
            de: "Neues Gestirn! Überstrahle jede Sternschnuppe!",
            fr: "Supernova, brille de tout ton feu et glorifie la lune rouge !");

        public bool EnableSpreadInStars = true;
        public InternationalString QuoteSpreadInStars = new(
            en: "From on high I descend, the moon and stars to bring!",
            jp: "我、舞い降りて月を仰ぎ 星降りの夜を招かん！",
            de: "Ich steige herab zu Ehre des roten Mondes! Einer Sternschnuppe gleich!",
            fr: "Du haut des cieux, j'appelle une pluie d'étoiles !");

        public bool EnableInSpread = true;
        public InternationalString QuoteInSpread = new(
            en: "From hallowed moon I descend, a rain of stars to bring!",
            jp: "我、月より舞い降りて 星降りの夜を招かん！",
            de: "O roter Mond, sieh mich herabsteigen! Einer Sternschnuppe gleich!",
            fr: "Depuis la lune, j'invoque une pluie d'étoiles !");

        public bool EnableInOutSpread = true;
        public InternationalString QuoteInOutSpread = new(
            en: "From hallowed moon I bare iron, in my descent to wield!",
            jp: "我、月より鉄を備え 舞い降りん！",
            de: "O roter Mond, als Künder deiner Herrschaft stieg ich herab!",
            fr: "De la lune je m'arme d'acier et descends !");

        public bool EnableInSpreadStack = true;
        public InternationalString QuoteInSpreadStack = new(
            en: "From hallowed moon I descend, upon burning earth to tread!",
            jp: "我、月より舞い降りて 赤熱せし地を歩まん！",
            de: "O roter Mond! Ich stieg herab, um deine Herrschaft zu bringen!",
            fr: "De la lune, je descends et marche sur la terre ardente !");

        public bool EnableOutStackSpread = true;
        public InternationalString QuoteOutStackSpread = new(
            en: "Unbending iron, take fire and descend!",
            jp: "鉄よ、赤熱せよ！ 舞い降りし我が刃となれ！",
            de: "Zur Herrschaft führt mein umloderter Pfad! Auf diesen steige ich herab!",
            fr: "Ô noble acier ! Rougis ardemment et deviens ma lame transperçante !");

        public bool EnableOutSpreadStack = true;
        public InternationalString QuoteOutSpreadStack = new(
            en: "Unbending iron, descend with fiery edge!",
            jp: "鉄よ、舞い降りし 我の刃となり赤熱せよ！",
            de: "Zur Herrschaft steige ich herab, auf umlodertem Pfad!",
            fr: "Fier acier ! Sois ma lame plongeante et deviens incandescent !");
    }

    #endregion

    #region LifeCycle

    public override void OnSetup()
    {
        _inDonut = Controller.RegisterElementFromCode(
            "in_donut",
            """{"Name":"in_donut","type":1,"Enabled":false,"radius":5.7,"Donut":16.56,"fillIntensity":0.17,"thicc":5.0,"refActorNPCID":2612,"refActorComparisonType":4,"mechanicType":1}""",
            overwrite: true);
        _inDonut.Enabled = false;

        _outCircle = Controller.RegisterElementFromCode(
            "out_circle",
            """{"Name":"out_circle","type":1,"Enabled":false,"radius":9.0,"fillIntensity":0.17,"thicc":5.0,"refActorNPCID":2612,"refActorComparisonType":4,"mechanicType":1}""",
            overwrite: true);
        _outCircle.Enabled = false;

        _stackMarker = Controller.RegisterElementFromCode(
            "stack_marker",
            """{"Name":"stack_self","type":1,"Enabled":false,"radius":3.75,"Donut":0.25,"fillIntensity":0.5,"refActorName":"Ieni Telara","refActorType":1}""",
            overwrite: true);
        _stackMarker.Enabled = false;

        _spreadMarker = Controller.RegisterElementFromCode(
            "spread_marker",
            """{"Name":"player circles","type":1,"Enabled":false,"radius":4.0,"color":3355507967,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"DistanceSourceX":0.21287155,"DistanceSourceY":9.486858,"DistanceMax":24.0}""",
            overwrite: true);
        _spreadMarker.Enabled = false;
    }

    public override void OnReset()
    {
        if(_inDonut != null) _inDonut.Enabled = false;
        if(_outCircle != null) _outCircle.Enabled = false;
        if(_stackMarker != null) _stackMarker.Enabled = false;
        if(_spreadMarker != null) _spreadMarker.Enabled = false;
    }

    public override void OnMessage(string message)
    {
        if(Matches(message, C.EnableSpreadIn, C.QuoteSpreadIn) || Matches(message, C.EnableSpreadInStars, C.QuoteSpreadInStars))
        {
            Activate(_spreadMarker, false);
            Activate(_inDonut, true);
            return;
        }

        if(Matches(message, C.EnableSpreadOut, C.QuoteSpreadOut))
        {
            Activate(_spreadMarker, false);
            Activate(_outCircle, true);
            return;
        }

        if(Matches(message, C.EnableStackIn, C.QuoteStackIn))
        {
            Activate(_stackMarker, false);
            Activate(_inDonut, true);
            return;
        }

        if(Matches(message, C.EnableStackOut, C.QuoteStackOut))
        {
            Activate(_stackMarker, false);
            Activate(_outCircle, true);
            return;
        }

        if(Matches(message, C.EnableInStack, C.QuoteInStack))
        {
            Activate(_inDonut, false);
            Activate(_stackMarker, true);
            return;
        }

        if(Matches(message, C.EnableInOut, C.QuoteInOut))
        {
            Activate(_inDonut, false);
            Activate(_outCircle, true);
            return;
        }

        if(Matches(message, C.EnableTankStack, C.QuoteTankStack))
        {
            Activate(_stackMarker, true);
            return;
        }

        if(Matches(message, C.EnableSpreadTank, C.QuoteSpreadTank))
        {
            Activate(_spreadMarker, false);
            return;
        }

        if(Matches(message, C.EnableInSpread, C.QuoteInSpread))
        {
            Activate(_inDonut, false);
            Activate(_spreadMarker, true);
            return;
        }

        if(Matches(message, C.EnableInOutSpread, C.QuoteInOutSpread))
        {
            Activate(_inDonut, false);
            Activate(_outCircle, true);
            Activate3(_spreadMarker);
            return;
        }

        if(Matches(message, C.EnableInSpreadStack, C.QuoteInSpreadStack))
        {
            Activate(_inDonut, false);
            Activate(_spreadMarker, true);
            Activate3(_stackMarker);
            return;
        }

        if(Matches(message, C.EnableOutStackSpread, C.QuoteOutStackSpread))
        {
            Activate(_outCircle, false);
            Activate(_stackMarker, true);
            Activate3(_spreadMarker);
            return;
        }

        if(Matches(message, C.EnableOutSpreadStack, C.QuoteOutSpreadStack))
        {
            Activate(_outCircle, false);
            Activate(_spreadMarker, true);
            Activate3(_stackMarker);
        }
    }

    public override void OnSettingsDraw()
    {
        DrawQuote("Spread → In", ref C.EnableSpreadIn, C.QuoteSpreadIn);
        DrawQuote("Spread → Out", ref C.EnableSpreadOut, C.QuoteSpreadOut);
        DrawQuote("Stack → In", ref C.EnableStackIn, C.QuoteStackIn);
        DrawQuote("Stack → Out", ref C.EnableStackOut, C.QuoteStackOut);
        DrawQuote("In → Stack", ref C.EnableInStack, C.QuoteInStack);
        DrawQuote("In → Out", ref C.EnableInOut, C.QuoteInOut);
        DrawQuote("Away from Tank → Stack", ref C.EnableTankStack, C.QuoteTankStack);
        DrawQuote("Spread → Away from Tank", ref C.EnableSpreadTank, C.QuoteSpreadTank);
        DrawQuote("Spread → In (stars)", ref C.EnableSpreadInStars, C.QuoteSpreadInStars);
        DrawQuote("In → Spread", ref C.EnableInSpread, C.QuoteInSpread);
        DrawQuote("In → Out → Spread", ref C.EnableInOutSpread, C.QuoteInOutSpread);
        DrawQuote("In → Spread → Stack", ref C.EnableInSpreadStack, C.QuoteInSpreadStack);
        DrawQuote("Out → Stack → Spread", ref C.EnableOutStackSpread, C.QuoteOutStackSpread);
        DrawQuote("Out → Spread → Stack", ref C.EnableOutSpreadStack, C.QuoteOutSpreadStack);
    }

    #endregion

    #region Private Method

    // Match the quote in EN, JP, DE, or FR. Narrow no-break spaces count as regular spaces.
    private static bool Matches(string message, bool enabled, InternationalString quote)
    {
        if(!enabled)
            return false;

        var text = message.Replace('\n', ' ').Replace('\u00A0', ' ');
        return StartsWith(text, quote.En)
            || StartsWith(text, quote.Jp)
            || StartsWith(text, quote.De)
            || StartsWith(text, quote.Fr);
    }

    // Skip a blank language so an empty field does not match every line.
    private static bool StartsWith(string message, string quote)
        => !string.IsNullOrEmpty(quote) && message.StartsWith(quote, StringComparison.Ordinal);

    // Show the marker now, or after the first marker, then hide it.
    private void Activate(Element? element, bool later)
    {
        if(element == null)
        {
            return;
        }

        if(later)
        {
            Controller.Schedule(() => element.Enabled = true, 6000);
            Controller.Schedule(() => element.Enabled = false, 9000);
        }
        else
        {
            element.Enabled = true;
            Controller.Schedule(() => element.Enabled = false, 6000);
        }
    }

    // Show the third marker after the second one ends.
    private void Activate3(Element? element)
    {
        if(element == null)
        {
            return;
        }

        Controller.Schedule(() => element.Enabled = true, 9000);
        Controller.Schedule(() => element.Enabled = false, 12000);
    }

    // Draw one quote as an editable international string.
    private static void DrawQuote(string label, ref bool enabled, InternationalString quote)
    {
        ImGui.Checkbox($"##{label}", ref enabled);
        ImGui.SameLine();
        if(!enabled)
            ImGui.BeginDisabled();
        ImGui.SetNextItemWidth(400f);
        quote.ImGuiEditNoDefault();
        ImGui.SameLine();
        ImGuiEx.Text(label);
        if(!enabled)
            ImGui.EndDisabled();
    }

    #endregion
}
