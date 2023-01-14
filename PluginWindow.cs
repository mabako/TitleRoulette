using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Components;

namespace TitleRoulette
{
    public partial class PluginWindow : Window
    {
        private ulong contentId;
        private List<Configuration.TitleGroup> groups;
        private int currentGroup = 0;
        private bool female = false;
        private List<Title> sortedTitles = new List<Title>();
        private bool manageGroups = false;
        private Dictionary<Configuration.TitleGroup, string> isEditingGroups = new();
        private string newGroupName = string.Empty;

        public PluginWindow() : base("Title Roulette###TitleRoulette")
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

            if (Service.ClientState.LocalContentId != contentId)
            {
                contentId = Service.ClientState.LocalContentId;
                groups = Service.Configuration.GetCurrentCharacterGroups();
                currentGroup = 0;
                female = Service.ClientState.LocalPlayer.Customize[(int)CustomizeIndex.Gender] == 1;
                sortedTitles = Service.Titles.OrderBy(x => female ? x.FeminineName : x.MasculineName, StringComparer.CurrentCultureIgnoreCase).ToList();
                isEditingGroups.Clear();
            }

            if (manageGroups)
            {
                DrawManageGroups();

                ImGui.Separator();
                if (ImGui.Button("Save"))
                {
                    manageGroups = false;
                    isEditingGroups.Clear();
                    newGroupName = string.Empty;
                }
            }
            else
            {
                DrawGroups();

                ImGui.Separator();
                if (ImGui.Button("Save and Close"))
                {
                    Service.PluginInterface.SavePluginConfig(Service.Configuration);
                    IsOpen = false;
                }
            }


            if (!Service.GameFunctions.IsAnyTitleUnlocked())
            {
                DrawNotAvailable("The game currently knows of no unlocked titles.\nPlease open 'Character' > 'Acquired Titles' once.");
            }
        }

        private void DrawManageGroups()
        {
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
                            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 64);
                            ImGui.TableHeadersRow();

                            for (int i = 0; i < groups.Count; ++i)
                            {
                                var group = groups[i];
                                ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

                                bool isEditing = isEditingGroups.TryGetValue(group, out string name);
                                if (!isEditing)
                                    name = group.Name;

                                if (ImGui.TableNextColumn())
                                {
                                    if (isEditing)
                                    {
                                        ImGui.InputText($"###Edit{i}", ref name, 32);
                                        isEditingGroups[group] = name;
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
                                        bool canSave = !string.IsNullOrEmpty(name) && !groups.Any(x => group != x && x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
                                        ImGui.BeginDisabled(!canSave);
                                        ImGui.PushID($"###Save{i}");
                                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                                        {
                                            group.Name = name;
                                            isEditingGroups.Remove(group);
                                        }
                                        ImGui.PopID();
                                        ImGui.EndDisabled();

                                        ImGui.SameLine();
                                        ImGui.PushID($"###Discard{i}");
                                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Ban))
                                            isEditingGroups.Remove(group);
                                        ImGui.PopID();
                                    }
                                    else if (name != "default")
                                    {
                                        ImGui.PushID($"###Edit{i}");
                                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Edit))
                                            isEditingGroups[group] = name;
                                        ImGui.PopID();

                                        ImGui.SameLine();
                                        ImGui.PushID($"###Edit{i}");
                                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
                                            groups.Remove(group);
                                        ImGui.PopID();
                                    }
                                }
                            }

                            ImGui.TableNextRow();
                            if (ImGui.TableNextColumn())
                                ImGui.InputText("###NewGroup", ref newGroupName, 32);

                            if (ImGui.TableNextColumn())
                                ImGui.Text(string.Empty);

                            if (ImGui.TableNextColumn())
                            {
                                bool canAdd = !string.IsNullOrEmpty(newGroupName) && !groups.Any(x => x.Name.Equals(newGroupName, StringComparison.CurrentCultureIgnoreCase));
                                ImGui.BeginDisabled(!canAdd);
                                ImGui.PushID("###AddNew");
                                if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                                {
                                    groups.Add(new Configuration.TitleGroup { Name = newGroupName });
                                    newGroupName = string.Empty;
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
        }

        private void DrawGroups()
        {
            if (groups.Count == 0)
            {
                manageGroups = true;
                return;
            }

            if (ImGui.BeginTabBar("SelectTitlesBar"))
            {
                if (ImGui.BeginTabItem("Select Titles"))
                {
                    currentGroup = Math.Min(currentGroup, groups.Count - 1);
                    if (ImGui.BeginCombo("###GroupCombo", $"{groups[currentGroup].Name} ({FormatTitleCount(groups[currentGroup])})"))
                    {
                        for (int i = 0; i < groups.Count; ++i)
                        {
                            if (ImGui.Selectable($"{groups[i].Name} ({FormatTitleCount(groups[i])})", i == currentGroup))
                                currentGroup = i;
                        }

                        ImGui.EndCombo();
                    }
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
                        manageGroups = true;

                    ImGui.Separator();
                    Vector2 windowSize = ImGui.GetWindowSize();
                    Vector2 size = new Vector2(windowSize.X - 15, windowSize.Y - 130);
                    if (ImGui.BeginChild("titleTable", size))
                    {
                        if (ImGui.BeginTable("SelectedTitles", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY))
                        {
                            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);
                            ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthStretch);
                            ImGui.TableHeadersRow();

                            foreach (var title in sortedTitles)
                            {
                                if (!Service.GameFunctions.IsTitleUnlocked(title.Id))
                                    continue;

                                ImGui.TableNextRow();
                                if (ImGui.TableNextColumn())
                                {
                                    bool used = groups[currentGroup].Titles.Contains(title.Id);
                                    if (ImGui.Checkbox($"###{title.Id}", ref used))
                                    {
                                        if (used)
                                            groups[currentGroup].Titles.Add(title.Id);
                                        else
                                            groups[currentGroup].Titles.Remove(title.Id);
                                    }
                                }

                                if (ImGui.TableNextColumn())
                                    ImGui.Text($"{(title.IsPrefix ? "" : "... ")}{(female ? title.FeminineName : title.MasculineName)}");
                            }

                            ImGui.EndTable();
                        }
                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }

        private string FormatTitleCount(Configuration.TitleGroup group)
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
    }
}
