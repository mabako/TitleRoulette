using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace TitleRoulette;

internal sealed class PluginWindow : Window
{
    private ConfigState _configState = new ConfigState(0);

    public PluginWindow()
        : base("Title Roulette###TitleRoulette")
    {
        Size = new Vector2(400, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 400),
            MaximumSize = new Vector2(1000, 1000),
        };
    }

    public override void Draw()
    {
        if (!Service.ClientState.IsLoggedIn || Service.ClientState.LocalContentId == 0)
        {
            DrawNotAvailable("You are currently not logged in.");
            return;
        }

        if (Service.ClientState.LocalContentId != _configState.ContentId)
        {
            _configState = new ConfigState(Service.ClientState.LocalContentId);
        }


        if (!Service.GameFunctions.IsTitleListLoaded())
        {
            DrawNotAvailable("Please open 'Character' > 'Acquired Titles' once.");
        }
        else
        {
            _configState.Draw(out bool close);
            if (close)
                IsOpen = false;
        }
    }

    private static string FormatGroupName(Configuration.TitleGroup group)
    {
        string name = group.Name;
        if (group.IsDefault)
            name += $" {SeIconChar.BoxedStar.ToIconString()}";
        return name;
    }

    private static string FormatTitleCount(Configuration.TitleGroup group)
    {
        return group.Titles.Count switch
        {
            0 => "empty",
            1 => $"{group.Titles.Count} title",
            _ => $"{group.Titles.Count} titles",
        };
    }

    private void DrawNotAvailable(string message)
    {
        ImGui.SetCursorPos(Vector2.Zero);

        var windowSize = ImGui.GetWindowSize();
        var titleHeight = ImGui.GetFontSize() + (ImGui.GetStyle().FramePadding.Y * 2);

        if (ImGui.BeginChild("###noTitlesAvailable", new Vector2(-1, -1), false))
        {
            ImGui.GetWindowDrawList().PushClipRectFullScreen();
            ImGui.GetWindowDrawList().AddRectFilled(
                ImGui.GetWindowPos() + new Vector2(0, titleHeight),
                ImGui.GetWindowPos() + windowSize,
                0xCC000000,
                ImGui.GetStyle().WindowRounding,
                ImDrawFlags.RoundCornersBottom);
            ImGui.PopClipRect();

            ImGui.SetCursorPosY(windowSize.Y / 2);
            ImGuiHelpers.CenteredText(message);
            ImGui.EndChild();
        }
    }

    internal sealed class ConfigState
    {
        public ConfigState(ulong contentId)
        {
            ContentId = contentId;
            if (contentId > 0)
            {
                IsFemale = Service.ClientState.LocalPlayer!.Customize[(int)CustomizeIndex.Gender] == 1;
                TitleSelection = new TitleSelection
                {
                    Groups = Service.Configuration.GetCurrentCharacterGroups(),
                    SortedTitles = Service.Titles.Values.OrderBy(x => IsFemale ? x.FeminineName : x.MasculineName,
                        StringComparer.CurrentCultureIgnoreCase).ToList(),
                    IsFemale = IsFemale,
                };
            }
        }

        public ulong ContentId { get; }
        public bool IsFemale { get; }
        private TitleSelection TitleSelection { get; }
        private GroupManagement GroupManagement { get; set; }

        public void Draw(out bool close)
        {
            close = false;

            if (GroupManagement != null)
            {
                GroupManagement.Draw(out bool save, out bool closeGm, out bool reset);

                if (save)
                    TitleSelection.Groups = GroupManagement.Groups.Select(x => x.Copy()).ToList();

                if (reset)
                    GroupManagement.Groups = TitleSelection.Groups.Select(x => x.Copy()).ToList();

                if (closeGm)
                    GroupManagement = null;
            }
            else
            {
                TitleSelection.Draw(out bool manageGroups, out close);

                if (manageGroups)
                {
                    GroupManagement = new GroupManagement
                    {
                        Groups = TitleSelection.Groups.Select(x => x.Copy()).ToList(),
                    };
                }

                if (close)
                    TitleSelection.Groups = Service.Configuration.GetCurrentCharacterGroups();
            }
        }
    }

    internal sealed class TitleSelection
    {
        private int _currentGroup;
        private         int                            currentGroupForRandom = 0;

        public required List<Configuration.TitleGroup> Groups { get; set; }
        public required List<Title> SortedTitles { get; set; }
        public required bool IsFemale { get; set; }

        public void Draw(out bool manageGroups, out bool close)
        {
            manageGroups = false;
            close = false;

            if (Groups.Count == 0)
                Groups.Add(new Configuration.TitleGroup { Name = "default", IsDefault = true });

            if (Groups.Count > 0 && !Groups.Any(x => x.IsDefault))
                Groups.First().IsDefault = true;

            _currentGroup = Math.Min(_currentGroup, Groups.Count - 1);
            Vector2 windowSize = ImGui.GetWindowSize();
            if (ImGui.BeginTabBar("SelectTitlesBar"))
            {
                if (ImGui.BeginTabItem("Select Titles"))
                {
                    if (ImGui.BeginCombo("###GroupCombo",
                            $"{FormatGroupName(Groups[_currentGroup])} ({FormatTitleCount(Groups[_currentGroup])})"))
                    {
                        for (int i = 0; i < Groups.Count; ++i)
                        {
                            if (ImGui.Selectable($"{FormatGroupName(Groups[i])} ({FormatTitleCount(Groups[i])})",
                                    i == _currentGroup))
                                _currentGroup = i;
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
                        manageGroups = true;

                    ImGui.Separator();
                    Vector2 size = new Vector2(windowSize.X - 15, windowSize.Y - 130);
                    if (ImGui.BeginChild("titleTable", size))
                    {
                        if (ImGui.BeginTable("SelectedTitles", 2,
                                ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY))
                        {
                            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);
                            ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableHeadersRow();

                            foreach (var title in SortedTitles)
                            {
                                if (title.Id > 0 && !Service.GameFunctions.IsTitleUnlocked(title.Id))
                                    continue;

                                ImGui.TableNextRow();
                                if (ImGui.TableNextColumn())
                                {
                                    bool used = Groups[_currentGroup].Titles.Contains(title.Id);
                                    if (ImGui.Checkbox($"###{title.Id}", ref used))
                                    {
                                        if (used)
                                            Groups[_currentGroup].Titles.Add(title.Id);
                                        else
                                            Groups[_currentGroup].Titles.Remove(title.Id);
                                    }
                                }

                                if (ImGui.TableNextColumn())
                                    ImGui.Text(
                                        $"{(title.IsPrefix ? "" : "... ")}{(IsFemale ? title.FeminineName : title.MasculineName)}");
                            }

                            ImGui.EndTable();
                        }

                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Separator();
            bool save = false;

            ushort currentTitleId = Service.GameFunctions.GetCurrentTitleId();
            ImGui.BeginDisabled(Groups[_currentGroup].Titles.All(v => v == currentTitleId));
            ImGui.PushID("###PickTitle");
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Dice))
            {
                List<ushort> differentTitles = Groups[_currentGroup].Titles.Where(v => v != currentTitleId).ToList();
                if (differentTitles.Count > 0)
                {
                    ushort titleId = differentTitles[new Random().Next(differentTitles.Count)];
                    Service.GameFunctions.SetTitle(titleId);
                }
            }

            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("Pick a random title from this list as your active title.");
            ImGui.PopID();

            ImGui.SameLine();

            if (ImGui.Button("Save"))
                save = true;
            ImGui.SameLine();
            if (ImGui.Button("Save and Close"))
            {
                save = true;
                close = true;
            }

            ImGui.SameLine();

            ImGui.BeginDisabled(!ImGui.GetIO().KeyCtrl);
            if (ImGui.Button("Discard Changes"))
                Groups = Service.Configuration.GetCurrentCharacterGroups();
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && !ImGui.GetIO().KeyCtrl)
                ImGui.SetTooltip("Hold CTRL to discard any changes you've made since you've last saved.");

            ImGui.SameLine();
            bool randomAssign = Service.Configuration.AssignRandomTitleOnAreaChange;
            if (ImGui.Checkbox("Random title when switching Zones", ref randomAssign))
                save = true;
            Service.Configuration.assignRandomTitleOnAreaChange = randomAssign;

            if (randomAssign)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200);
                if (ImGui.BeginCombo("###GroupComboRandom", $"{Groups[currentGroupForRandom].Name} ({FormatTitleCount(Groups[currentGroupForRandom])})"))
                {
                    for (int i = 0; i < Groups.Count; ++i)
                    {
                        if (ImGui.Selectable($"{Groups[i].Name} ({FormatTitleCount(Groups[i])})", i == currentGroupForRandom))
                        {
                            currentGroupForRandom                  = i;
                            Service.Configuration.randomTitleGroup = Groups[currentGroupForRandom];
                            save                                   = true;
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            if (save)
            {
                Service.Configuration.SetCurrentCharacterGroups(Groups);
                Service.PluginInterface.SavePluginConfig(Service.Configuration);
            }
        }
    }

    internal sealed class GroupManagement
    {
        private readonly Dictionary<Configuration.TitleGroup, string> _isEditingGroups = new();
        private string _newGroupName = string.Empty;

        public List<Configuration.TitleGroup> Groups { get; set; }

        public void Draw(out bool save, out bool close, out bool reset)
        {
            save = false;
            close = false;
            reset = false;

            if (ImGui.BeginTabBar("ManageGroupsBar"))
            {
                if (ImGui.BeginTabItem("Manage Groups"))
                {
                    Vector2 windowSize = ImGui.GetWindowSize();
                    Vector2 size = new Vector2(windowSize.X - 15, windowSize.Y - 100);
                    if (ImGui.BeginChild("titleTable", size))
                    {
                        if (ImGui.BeginTable("GroupNames", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY))
                        {
                            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableSetupColumn("Titles", ImGuiTableColumnFlags.WidthFixed, 64);
                            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 96);
                            ImGui.TableHeadersRow();

                            for (int i = 0; i < Groups.Count; ++i)
                            {
                                var group = Groups[i];
                                ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                                bool isEditing = _isEditingGroups.TryGetValue(group, out string name);
                                if (!isEditing)
                                    name = group.Name;

                                if (ImGui.TableNextColumn())
                                {
                                    if (isEditing)
                                    {
                                        ImGui.InputText($"###Edit{i}", ref name, 32);
                                        _isEditingGroups[group] = name;
                                    }
                                    else
                                        ImGui.Text(name);
                                }

                                if (ImGui.TableNextColumn())
                                    ImGui.Text(FormatTitleCount(group));

                                if (ImGui.TableNextColumn())
                                {
                                    if (isEditing)
                                    {
                                        bool canSave = !string.IsNullOrEmpty(name) && !Groups.Any(x =>
                                            group != x && x.Name.Equals(name,
                                                StringComparison.CurrentCultureIgnoreCase));
                                        ImGui.BeginDisabled(!canSave);
                                        ImGui.PushID($"###Save{i}");
                                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Check))
                                        {
                                            group.Name = name;
                                            _isEditingGroups.Remove(group);
                                        }

                                        ImGui.PopID();
                                        ImGui.EndDisabled();

                                        ImGui.SameLine();
                                        ImGui.PushID($"###Discard{i}");
                                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Ban))
                                            _isEditingGroups.Remove(group);
                                        if (ImGui.IsItemHovered())
                                            ImGui.SetTooltip($"Revert to previous name '{group.Name}'.");
                                        ImGui.PopID();
                                    }
                                    else
                                    {
                                        using (ImRaii.PushId($"###Edit{i}"))
                                        {
                                            if (ImGuiComponents.IconButton(FontAwesomeIcon.Edit))
                                                _isEditingGroups[group] = name;
                                            if (ImGui.IsItemHovered())
                                                ImGui.SetTooltip("Edit the name of this group.");
                                        }

                                        ImGui.SameLine();

                                        using (ImRaii.PushId($"###MarkDefault{i}"))
                                        {
                                            if (group.IsDefault)
                                            {
                                                using (ImRaii.Disabled())
                                                {
                                                    using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow))
                                                        ImGuiComponents.IconButton(FontAwesomeIcon.Star);
                                                }

                                                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                                                    ImGui.SetTooltip("This group will be used when you use\n" +
                                                                     "/ptitle without a group name");
                                            }
                                            else
                                            {
                                                if (ImGuiComponents.IconButton(FontAwesomeIcon.Star))
                                                {
                                                    foreach (var g in Groups)
                                                        g.IsDefault = false;
                                                    group.IsDefault = true;
                                                }

                                                if (ImGui.IsItemHovered())
                                                    ImGui.SetTooltip(
                                                        "Mark as default group that'll be used if you use\n" +
                                                        "/ptitle without a group name");
                                            }
                                        }

                                        if (!group.IsDefault)
                                        {
                                            ImGui.SameLine();

                                            using (ImRaii.PushId($"###Remove{i}"))
                                            {
                                                if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                                                    Groups.Remove(group);
                                                if (ImGui.IsItemHovered())
                                                    ImGui.SetTooltip("Remove this group.");
                                            }
                                        }
                                    }
                                }
                            }

                            ImGui.TableNextRow();
                            if (ImGui.TableNextColumn())
                                ImGui.InputText("###NewGroup", ref _newGroupName, 32);

                            if (ImGui.TableNextColumn())
                                ImGui.Text(string.Empty);

                            if (ImGui.TableNextColumn())
                            {
                                bool canAdd = !string.IsNullOrEmpty(_newGroupName) && !Groups.Any(x =>
                                    x.Name.Equals(_newGroupName, StringComparison.CurrentCultureIgnoreCase));
                                ImGui.BeginDisabled(!canAdd);
                                ImGui.PushID("###AddNew");
                                if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                                {
                                    Groups.Add(new Configuration.TitleGroup { Name = _newGroupName });
                                    _newGroupName = string.Empty;
                                }

                                ImGui.PopID();
                                ImGui.EndDisabled();
                            }

                            ImGui.EndTable();
                        }

                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Separator();
            if (ImGui.Button("Save"))
                save = true;
            ImGui.SameLine();
            if (ImGui.Button("Save and Go Back"))
            {
                save = true;
                close = true;
            }

            ImGui.SameLine();


            ImGui.BeginDisabled(!ImGui.GetIO().KeyCtrl);
            if (ImGui.Button("Discard Changes"))
            {
                reset = true;
                _isEditingGroups.Clear();
                _newGroupName = string.Empty;
            }

            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && !ImGui.GetIO().KeyCtrl)
                ImGui.SetTooltip("Hold CTRL to discard any changes you've made since you've last saved.");

            if (save)
            {
                Service.Configuration.SetCurrentCharacterGroups(Groups);
                Service.PluginInterface.SavePluginConfig(Service.Configuration);
            }
        }
    }
}
