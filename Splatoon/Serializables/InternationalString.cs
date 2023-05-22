using Dalamud;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons.LanguageHelpers;
using Splatoon.Utils;
using System.ComponentModel;

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

    public string Get(string Default = "")
    {
        if (Svc.Data.Language == ClientLanguage.English) return En == string.Empty ? Default : En;
        if (Svc.Data.Language == ClientLanguage.Japanese) return Jp == string.Empty ? Default : Jp;
        if (Svc.Data.Language == ClientLanguage.German) return De == string.Empty ? Default : De;
        if (Svc.Data.Language == ClientLanguage.French) return Fr == string.Empty ? Default : Fr;
        return Other == string.Empty ? Default : Other;
    }

    internal ref string CurrentLangString
    {
        get
        {
            if (Svc.Data.Language == ClientLanguage.English)
            {
                return ref En;
            }
            else if (Svc.Data.Language == ClientLanguage.Japanese)
            {
                return ref Jp;
            }
            else if (Svc.Data.Language == ClientLanguage.German)
            {
                return ref De;
            }
            else if (Svc.Data.Language == ClientLanguage.French)
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
        if (ImGui.BeginCombo($"##{guid}", Get(DefaultValue)))
        {
            ImGuiEx.ImGuiLineCentered($"line{guid}", delegate
            {
                ImGuiEx.Text("International string".Loc());
            });
            EditLangSpecificString(ClientLanguage.English, ref En);
            EditLangSpecificString(ClientLanguage.Japanese, ref Jp);
            EditLangSpecificString(ClientLanguage.French, ref Fr);
            EditLangSpecificString(ClientLanguage.German, ref De);
            if (!Svc.Data.Language.EqualsAny(ClientLanguage.English, ClientLanguage.Japanese, ClientLanguage.German, ClientLanguage.French))
            {
                EditLangSpecificString(Svc.Data.Language, ref Other);
            }
            else
            {
                if (Other != "")
                {
                    EditLangSpecificString((ClientLanguage)(-1), ref Other);
                }
            }

            SImGuiEx.SizedText("Default:".Loc(), 100);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(300f);
            ImGui.InputText($"##{guid}default", ref DefaultValue, 1000);
            ImGuiComponents.HelpMarker("Default value will be applied when language-specific is missing.".Loc());
            ImGui.EndCombo();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            if (!helpMessage.IsNullOrEmpty())
            {
                ImGuiEx.Text(helpMessage + "\n");
            }
            ImGuiEx.Text(ImGuiColors.DalamudGrey, "International string\nFor your current language value is:".Loc());
            ImGuiEx.Text(Get(DefaultValue));
            ImGui.EndTooltip();

        }
    }

    public bool IsEmpty()
    {
        return En.IsNullOrEmpty() && Jp.IsNullOrEmpty() && De.IsNullOrEmpty() && Fr.IsNullOrEmpty() && Other.IsNullOrEmpty();
    }

    void EditLangSpecificString(ClientLanguage language, ref string str)
    {
        var col = false;
        if (str == string.Empty)
        {
            col = true;
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey3);
        }
        else if (Svc.Data.Language == language)
        {
            col = true;
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
        }
        SImGuiEx.SizedText($"{language.ToString().Loc()}:", 100);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300f);
        ImGui.InputText($"##{guid}{language}", ref str, 1000);
        if (col)
        {
            ImGui.PopStyleColor();
        }
    }
}
