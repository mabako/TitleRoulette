using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace TitleRoulette;

internal sealed class GameFunctions
{
    public unsafe void SetTitle(ushort titleId)
    {
        UIState* uiState = UIState.Instance();
        if (uiState != null)
            uiState->TitleController.SendTitleIdUpdate(titleId);
    }

    public bool IsTitleUnlocked(ushort titleId)
    {
        if (Service.Titles.ContainsKey(titleId))
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
            return uiState != null && uiState->TitleList.TitlesUnlockBitmask.ContainsAnyExcept((byte)0);
        }
    }

    public unsafe ushort GetCurrentTitleId()
    {
        var localPlayer = Service.ClientState.LocalPlayer;
        if (localPlayer != null && localPlayer.Address != IntPtr.Zero)
        {
            Character* localChar = (Character*)localPlayer.Address;
            return localChar->CharacterData.TitleId;
        }
        else
            return ushort.MaxValue;
    }
}
