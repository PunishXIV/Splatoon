using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Splatoon.Render;

/**
 * Automatically clip UI elements so they appear in front of Splatoon elements.
 * Credit to https://github.com/DelvUI/DelvUI/blob/47d5e45b1ad20034697fd82308dc044a47757623/DelvUI/Helpers/ClipRectsHelper.cs
 */
internal class AutoClipZones
{
    private static bool logged_error_once = false;
    private readonly string[] _hotbarAddonNames = { "_ActionBar", "_ActionBar01", "_ActionBar02", "_ActionBar03", "_ActionBar04", "_ActionBar05", "_ActionBar06", "_ActionBar07", "_ActionBar08", "_ActionBar09", "_ActionBarEx" };
    private static List<string> _ignoredAddonNames = new List<string>()
        {
            "_FocusTargetInfo",
        };
    private Renderer renderer;
    private Splatoon p;

    public AutoClipZones(Renderer renderer, Splatoon p)
    {
        this.renderer = renderer;
        this.p = p;
    }

    public void Update()
    {
        try
        {
            UpdateWindows();
            UpdateTargetCastbarClipRect();
            UpdateHotbarsClipRects();
            UpdatePartyListClipRects();
            UpdateChatBubbleClipRect();
        }
        catch (ArgumentOutOfRangeException e)
        {
            // Swallow the exception and proceed. Autoclip is not important enough to fail rendering.
            if (!logged_error_once)
            {
                PrintDebugState(e);
                logged_error_once = true;
            }
        }
    }

    private unsafe void PrintDebugState(ArgumentOutOfRangeException e)
    {
        const ushort UIForegroundRed = 17;
        p.Log("Shiny Splatoon exception: please report it to developer", true, UIForegroundRed);
        p.Log(GetDebugString(), true, UIForegroundRed);
        p.Log(e.StackTrace, true, UIForegroundRed);
    }

