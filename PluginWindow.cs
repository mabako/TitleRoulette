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

        public PluginWindow() : base("Title Roulette###TitleRoulette")
        {
            IsOpen = true;
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
            if (!Service.ClientState.IsLoggedIn)
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
                sortedTitles = Service.Titles.OrderBy(x => female ? x.NameF : x.NameM, StringComparer.CurrentCultureIgnoreCase).ToList();
            }

            DrawGroups();
            if (ImGui.Button("Save and Close"))
            {
                Service.PluginInterface.SavePluginConfig(Service.Configuration);
                this.IsOpen = false;
            }


            if (!Service.GameFunctions.IsAnyTitleUnlocked())
            {
                DrawNotAvailable("The game currently knows of no unlocked titles.\nTo fix this, open the Character > Acquired Titles menu once.");
            }
        }

        public void DrawGroups()
        {
            if (ImGui.BeginCombo("", currentGroup < groups.Count ? groups[currentGroup].Name : ""))
            {
                for (int i = 0; i < groups.Count; ++i)
                {
                    if (ImGui.Selectable(groups[i].Name, i == currentGroup))
                        currentGroup = i;
                }
            }
            ImGui.SameLine();
            ImGuiComponents.IconButton(FontAwesomeIcon.Cog);

            Vector2 windowSize = ImGui.GetWindowSize();
            Vector2 size = new Vector2(windowSize.X - 15, windowSize.Y - 95);
            if (currentGroup < groups.Count && ImGui.BeginChild("titleTable", size))
            {
                ImGui.Separator();
                if (ImGui.BeginTable("SelectedTitles", 2, ImGuiTableFlags.Borders))
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
                            ImGui.Text($"{(title.IsPrefix ? "" : "... ")}{(female ? title.NameF : title.NameM)}");
                    }

                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
        }

        public void DrawNotAvailable(string message)
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
