using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Serializables;
[Serializable]
public class LayoutSubconfiguration
{
    public Guid Guid = Guid.NewGuid();
    public string Name = "";
    public List<Element> Elements = [];
}
