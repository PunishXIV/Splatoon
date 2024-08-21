using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Structures;
public readonly record struct CachedTetherInfo
{
    public readonly long SpawnTime = Environment.TickCount64;
    public readonly int Param1;
    public readonly int Param2;
    public readonly int Param3;
    public readonly uint Target;

    public CachedTetherInfo()
    {
    }

    public CachedTetherInfo(int param1, int param2, int param3, uint target) : this()
    {
        Param1 = param1;
        Param2 = param2;
        Param3 = param3;
        Target = target;
    }

    public long Age
    {
        get
        {
            return Environment.TickCount64 - SpawnTime;
        }
    }

    public float AgeF
    {
        get
        {
            return (float)Age / 1000f;
        }
    }

    public bool ParamEqual(CachedTetherInfo other)
    {
        return other.Param1 == Param1 && other.Param2 == Param2 && other.Param3 == Param3 && other.Target == Target;
    }

    public bool ParamEqual(int p1, int p2, int p3, uint target)
    {
        return p1 == Param1 && p2 == Param2 && p3 == Param3 && target == Target;
    }

    public bool ParamEqual(int p1, int p2, int p3)
    {
        return p1 == Param1 && p2 == Param2 && p3 == Param3;
    }
}
