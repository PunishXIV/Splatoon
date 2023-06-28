using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX
{
    internal unsafe interface IItemDescriptor<T> where T:unmanaged
    {
        internal string Name { get; }
        internal void Transform(T* objectPtr);
    }
}
