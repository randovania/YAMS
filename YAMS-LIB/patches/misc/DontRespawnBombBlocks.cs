using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.misc;

public class DontRespawnBombBlocks
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Stop Bomb blocks from respawning
        if (seedObject.Patches.RespawnBombBlocks) characterVarsCode.ReplaceGMLInCode("global.respawnBombBlocks = 0", "global.respawnBombBlocks = 1");

        // The position here is for a puzzle in a2, that when not respawned makes it a tad hard.
        gmData.Code.ByName("gml_Object_oBlockBomb_Other_10").PrependGMLInCode("if (!global.respawnBombBlocks && !(room == rm_a2a06 && x == 624 && y == 128)) regentime = -1");

        // The bomb block puzzle in the room before varia don't need to have their special handling
        gmData.Code.ByName("gml_RoomCC_rm_a2a06_4761_Create").ReplaceGMLInCode(
            "if (oControl.mod_randomgamebool == 1 && global.hasBombs == 0 && (!global.hasJumpball) && global.hasGravity == 0)",
            "if (false)");
        gmData.Code.ByName("gml_RoomCC_rm_a2a06_4759_Create").ReplaceGMLInCode("if (oControl.mod_randomgamebool == 1 && global.hasBombs == 0 && global.hasGravity == 0)",
            "if (false)");
    }
}
