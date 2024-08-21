using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using Lumina.Excel.GeneratedSheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace SplatoonScriptsOfficial.Generic;
public class AetherSight : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override void OnUpdate()
    {
        this.Controller.ClearRegisteredElements();
        var i = 0;
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            i++;
            if(x.IsCasting)
            {
                var data = Svc.Data.GetExcelSheet<Action>()!.GetRow(x.CastActionId);
                if(data != null && (data.EffectRange < 30 || !data.CastType.EqualsAny<byte>(2, 5)))
                {
                    if(data.CastType == 2) //circle
                    {
                        this.Controller.RegisterElement($"Circle{i}", new(1)
                        {
                            refActorComparisonType = 2,
                            refActorObjectID = x.EntityId,
                            radius = data.EffectRange,
                        });
                    }
                    else if(data.CastType == 3)//cone
                    {
                        var angle = DetermineConeAngle(data);
                        this.Controller.RegisterElement($"Cone{i}", new(4)
                        {
                            refActorComparisonType = 2,
                            refActorObjectID = x.EntityId,
                            radius = data.EffectRange + x.HitboxRadius,
                            coneAngleMin = -angle / 2,
                            coneAngleMax = angle / 2,
                        });
                    }
                }
            }
        }
    }

    private int DetermineConeAngle(Lumina.Excel.GeneratedSheets.Action data)
    {
        var omen = data.Omen.Value;
        if(omen == null)
        {
            PluginLog.Log($"[AutoHints] No omen data for {data.RowId} '{data.Name}'...");
            return 180;
        }
        var path = omen.Path.ToString();
        var pos = path.IndexOf("fan", StringComparison.Ordinal);
        if(pos < 0 || pos + 6 > path.Length)
        {
            PluginLog.Log($"[AutoHints] Can't determine angle from omen ({path}/{omen.PathAlly}) for {data.RowId} '{data.Name}'...");
            return 180;
        }

        if(!int.TryParse(path.AsSpan(pos + 3, 3), out var angle))
        {
            PluginLog.Log($"[AutoHints] Can't determine angle from omen ({path}/{omen.PathAlly}) for {data.RowId} '{data.Name}'...");
            return 180;
        }

        return angle;
    }
}
