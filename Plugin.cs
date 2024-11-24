using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;

namespace TitleRoulette;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.GameFunctions = new GameFunctions();
        Service.Configuration = (Configuration?)pluginInterface.GetPluginConfig()
                                ?? pluginInterface.Create<Configuration>()!;
        InitializeTitles();

        Service.WindowSystem.AddWindow(new PluginWindow());
        Service.WindowSystem.AddWindow(new ConfigWindow());

        Service.PluginInterface.UiBuilder.OpenMainUi += OpenMainWindow;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;
        Service.PluginInterface.UiBuilder.Draw += Service.WindowSystem.Draw;
        Service.CommandManager.AddHandler("/ptitle", new CommandInfo(this.PickRandomTitle)
        {
            HelpMessage = "Picks a random title - optionally specified by a group."
        });
        Service.CommandManager.AddHandler("/ptitlecfg", new CommandInfo(this.OpenMainWindow)
        {
            HelpMessage = "Configures which titles are used in title roulette."
        });
    }

    private void InitializeTitles()
    {
        Service.Titles.Add(0, new Title
        {
            Id = 0,
            MasculineName = "<no title>",
            FeminineName = "<no title>",
            IsPrefix = true
        });

        foreach (var title in Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Title>())
        {
            if (string.IsNullOrEmpty(title.Masculine.ToString()) || string.IsNullOrEmpty(title.Feminine.ToString()))
                continue;

            Service.Titles.Add((ushort)title.RowId, new Title
            {
                Id = (ushort)title.RowId,
                MasculineName = title.Masculine.ToString(),
                FeminineName = title.Feminine.ToString(),
                IsPrefix = title.IsPrefix
            });
        }
    }

    private void PickRandomTitle(string command, string args)
    {
        if (string.IsNullOrEmpty(args))
            args = "default";

        var groups = Service.Configuration.GetCurrentCharacterGroups();
        var group = groups.FirstOrDefault(x => args.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase));
        if (group == null)
        {
            if (Service.Configuration.ShowErrorOnMissingGroup)
            {
                Service.Chat.PrintError($"[Title Roulette] Group '{args}' does not exist.");
            }

            return;
        }

        int titleCount = group.Titles.Count;
        if (titleCount == 0)
        {
            if (Service.Configuration.ShowErrorOnEmptyGroup)
            {
                Service.Chat.PrintError(
                    $"[Title Roulette] Can't pick a random title from group '{args}' as it is empty.");
            }

            return;
        }

        ushort currentTitleId = Service.GameFunctions.GetCurrentTitleId();
        List<ushort> differentTitles = group.Titles.Where(v => v != currentTitleId).ToList();
        if (differentTitles.Count > 0)
        {
            ushort titleId = differentTitles[new Random().Next(differentTitles.Count)];
            Service.GameFunctions.SetTitle(titleId);
        }
    }

    private void OpenMainWindow(string command, string args) => OpenMainWindow();

    private void OpenMainWindow()
    {
        var window = Service.WindowSystem.Windows.FirstOrDefault(t => t is PluginWindow);
        if (window != null)
            window.IsOpen = !window.IsOpen;
    }

    private void OpenConfigWindow()
    {
        var window = Service.WindowSystem.Windows.FirstOrDefault(t => t is ConfigWindow);
        if (window != null)
            window.IsOpen = !window.IsOpen;
    }

    public void Dispose()
    {
        Service.CommandManager.RemoveHandler("/ptitlecfg");
        Service.CommandManager.RemoveHandler("/ptitle");
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;
        Service.PluginInterface.UiBuilder.OpenMainUi -= OpenMainWindow;
        Service.PluginInterface.UiBuilder.Draw -= Service.WindowSystem.Draw;
    }
}
