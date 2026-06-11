using ECommons.LanguageHelpers;

namespace Splatoon;

internal partial class CGui
{
    private void DisplayDynamicElements()
    {
        if(ImGui.Button("Destroy all".Loc()))
        {
            P.dynamicElements.Clear();
        }
        ImGui.BeginChild("##splatoondynamicelements");
        for(var i = P.dynamicElements.Count - 1; i >= 0; i--)
        {
            var dynElem = P.dynamicElements[i];
            ImGui.TextWrapped($"[{dynElem.Name}]\n(Elements: {dynElem.Elements.Length}, " +
                        $"Layouts: {dynElem.Layouts.Length}, " +
                        $"destroyAt: {string.Join(",", dynElem.DestroyTime.Select(x => x.ToString()).ToArray())})");
            if(ImGui.SmallButton("Destroy##" + i))
            {
                P.dynamicElements.RemoveAt(i);
            }
            ImGui.SameLine();
            if(ImGui.SmallButton("Destroy namespace".Loc() + "##" + i))
            {
                P.dynamicElements.RemoveAll(e => e.Name == dynElem.Name);
                break;
            }
            ImGui.Separator();
        }
        ImGui.EndChild();
    }
}
