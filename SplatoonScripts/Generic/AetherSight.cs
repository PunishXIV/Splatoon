using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Generic;
public class AetherSight : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;

    public override Metadata? Metadata { get; } = new(2, "NightmareXIV");
    public override void OnUpdate()
    {
        Controller.ClearRegisteredElements();
        var i = 0;
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            i++;
            if(x.IsCasting)
            {
                var data = Svc.Data.GetExcelSheet<Action>()!.GetRowOrDefault(x.CastActionId);
                if(data != null && (data.Value.EffectRange < 30 || !data.Value.CastType.EqualsAny<byte>(2, 5)))
                {
                    if(data.Value.CastType == 2) //circle
                    {
                        Controller.RegisterElement($"Circle{i}", new(1)
                        {
                            refActorComparisonType = 2,
                            refActorObjectID = x.EntityId,
                            radius = data.Value.EffectRange,
                        });
                    }
                    else if(data.Value.CastType == 3)//cone
                    {
                        var angle = DetermineConeAngle(data.Value);
                        Controller.RegisterElement($"Cone{i}", new(4)
                        {
                            refActorComparisonType = 2,
                            refActorObjectID = x.EntityId,
                            radius = data.Value.EffectRange + x.HitboxRadius,
                            coneAngleMin = -angle / 2,
                            coneAngleMax = angle / 2,
                        });
                    }
                }
            }
        }
    }

    private int DetermineConeAngle(Lumina.Excel.Sheets.Action data)
    {
        var omen = data.Omen.ValueNullable;
        if(omen == null)
        {
            PluginLog.Log($"[AutoHints] No omen data for {data.RowId} '{data.Name}'...");
            return 180;
        }
        var path = omen.Value.Path.ToString();
        var pos = path.IndexOf("fan", StringComparison.Ordinal);
        if(pos < 0 || pos + 6 > path.Length)
        {
            PluginLog.Log($"[AutoHints] Can't determine angle from omen ({path}/{omen.Value.PathAlly}) for {data.RowId} '{data.Name}'...");
            return 180;
        }

        if(!int.TryParse(path.AsSpan(pos + 3, 3), out var angle))
        {
            PluginLog.Log($"[AutoHints] Can't determine angle from omen ({path}/{omen.Value.PathAlly}) for {data.RowId} '{data.Name}'...");
            return 180;
        }

        return angle;
    }
}
