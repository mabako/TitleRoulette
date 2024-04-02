using System;
using Dalamud.Utility.Signatures;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TitleRoulette;

internal sealed class GameFunctions
{
#pragma warning disable CS0649
    private delegate byte ExecuteCommandDelegate(int id, int titleId, uint unk1, int unk2, int unk3);
    [Signature("E8 ?? ?? ?? ?? 8D 43 0A")]
    private ExecuteCommandDelegate _executeCommand;

#pragma warning restore CS0649

    public GameFunctions()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public byte SetTitle(ushort titleId) => _executeCommand.Invoke(302, titleId, 0, 0, 0);

    public bool IsTitleUnlocked(ushort titleId)
    {
        if (Service.Titles.Any(x => x.Id == titleId))
        {
            unsafe
            {
                UIState* uiState = UIState.Instance();
                return uiState != null && uiState->TitleList.IsTitleUnlocked(titleId);
            }
        }

        return false;
    }

    public bool IsTitleListLoaded()
    {
        unsafe
        {
            UIState* uiState = UIState.Instance();
            return uiState != null && uiState->TitleList.DataReceived;
        }
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
