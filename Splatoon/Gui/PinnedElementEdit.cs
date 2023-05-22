using Dalamud.Interface.Windowing;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Gui
{
    internal class PinnedElementEdit : Window
    {
        internal Layout DummyLayout = new();
        internal Element EditingElement;
        internal SplatoonScript Script;
        public PinnedElementEdit() : base("###Pinned element editor")
        {
            this.SizeConstraints = new()
            {
                MinimumSize = new(200, 200),
                MaximumSize = new(float.MaxValue, float.MaxValue),
            };
        }

        public override void Draw()
        {
            if(EditingElement != null && Script != null)
            {
                P.ConfigGui.LayoutDrawElement(DummyLayout, EditingElement, true);
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
            if(EditingElement != null && Script != null)
            {
                this.OnClose();
            }
            EditingElement = s.InternalData.Overrides.Elements[name];
            Script = s;
            this.WindowName = $"Editing element [{name}] from {s.InternalData.FullName}###Pinned element editor";
            this.IsOpen = true;
        }

        public override bool DrawConditions()
        {
            return P.s2wInfo == null;
        }
    }
}
