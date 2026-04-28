using HarmonyLib;
using Keen.Game2.Client.WorldObjects.Character;
using Keen.VRage.Library.Diagnostics;

namespace ClientPlugin.Patches
{
    internal class HeadTrackingOnFootPatch
    {
        #region Methods

        public static CharacterMovementInputHandlerComponent? Instance { get; private set; }

        [HarmonyPatch(typeof(CharacterMovementInputHandlerComponent), "OnAddedToScene")]
        internal class CacheCharacterInputPatch
        {
            private static void Postfix(CharacterMovementInputHandlerComponent __instance)
            {
                Instance = __instance;
                Log.Default.WriteLine($"[{Plugin.Name}] Cached CharacterMovementInputHandlerComponent.");
            }
        }

        [HarmonyPatch(typeof(CharacterMovementInputHandlerComponent), "OnBeforeRemovedFromScene")]
        internal class Patch_ClearCharacterInput
        {
            private static void Postfix()
            {
                HeadTrackingOnFootPatch.Instance = null;
                Log.Default.WriteLine($"[{Plugin.Name}] Cleared CharacterMovementInputHandlerComponent.");
            }
        }

        #endregion Methods
    }
}