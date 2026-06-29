using ECommons.LanguageHelpers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Splatoon.Structures;

[Serializable]
public struct PointerLineStyle
{
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public float? ChunkLength = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public float? IntervalLength = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public float? Width = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public int? AnimationDuration = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public float? TipLength = null;
    [DefaultValue(false)] public bool Inverted = false;
    [DefaultValue(false)] public bool Double = false;
    /// <summary>
    /// MAY BE NULL
    /// </summary>
    [DefaultValue(null)] public float? Thickness = null;

    /// <summary>
    /// MAY BE NULL
    /// </summary>
    [DefaultValue(null)] public uint? Accent = null;
    /// <summary>
    /// MAY BE NULL
    /// </summary>
    [DefaultValue(null)] public uint? Background = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public float? AutoBgAccentTpSpread = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public int? TotalSegments = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public int? AccentLength = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public int? GradientStrength = null;
    /// <summary>
    /// Can not be null
    /// </summary>
    [DefaultValue(null)] public int? ColorShiftDuration = null;

    public static PointerLineStyle Empty { get; } = new();
    public static PointerLineStyle Default { get; } = new PointerLineStyle()
    {
        ChunkLength = 0f,
        IntervalLength = 0.7f,
        AnimationDuration = 500,
        AutoBgAccentTpSpread = (1f - 200f/255f) * 2,
        TipLength = 1f,
        TotalSegments = 50,
        GradientStrength = 7,
        Width = 0.25f,
        ColorShiftDuration = 20,
        AccentLength = 3,
    };
    public void EnsureDefaults()
    {
        this.ChunkLength ??= Default.ChunkLength.Value;
        this.IntervalLength ??= Default.IntervalLength.Value;
        this.AnimationDuration ??= Default.AnimationDuration.Value;
        this.AutoBgAccentTpSpread ??= Default.AutoBgAccentTpSpread.Value;
        this.TipLength ??= Default.TipLength.Value;
        this.TotalSegments ??= Default.TotalSegments.Value;
        this.GradientStrength ??= Default.GradientStrength.Value;
        this.Width ??= Default.Width.Value;
        this.AccentLength ??= Default.AccentLength.Value;
        this.ColorShiftDuration ??= Default.ColorShiftDuration.Value;
    }

    public PointerLineStyle GetStyleCombinedWithUserOverrides(Element e)
    {
        var ret = new PointerLineStyle()
        {
            Accent = this.Accent ?? P.Config.DefaultPointerLineStyle.Accent,
            AccentLength = this.AccentLength ?? P.Config.DefaultPointerLineStyle.AccentLength,
            AnimationDuration = this.AnimationDuration ?? P.Config.DefaultPointerLineStyle.AnimationDuration,
            AutoBgAccentTpSpread = this.AutoBgAccentTpSpread ?? P.Config.DefaultPointerLineStyle.AutoBgAccentTpSpread,
            Background = this.Background ?? P.Config.DefaultPointerLineStyle.Background,
            ChunkLength = this.ChunkLength ?? P.Config.DefaultPointerLineStyle.ChunkLength,
            GradientStrength = this.GradientStrength ?? P.Config.DefaultPointerLineStyle.GradientStrength,
            IntervalLength = this.IntervalLength ?? P.Config.DefaultPointerLineStyle.IntervalLength,
            Thickness = this.Thickness ?? P.Config.DefaultPointerLineStyle.Thickness ?? e.thicc,
            TipLength = this.TipLength ?? P.Config.DefaultPointerLineStyle.TipLength,
            TotalSegments = this.TotalSegments ?? P.Config.DefaultPointerLineStyle.TotalSegments,
            Width = this.Width ?? P.Config.DefaultPointerLineStyle.Width,
            ColorShiftDuration = this.ColorShiftDuration ?? P.Config.DefaultPointerLineStyle.ColorShiftDuration,
            Double = this.Double,
            Inverted = this.Inverted,
        };
        if(ret.Accent == null || ret.Background == null)
        {
            var col = Utils.GetSpreadColors(e.GetDisplayStyleWithOverride().strokeColor, ret.AutoBgAccentTpSpread.Value);
            ret.Accent ??= col.Accent;
            ret.Background ??= col.Background;
        }
        return ret;
    }

    public PointerLineStyle()
    {
    }

    public void SetAccent(uint? accent) => this.Accent = accent;
    public void SetBackground(uint? background) => this.Background = background;


    internal void DrawDefaultsEditor()
    {
        this.EnsureDefaults();
        ImGuiEx.Text("Visuals:");
        ImGui.Indent();
        DrawVisualsEdits(false);
        ImGui.Unindent();

        ImGuiEx.HelpMarker("Will use element's thickness without override.".Loc());
        ImGuiEx.Text("Colors:");
        ImGui.Indent();
        DrawColorEdits(false);
        ImGui.Unindent();
    }

