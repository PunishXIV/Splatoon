using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.SplatoonScripting
{
    public class OverrideData : IEzConfig
    {
        public Dictionary<string, Element> Elements = new();
    }
}
