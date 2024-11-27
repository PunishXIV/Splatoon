using System.Diagnostics.CodeAnalysis;
#nullable enable

namespace Splatoon.SplatoonScripting.Priority;
public class PriorityList
{
    internal string ID = GetTemporaryId();
    public List<JobbedPlayer> List = [];
    internal ImGuiEx.RealtimeDragDrop<JobbedPlayer> DragDrop;

    public PriorityList()
    {
        DragDrop = new(ID, x => x.ID);
    }

    internal void Draw()
    {
        for(var q = 0; q < List.Count; q++)
        {
            var player = List[q];
            ImGui.PushID(player.ID);
            ImGui.TableNextRow();
            DragDrop.SetRowColor(player.ID);
            ImGui.TableNextColumn();
            DragDrop.NextRow();
            DragDrop.DrawButtonDummy(player, List, q);
            ImGui.TableNextColumn();
            player.DrawSelector();
            ImGui.TableNextColumn();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
            {
                player.Name = "";
                player.Jobs.Clear();
            }
            ImGui.PopID();
        }
    }

    public bool Test([NotNullWhen(false)] out string? error)
    {
        if(List.Any(x => x.Name == "" && x.Jobs.Count == 0))
        {
            error = "There are unfilled slots in this priority list.";
            return false;
        }
        var ret = List.All(x => x.IsInParty(out _));
        error = ret ? null : "Current party does not matches this priority list.";
        return ret;
    }
}