    public void DrawEditor()
    {
        ImGuiEx.DragFloat(70f, "Width:".Loc(), ref Width, 0.02f, 0, 20, isLabelPrefix:true);
        ImGuiEx.Tooltip("When not enabled, will use user's default".Loc());
        ImGui.SameLine();
        /*
        ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Backward, ref this.Inverted);
        ImGuiEx.Tooltip("Reverse direction. Will point from point B to point A instead.");
        ImGui.SameLine();

        ImGuiEx.ButtonCheckbox(FontAwesomeIcon.ArrowsLeftRight, ref this.Double);
        ImGuiEx.Tooltip("Make this line double-angled, pointing towards it's middle.");
        ImGui.SameLine();
        */
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.AngleDoubleDown, "Extra...".Loc()))
        {
            ImGui.OpenPopup("ExtraTetherSettings");
        }

        if(ImGui.BeginPopup("ExtraTetherSettings"))
        {
            ImGuiEx.Text(EColor.RedBright, "Alpha feature. \nSettings may be reset without warning. \nNo safeguards for now, invalid settings will crash your game.");
            ImGuiEx.Text("Visual overrides. \nIf not overridden, will use user's default.");
            ImGui.Indent();
            DrawVisualsEdits(true);
            ImGui.Unindent();

            ImGuiEx.HelpMarker("Will use element's thickness without override.".Loc());
            ImGuiEx.Text("Color overrides. \nIf not overridden, will use user's default.");
            ImGui.Indent();
            DrawColorEdits(true);
            ImGui.Unindent();
            ImGui.EndPopup();
        }
    }

    void DrawVisualsEdits(bool showCheckbox)
    {
        ImGuiEx.DragFloat(100, "Chunk length".Loc(), ref ChunkLength, 0.02f, showCheckbox: showCheckbox);
        ImGuiEx.HelpMarker("How long arrow base is");
        ImGuiEx.DragFloat(100, "Interval length".Loc(), ref IntervalLength, 0.02f, showCheckbox: showCheckbox);
        ImGuiEx.HelpMarker("How long interval between arrows is");
        ImGuiEx.DragInt(100, "Animation duration, ms".Loc(), ref AnimationDuration, 10f, showCheckbox: showCheckbox);
        ImGuiEx.HelpMarker("How fast arrows are moving. Higher value = slower movement. 0 disables color animation entirely.".Loc());
        ImGuiEx.DragFloat(100, "Tip Length, % of chunk+interval".Loc(), ref TipLength, 0.005f, showCheckbox: showCheckbox);
        ImGuiEx.HelpMarker("How long tip of an arrow will be, in percentage from sum of chunk + interval length".Loc());
        ImGuiEx.DragFloat(100, "Thickness".Loc(), ref Thickness, 0.02f, showCheckbox: true);
    }

    void DrawColorEdits(bool showCheckbox)
    {
        ImGuiEx.ColorEdit4(0, "Accent color override", ref Accent, defaultValue:EColor.RedBright.ToUint());
        ImGuiEx.Tooltip("Defines color that will pulse over background periodically.");
        ImGuiEx.ColorEdit4(0, "Background color override", ref Background, defaultValue:EColor.White.ToUint());
        ImGuiEx.Tooltip("Defines primary color of the line.");
        ImGuiEx.DragFloat(100, "Animation autocolor spread", ref this.AutoBgAccentTpSpread, 0.005f, 0, 1, showCheckbox: showCheckbox);
        ImGuiEx.Tooltip("Derives accent and background colors from element settings. Background will become more transparent than accent by this percentage. Background is capped at 0% alpha value, while accent is capped at 100%.");
        ImGuiEx.DragInt(100, "Interval between animation", ref this.TotalSegments, 1, showCheckbox: showCheckbox);
        ImGuiEx.Tooltip("How many segments there will be between accent pulses.");
        ImGuiEx.DragInt(100, "Accent length", ref this.AccentLength, 0.05f, showCheckbox: showCheckbox);
        ImGuiEx.Tooltip("How many segments of accent color will be there.");
        ImGuiEx.DragInt(100, "Transition length", ref this.GradientStrength, 0.05f, showCheckbox: showCheckbox);
        ImGuiEx.Tooltip("How long (in segments) transition between accent and background will take.");
        ImGuiEx.DragInt(100, "Color animation duration", ref this.ColorShiftDuration, 0.05f, showCheckbox: showCheckbox);
        ImGuiEx.Tooltip("How fast color animation happens. Lower is faster. Set to 0 to completely disable color animation. ");
    }
}
