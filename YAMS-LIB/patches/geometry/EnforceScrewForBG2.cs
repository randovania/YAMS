using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.geometry;

public class EnforceScrewForBG2
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        characterVarsCode.AppendGMLInCode($"global.enforceScrewForBG2 = {(seedObject.Patches.EnforceScrewForBG2 ? "1" : "0")};");

        gmData.Code.ByName("gml_Object_Rooma3c03Fix_Collision_267").ReplaceGMLInCode("global.screwattack == 0", "!global.screwattack && !global.enforceScrewForBG2");
    }
}