    private unsafe string GetDebugString()
    {
        AtkStage* stage = AtkStage.GetSingleton();
        if (stage == null) { return "stage is null"; }

        RaptureAtkUnitManager* manager = stage->RaptureAtkUnitManager;
        if (manager == null) { return "manager is null"; }

        AtkUnitList* loadedUnitsList = &manager->AtkUnitManager.AllLoadedUnitsList;
        if (loadedUnitsList == null) { return "units is null"; }

        List<string> names = new();
        for (int i = 0; i < loadedUnitsList->Count; i++)
        {
            try
            {
                AtkUnitBase* addon = *(AtkUnitBase**)Unsafe.AsPointer(ref loadedUnitsList->EntriesSpan[i]);
                if (addon == null || !addon->IsVisible || addon->WindowNode == null || addon->Scale == 0)
                {
                    continue;
                }

                string? name = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));
                if (name != null && !_ignoredAddonNames.Contains(name))
                {
                    names.Add(name);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
        return "Windows: [" + string.Join(", ", names) + "]";
    }

    private unsafe void UpdatePartyListClipRects()
    {
        AtkUnitBase* addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_PartyList", 1);
        if (addon == null || !addon->IsVisible) { return; }

        if (addon->UldManager.NodeListCount < 2) { return; }

        AtkResNode* baseNode = addon->UldManager.NodeList[0];

        for (int i = 6; i < 23; i++)
        {
            AtkResNode* slotNode = addon->UldManager.NodeList[i];
            if (slotNode is null) continue;

            if (slotNode->IsVisible)
            {
                Vector2 pos = new Vector2(
                    slotNode->ScreenX + (18f * addon->Scale),
                    slotNode->ScreenY
                    );
                Vector2 size = new Vector2(
                    slotNode->Width * addon->Scale - 25f * addon->Scale,
                    slotNode->Height * addon->Scale - 5f * addon->Scale
                    );

                renderer.AddClipZone(ClipRect(pos, pos + size));
            }
        }
    }

    public unsafe void UpdateWindows()
    {
        AtkStage* stage = AtkStage.GetSingleton();
        if (stage == null) { return; }

        RaptureAtkUnitManager* manager = stage->RaptureAtkUnitManager;
        if (manager == null) { return; }

        AtkUnitList* loadedUnitsList = &manager->AtkUnitManager.AllLoadedUnitsList;
        if (loadedUnitsList == null) { return; }

        for (int i = 0; i < loadedUnitsList->Count; i++)
        {
            try
            {
                AtkUnitBase* addon = *(AtkUnitBase**)Unsafe.AsPointer(ref loadedUnitsList->EntriesSpan[i]);
                if (addon == null || !addon->IsVisible || addon->WindowNode == null || addon->Scale == 0)
                {
                    continue;
                }

                string? name = Marshal.PtrToStringAnsi(new IntPtr(addon->Name));
                if (name != null && _ignoredAddonNames.Contains(name))
                {
                    continue;
                }

                float margin = 5 * addon->Scale;
                float bottomMargin = 13 * addon->Scale;

                Vector2 pos = new Vector2(addon->X + margin, addon->Y + margin);
                Vector2 size = new Vector2(
                    addon->WindowNode->AtkResNode.Width * addon->Scale - margin,
                    addon->WindowNode->AtkResNode.Height * addon->Scale - bottomMargin
                );

                // special case for duty finder
                if (name == "ContentsFinder")
                {
                    size.X += size.X + (16 * addon->Scale);
                    size.Y += (30 * addon->Scale);
                }

                renderer.AddClipZone(ClipRect(pos, pos + size));
            }
            catch { }
        }
    }

    private unsafe void UpdateTargetCastbarClipRect()
    {
        AtkUnitBase* addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_TargetInfoCastBar", 1);
        if (addon == null || !addon->IsVisible) { return; }

        if (addon->UldManager.NodeListCount < 2) { return; }

        AtkResNode* baseNode = addon->UldManager.NodeList[1];
        AtkResNode* imageNode = addon->UldManager.NodeList[2];

        if (baseNode == null || !baseNode->IsVisible) { return; }
        if (imageNode == null || !imageNode->IsVisible) { return; }

        Vector2 pos = new Vector2(
            addon->X + (baseNode->X * addon->Scale),
            addon->Y + (baseNode->Y * addon->Scale)
        );
        Vector2 size = new Vector2(
            imageNode->Width * addon->Scale,
            imageNode->Height * addon->Scale
        );

        renderer.AddClipZone(ClipRect(pos, pos + size));
    }

    private unsafe void UpdateHotbarsClipRects()
    {
        foreach (string addonName in _hotbarAddonNames)
        {
            AtkUnitBase* addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName(addonName, 1);
            if (addon == null || !addon->IsVisible) { continue; }

            if (addon->UldManager.NodeListCount < 20) { continue; }

            AtkResNode* firstNode = addon->UldManager.NodeList[20];
            AtkResNode* lastNode = addon->UldManager.NodeList[9];

            if (firstNode == null || lastNode == null) { continue; }

            float margin = 15f * addon->Scale;

            Vector2 min = new Vector2(
                addon->X + (firstNode->X * addon->Scale) + margin,
                addon->Y + (firstNode->Y * addon->Scale) + margin
            );
            Vector2 max = new Vector2(
                addon->X + (lastNode->X * addon->Scale) + (lastNode->Width * addon->Scale) - margin,
                addon->Y + (lastNode->Y * addon->Scale) + (lastNode->Height * addon->Scale) - margin
            );

            renderer.AddClipZone(ClipRect(min, max));
        }
    }

    private unsafe void UpdateChatBubbleClipRect()
    {
        AtkUnitBase* addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_MiniTalk", 1);
        if (addon == null || !addon->IsVisible) { return; }
        if (addon->UldManager.NodeListCount < 10) { return; }

        for (int i = 1; i <= 10; i++)
        {
            AtkResNode* node = addon->UldManager.NodeList[i];
            if (node == null || !node->IsVisible) { continue; }

            AtkComponentNode* component = node->GetAsAtkComponentNode();
            if (component == null) { continue; }
            if (component->Component->UldManager.NodeListCount < 1) { continue; }

            AtkResNode* bubble = component->Component->UldManager.NodeList[1];
            Vector2 pos = new Vector2(
                node->X + (bubble->X * addon->Scale),
                node->Y + (bubble->Y * addon->Scale)
            );
            Vector2 size = new Vector2(
                bubble->Width * addon->Scale,
                bubble->Height * addon->Scale
            );

            renderer.AddClipZone(ClipRect(pos, pos + size));
        }
    }

    public Rectangle ClipRect(Vector2 min, Vector2 max)
    {
        Vector2 size = max - min;
        return new((int)min.X, (int)min.Y, (int)size.X, (int)size.Y);
    }
}
