using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.VFX
{
    internal struct VFXRequest : IEquatable<VFXRequest>
    {
        internal Vector3 Position;
        internal float radius;
        internal float donutRadius;
        internal uint Color;

        public override bool Equals(object obj)
        {
            return obj is VFXRequest request && Equals(request);
        }

        public bool Equals(VFXRequest other)
        {
            return Position.Equals(other.Position) &&
                   radius == other.radius &&
                   donutRadius == other.donutRadius &&
                   Color == other.Color;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, radius, donutRadius, Color);
        }

        public static bool operator ==(VFXRequest left, VFXRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VFXRequest left, VFXRequest right)
        {
            return !(left == right);
        }
    }
}
