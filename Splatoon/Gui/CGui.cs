using Dalamud.Interface.Colors;
using ECommons;
using ECommons.Funding;
using ECommons.LanguageHelpers;
using Newtonsoft.Json;
using PInvoke;
using Splatoon.ConfigGui;
using Splatoon.Gui;
using Splatoon.Gui.Scripting;
using Splatoon.Memory;
using Splatoon.SplatoonScripting;
using Splatoon.Utility;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web;

namespace Splatoon;

// Master class
unsafe partial class CGui:IDisposable
{
    Dictionary<uint, string> ActionNames;
    Dictionary<uint, string> BuffNames;
    internal const float WidthLayout = 150f;
    internal static float WidthElement
    {
        get
        {
            return 130f.Scale();
        }
    }
    internal const float WidthCombo = 200f;
    internal readonly Splatoon p;
    public bool Open = false;
    bool WasOpen = false;
    float RightWidth = 0;
    internal string TabRequest = null;

    public CGui(Splatoon p)
    {
        this.p = p;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        ActionNames = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().ToDictionary(x => x.RowId, x => $"{x.RowId} | {x.Name}");
        BuffNames = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Status>().ToDictionary(x => x.RowId, x => $"{x.RowId} | {x.Name}");
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }
            
    void Draw()
    {
        if (p.s2wInfo != null || P.PinnedElementEditWindow.IsOpen) return;
        if (!Open) 
        { 
            if(WasOpen)
            {
                p.Config.Save();
                WasOpen = false;
                Notify.Success("Configuration saved".Loc());
                if(p.Config.verboselog) p.Log("Configuration saved");
                P.SaveArchive();
                ScriptingProcessor.Scripts.Each(x => x.InternalData.UnconditionalDraw = false);
            }
            return;
        }
        else
        {
            if (!WasOpen)
            {
                p.Config.Backup();
            }
            if(p.s2wInfo == null && Svc.PluginInterface.UiBuilder.FrameCount % 600 == 0)
            {
                p.Config.Save();
                //p.Log("Configuration autosaved");
            }
        }
        WasOpen = true;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(700, 200));
        var titleColored = false;
        var ctspan = TimeSpan.FromMilliseconds(Environment.TickCount64 - p.CombatStarted);
        var title = $"Splatoon v{p.loader.splatoonVersion} | {GenericHelpers.GetTerritoryName(Svc.ClientState.TerritoryType).Replace("| ", "")} | {(p.CombatStarted == 0?"Not in combat".Loc(): $"{Loc("Combat")}: {ctspan.Minutes:D2}{(ctspan.Milliseconds < 500?":":" ")}{ctspan.Seconds:D2} ({(int)ctspan.TotalSeconds}.{(ctspan.Milliseconds / 100):D1}s)")} | {Loc("Phase")}: {p.Phase} | {Loc("Scene")}: {*Scene.ActiveScene} | {Loc("Layouts")}: {p.LayoutAmount} | {Loc("Elements")}: {p.ElementAmount} | {Utils.GetPlayerPositionXZY().X:F1}, {Utils.GetPlayerPositionXZY().Y:F1}###Splatoon";
        if (ImGui.Begin(title, ref Open))
        {
            try
            {
                if (RapidImport.RapidImportEnabled)
                {
                    RapidImport.Draw();
                }
                else
                {
                    var curCursor = ImGui.GetCursorPos();
                    ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - RightWidth);
                    RightWidth = ImGuiEx.Measure(delegate
                    {
                        ImGui.SetNextItemWidth(80f);
                        if (ImGui.BeginCombo("##phaseSelector", $"Phase ??".Loc(p.Phase)))
                        {
                            if (ImGui.Selectable("Phase 1 (doorboss)".Loc())) p.Phase = 1;
                            if (ImGui.Selectable("Phase 2 (post-doorboss)".Loc())) p.Phase = 2;
                            ImGuiEx.Text("Manual phase selection:".Loc());
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(30f);
                            var ph = p.Phase;
                            if(ImGui.DragInt("##mPSel", ref ph, 0.1f, 1, 9))
                            {
                                p.Phase = ph;
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        PatreonBanner.DrawButton();
                    }, false);
                    ImGui.SetCursorPos(curCursor);

                    ImGuiEx.EzTabBar("SplatoonSettings", null, TabRequest,
                        ("General".Loc() + "###tab1", DisplayGeneralSettings, null, true),
                        ("Render".Loc() + "###tab2", DisplayRenderers, null, true),
                        ("Layouts".Loc(), DislayLayouts, Colors.Green.ToVector4(), true),
                        ("Scripts".Loc(), TabScripting.Draw, Colors.Yellow.ToVector4(), true),
                        ("Mass Import".Loc(), RapidImport.Draw, null, true),
                        ("Tools".Loc(), delegate
                        {
                            ImGuiEx.EzTabBar("Tools",
                            ("Logger".Loc(), DisplayLogger, null, true),
                            ("Explorer".Loc(), Explorer.Draw, null, true),
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
                ImGuiEx.Text(ImGuiColors.DalamudRed, $"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        ImGui.PopStyleVar();
        if (titleColored)
        {
            ImGui.PopStyleColor(2);
        }
        ImGui.EndTabBar();
        ImGui.End();
    }

    bool Convert = false;
    string lastContent = "";
    void DisplayConversion()
    {
        ImGui.Checkbox($"Convert clipboard content from github to wiki", ref Convert);
        if(Convert)
        {
            try
            {
                var content = ImGui.GetClipboardText();
                if(content != lastContent)
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        if (content.Contains("```"))
                        {
                            content = content.ReplaceFirst("```", "<pre>").ReplaceFirst("```", "</pre>");
                        }
                        else
                        {
                            break;
                        }
                    }
                    var cArray = content.Split('\n');
                    for (int i = 0; i < cArray.Length; i++)
                    {
                        if (cArray[i].StartsWith("#"))
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
            catch (Exception e)
            {
                e.Log();
            }
        }
    }

    private void HTTPExportToClipboard(Layout el)
    {
        var l = JsonConvert.DeserializeObject<Layout>(JsonConvert.SerializeObject(el));
        l.Enabled = true;
        foreach (var e in l.ElementsL) e.Enabled = true;
        var json = "~" + JsonConvert.SerializeObject(l, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        var jsonraw = "~" + JsonConvert.SerializeObject(l, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        var compressed = json.Compress();
        var base64 = json.ToBase64UrlSafe();
        ImGui.SetClipboardText(ImGui.GetIO().KeyAlt ? jsonraw : ImGui.GetIO().KeyCtrl ? HttpUtility.UrlEncode(json) : compressed.Length>base64.Length?base64:compressed);
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
        if (Utils.WorldToScreen(new Vector3(refX, refZ, refY), out var screenPos))
        {
            var point = new POINT() { x = (int)screenPos.X, y = (int)screenPos.Y };
            //Chat.Print(point.X + "/" + point.Y);
            if (User32.ClientToScreen(Process.GetCurrentProcess().MainWindowHandle, ref point))
            {
                //Chat.Print(point.X + "/" + point.Y);
                User32.SetCursorPos(point.x, point.y);
            }
        }
    }
}
