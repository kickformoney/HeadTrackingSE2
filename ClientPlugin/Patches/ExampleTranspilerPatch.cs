#if DISABLED_TRANSPILER_PATCH_EXAMPLE

using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Keen.Game2.Client.UI.HUD.Character;
using ClientPlugin.Tools;

namespace ClientPlugin.Patches;

// ReSharper disable once UnusedType.Global
[HarmonyPatch(typeof(CharacterHUDStatusViewModel))]
public static class CharacterHUDStatusViewModelPatch
{
    private static Config Config => Config.Current;

    // ReSharper disable once UnusedMember.Local
    [HarmonyTranspiler]
    [HarmonyPatch("Update")]
    private static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod, ILGenerator ilGenerator)
    {
        // Make your patch configurable.
        // Here it needs restarting the game, since patching happens only once.
        // Alternatively patch unconditionally, but make the functionality configurable
        // inside your logic, so changing the config does not need restarting the game.
        // There is a trade-off with performance and plugin compatibility here.
        if (!Config.Enabled)
            return instructions;

        // This call will create a .il file next to this patch file with the original
        // IL code of the patched method. Compare that with the modified IL code (see below).
        // It is good practice to commit these IL files into your project for later reference,
        // so you can instantly see any changes in the game's code after game updates.
        var il = instructions.ToList();
        il.RecordOriginalCode(patchedMethod);

        // You may want to verify whether the game's code has changed on an update.
        // If this check fails, then your plugin will fail to load with a clean error
        // message. It allows you to look into game code changes efficiently and fix
        // your patch in the next version of your plugin. This check will also fail
        // if another plugin loaded before yours has already patched the same method.
        // This verification can be disabled by setting this environment variable:
        // `SE2_PLUGIN_DISABLE_METHOD_VERIFICATION`
        // This may be required on Linux if Wine/Proton is using Mono.
        // TODO: Replace the hash below with the actual hash from the .il file
        //il.VerifyCodeHash(patchedMethod, "FIXME");

        // Modify the IL code of the method as needed to remove/replace game code.
        // Make sure to keep the stack balanced and don't delete labels which are still in use.
        // Use ilGenerator.DefineLabel() to define new labels (sometimes required).
        // Use ilGenerator.DeclareLocal() to create new local variables (usually not required).
        // You can remove the ilGenerator argument if it remains unused.
        // Some example logic follows:

        // TODO: Modify the IL code in `il` accordingly

        // This call will create a .il file next to this patch file with the modified
        // IL code of the patched method. Compare this with the original (see above).
        // It is good practice to commit these IL files into your project for later reference,
        // so you can instantly see any changes in the game's code after game updates.
        il.RecordPatchedCode(patchedMethod);
        return il;
    }
}

#endif
