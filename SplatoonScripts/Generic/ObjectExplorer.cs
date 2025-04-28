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
using System.Linq;
using System.Numerics;

namespace SplatoonScriptsOfficial.Generic;
public unsafe class ObjectExplorer : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = null;
    public override Metadata? Metadata => new(1, "Redmoon");
    private uint HoveredID = 0;
    private bool HideExpired = false;

    public override void OnUpdate()
    {
        Controller.ClearRegisteredElements();
        var positions = new List<Vector3>();
        foreach(var x in Svc.Objects.OfType<IBattleNpc>())
        {
            if(x.IsCharacterVisible())
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
                Controller.RegisterElement(Guid.NewGuid().ToString(), circle);
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
            var col = x.IsCasting ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey3;
            Entries.Add(new("Name", true, () => ImGuiEx.TextCopy(col, x.Name.ToString())));
            Entries.Add(new("Cast ID", () => ImGuiEx.TextCopy(col, $"{x.CastActionId}")));
            Entries.Add(new("Cast name", () => ImGuiEx.TextCopy(col, $"{ExcelActionHelper.GetActionName(x.CastActionId)}")));
            Entries.Add(new("Time", () => ImGuiEx.TextCopy(col, $"{x.CurrentCastTime:F1}/{x.BaseCastTime:F1}")));
            Entries.Add(new("Name ID", () => ImGuiEx.TextCopy(col, x.GameObjectId.ToString("x"))));
            Entries.Add(new("Name ID", () => ImGuiEx.TextCopy(col, x.NameId.ToString("x"))));
            Entries.Add(new("Data ID", () => ImGuiEx.TextCopy(col, x.DataId.ToString("x"))));
            Entries.Add(new("Model ID", () => ImGuiEx.TextCopy(col, x.Struct()->ModelContainer.ModelCharaId.ToString("x"))));
            Entries.Add(new("Tar", () => ImGuiEx.Text(col, x.IsTargetable.ToString())));
            Entries.Add(new("Vis", () => ImGuiEx.Text(col, x.IsCharacterVisible().ToString())));
            Entries.Add(new("Dist", () => ImGuiEx.Text(col, $"{Vector3.Distance(Player.Position, x.Position):F1}")));
            Entries.Add(new("Rot", () => ImGuiEx.Text(col, $"{x.Rotation * (180.0 / Math.PI)}")));
            Entries.Add(new("Find", () =>
            {
                if(ImGui.SmallButton("Find##" + x.Address))
                {

                }
                if(ImGui.IsItemHovered())
                {
                    HoveredID = x.EntityId;
                }
            }));
        }
        ImGuiEx.EzTable(Entries);
    }

    private Element CreateCircle(IBattleNpc b)
    {
        var ret = new Element(0)
        {
            overlayPlaceholders = true,
            overlayText = $"C: {b.Name} {b.CastActionId} gid: {b.GameObjectId.ToString("x")} /nid: {b.NameId}/did: {b.DataId}",
            overlayTextColor = b.IsCasting ? EColor.White.ToUint() : ImGuiColors.DalamudGrey.ToUint(),
            overlayBGColor = ImGuiEx.Vector4FromRGBA(0x000000CC).ToUint(),
        };
        return ret;
    }
}
