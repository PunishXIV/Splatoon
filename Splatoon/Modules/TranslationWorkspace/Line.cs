using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Modules.TranslationWorkspace;
[Serializable]
public unsafe sealed class Line
{
    public Line(string text)
    {
        Kind = LineKind.Text;
        Text = text;
    }

    public Line(Layout layout)
    {
        Kind = LineKind.Layout;
        Layout = layout;
    }

    public LineKind Kind;
    public string Text;
    public Layout Layout;
}