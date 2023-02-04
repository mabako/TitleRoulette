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

            if (TitleGroups.TryGetValue(Service.ClientState.LocalContentId, out var groups))
                groups = groups.Select(x => x.Copy()).ToList();
            else
                groups = new List<TitleGroup>();

            if (!groups.Any(x => x.Name == "default"))
                groups.Insert(0, new TitleGroup { Name = "default" });
            return groups;
        }

        public bool SetCurrentCharacterGroups(List<TitleGroup> groups)
        {
            if (!Service.ClientState.IsLoggedIn)
                return false;

            TitleGroups[Service.ClientState.LocalContentId] = groups;
            return true;
        }

        public class TitleGroup
        {
            public string Name { get; set; }
            public HashSet<ushort> Titles { get; set; } = new HashSet<ushort>();

            public TitleGroup Copy()
            {
                return new TitleGroup
                {
                    Name = Name,
                    Titles = new HashSet<ushort>(Titles),
                };
            }
        }
    }
}
