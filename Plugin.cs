using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using TitleRoulette.Attributes;

namespace TitleRoulette;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.GameFunctions = new GameFunctions();
        Service.Configuration = (Configuration)pluginInterface.GetPluginConfig()
                                ?? pluginInterface.Create<Configuration>();
        InitializeTitles();
        var window = pluginInterface.Create<PluginWindow>();
        if (window is not null)
        {
            Service.WindowSystem.AddWindow(window);
        }

        Service.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigWindow;
        Service.PluginInterface.UiBuilder.Draw += Service.WindowSystem.Draw;
        Service.PluginCommandManager = new PluginCommandManager<Plugin>(this, Service.CommandManager);
    }

    private void InitializeTitles()
    {
        Service.Titles.Add(new Title
        {
            Id = 0,
            MasculineName = "<no title>",
            FeminineName = "<no title>",
            IsPrefix = true
        });

        foreach (var title in Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Title>()!)
        {
            if (string.IsNullOrEmpty(title.Masculine) || string.IsNullOrEmpty(title.Feminine))
                continue;

            Service.Titles.Add(new Title
            {
                Id = (ushort)title.RowId,
                MasculineName = title.Masculine,
                FeminineName = title.Feminine,
                IsPrefix = title.IsPrefix
            });
        }

        Service.MaxTitleId = Service.Titles.Max(x => x.Id);
    }

    [Command("/ptitle")]
    [HelpMessage("Picks a random title - optionally specified by a group.")]
    public void PickRandomTitle(string command, string args)
    {
        if (string.IsNullOrEmpty(args))
            args = "default";

        var groups = Service.Configuration.GetCurrentCharacterGroups();
        var group = groups.FirstOrDefault(x => args.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase));
        if (group == null)
        {
            Service.Chat.PrintError($"[Title Roulette] Group '{args}' does not exist.");
            return;
        }

        int titleCount = group.Titles.Count;
        if (titleCount == 0)
        {
            Service.Chat.PrintError(
                $"[Title Roulette] Can't pick a random title from group '{args}' as it is empty.");
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

    [Command("/ptitlecfg")]
    [HelpMessage("Opens the configuration window")]
    public void OpenConfigWindow(string command, string args) => OpenConfigWindow();

    private void OpenConfigWindow()
    {
        var window = Service.WindowSystem.Windows.FirstOrDefault(t => t is PluginWindow);
        if (window != null)
            window.IsOpen = !window.IsOpen;
    }

    public void Dispose()
    {
        Service.PluginCommandManager.Dispose();
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigWindow;
        Service.PluginInterface.UiBuilder.Draw -= Service.WindowSystem.Draw;
    }
}
