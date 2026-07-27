using System;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using XIVSlothComboX.Core;

namespace XIVSlothComboX.Services
{
    public static unsafe class PartyTargetingService
    {
        private static readonly IntPtr pronounModule = (IntPtr)Framework.Instance()->GetUIModule()->GetPronounModule();
        public static GameObject* UITarget => (GameObject*)*(IntPtr*)(pronounModule + 0x290);

        public static ulong GetObjectID(GameObject* o)
        {
            var id = o->GetGameObjectId();
            return id.ObjectId;
        }

        private static readonly delegate* unmanaged<IntPtr, uint, GameObject*> getGameObjectFromPronounID = (delegate* unmanaged<IntPtr, uint, GameObject*>)
            Svc.SigScanner.ScanText(HookAddress.GetGameObjectFromPronounID);
        public static GameObject* GetGameObjectFromPronounID(uint id) => getGameObjectFromPronounID(pronounModule, id);
    }
}
