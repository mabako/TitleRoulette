using Dalamud.Game;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace TitleRoulette
{
    internal class GameFunctions
    {
        private delegate byte ExecuteCommandDelegate(int id, int titleId, uint unk1, int unk2, int unk3);
        private ExecuteCommandDelegate executeCommand;

        public IntPtr titleListPtr;

        /*
        private delegate bool IsTitleUnlockedDelegate(IntPtr titleListPtr, ushort titleId);
        private static IsTitleUnlockedDelegate isTitleUnlocked;
        */

        public GameFunctions(SigScanner sigScanner)
        {
            var executeCommandPtr = sigScanner.ScanText("E8 ?? ?? ?? ?? 8D 43 0A");
            executeCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(executeCommandPtr);

            titleListPtr = sigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? BD 01 00 00 00 E8 ?? ?? ?? ??");

            /*
            var isTitleUnlockedPtr = sigScanner.ScanText("48 8D 0D ?? ?? ?? ?? BD 01 00 00 00 E8 ?? ?? ?? ??");
            isTitleUnlocked = Marshal.GetDelegateForFunctionPointer<IsTitleUnlockedDelegate>(isTitleUnlockedPtr + 12);
            */
        }

        public byte ClearTitle() => executeCommand.Invoke(303, 0, 0, 0, 0);

        public byte SetTitle(ushort titleId) => executeCommand.Invoke(302, titleId, 0, 0, 0);

        //public static bool IsTitleUnlocked(ushort titleId) => isTitleUnlocked.Invoke(titleListPtr, titleId);

        // 6.3 hotfix: this is the function at 140937270, but I have no idea how straightforward it is to invoke that
        // [rcx = titleListPtr, dx = id]
        //
        // This only works if the title window has been opened once, so there's presumably some initialization happening somewhere
        public bool IsTitleUnlocked(ushort titleId)
        {
            if (titleId > Service.MaxTitleId)
                return false;
            byte titleBits = Marshal.ReadByte(titleListPtr + 8 + (titleId >> 3));

            return (titleBits & (1 << (titleId & 7))) != 0;
        }

        public bool IsAnyTitleUnlocked()
        {
            return Enumerable.Range(0, Service.MaxTitleId).Any(t => IsTitleUnlocked((ushort)t));
        }
    }
}
