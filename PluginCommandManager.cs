using Dalamud.Game.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin.Services;
using TitleRoulette.Attributes;
using static Dalamud.Game.Command.CommandInfo;

namespace TitleRoulette;

internal sealed class PluginCommandManager<THost> : IDisposable
{
    private readonly ICommandManager _commandManager;
    private readonly (string, CommandInfo)[] _pluginCommands;
    private readonly THost _host;

    public PluginCommandManager(THost host, ICommandManager commandManager)
    {
        this._commandManager = commandManager;
        this._host = host;

        this._pluginCommands = host.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public |
                                                        BindingFlags.Static | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<CommandAttribute>() != null)
            .SelectMany(GetCommandInfoTuple)
            .ToArray();

        AddCommandHandlers();
    }

    private void AddCommandHandlers()
    {
        foreach (var (command, commandInfo) in this._pluginCommands)
        {
            this._commandManager.AddHandler(command, commandInfo);
        }
    }

    private void RemoveCommandHandlers()
    {
        foreach (var (command, _) in this._pluginCommands)
        {
            this._commandManager.RemoveHandler(command);
        }
    }

    private IEnumerable<(string, CommandInfo)> GetCommandInfoTuple(MethodInfo method)
    {
        var handlerDelegate = (HandlerDelegate)Delegate.CreateDelegate(typeof(HandlerDelegate), this._host, method);

        var command = handlerDelegate.Method.GetCustomAttribute<CommandAttribute>();
        var aliases = handlerDelegate.Method.GetCustomAttribute<AliasesAttribute>();
        var helpMessage = handlerDelegate.Method.GetCustomAttribute<HelpMessageAttribute>();
        var doNotShowInHelp = handlerDelegate.Method.GetCustomAttribute<DoNotShowInHelpAttribute>();

        var commandInfo = new CommandInfo(handlerDelegate)
        {
            HelpMessage = helpMessage?.HelpMessage ?? string.Empty,
            ShowInHelp = doNotShowInHelp == null,
        };

        // Create list of tuples that will be filled with one tuple per alias, in addition to the base command tuple.
        var commandInfoTuples = new List<(string, CommandInfo)> { (command!.Command, commandInfo) };
        if (aliases != null)
        {
            foreach (var alias in aliases.Aliases)
            {
                commandInfoTuples.Add((alias, commandInfo));
            }
        }

        return commandInfoTuples;
    }

    public void Dispose()
    {
        RemoveCommandHandlers();
    }
}
