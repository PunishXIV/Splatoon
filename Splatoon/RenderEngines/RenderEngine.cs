using ECommons.LanguageHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Splatoon.Serializables;
using Splatoon.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines;
public abstract class RenderEngine : IDisposable
{
    internal Exception LoadError { get; set; } = null;
    internal abstract RenderEngineKind RenderEngineKind { get; }
    internal List<DisplayObject> DisplayObjects { get; private set; } = [];
    private List<DisplayObject> TempObjects = null;
    internal virtual bool CanBeDisabled { get; } = true;
    internal bool Enabled => P.Config.EnabledRenderers.Contains(RenderEngineKind);

    internal abstract void AddLine(float ax, float ay, float az, float bx, float by, float bz, float thickness, uint color, LineEnd startStyle = LineEnd.None, LineEnd endStyle = LineEnd.None);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="i"></param>
    /// <param name="forceEnable"></param>
    /// <returns>Whether anything was drawn</returns>
    internal abstract bool ProcessElement(Element e, Layout i = null, bool forceEnable = false);

    internal void AddCapturedObject(Layout l, Element e, Vector3 pos)
    {
        CapturedPositions.GetOrCreate(l.GetName()).GetOrCreate(e.GetName()).Add(pos);
    }

    internal void AddCapturedObject(Layout l, Element e, IGameObject pos)
    {
        CapturedObjectPositions.GetOrCreate(l.GetName()).GetOrCreate(e.GetName()).Add(pos);
    }

    internal void StoreDisplayObjects()
    {
        TempObjects = DisplayObjects;
        DisplayObjects = [];
    }

    internal void RestoreDisplayObjects()
    {
        DisplayObjects = TempObjects;
        TempObjects = null;
    }

    internal void DrawSettings()
    {
        ImGui.PushID(RenderEngineKind.ToString());
        if(CanBeDisabled)
        {
            if(ImGuiEx.CollectionCheckbox("Enable", RenderEngineKind, P.Config.EnabledRenderers))
            {
                if(!P.Config.EnabledRenderers.Contains(RenderEngineKind) && P.Config.RenderEngineKind == RenderEngineKind)
                {
                    DuoLog.Warning($"Because you have disabled active render engine, active render engine was set to ImGui Legacy.");
                    P.Config.RenderEngineKind = RenderEngineKind.ImGui_Legacy;
                }
            }
        }
        else
        {
            var x = true;
            ImGui.BeginDisabled();
            ImGui.Checkbox("Enable", ref x);
            ImGui.EndDisabled();
            ImGuiEx.HelpMarker("This render engine can not be disabled.");
        }
        ImGui.SameLine();
        if(ImGui.RadioButton("Default", P.Config.RenderEngineKind == RenderEngineKind))
        {
            P.Config.RenderEngineKind = RenderEngineKind;
        }
        ImGuiEx.HelpMarker($"All drawings, unless overriden per element, will be drawn using this render engine.");
        if(LoadError == null)
        {
            ImGuiEx.Text(EColor.GreenBright, $"Render engine loaded successfully.".Loc());
            if(!Enabled)
            {
                ImGuiEx.TextWrapped(EColor.OrangeBright, $"You have disabled this render engine. However, until you restart Splatoon or game, it will remain loaded but inactive.");
            }
        }
        else
        {
            ImGui.NewLine();
            if(LoadError is RenderEngineDisabledException)
            {
                ImGuiEx.HelpMarker(LoadError.ToString(), EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                ImGui.SameLine();
                ImGuiEx.TextWrapped(EColor.OrangeBright, $"Render engine was disabled by user.".Loc());
                ImGuiEx.TextWrapped(EColor.OrangeBright, $"In order to use this render engine again, you must enable it and restart Splatoon or game.".Loc());
            }
            else
            {
                ImGuiEx.HelpMarker(LoadError.ToString(), EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                ImGui.SameLine();
                ImGuiEx.TextWrapped(EColor.RedBright, $"An error occurred during loading this render engine.".Loc());
                ImGuiEx.TextWrapped(EColor.RedBright, $"Any draw calls directed to this render engine will be redirected to either default render engine or ImGui Legacy engine.".Loc());
            }
        }
        ImGui.PopID();
    }

    public abstract void Dispose();
}
