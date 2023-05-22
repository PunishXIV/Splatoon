using ECommons.LanguageHelpers;

namespace Splatoon;

partial class CGui
{
    void DisplayDynamicElements()
    {
        if (ImGui.Button("Destroy all".Loc()))
        {
            p.dynamicElements.Clear();
        }
        ImGui.BeginChild("##splatoondynamicelements");
        for (var i = p.dynamicElements.Count - 1; i >= 0; i--)
        {
            var dynElem = p.dynamicElements[i];
            ImGui.TextWrapped($"[{dynElem.Name}]\n(Elements: {dynElem.Elements.Length}, " +
                        $"Layouts: {dynElem.Layouts.Length}, " +
                        $"destroyAt: {string.Join(",", dynElem.DestroyTime.Select(x => x.ToString()).ToArray())})");
            if (ImGui.SmallButton("Destroy##" + i))
            {
                p.dynamicElements.RemoveAt(i);
            }
            ImGui.SameLine();
            if (ImGui.SmallButton("Destroy namespace".Loc()+"##" + i))
            {
                p.dynamicElements.RemoveAll(e => e.Name == dynElem.Name);
                break;
            }
            ImGui.Separator();
        }
        ImGui.EndChild();
    }
}
