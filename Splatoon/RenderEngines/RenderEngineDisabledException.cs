using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.RenderEngines;
public class RenderEngineDisabledException : Exception
{
    public RenderEngineDisabledException() : base("Render engine was disabled by user.") { }
}
