using ECommons.GameHelpers;
using ECommons.PartyFunctions;
#nullable enable

namespace Splatoon.SplatoonScripting.Priority;
/// <summary>
/// If you need other amount of players in your priority list, create a class that inherits PriorityData and override GetNumPlayers method.
/// </summary>
public class PriorityData
{
    internal string ID = GetTemporaryId();
    public string Name = "Priority list";
    public string Description = "";
    public virtual int GetNumPlayers() => 8;
    /// <summary>
    /// Do not access directly!
    /// </summary>
    public List<PriorityList> PriorityLists = [];

    public PriorityData() { }

    public void Draw()
    {
        ImGui.PushID(ID);
        if(PriorityLists.Count == 0) PriorityLists.Add(new());
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add new priority list"))
        {
            PriorityLists.Add(new());
        }
        var matched = false;
        for(var i = 0; i < PriorityLists.Count; i++)
        {
            var playerList = PriorityLists[i];
            playerList.DragDrop.Begin();
            while(playerList.List.Count > GetNumPlayers())
            {
                playerList.List.RemoveAt(playerList.List.Count - 1);
            }
            while(playerList.List.Count < GetNumPlayers())
            {
                playerList.List.Add(new());
            }
            ImGui.PushID($"##{ID}-{i}");
            var statusCursor = Vector2.Zero;
            if(ImGui.BeginTable("##PriorityList", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("##reorder");
                ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##delete");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                statusCursor = ImGui.GetCursorPos();
                ImGuiEx.TextV($"");
                ImGuiEx.TextV($"");
                ImGuiEx.TextV($"");

                ImGui.PushID(playerList.ID);
                try
                {
                    playerList.Draw();
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Minus, "Delete this priority list (Hold CTRL)", enabled: ImGuiEx.Ctrl))
                    {
                        new TickScheduler(() => PriorityLists.Remove(playerList));
                    }
                }
                catch(Exception e)
                {
                    e.Log();
                }
                ImGui.PopID();

                ImGui.EndTable();

                //
                var cur = ImGui.GetCursorPos();
                ImGui.SetCursorPos(statusCursor);

                if(GetNumPlayers() > UniversalParty.LengthPlayback)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.TextV(EColor.OrangeBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine();
                    ImGuiEx.Text(EColor.OrangeBright, $"Can't validate list: there are less than {GetNumPlayers()} players in your party. ");
                }
                else
                {
                    var result = playerList.Test(out var error);
                    if(result)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.TextV(matched ? EColor.YellowBright : EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGuiEx.Text(matched ? EColor.YellowBright : EColor.GreenBright, $"This list matches your current party. " + (matched ? "However, one of previous lists also match your current party, so this list will never be selected." : ""));
                        matched = true;
                    }
                    else
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGuiEx.TextV(EColor.RedBright, FontAwesomeIcon.Ban.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGuiEx.Text(EColor.RedBright, error);
                    }
                }

                playerList.DrawModeSelector();

                ImGui.SetCursorPos(cur);
            }
            ImGui.PopID();
            playerList.DragDrop.End();
        }
        ImGui.PopID();
    }

    public PriorityList? GetFirstValidList()
    {
        foreach(var l in PriorityLists)
        {
            if(l.Test(out _)) return l;
        }
        return null;
    }

    /// <summary>
    /// This function gets player at certain position in priority list that matches set condition.
    /// </summary>
    /// <param name="predicate">A check to whether include a player into priority list resolution or not.</param>
    /// <param name="position">A position to retrieve. Overshooting will result in null. Starts with 1.</param>
    /// <param name="fromEnd">Whether to start from end instead of start of the list.</param>
    /// <returns></returns>
    public UniversalPartyMember? GetPlayer(Predicate<UniversalPartyMember> predicate, int position = 1, bool fromEnd = false)
    {
        var list = GetFirstValidList();
        if(list == null) return null;
        var skip = 0;
        for(var i = 0; i < list.List.Count; i++)
        {
            var index = fromEnd ? list.List.Count - 1 - i : i;
            var member = list.List[index];
            if(member.IsInParty(list.IsRole, out var ret) && predicate(ret))
            {
                if(++skip >= position)
                {
                    return ret;
                }
            }
        }
        return null;
    }

    /// <inheritdoc cref="GetPlayer(Predicate{UniversalPartyMember}, int, bool)"/>
    /// <summary>
    /// Retrieve player list according to the priority list.
    /// </summary>
    /// <returns></returns>
    public List<UniversalPartyMember>? GetPlayers(Predicate<UniversalPartyMember> predicate, bool fromEnd = false)
    {
        var list = GetFirstValidList();
        if(list == null) return null;
        var ret = new List<UniversalPartyMember>();
        foreach(var x in list.List)
        {
            if(x.IsInParty(list.IsRole, out var upm) && predicate(upm))
            {
                ret.Add(upm);
            }
        }
        return ret;
    }

    /// <inheritdoc cref="GetPlayer(Predicate{UniversalPartyMember}, int, bool)"/>
    /// <summary>
    /// Get own index in the priority list. Will return -1 if not found. Starts with 0. 
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="fromEnd"></param>
    /// <returns></returns>
    public int GetOwnIndex(Predicate<UniversalPartyMember> predicate, bool fromEnd = false)
    {
        if(!Player.Available) return -1;
        var list = GetPlayers(predicate);
        if(list == null) return -1;
        for(var i = 0; i < list.Count; i++)
        {
            var index = fromEnd ? list.Count - 1 - i : i;
            var p = list[index];
            if(p.IGameObject.AddressEquals(Player.Object)) return i;
        }
        return -1;
    }
}
