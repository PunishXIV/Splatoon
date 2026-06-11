using Dalamud.Interface.Windowing;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Splatoon.Gui.Windows;

internal class PinnedElementEdit : Window
{
    internal Layout DummyLayout = new();
    internal Element EditingElement;
    internal SplatoonScript Script;
    internal string Key;
    public PinnedElementEdit() : base("###Pinned element editor")
    {
        SizeConstraints = new()
        {
            MinimumSize = new(200, 200),
            MaximumSize = new(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        if(EditingElement != null && Script != null)
        {
            var name = EditingElement.Name;
            P.ConfigGui.LayoutDrawElement(DummyLayout, EditingElement, true);
            if(Script.Controller.ElementNamesAsKeys.Contains(name))
            {
                EditingElement.Name = name;
            }
        }
        else
        {
            ImGuiEx.Text($"An error has occurred.");
        }
    }

    public override void OnClose()
    {
        Script.Controller.SaveOverrides();
        Notify.Info("Override saved");
        Script.Controller.ApplyOverrides();
        EditingElement = null;
        Script = null;
    }

    internal void Open(SplatoonScript s, string name)
    {
        Key = name;
        if(EditingElement != null && Script != null)
        {
            OnClose();
        }
        EditingElement = s.InternalData.Overrides.Elements[name];
        Script = s;
        WindowName = $"Editing element [{name}] from {s.InternalData.FullName}###Pinned element editor";
        IsOpen = true;
    }

    public override bool DrawConditions()
    {
        return P.s2wInfo == null;
    }

    public override void Update()
    {
        Script.Controller.ApplySingleElementOverride(Key, EditingElement);
    }
}
