using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class SpeedBoosterUpgradeItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Add speedbooster reduction
        characterVarsCode.PrependGMLInCode("global.speedBoosterFramesReduction = 0;");
        gmData.Code.ByName("gml_Script_characterStepEvent")
            .ReplaceGMLInCode("speedboost_steps > 75", "speedboost_steps >= 1 && speedboost_steps > (75 - global.speedBoosterFramesReduction)");
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("dash == 30", "dash >= 1 && dash >= (30 - (max(global.speedBoosterFramesReduction, 76)-76))");
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("""
                                                                                 speedboost = 1
                                                                                 canturn = 0
                                                                                 sjball = 0
                                                                                 charge = 0
                                                                                 sfx_play(sndSBStart)
                                                                                 alarm[2] = 30
                                                                             """, """
                                                                                      dash = 30
                                                                                      speedboost = 1
                                                                                      canturn = 0
                                                                                      sjball = 0
                                                                                      charge = 0
                                                                                      sfx_play(sndSBStart)
                                                                                      alarm[2] = 30
                                                                                  """);
    }
}
