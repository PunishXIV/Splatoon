using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Splatoon.Memory;

public class Marking
{
    public unsafe static long GetMarker(uint index) => MarkingController.Instance()->MarkerArray[index];

    public unsafe static bool HaveMark(Character obj, uint index)
    {
        if (obj.Struct()->CharacterData.ModelCharaId != 0)
        {
            if (Svc.ClientState.LocalPlayer.ObjectId == GetMarker(index))
            {
                return true;
            }
        }
        else
        {
            if (obj.ObjectId == GetMarker(index))
            {
                return true;
            }
        }

        return false;
    }
    private Dictionary<long, string> markers = new Dictionary<long, string>()
    {
        { GetMarker(0), "attack1" },
        { GetMarker(1), "attack2" },
        { GetMarker(2), "attack3" },
        { GetMarker(3), "attack4" },
        { GetMarker(4), "attack5" },
        { GetMarker(5), "bind1" },
        { GetMarker(6), "bind2" },
        { GetMarker(7), "bind3" },
        { GetMarker(8), "stop1" },
        { GetMarker(9), "stop2" },
        { GetMarker(10), "square" },
        { GetMarker(11), "circle" },
        { GetMarker(12), "cross" },
        { GetMarker(13), "triangle" },
        { GetMarker(14), "attack6" },
        { GetMarker(15), "attack7" },
        { GetMarker(16), "attack8" },

    };

    public string Mark(uint objectid)
    {
        if (markers.TryGetValue(objectid, out string attack))
        {
            return attack;
        }
        else
        {
            return "无标记";
        }
    }

    public static unsafe GameObject GetPlayer(string x)
    {
        var ph = FakePronoun.Resolve(x);
        if (ph != null)
        {
            var obj = Svc.Objects.CreateObjectReference((nint)ph);
            return obj;
        }
        return null;
    }
}