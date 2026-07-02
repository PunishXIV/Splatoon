using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;
using ECommons.SimpleGui;
using Splatoon.Serializables;
using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;

namespace Splatoon.Gui.Windows;

internal class AttentionOverlayWindow : Window, IDisposable
{
    private long OpenedAt = 0;
    public string Title = "";
    public Vector2 TableSize;
    private IFontHandle Font;
    static ImGuiWindowFlags SharedFlags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysUseWindowPadding;

    public AttentionOverlayWindow() : base($"###SplatoonAttention", SharedFlags | ImGuiWindowFlags.NoBackground, true)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
        IsOpen = true;
        RespectCloseHotkey = false;
        AllowPinning = false;
        DisableFadeInFadeOut = true;
        DisableWindowSounds = true;
        ShowCloseButton = false;
        AllowClickthrough = false;
        AllowBackgroundBlur = false;
        TitleBarButtons.Clear();
        Size = ImGuiHelpers.MainViewport.Size;
        Position = new(0, 0);
        RebuildFont();
    }

    public List<(Action Action, bool Centered)> ActionQueueCommand = [];

    private IDisposable FontDispose = null;

    public void RebuildFont()
    {
        Font?.Dispose();
        Font = null;
        if(P.Config.AttentionFontSize != 1f)
        {
            Font = Svc.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(
            Svc.PluginInterface.UiBuilder.DefaultFontSpec.SizePx * P.Config.AttentionFontSize.ValidateRange(0.1f, 10f))));
        }
    }

    public override void PreDraw()
    {
        Size = ImGuiHelpers.MainViewport.Size;
        if(Font?.Available == true)
        {
            FontDispose = Font.Push();
        }
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw()
    {
        ActionQueueCommand.Clear();
        FontDispose?.Dispose();
        FontDispose = null;
        ImGui.PopStyleVar();
    }

    private bool ShouldDisplay = false;

    public override void PreOpenCheck()
    {
        if(ActionQueueCommand.Count == 0)
        {
            OpenedAt = Environment.TickCount64;
            ShouldDisplay = false;
            TableSize = default;
            RowWidth.Clear();
        }
        else
        {
            bool ret;

            var time = Environment.TickCount64 - OpenedAt;
            var tAppear = 400;
            if(!P.Config.AttentionNoAnimate && time < tAppear * 3)
            {
                ret = time % tAppear < tAppear / 2;
            }
            else
            {
                ret = true;
            }
            if(!ret)
            {
                ActionQueueCommand.Clear();
            }
            ShouldDisplay = ret;
        }
    }

    public override bool DrawConditions()
    {
        return ShouldDisplay;
    }

    public override void Draw()
    {
        var t = TimeSpan.FromMilliseconds(Environment.TickCount64 - OpenedAt);
        var titleBar = $"{Title} [{(int)t.TotalMinutes:D2}:{t.Seconds:D2}]";
        var position = new Vector2(P.Config.AttentionBasePositionX switch
        {
            WindowBasePosition.Start => 0,
            WindowBasePosition.End => ImGuiHelpers.MainViewport.Size.X - TableSize.X,
            _ => (ImGuiHelpers.MainViewport.Size.X / 2) - (TableSize.X / 2)
        }, P.Config.AttentionBasePositionY switch
        {
            WindowBasePosition.Middle => (ImGuiHelpers.MainViewport.Size.Y / 2) - (TableSize.Y / 2),
            WindowBasePosition.End => ImGuiHelpers.MainViewport.Size.Y - TableSize.Y,
            _ => 0
        }) + P.Config.AttentionBaseOffset;
        ImGui.SetCursorPos(position);
        var bgColor = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg].ToUint();
        var colWidth = 0f;
        if(ImGui.BeginTable("AttentionTable", 1, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, Utils.BlendColors(bgColor, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg].ToUint()));
            DrawCenteredRow(-1, () => ImGuiEx.Text(titleBar));
            for(var i = 0; i < ActionQueueCommand.Count; i++)
            {
                var x = ActionQueueCommand[i];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, bgColor);
                ImGui.BeginGroup();
                if(x.Centered)
                {
                    DrawCenteredRow(i, x.Action);
                }
                else
                {
                    try
                    {
                        x.Action();
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                }
                ImGui.EndGroup();
                var c = ImGui.GetItemRectSize().X + ImGui.GetStyle().CellPadding.X;
                if(c > colWidth) colWidth = c;
            }
            ImGui.EndTable();
            ImGui.SameLine(0, 0);
        }
        TableSize = new(colWidth, ImGui.GetItemRectSize().Y);
    }

    private Dictionary<int, float> RowWidth = [];
    private void DrawCenteredRow(int rowIndex, Action drawContent)
    {
        RowWidth.TryGetValue(rowIndex, out var lastWidth);
        var offset = MathF.Max(0f, (ImGui.GetContentRegionAvail().X - lastWidth) * 0.5f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.BeginGroup();
        try
        {
            drawContent();
        }
        catch(Exception e)
        {
            e.Log();
        }
        ImGui.EndGroup();
        RowWidth[rowIndex] = ImGui.GetItemRectSize().X;
    }

    public void Dispose()
    {
        Font?.Dispose();
    }
}
