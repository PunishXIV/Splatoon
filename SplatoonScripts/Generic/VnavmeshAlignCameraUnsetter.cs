using ECommons.EzIpcManager;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;

public class VnavmeshAlignCameraUnsetter : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;

    [EzIPC("Path.GetAlignCamera", true)] private Func<bool> GetAlignCamera;
    [EzIPC("Path.SetAlignCamera", true)] private Action<bool> SetAlignCamera;

    public override void OnSetup()
    {
        EzIPC.Init(this, "vnavmesh", SafeWrapper.AnyException);
    }

    public override void OnUpdate()
    {
        if(GetAlignCamera()) SetAlignCamera(false);
    }
}
