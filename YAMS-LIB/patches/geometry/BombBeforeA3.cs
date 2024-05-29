using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.geometry;

public class BombBeforeA3
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Bomb block before a3 entry
        if (seedObject.Patches.A3EntranceBlocks)
        {
            characterVarsCode.ReplaceGMLInCode("global.a3Block = 0", "global.a3Block = 1;");
        }

        gmData.Code.ByName("gml_RoomCC_rm_a3h03_5279_Create").ReplaceGMLInCode(
            "if ((oControl.mod_randomgamebool == 1 || oControl.mod_splitrandom == 1) && global.hasBombs == 0 && global.ptanks == 0)",
            "if (!global.a3Block)");
    }
}
