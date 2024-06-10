using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class FlashLightItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Flashlight
        characterVarsCode.PrependGMLInCode("global.flashlightLevel = 0;");
        gmData.Code.ByName("gml_Script_ApplyLightPreset").ReplaceGMLInCode("global.darkness", "lightLevel");
        gmData.Code.ByName("gml_Script_ApplyLightPreset").PrependGMLInCode(
            """
            var lightLevel = 0
            lightLevel = global.darkness - global.flashlightLevel
            if (lightLevel < 0)
                lightLevel = 0
            if (lightLevel > 4)
                lightLevel = 4
            """);
    }
}
