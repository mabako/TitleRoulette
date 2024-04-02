using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace TitleRoulette;

public sealed class ConfigWindow : Window
{
    public ConfigWindow()
        : base("Title Roulette Config###TitleRouletteConfig")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void Draw()
    {
        bool showErrorOnEmptyGroups = Service.Configuration.ShowErrorOnEmptyGroup;
        if (ImGui.Checkbox(
                "Show error if '/ptitle' or '/ptitle groupName' specifies a group with no configured titles",
                ref showErrorOnEmptyGroups))
        {
            Service.Configuration.ShowErrorOnEmptyGroup = showErrorOnEmptyGroups;
            Service.PluginInterface.SavePluginConfig(Service.Configuration);
        }

        bool showErrorOnMissingGroup = Service.Configuration.ShowErrorOnMissingGroup;
        if (ImGui.Checkbox("Show error if '/ptitle groupName' references a non-existent group",
                ref showErrorOnMissingGroup))
        {
            Service.Configuration.ShowErrorOnMissingGroup = showErrorOnMissingGroup;
            Service.PluginInterface.SavePluginConfig(Service.Configuration);
        }
    }
}
