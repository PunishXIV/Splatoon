using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.SplatoonScripting;
[Serializable]
public class ExportedScriptConfiguration
{
    public string TargetScriptName;
    public string ConfigurationName;
    public byte[] Configuration;
    public byte[] Overrides;
}
