using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.qol;

public class ShowUnveiledBreakables
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Force all breakables (except the hidden super blocks) to be visible
        characterVarsCode.AppendGMLInCode($"global.unveilBlocks = {(seedObject.Cosmetics.UnveilBlocks ? "1" : "0")}");

        gmData.Code.ByName("gml_Object_oSolid_Alarm_5").AppendGMLInCode("if (global.unveilBlocks && sprite_index >= sBlockShoot && sprite_index <= sBlockSand)\n" +
                                                                        "{ event_user(1); visible = true; }");
    }
}
