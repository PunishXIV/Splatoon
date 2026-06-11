using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.ExcelServices;
using ECommons.Funding;
using ECommons.GameFunctions;
using ECommons.GameHelpers.LegacyPlayer;
using ECommons.Interop;
using ECommons.LanguageHelpers;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using Newtonsoft.Json;
using Splatoon.ConfigGui;
using Splatoon.Gui;
using Splatoon.Gui.Scripting;
using Splatoon.Gui.Tabs;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web;
using TerraFX.Interop.Windows;

namespace Splatoon;

// Master class
internal unsafe partial class CGui : ConfigWindow
{
    private Dictionary<uint, string> ActionNames;
    private Dictionary<uint, string> BuffNames;
    internal const float WidthLayout = 150f;
    internal static float WidthElement
    {
        get
        {
            return 130f.Scale();
        }
    }
    internal const float WidthCombo = 200f;
    private float RightWidth = 0;
    internal string TabRequest = null;
    TitleBarButton SplatoonButton;
    TitleBarButton ResetButton;
    TitleBarButton PhaseButton;

    public CGui() : base("Splatoon")
    {
        EzConfigGui.Init(this);
        ActionNames = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().ToDictionary(x => x.RowId, x => $"{x.RowId} | {x.Name}");
        BuffNames = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().ToDictionary(x => x.RowId, x => $"{x.RowId} | {x.Name}");
        this.SetSizeConstraints(new Vector2(700, 200), new(float.MaxValue, float.MaxValue));

        SplatoonButton = new()
        {
            Icon = Splatoon.P.Draw ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash,
            Click = x =>
            {
                if(x == ImGuiMouseButton.Left) Splatoon.P.Draw = !P.Draw;
                SplatoonButton.Icon = Splatoon.P.Draw ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
            },
            IconOffset = new(1, 1),
            ShowTooltip = () => ImGuiEx.Tooltip("Hide rendered elements. Processing won't be stopped. Some elements may remain on screen. ")
        };
        this.TitleBarButtons.Add(SplatoonButton);

        ResetButton = new()
        {
            Icon = (FontAwesomeIcon)'\uf0e2',
            Click = x =>
            {
                Utils.Reset();
            },
            IconOffset = new(1, 1),
            ShowTooltip = () => ImGuiEx.Tooltip("Reset Splatoon state.")
        };
        this.TitleBarButtons.Add(ResetButton);

        PhaseButton = new()
        {
            Icon = (FontAwesomeIcon)FontAwesomeIcon.DiceOne,
            Click = x =>
            {
                P.Phase = P.Phase == 1 ? 2 : 1;
            },
            IconOffset = new(2, 1),
            ShowTooltip = () => ImGuiEx.Tooltip("Click to switch between phase 1 and 2.")
        };
        this.TitleBarButtons.Add(PhaseButton);
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }

    public override void OnOpen()
    {
        P.Config.Backup();
    }

    public override void OnClose()
    {
        P.Config.Save();
        //Notify.Success("Configuration saved".Loc());
        if(P.Config.verboselog)
        {
            P.Log("Configuration saved");
        }

        Splatoon.P.SaveArchive();
        ScriptingProcessor.Scripts.Each(x => x.InternalData.UnconditionalDraw = false);
    }

    public override bool DrawConditions()
    {
        return !(P.s2wInfo != null || Splatoon.P.PinnedElementEditWindow.IsOpen);
    }

