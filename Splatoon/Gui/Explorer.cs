using Dalamud.Interface.Colors;
using ECommons.GameFunctions;
using ECommons.LanguageHelpers;
using ECommons.MathHelpers;
using Splatoon.Memory;

namespace Splatoon.Gui;

internal static unsafe class Explorer
{
    internal static nint Ptr = nint.Zero;
    internal static void Draw()
    {
        ImGui.BeginChild("##exch");
        var x = Svc.Objects.FirstOrDefault(x => x.Address == Ptr);
        ImGuiEx.Text(ImGuiColors.DalamudOrange, "Beta");
        if(ImGui.BeginCombo("##selector", $"{(Ptr == nint.Zero ? "Target".Loc() : $"{(x == null ? $"{Ptr:X16} - " + "invalid pointer".Loc() : $"{x}")}")}"))
        {
            if(ImGui.Selectable("Target".Loc()))
            {
                Ptr = nint.Zero;
            }
            foreach(var o in Svc.Objects)
            {
                if(ImGui.Selectable($"{o}"))
                {
                    Ptr = o.Address;
                }
            }
            ImGui.EndCombo();
        }
        if(Ptr == nint.Zero)
        {
            if(Svc.Targets.Target != null && Svc.ClientState.LocalPlayer != null)
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

    internal static void DrawGameObject(IGameObject obj)
    {
        ImGuiEx.TextCopy($"GameObject {obj}");
        ImGuiEx.TextCopy($"ObjectKind: {obj.ObjectKind}");
        ImGuiEx.TextCopy($"{"Position".Loc()}: {obj.Position.X} {obj.Position.Y} {obj.Position.Z}");
        ImGuiEx.TextCopy($"{"Rotation".Loc()}: {obj.Rotation}/{360 - (obj.Rotation.RadiansToDegrees() + 180)}");
        ImGuiEx.TextCopy($"Vector3 {"distance".Loc()}: {Vector3.Distance(obj.Position, Svc.ClientState.LocalPlayer.Position)}");
        ImGuiEx.TextCopy($"Vector2 {"distance".Loc()}: {Vector2.Distance(obj.Position.ToVector2(), Svc.ClientState.LocalPlayer.Position.ToVector2())}");
        ImGuiEx.TextCopy($"{"Object ID".Loc()} long: {((ulong)obj.Struct()->GetGameObjectId()).Format()}");
        ImGuiEx.TextCopy($"{"Object ID".Loc()}: {obj.EntityId.Format()}");
        ImGuiEx.TextCopy($"{"Data ID".Loc()}: {obj.DataId.Format()}");
        ImGuiEx.TextCopy($"{"Owner ID".Loc()}: {obj.OwnerId.Format()}");
        ImGuiEx.TextCopy($"{"NPC ID".Loc()}: {obj.Struct()->GetNameId()}");
        ImGuiEx.TextCopy($"{"Dead".Loc()}: {obj.Struct()->IsDead()}");
        ImGuiEx.TextCopy($"{"Hitbox radius".Loc()}: {obj.HitboxRadius}");
        ImGuiEx.TextCopy($"{"Targetable".Loc()}: {obj.Struct()->GetIsTargetable()}");
        ImGuiEx.TextCopy($"{"Nameplate".Loc()}: {ObjectFunctions.GetNameplateKind(obj)}");
        ImGuiEx.TextCopy($"{"Is hostile".Loc()}: {ObjectFunctions.IsHostile(obj)}");
        ImGuiEx.TextCopy($"{"VfxScale".Loc()}: {obj.Struct()->VfxScale}");
        ImGui.SameLine();
        if(ImGui.Button("++"))
        {
            obj.Struct()->VfxScale = obj.Struct()->VfxScale + 0.5f;
        }
        ImGuiEx.TextCopy($"{"RenderFlags".Loc()}: {obj.Struct()->RenderFlags}");
        ImGuiEx.TextCopy($"{"SubKind".Loc()}: {obj.Struct()->SubKind}");
        ImGuiEx.TextCopy($"{"TargetStatus".Loc()}: {obj.Struct()->TargetStatus}");
        ImGuiEx.TextCopy($"RenderFlags:  {Convert.ToString(obj.Struct()->RenderFlags, 2)}");
        ImGuiEx.TextCopy($"NamePlateIconId:  {obj.Struct()->NamePlateIconId}");
        ImGuiEx.TextCopy($"DrawObject:  {(nint)obj.Struct()->DrawObject:X16}");
        ImGuiEx.TextCopy($"LayoutID:  {obj.Struct()->LayoutId}");
        if(obj is ICharacter c)
        {
            ImGuiEx.TextCopy("---------- Character ----------");
            ImGuiEx.TextCopy($"{"HP".Loc()}: {c.CurrentHp} / {c.MaxHp}");
            ImGuiEx.TextCopy($"{"Name NPC ID".Loc()}: {c.NameId}");
            ImGuiEx.TextWrappedCopy($"Customize: {c.Customize.Select(x => $"{x:X2}").Join(" ")}");
            ImGuiEx.TextCopy($"ModelCharaId: {c.Struct()->ModelContainer.ModelCharaId}");
            ImGuiEx.TextCopy($"{"Visible".Loc()}: {c.IsCharacterVisible()}");
            ImGuiEx.TextCopy($"VfxData: {(nint)c.Struct()->Vfx.VfxData:X16}");
            ImGuiEx.TextCopy($"VfxData2: {(nint)c.Struct()->Vfx.VfxData2:X16}");
            ImGuiEx.TextCopy($"Omen: {(nint)c.Struct()->Vfx.Omen:X16}");
            ImGuiEx.TextCopy($"TargetObject: {c.TargetObject}");
            ImGuiEx.TextCopy($"TargetObjectID: {c.TargetObjectId}");
            ImGuiEx.TextCopy($"EventState:  {c.Struct()->EventState}");
            ImGuiEx.TextCopy($"VFX Container:  {(nint)(&c.Struct()->Vfx):X16}");
            ImGuiEx.Text("VFX");
            if(c.TryGetVfx(out var fx))
            {
                foreach(var x in fx)
                {
                    ImGuiEx.TextCopy($"{x.Key}, {"Age".Loc()} = {x.Value.AgeF:F1}");
                }
            }
            ImGuiEx.Text("ObjectEffect");
            foreach(var x in AttachedInfo.ObjectEffectInfos)
            {
                if(x.Key == c.Address)
                {
                    ImGuiEx.Text($"{((long)x.Key).Format()}");
                    foreach(var z in x.Value)
                    {
                        ImGuiEx.TextCopy($"    {z.data1}, {z.data2} / {z.AgeF}");
                    }
                }
            }
            ImGuiEx.TextCopy($"IsFlying: {*(byte*)(c.Address + 528 + 1020):X16}");
        }
        if(obj is IBattleChara b)
        {
            ImGuiEx.TextCopy("---------- Battle chara ----------");
            ImGuiEx.TextCopy($"{"Casting".Loc()}: {b.IsCasting}, {"Action ID".Loc()} = {b.CastActionId.Format()}, {"Type".Loc()} = {b.CastActionType}, {"Cast time".Loc()}: {b.CurrentCastTime:F1}/{b.TotalCastTime:F1}");
            if(AttachedInfo.CastInfos.TryGetValue(b.Address, out var info))
            {
                ImGuiEx.TextCopy($"{"Overcast".Loc()}: ID={info.ID}, StartTime={info.StartTime}, Age={info.AgeF:F1}");
            }
            ImGuiEx.TextCopy($"Status list:".Loc());
            foreach(var x in b.StatusList)
            {
                ImGuiEx.TextCopy($"  {x.GameData.ValueNullable?.Name} ({x.StatusId.Format()}), {"Remains".Loc()} = {x.RemainingTime:F1}, Param = {x.Param}, {"Count".Loc()} = {x.Param}");
            }
        }
    }
}
