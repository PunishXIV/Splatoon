using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

namespace Splatoon;

[Serializable]
public class InternationalString
{
    [NonSerialized] internal string guid = Guid.NewGuid().ToString();
    [DefaultValue("")] public string En = string.Empty;
    [DefaultValue("")] public string Jp = string.Empty;
    [DefaultValue("")] public string De = string.Empty;
    [DefaultValue("")] public string Fr = string.Empty;
    [DefaultValue("")] public string Other = string.Empty;

    public InternationalString() { }

    public InternationalString(string en = "", string jp = "", string de = "", string fr = "", string other = "")
    {
        this.En = en ?? string.Empty;
        this.Jp = jp ?? string.Empty;
        this.De = de ?? string.Empty;
        this.Fr = fr ?? string.Empty;
        this.Other = other ?? string.Empty;
    }

    /// <summary>
    /// If you want to print strings depending on game locale, use this.
    /// </summary>
    /// <param name="default"></param>
    /// <param name="en"></param>
    /// <param name="jp"></param>
    /// <param name="de"></param>
    /// <param name="fr"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static string Print(string @default = "", string en = "", string jp = "", string de = "", string fr = "", string other = "")
    {
        return new InternationalString(en, jp, de, fr, other).Get(@default);
    }

    public string Get(string defaultString = "", ClientLanguage? language = null)
    {
        Normalize();
        defaultString ??= string.Empty;
        language ??= Svc.Data.Language;
        var ret = language switch
        {
            ClientLanguage.English => En,
            ClientLanguage.Japanese => Jp,
            ClientLanguage.German => De,
            ClientLanguage.French => Fr,
            _ => Other
        };

        if(!ret.IsNullOrEmpty())
            return ret;
        if(!defaultString.IsNullOrEmpty())
            return defaultString;
        if(!En.IsNullOrEmpty())
            return En;
        if(!Other.IsNullOrEmpty())
            return Other;
        if(!Jp.IsNullOrEmpty())
            return Jp;
        if(!De.IsNullOrEmpty())
            return De;
        if(!Fr.IsNullOrEmpty())
            return Fr;
        return string.Empty;
    }

    private void Normalize()
    {
        guid ??= Guid.NewGuid().ToString();
        En ??= string.Empty;
        Jp ??= string.Empty;
        De ??= string.Empty;
        Fr ??= string.Empty;
        Other ??= string.Empty;
    }

    internal ref string CurrentLangString
    {
        get
        {
            if(Svc.Data.Language == ClientLanguage.English)
            {
                return ref En;
            }
            else if(Svc.Data.Language == ClientLanguage.Japanese)
            {
                return ref Jp;
            }
            else if(Svc.Data.Language == ClientLanguage.German)
            {
                return ref De;
            }
            else if(Svc.Data.Language == ClientLanguage.French)
            {
                return ref Fr;
            }
            else
            {
                return ref Other;
            }
        }
    }

    public void ImGuiEdit(ref string DefaultValue, string helpMessage = null)
    {
        Normalize();
        DefaultValue ??= string.Empty;
        if(ImGui.BeginCombo($"##{guid}", Get(DefaultValue)))
        {
            ImGuiEx.LineCentered($"line{guid}", delegate
            {
                ImGuiEx.Text("International string".Loc());
            });
            EditLangSpecificString(ClientLanguage.English, ref En);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", En, x => En = x);
            EditLangSpecificString(ClientLanguage.Japanese, ref Jp);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", Jp, x => Jp = x);
            EditLangSpecificString(ClientLanguage.French, ref Fr);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", Fr, x => Fr = x);
            EditLangSpecificString(ClientLanguage.German, ref De);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", De, x => De = x);
            if(!Svc.Data.Language.EqualsAny(ClientLanguage.English, ClientLanguage.Japanese, ClientLanguage.German, ClientLanguage.French))
            {
                EditLangSpecificString(Svc.Data.Language, ref Other);
                ImGuiEx.DragDropRepopulateClass("RepopIStr", Other, x => Other = x);
            }
            else
            {
                if(Other != "")
                {
                    EditLangSpecificString((ClientLanguage)(-1), ref Other);
                }
            }

            ImGuiUtils.SizedText("Default:".Loc(), 100);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(300f);
            ImGui.InputText($"##{guid}default", ref DefaultValue, 1000);
            ImGuiComponents.HelpMarker("Default value will be applied when language-specific is missing.".Loc());
            ImGui.EndCombo();
        }
        if(ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            if(!helpMessage.IsNullOrEmpty())
            {
                ImGuiEx.Text(helpMessage + "\n");
            }
            ImGuiEx.Text(ImGuiColors.DalamudGrey, "International string\nFor your current language value is:".Loc());
            ImGuiEx.Text(Get(DefaultValue));
            ImGui.EndTooltip();

        }
    }

    public void ImGuiEdit(string helpMessage = null)
    {
        Normalize();
        if(ImGui.BeginCombo($"##{guid}", Get()))
        {
            ImGuiEx.LineCentered($"line{guid}", delegate
            {
                ImGuiEx.Text("International string".Loc());
            });
            EditLangSpecificString(ClientLanguage.English, ref En);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", En, x => En = x);
            ImGuiEx.HelpMarker("This will be default value", ImGuiColors.DalamudOrange, FontAwesomeIcon.Globe.ToIconString());
            EditLangSpecificString(ClientLanguage.Japanese, ref Jp);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", Jp, x => Jp = x);
            EditLangSpecificString(ClientLanguage.French, ref Fr);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", Fr, x => Fr = x);
            EditLangSpecificString(ClientLanguage.German, ref De);
            ImGuiEx.DragDropRepopulateClass("RepopIStr", De, x => De = x);
            if(!Svc.Data.Language.EqualsAny(ClientLanguage.English, ClientLanguage.Japanese, ClientLanguage.German, ClientLanguage.French))
            {
                EditLangSpecificString(Svc.Data.Language, ref Other);
                ImGuiEx.DragDropRepopulateClass("RepopIStr", Other, x => Other = x);
            }
            else
            {
                if(Other != "")
                {
                    EditLangSpecificString((ClientLanguage)(-1), ref Other);
                }
            }
            ImGui.EndCombo();
        }
        if(ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            if(!helpMessage.IsNullOrEmpty())
            {
                ImGuiEx.Text(helpMessage + "\n");
            }
            ImGuiEx.Text(ImGuiColors.DalamudGrey, "International string\nFor your current language value is:".Loc());
            ImGuiEx.Text(Get());
            ImGui.EndTooltip();

        }
    }

    public bool IsEmpty()
    {
        Normalize();
        return En.IsNullOrEmpty() && Jp.IsNullOrEmpty() && De.IsNullOrEmpty() && Fr.IsNullOrEmpty() && Other.IsNullOrEmpty();
    }

    public ref string GetRefString(ClientLanguage language)
    {
        Normalize();
        if(language == ClientLanguage.English) return ref En;
        else if(language == ClientLanguage.Japanese) return ref Jp;
        else if(language == ClientLanguage.German) return ref De;
        else if(language == ClientLanguage.French) return ref Fr;
        else return ref Other;
    }

    private void EditLangSpecificString(ClientLanguage language, ref string str)
    {
        str ??= string.Empty;
        var col = false;
        if(str == string.Empty)
        {
            col = true;
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
        }
        else if(Svc.Data.Language == language)
        {
            col = true;
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
        }
        ImGuiUtils.SizedText($"{language.ToString().Loc()}:", 100);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300f);
        ImGui.InputText($"##{guid}{language}", ref str, 2000);
        if(col)
        {
            ImGui.PopStyleColor();
        }
    }
}
