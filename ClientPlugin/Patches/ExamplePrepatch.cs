#if DISABLED_PRELOADER_EXAMPLE

using ClientPlugin.Tools;
using Mono.Cecil;

namespace ClientPlugin.Patches;

/*
Fix of DecodePixelData in SixLabors.ImageSharp developed by @SpaceGT.
The issue is that the original code expects `stream.Read()` to always
returns all the bytes requested, but this is not guaranteed on .NET Core.
*/
public static class ExamplePrepatch
{
    public static void Prepatch(AssemblyDefinition asmDef)
    {
        // TODO: Filter for the assembly to patch
        if (asmDef.Name.Name != "Game2.Game.dll")
            return;

        // TODO: Filter for the type to patch
        var decoderType = asmDef.MainModule.Types.First(t =>
            t.FullName == "AnalyticsSessionComponent"
        );

        // TODO: Filter for the member to patch
        var method = decoderType.Methods.First(m =>
            m.Name == "UpdatePlayerPosition"
        );

        var il = method.Body.Instructions;
        
        // Record original IL code (debug only)
        il.RecordOriginalCode(method);
        
        // TODO: Optional: Ensure that game's code hasn't changed.
        il.VerifyCodeHash(method, "8e787d98");

        // TODO: Change the IL code in `il` accordingly

        // Record modified IL code (debug only)
        il.RecordPatchedCode(method);
    }
}

#endif
