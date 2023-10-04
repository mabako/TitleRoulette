using System;
using Dalamud.Utility.Signatures;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace TitleRoulette;

internal sealed class GameFunctions
{
#pragma warning disable CS0649
    private delegate byte ExecuteCommandDelegate(int id, int titleId, uint unk1, int unk2, int unk3);
    [Signature("E8 ?? ?? ?? ?? 8D 43 0A")]
    private ExecuteCommandDelegate _executeCommand;

    [Signature("48 8D 0D ?? ?? ?? ?? BD 01 00 00 00 E8 ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
    private nint _titleListPtr;

    private delegate bool IsTitleUnlockedDelegate(nint titleListPtr, ushort titleId);
    [Signature("B8 ?? ?? ?? ?? 44 0F B7 C2 4C 8B C9")]
    private IsTitleUnlockedDelegate _isTitleUnlocked;
#pragma warning restore CS0649

    public GameFunctions()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public byte SetTitle(ushort titleId) => _executeCommand.Invoke(302, titleId, 0, 0, 0);

    public bool IsTitleUnlocked(ushort titleId) => Service.Titles.Any(x => x.Id == titleId) && _isTitleUnlocked.Invoke(_titleListPtr, titleId);

    // TODO There's probably a better way to figure out if titles are loaded?
    public bool IsAnyTitleUnlocked()
    {
        return Enumerable.Range(0, Service.MaxTitleId).Any(t => IsTitleUnlocked((ushort)t));
    }

    public unsafe ushort GetCurrentTitleId()
    {
        var localPlayer = Service.ClientState.LocalPlayer;
        if (localPlayer != null && localPlayer.Address != IntPtr.Zero)
        {
            Character* localChar = (Character*)localPlayer.Address;
            return localChar->CharacterData.TitleID;
        }
        else
            return ushort.MaxValue;
    }
}
