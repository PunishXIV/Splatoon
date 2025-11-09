using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.Serializables;
public enum TargetAlteration
{
    None,
    Tethered,
    Targeted,

    Closest_Player_1 = 1100,
    Closest_Player_2,
    Closest_Player_3,
    Closest_Player_4,

    Furthest_Player_1 = 2100,
    Furthest_Player_2,
    Furthest_Player_3,
    Furthest_Player_4,

    /*Enmity_Top_1 = 3100,
    Enmity_Top_2,
    Enmity_Top_3,
    Enmity_Top_4,
    Enmity_Top_5,
    Enmity_Top_6,
    Enmity_Top_7,
    Enmity_Top_8,*/
}