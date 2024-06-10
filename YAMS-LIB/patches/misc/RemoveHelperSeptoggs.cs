using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.misc;

public class RemoveHelperSeptoggs
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Turn off Septoggs if the wished configuration
        if (seedObject.Patches.SeptoggHelpers) characterVarsCode.ReplaceGMLInCode("global.septoggHelpers = 0", "global.septoggHelpers = 1");

        foreach (UndertaleCode? code in gmData.Code.Where(c => c.Name.Content.StartsWith("gml_Script_scr_septoggs_")))
        {
            code.PrependGMLInCode("if (!global.septoggHelpers) return true; else return false;");
        }

        // TODO: do not use ignoreErrors, also get rid of instance IDs
        UndertaleGameObject? elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (UndertaleRoom.GameObject go in room.GameObjects.Where(go => go.ObjectDefinition == elderSeptogg && go.CreationCode is not null))
            {
                go.CreationCode.ReplaceGMLInCode("oControl.mod_septoggs_bombjumps_easy == 0 && global.hasBombs == 1",
                    "!global.septoggHelpers", true);
            }
        }

        gmData.Code.ByName("gml_RoomCC_rm_a0h25_4105_Create").ReplaceGMLInCode("else if (global.hasBombs == 1 || global.hasSpiderball == 1 || global.hasSpacejump == 1)",
            "else if (!global.septoggHelpers)");
        // Make these septoggs always appear instead of only when coming from certain room
        gmData.Code.ByName("gml_RoomCC_rm_a2a13_5007_Create").ReplaceGMLInCode("&& oControl.mod_previous_room == 103", "");
        gmData.Code.ByName("gml_RoomCC_rm_a3a07_5533_Create").ReplaceGMLInCode("&& oControl.mod_previous_room == 136", "");
        gmData.Code.ByName("gml_RoomCC_rm_a5a05_8701_Create").ReplaceGMLInCode("&& oControl.mod_previous_room == 300", "");

    }
}
