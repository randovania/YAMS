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
        characterVarsCode.AppendGMLInCode($"global.a3Block = {(seedObject.Patches.A3EntranceBlocks ? "1" : "0")};");


        var bombBlock = gmData.Rooms.ByName("rm_a3h03").GameObjects.First(go => go.X == 896 && go.Y == 160 && go.ObjectDefinition.Name.Content == "oBlockBomb");
        bombBlock.CreationCode.ReplaceGMLInCode(
            "if ((oControl.mod_randomgamebool == 1 || oControl.mod_splitrandom == 1) && global.hasBombs == 0 && global.ptanks == 0)",
            "if (!global.a3Block)");
    }
}
