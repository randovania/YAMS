using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class LongBeamItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Add Long Beam as an item
        characterVarsCode.PrependGMLInCode("global.hasLongBeam = 0;");
        gmData.Code.ByName("gml_Object_oBeam_Create_0").AppendGMLInCode("existingTimer = 12;");
        gmData.Code.ByName("gml_Object_oBeam_Step_0").PrependGMLInCode("if (existingTimer > 0) existingTimer--; if (existingTimer <= 0 && !global.hasLongBeam) instance_destroy()");

    }
}
