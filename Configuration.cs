using Dalamud.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace TitleRoulette;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    #region Saved configuration values

    public TitleGroup randomTitleGroup { get; set; }
    public bool       assignRandomTitleOnAreaChange { get; set; }

    public Dictionary<ulong, List<TitleGroup>> TitleGroups { get; set; } = new();
    public bool ShowErrorOnEmptyGroup { get; set; } = true;
    public bool ShowErrorOnMissingGroup { get; set; } = true;

    #endregion

    public List<TitleGroup> GetCurrentCharacterGroups()
    {
        if (!Service.ClientState.IsLoggedIn)
            return new List<TitleGroup>();

        if (TitleGroups.TryGetValue(Service.ClientState.LocalContentId, out var groups))
            groups = groups.Select(x => x.Copy()).ToList();
        else
            groups = new List<TitleGroup>();

        return groups;
    }

    public bool SetCurrentCharacterGroups(List<TitleGroup> groups)
    {
        if (!Service.ClientState.IsLoggedIn)
            return false;

        TitleGroups[Service.ClientState.LocalContentId] = groups;
        return true;
    }

    public sealed class TitleGroup
    {
        public required string Name { get; set; }
        public HashSet<ushort> Titles { get; set; } = [];
        public bool IsDefault { get; set; }

        public TitleGroup Copy()
        {
            return new TitleGroup
            {
                Name = Name,
                Titles = [.. Titles],
                IsDefault = IsDefault,
            };
        }
    }
}
