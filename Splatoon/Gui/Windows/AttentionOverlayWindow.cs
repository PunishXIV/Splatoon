using Dalamud.Interface.Windowing;
using ECommons.SimpleGui;
using System;
using System.Collections.Generic;
using System.Text;

namespace Splatoon.Gui.Windows;

public class AttentionOverlayWindow : Window
{
    public AttentionOverlayWindow() : base($"###SplatoonAttention", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
        this.AllowPinning = false;
        this.DisableFadeInFadeOut = true;
        this.DisableWindowSounds = true;
    }

    public override bool DrawConditions()
    {
        return false;
    }

    public override void Draw()
    {
        
    }
}
