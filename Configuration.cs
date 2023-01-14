using Dalamud.Configuration;
using Dalamud.Plugin;
using System.Collections.Generic;
using System.Linq;

namespace TitleRoulette
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        #region Saved configuration values
        public Dictionary<ulong, List<TitleGroup>> TitleGroups { get; set; } = new();
        #endregion

        public List<TitleGroup> GetCurrentCharacterGroups()
        {
            if (!Service.ClientState.IsLoggedIn)
                return new List<TitleGroup>();

            if (!TitleGroups.TryGetValue(Service.ClientState.LocalContentId, out var groups))
            {
                TitleGroups[Service.ClientState.LocalContentId] = groups = new List<TitleGroup>();
            }

            if (!groups.Any(x => x.Name == "default"))
                groups.Insert(0, new TitleGroup { Name = "default" });
            return groups;
        }

        public class TitleGroup
        {
            public string Name { get; set; }
            public HashSet<ushort> Titles { get; set; } = new HashSet<ushort>();
        }
    }
}
