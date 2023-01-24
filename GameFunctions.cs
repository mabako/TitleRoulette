using Dalamud.Utility.Signatures;
using System.Linq;

namespace TitleRoulette
{
    internal class GameFunctions
    {
#pragma warning disable CS0649
        private delegate byte ExecuteCommandDelegate(int id, int titleId, uint unk1, int unk2, int unk3);
        [Signature("E8 ?? ?? ?? ?? 8D 43 0A")]
        private ExecuteCommandDelegate executeCommand;

        [Signature("48 8D 0D ?? ?? ?? ?? BD 01 00 00 00 E8 ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
        public nint titleListPtr;

        private delegate bool IsTitleUnlockedDelegate(nint titleListPtr, ushort titleId);
        [Signature("B8 ?? ?? ?? ?? 44 0F B7 C2 4C 8B C9")]
        private static IsTitleUnlockedDelegate isTitleUnlocked;
#pragma warning restore CS0649

        public GameFunctions()
        {
            SignatureHelper.Initialise(this);
        }

        public byte ClearTitle() => executeCommand.Invoke(303, 0, 0, 0, 0);

        public byte SetTitle(ushort titleId) => executeCommand.Invoke(302, titleId, 0, 0, 0);

        public bool IsTitleUnlocked(ushort titleId) => Service.Titles.Any(x => x.Id == titleId) && isTitleUnlocked.Invoke(titleListPtr, titleId);

        // TODO There's probably a better way to figure out if titles are loaded?
        public bool IsAnyTitleUnlocked()
        {
            return Enumerable.Range(0, Service.MaxTitleId).Any(t => IsTitleUnlocked((ushort)t));
        }
    }
}
