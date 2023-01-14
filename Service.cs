using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Collections.Generic;

namespace TitleRoulette
{
    internal class Service
    {
        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; set; } = null!;
        [PluginService] public static ChatGui Chat { get; private set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; set; } = null!;
        [PluginService] public static DataManager DataManager { get; set; } = null!;

        public static GameFunctions GameFunctions { get; set; } = null!;
        public static Configuration Configuration { get; set; } = null!;
        public static WindowSystem WindowSystem { get; } = new WindowSystem(typeof(Plugin).AssemblyQualifiedName);
        public static PluginCommandManager<Plugin> PluginCommandManager { get; set; } = null!;
        public static List<Title> Titles { get; set; } = new List<Title>();
        public static ushort MaxTitleId { get; set; } = 676;
    }
}
