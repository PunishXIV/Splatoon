using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class CastExplorer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(1, "NightmareXIV");
    uint HoveredID = 0;
    bool HideExpired = false;

    public override void OnUpdate()
    {
        Controller.ClearRegisteredElements();
        var positions = new List<Vector3>();
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(x.IsCasting || x.CastActionId != 0)
            {
                if(HideExpired && !x.IsCasting && x.TotalCastTime == x.BaseCastTime) continue;
                var offsetMod = positions.Count(p => Vector3.Distance(p, x.Position) < 0.5f);
                positions.Add(x.Position);
                var elementPos = x.Position + new Vector3(0, offsetMod * 1, 0);
                var circle = CreateCircle(x);
                if(HoveredID == x.EntityId)
                {
                    circle.overlayBGColor = EColor.Red.ToUint();
                }
                circle.SetRefPosition(elementPos);
                var line = CreateLine(x);
                line.offZ = offsetMod * 1f;
                Controller.RegisterElement(Guid.NewGuid().ToString(), circle);
                Controller.RegisterElement(Guid.NewGuid().ToString(), line);
            }
        }
        HoveredID = 0;
    }

    public override void OnSettingsDraw()
    {
        ImGui.Checkbox("Hide expired casts", ref HideExpired);
        List<ImGuiEx.EzTableEntry> Entries = [];
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(x.IsCasting || x.CastActionId != 0)
            {
                if(HideExpired && !x.IsCasting && x.TotalCastTime == x.BaseCastTime) continue;
                var col = x.IsCasting ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey3;
                Entries.Add(new("Name", true, () => ImGuiEx.TextCopy(col, x.Name.ToString())));
                Entries.Add(new("Cast ID", () => ImGuiEx.TextCopy(col, $"{x.CastActionId}")));
                Entries.Add(new("Cast name", () => ImGuiEx.TextCopy(col, $"{ExcelActionHelper.GetActionName(x.CastActionId)}")));
                Entries.Add(new("Time", () => ImGuiEx.TextCopy(col, $"{x.CurrentCastTime:F1}/{x.BaseCastTime:F1}")));
                Entries.Add(new("Name ID", () => ImGuiEx.TextCopy(col, x.NameId.ToString())));
                Entries.Add(new("Data ID", () => ImGuiEx.TextCopy(col, x.DataId.ToString())));
                Entries.Add(new("Model ID", () => ImGuiEx.TextCopy(col, x.Struct()->ModelCharaId.ToString())));
                Entries.Add(new("Tar", () => ImGuiEx.Text(col, x.IsTargetable.ToString())));
                Entries.Add(new("Vis", () => ImGuiEx.Text(col, x.IsCharacterVisible().ToString())));
                Entries.Add(new("Dist", () => ImGuiEx.Text(col, $"{Vector3.Distance(Player.Position, x.Position):F1}")));
                Entries.Add(new("Find", () =>
                {
                    if(ImGui.SmallButton("Find##" + x.Address))
                    {

                    }
                    if(ImGui.IsItemHovered())
                    {
                        this.HoveredID = x.EntityId;
                    }
                }));
            }
        }
        ImGuiEx.EzTable(Entries);
    }

    Element CreateCircle(IBattleNpc b)
    {
        var ret = new Element(0)
        {
            overlayPlaceholders = true,
            overlayText = $"C: {b.CastActionId} {b.CurrentCastTime:F1}/{b.BaseCastTime:F1} ({ExcelActionHelper.GetActionName(b.CastActionId)})\n{b.Name}/nid:{b.NameId}/did:{b.DataId}",
            overlayTextColor = b.IsCasting ? EColor.White.ToUint() : ImGuiColors.DalamudGrey.ToUint(),
            overlayBGColor = ImGuiEx.Vector4FromRGBA(0x000000CC).ToUint(),
        };
        return ret;
    }

    Element CreateLine(IBattleNpc b)
    {
        var ret = new Element(3)
        {
            includeRotation = true,
            offY = 10f,
            refActorObjectID = b.EntityId,
            refActorComparisonType = 2,
        };
        return ret;
    }
}