    public override void Update()
    {
        try
        {
            var ctspan = TimeSpan.FromMilliseconds(Environment.TickCount64 - P.CombatStarted);
            WindowName = $"Splatoon v{P.loader.splatoonVersion} | {GenericHelpers.GetTerritoryName(Svc.ClientState.TerritoryType).Replace("| ", "")} | {(P.CombatStarted == 0 ? "Not in combat".Loc() : $"{Loc("Combat")}: {ctspan.Minutes:D2}{(ctspan.Milliseconds < 500 ? ":" : " ")}{ctspan.Seconds:D2} ({(int)ctspan.TotalSeconds}.{ctspan.Milliseconds / 100:D1}s)")} | {Loc("Phase")}: {P.Phase} | {Loc("Scene")}: {*Scene.ActiveScene} | {Loc("Layouts")}: {P.LayoutAmount} | {Loc("Elements")}: {P.ElementAmount} | {Utils.GetPlayerPositionXZY().X:F1}, {Utils.GetPlayerPositionXZY().Y:F1}###Splatoon";
            this.SplatoonButton.IconColor = Splatoon.P.Draw ? null : global::System.Environment.TickCount64 % 1000 > 500 ? global::ECommons.ImGuiMethods.EColor.RedBright : null;
            this.PhaseButton.Icon = (FontAwesomeIcon)(P.Phase == 1 ? FontAwesomeIcon.AngleRight : P.Phase == 2 ? FontAwesomeIcon.AngleDoubleRight : FontAwesomeIcon.ExclamationCircle);
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    public override void Draw()
    {
        if(P.s2wInfo == null && EzThrottler.Throttle("AutoSave", 60000))
        {
            P.Config.Save();
            //p.Log("Configuration autosaved");
        }
        try
        {
            if(RapidImport.RapidImportEnabled)
            {
                RapidImport.Draw();
            }
            else
            {
                var curCursor = ImGui.GetCursorPos();
                ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - RightWidth);
                RightWidth = ImGuiEx.Measure(delegate
                {
                    if(Svc.Condition[ConditionFlag.DutyRecorderPlayback])
                    {
                        ImGui.SetNextItemWidth(100f);
                        var col = !BasePlayer.AddressEquals(Svc.Objects.LocalPlayer);
                        if(col)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, EColor.GreenBright);
                        }

                        if(ImGui.BeginCombo("##bpo", BasePlayerOverride == "" ? "No Override" : BasePlayerOverride, ImGuiComboFlags.HeightLarge))
                        {
                            if(col)
                            {
                                ImGui.PopStyleColor();
                            }

                            if(ImGui.Selectable("No Override", BasePlayerOverride == ""))
                            {
                                BasePlayerOverride = "";
                            }

                            foreach(var x in Svc.Objects.OfType<IPlayerCharacter>().OrderBy(x => x.GetRole()).ThenBy(x => x.GetJob().IsRangedDps()).ThenBy(x => x.GetJob().IsMagicalRangedDps()))
                            {
                                if(ThreadLoadImageHandler.TryGetIconTextureWrap(x.GetJob().GetIcon(), false, out var tex))
                                {
                                    ImGui.Image(tex.Handle, new(ImGui.GetTextLineHeight()));
                                    ImGui.SameLine();
                                }
                                if(ImGui.Selectable($"{x.GetNameWithWorld()}", x.GetNameWithWorld() == BasePlayerOverride))
                                {
                                    BasePlayerOverride = x.GetNameWithWorld();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        else
                        {
                            if(col)
                            {
                                ImGui.PopStyleColor();
                            }
                        }
                        ImGui.SameLine();
                    }
                    PatreonBanner.DrawButton();
                }, false);
                ImGui.SetCursorPos(curCursor);

                ImGuiEx.EzTabBar("SplatoonSettings", null, TabRequest,
                    ("General".Loc() + "###tab1", DisplayGeneralSettings, null, true),
                    ("Render".Loc() + "###tab2", DisplayRenderers, null, true),
                    ("Layouts".Loc(), DislayLayouts, Colors.Green.ToVector4(), true),
                    ("Scripts".Loc(), TabScripting.Draw, Colors.Yellow.ToVector4(), true),
                    ("Configurations".Loc(), CGuiConfigurations.Draw, EColor.PurpleBright, true),
                    ("Projection".Loc(), TabProjection.Draw, EColor.CyanBright, true),
                    ("Tools".Loc(), delegate
                    {
                        ImGuiEx.EzTabBar("Tools",
                        ("Mass Import".Loc(), RapidImport.Draw, null, true),
                        ("Translator".Loc(), TabTranslator.Draw, null, true),
                        ("Logger".Loc(), DisplayLogger, null, true),
                        ("Explorer".Loc(), Explorer.Draw, null, true),
                        ("Map Effect".Loc(), TabMapEffect.Draw, null, true),
                        ("Archive".Loc(), DrawArchive, null, true),
                        ("Find".Loc(), TabFind.Draw, null, true),
                        ("Debug".Loc(), DisplayDebug, null, true),
                        ("Log".Loc(), InternalLog.PrintImgui, null, false),
                        ("Dynamic".Loc(), DisplayDynamicElements, null, true),
                        ("Trusted Repos".Loc(), TabTrustedRepos.Draw, null, true)
                        );
                    }, null, true),
                    ("Contribute".Loc(), Contribute.Draw, null, true)
                    //("Contributors".Loc(), TabContributors.Draw, null, true)
                    );
                TabRequest = null;
            }
        }
        catch(Exception ex)
        {
            ex.Log();
            ImGuiEx.Text(ImGuiColors.DalamudRed, $"Error: {ex.ToStringFull()}");
        }
        ImGui.EndTabBar();
    }

    private bool Convert = false;
    private string lastContent = "";
    private void DisplayConversion()
    {
        ImGui.Checkbox($"Convert clipboard content from github to wiki", ref Convert);
        if(Convert)
        {
            try
            {
                var content = ImGui.GetClipboardText();
                if(content != lastContent)
                {
                    for(var i = 0; i < 1000; i++)
                    {
                        if(content.Contains("```"))
                        {
                            content = content.ReplaceFirst("```", "<pre>").ReplaceFirst("```", "</pre>");
                        }
                        else
                        {
                            break;
                        }
                    }
                    var cArray = content.Split('\n');
                    for(var i = 0; i < cArray.Length; i++)
                    {
                        if(cArray[i].StartsWith("#"))
                        {
                            cArray[i] = cArray[i].Replace("#", "");
                        }
                        cArray[i] = cArray[i].Trim();
                    }
                    content = cArray.Join("\n");
                    lastContent = content;
                    ImGui.SetClipboardText(content);
                }
            }
            catch(Exception e)
            {
                e.Log();
            }
        }
    }

    private void HTTPExportToClipboard(Layout el)
    {
        var l = JsonConvert.DeserializeObject<Layout>(JsonConvert.SerializeObject(el));
        l.Enabled = true;
        foreach(var e in l.ElementsL)
        {
            e.Enabled = true;
        }

        var json = "~" + JsonConvert.SerializeObject(l, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        var jsonraw = "~" + JsonConvert.SerializeObject(l, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        var compressed = json.Compress();
        var base64 = json.ToBase64UrlSafe();
        ImGui.SetClipboardText(ImGui.GetIO().KeyAlt ? jsonraw : ImGui.GetIO().KeyCtrl ? HttpUtility.UrlEncode(json) : compressed.Length > base64.Length ? base64 : compressed);
    }

    private void HTTPExportToClipboard(Element el)
    {
        var l = JsonConvert.DeserializeObject<Element>(JsonConvert.SerializeObject(el)); ;
        l.Enabled = true;
        var json = JsonConvert.SerializeObject(l, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        var jsonraw = JsonConvert.SerializeObject(l, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        var compressed = json.Compress();
        var base64 = json.ToBase64UrlSafe();
        ImGui.SetClipboardText(ImGui.GetIO().KeyAlt ? jsonraw : ImGui.GetIO().KeyCtrl ? HttpUtility.UrlEncode(json) : compressed.Length > base64.Length ? base64 : compressed);
    }

    private void SetCursorTo(float refX, float refZ, float refY)
    {
        if(Utils.WorldToScreen(new Vector3(refX, refZ, refY), out var screenPos) && WindowFunctions.TryFindGameWindow(out var handle))
        {
            var point = new POINT() { x = (int)screenPos.X, y = (int)screenPos.Y };
            //Chat.Print(point.X + "/" + point.Y);
            if(TerraFX.Interop.Windows.Windows.ClientToScreen(handle, &point))
            {
                //Chat.Print(point.X + "/" + point.Y);
                TerraFX.Interop.Windows.Windows.SetCursorPos(point.x, point.y);
            }
        }
    }
}
