using ECommons.LanguageHelpers;

namespace Splatoon.Gui.Tabs;

public static class TabExperimental
{
    public static void Draw()
    {
        ImGuiEx.TextWrapped("Experimental features. They are off by default and are applied when Splatoon is loaded: after changing them, reload the plugin.".Loc());
        ImGui.Separator();

        if(ImGui.Checkbox("Status events from hooks".Loc(), ref P.Config.ExperimentalStatusHooks))
        {
            P.Config.Save();
        }
        ImGuiEx.HelpMarker("""
            OnGainBuffEffect / OnRemoveBuffEffect / OnUpdateBuffEffect are raised from the game functions that write status slots,
            instead of comparing every status of every object each frame.
            Events are raised at the moment the status changes rather than on the next frame, and param updates are no longer missed
            when another status of the same object changes in the same frame. Removed statuses have no remaining time.
            """.Loc());
        var active = P.StatusHookProcessor != null;
        ImGuiEx.Text("Currently used:".Loc() + " " + (active ? "hooks".Loc() : "frame comparison".Loc()));
        if(active != P.Config.ExperimentalStatusHooks)
        {
            ImGuiEx.Text(EColor.YellowBright, "Reload Splatoon to apply this change.".Loc());
        }
    }
}
