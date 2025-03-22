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
        EnsureBasicConfiguration();
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
        Service.ClientState.TerritoryChanged += RandomTitleEvent;
    }

    private void RandomTitleEvent(ushort _)
    {
        if(Service.Configuration.assignRandomTitleOnAreaChange)
            SetRandomTitleFromGroup(Service.Configuration.randomTitleGroup);
    }

    private void EnsureBasicConfiguration()
    {
        bool save = false;
        foreach (var (_, groups) in Service.Configuration.TitleGroups)
        {
            if (groups.Count == 0)
            {
                groups.Add(new Configuration.TitleGroup { Name = "default", IsDefault = true });
                save = true;
            }

            if (!groups.Any(x => x.IsDefault))
            {
                groups.First().IsDefault = true;
                save = true;
            }
        }

        if (save)
            Service.PluginInterface.SavePluginConfig(Service.Configuration);
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
        var groups = Service.Configuration.GetCurrentCharacterGroups();
        Configuration.TitleGroup? group;
        if (string.IsNullOrEmpty(args))
        {
            group = groups.FirstOrDefault(x => x.IsDefault);
            if (group == null)
            {
                Service.Chat.PrintError(
                    "[Title Roulette] No group is configured as the default when this command is used without a group name, open /ptitlecfg and click the 'cog' icon to open group management.");
                return;
            }
        }
        else
        {
            group = groups.FirstOrDefault(x => args.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase));
            if (group == null)
            {
                if (Service.Configuration.ShowErrorOnMissingGroup)
                    Service.Chat.PrintError($"[Title Roulette] Group '{args}' does not exist.");
                return;
            }
        }
        SetRandomTitleFromGroup(group);
    }

    private void SetRandomTitleFromGroup(Configuration.TitleGroup group)
    {
        int titleCount = group.Titles.Count;
        if (titleCount == 0)
        {
            if (Service.Configuration.ShowErrorOnEmptyGroup)
            {
                Service.Chat.PrintError(
                                        $"[Title Roulette] Can't pick a random title from group '{group.Name}' as it is empty.");
            }

            return;
        }

        ushort       currentTitleId  = Service.GameFunctions.GetCurrentTitleId();
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
        Service.ClientState.TerritoryChanged -= RandomTitleEvent;
    }
}
