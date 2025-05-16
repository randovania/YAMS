using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class MetroidLures
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Add Metroid Lures as an item
        characterVarsCode.PrependGMLInCode("global.hasAlphaLure = 0; global.hasGammaLure = 0; global.hasZetaLure = 0; global.hasOmegaLure = 0;");

        // Alpha
        // Triggerprox2 and triggerprox3 inherit from triggerprox
        gmData.Code.ByName("gml_Object_oMAlphaTriggerProx_Create_0").AppendGMLInCode("if (!global.hasAlphaLure) {instance_destroy() exit} ");
        gmData.Code.ByName("gml_Object_oMAlphaTriggerA2_Create_0").AppendGMLInCode("if (!global.hasAlphaLure) {instance_destroy(); exit}");
        gmData.Code.ByName("gml_Object_oMAlphaTriggerFirstCocoon_Create_0").ReplaceGMLInCode("global.metdead[0]", "(global.metdead[0]) || !global.hasAlphaLure");
        gmData.Code.ByName("gml_Object_oMAlphaTriggerA3A_Create_0").AppendGMLInCode("if (!global.hasAlphaLure) instance_destroy()");

        // Gamma
        gmData.Code.ByName("gml_Object_oMGammaTriggerProx_Create_0").AppendGMLInCode("if (!global.hasGammaLure) {instance_destroy(); exit }");
        gmData.Code.ByName("gml_Object_oMGammaA3BTrigger_Create_0").ReplaceGMLInCode("global.metdead[20] == 1", "(global.metdead[20] == 1) || !global.hasGammaLure");
        gmData.Code.ByName("gml_Object_oMGammaFirstTrigger_Create_0").ReplaceGMLInCode("global.metdead[11] == 0", "(global.metdead[11] == 0) || !global.hasGammaLure");

        // Zeta
        gmData.Code.ByName("gml_Object_oMZeta_Cocoon_Alarm_0").PrependGMLInCode("if (!global.hasZetaLure) {state = 3; exit}");

        // Omega
        gmData.Code.ByName("gml_Object_oMOmega_Alarm_9").ReplaceGMLInCode("global.metdead[myid] > 0", "(global.metdead[myid] > 0) || !global.hasOmegaLure");
    }
}