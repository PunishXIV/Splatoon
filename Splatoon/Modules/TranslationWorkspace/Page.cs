using ECommons;
using Newtonsoft.Json;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Modules.TranslationWorkspace;
[Serializable]
public unsafe sealed class Page
{
    internal string ID = Guid.NewGuid().ToString();
    public string Name = $"Imported page at {DateTime.Now}";
    public List<Line> Lines = [];

    public Page() { }

    public Page(string inputText)
    {
        var split = inputText.ReplaceLineEndings("\n").Split("\n");
        foreach(var x in split)
        {
            if(x.StartsWith("~Lv2~"))
            {
                try
                {
                    if(ScriptingEngine.TryDecodeLayout(x, out var layout))
                    {
                        if(!layout.IsValid()) throw new InvalidDataException("Layout data was corrupted");
                        Lines.Add(new(layout));
                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException("Layout data was invalid");
                    }
                }
                catch(Exception e)
                {
                    PluginLog.Error($"Attempted to deserialize {x} as layout, but failed. Assuming string.");
                    e.Log();
                }
            }
            Lines.Add(new(x));
        }
    }
}