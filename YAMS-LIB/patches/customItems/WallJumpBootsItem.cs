using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class WallJumpBootsItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Add WJ as item
        characterVarsCode.PrependGMLInCode("global.hasWJ = 0;");
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode(
            "if (state == JUMPING && statetime > 4 && position_meeting(x, y + 8, oSolid) == 0 && justwalljumped == 0 && walljumping == 0 && monster_drain == 0)",
            "if (state == JUMPING && statetime > 4 && position_meeting(x, (y + 8), oSolid) == 0 && justwalljumped == 0 && walljumping == 0 && monster_drain == 0 && global.hasWJ)");

    }
}
