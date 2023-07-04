using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX.Interfaces
{
    internal unsafe interface ItemDescriptor<T> where T : unmanaged
    {
        internal string Name { get; }
        internal void Transform(T* objectPtr);
        internal Vector3 Position { get; }
    }
}
