using Dalamud.Interface.Colors;
using ECommons.GameFunctions;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using Splatoon.Memory;

namespace Splatoon.Gui;

internal unsafe static class Explorer
{
    internal static nint Ptr = nint.Zero;
    internal static void Draw()
    {
        ImGui.BeginChild("##exch");
        var x = Svc.Objects.FirstOrDefault(x => x.Address == Ptr);
        ImGuiEx.Text(ImGuiColors.DalamudOrange, "Beta");
        if(ImGui.BeginCombo("##selector", $"{(Ptr == nint.Zero?"Target".Loc() : $"{(x == null?$"{Ptr:X16} - "+"invalid pointer".Loc() : $"{x}")}")}"))
        {
            if (ImGui.Selectable("Target".Loc()))
            {
                Ptr = nint.Zero;
            }
            foreach(var o in Svc.Objects)
            {
                if (ImGui.Selectable($"{o}"))
                {
                    Ptr = o.Address;
                }
            }
            ImGui.EndCombo();
        }
        if (Ptr == nint.Zero)
        {
            if (Svc.Targets.Target != null && Svc.ClientState.LocalPlayer != null)
            {
                DrawGameObject(Svc.Targets.Target);
            }
        }
        else
        {
            if(x != null)
            {
                DrawGameObject(x);
            }
        }
        ImGui.EndChild();
    }

    internal static void DrawGameObject(GameObject obj)
    {
        ImGuiEx.TextCopy($"GameObject {obj}");
        ImGuiEx.TextCopy($"ObjectKind: {obj.ObjectKind}");
        ImGuiEx.TextCopy($"{"Position".Loc()}: {obj.Position.X} {obj.Position.Y} {obj.Position.Z}");
        ImGuiEx.TextCopy($"{"Rotation".Loc()}: {obj.Rotation}/{360 - (obj.Rotation.RadiansToDegrees() + 180)}");
        ImGuiEx.TextCopy($"Vector3 {"distance".Loc()}: {Vector3.Distance(obj.Position, Svc.ClientState.LocalPlayer.Position)}");
        ImGuiEx.TextCopy($"Vector2 {"distance".Loc()}: {Vector2.Distance(obj.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2())}");
        ImGuiEx.TextCopy($"{"Object ID".Loc()} long: {((long)obj.Struct()->GetObjectID()).Format()}");
        ImGuiEx.TextCopy($"{"Object ID".Loc()}: {obj.ObjectId.Format()}");
        ImGuiEx.TextCopy($"{"Data ID".Loc()}: {obj.DataId.Format()}");
        ImGuiEx.TextCopy($"{"Owner ID".Loc()}: {obj.OwnerId.Format()}");
        ImGuiEx.TextCopy($"{"NPC ID".Loc()}: {obj.Struct()->GetNpcID()}");
        ImGuiEx.TextCopy($"{"Dead".Loc()}: {obj.Struct()->IsDead()}");
        ImGuiEx.TextCopy($"{"Hitbox radius".Loc()}: {obj.HitboxRadius}");
        ImGuiEx.TextCopy($"{"Targetable".Loc()}: {obj.Struct()->GetIsTargetable()}");
        ImGuiEx.TextCopy($"{"Nameplate".Loc()}: {ObjectFunctions.GetNameplateColor(obj.Address)}");
        ImGuiEx.TextCopy($"{"Is hostile".Loc()}: {ObjectFunctions.IsHostile(obj)}");
        ImGuiEx.TextCopy($"{"Gender".Loc()}: {obj.Struct()->Gender}");
        ImGuiEx.TextCopy($"{"VfxScale".Loc()}: {obj.Struct()->VfxScale}");
        ImGuiEx.TextCopy($"{"RenderFlags".Loc()}: {obj.Struct()->RenderFlags}");
        ImGuiEx.TextCopy($"{"SubKind".Loc()}: {obj.Struct()->SubKind}");
        ImGuiEx.TextCopy($"{"TargetStatus".Loc()}: {obj.Struct()->TargetStatus}");
        if (obj is Character c)
        {
            ImGuiEx.TextCopy("---------- Character ----------");
            ImGuiEx.TextCopy($"{"HP".Loc()}: {c.CurrentHp} / {c.MaxHp}");
            ImGuiEx.TextCopy($"{"Name NPC ID".Loc()}: {c.NameId}");
            ImGuiEx.TextWrappedCopy($"Customize: {c.Customize.Select(x => $"{x:X2}").Join(" ")}");
            ImGuiEx.TextCopy($"ModelCharaId: {c.Struct()->ModelCharaId}");
            ImGuiEx.TextCopy($"ModelCharaId_2: {c.Struct()->ModelCharaId_2}");
            ImGuiEx.TextCopy($"{"Visible".Loc()}: {c.IsCharacterVisible()}");
            ImGuiEx.TextCopy($"VfxData: {(nint)c.Struct()->VfxData:X16}");
            ImGuiEx.TextCopy($"VfxData2: {(nint)c.Struct()->VfxData2:X16}");
            ImGuiEx.TextCopy($"Omen: {(nint)c.Struct()->Omen:X16}");
            ImGuiEx.TextCopy($"ModelSkeletonId: {(nint)c.Struct()->ModelSkeletonId:X16}");
            ImGuiEx.TextCopy($"ModelSkeletonId2: {(nint)c.Struct()->ModelSkeletonId_2:X16}");
            ImGuiEx.TextCopy($"TargetObject: {c.TargetObject}");
            ImGuiEx.TextCopy($"TargetObjectID: {c.TargetObjectId}");
            ImGuiEx.Text("VFX");
            if(c.TryGetVfx(out var fx))
            {
                foreach(var x in fx)
                {
                    ImGuiEx.TextCopy($"{x.Key}, {"Age".Loc()} = {x.Value.AgeF:F1}");
                }
            }
        }
        if(obj is BattleChara b)
        {
            ImGuiEx.TextCopy("---------- Battle chara ----------");
            ImGuiEx.TextCopy($"{"Casting".Loc()}: {b.IsCasting}, {"Action ID".Loc()} = {b.CastActionId.Format()}, {"Type".Loc()} = {b.CastActionType}, {"Cast time".Loc()}: {b.CurrentCastTime:F1}/{b.TotalCastTime:F1}");
            if (AttachedInfo.CastInfos.TryGetValue(b.Address, out var info)) 
            {
                ImGuiEx.TextCopy($"{"Overcast".Loc()}: ID={info.ID}, StartTime={info.StartTime}, Age={info.AgeF:F1}");
            }
            ImGuiEx.TextCopy($"Status list:".Loc());
            foreach(var x in b.StatusList)
            {
                ImGuiEx.TextCopy($"  {x.GameData.Name} ({x.StatusId.Format()}), {"Remains".Loc()} = {x.RemainingTime:F1}, Param = {x.Param}, {"Count".Loc()} = {x.StackCount}");
            }
        }
    }
}
