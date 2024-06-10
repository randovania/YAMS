using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class IBJItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Add IBJ as item
        characterVarsCode.PrependGMLInCode("global.hasIBJ = 0;");
        gmData.Code.ByName("gml_Script_characterCreateEvent").AppendGMLInCode("IBJ_MIDAIR_MAX = 5; IBJLaidInAir = IBJ_MIDAIR_MAX; IBJ_MAX_BOMB_SEPERATE_TIMER = 4; IBJBombSeperateTimer = -1;");
        gmData.Code.ByName("gml_Object_oCharacter_Step_0").AppendGMLInCode("if (IBJBombSeperateTimer >= 0) IBJBombSeperateTimer-- if (!platformCharacterIs(IN_AIR)) IBJLaidInAir = IBJ_MIDAIR_MAX;");
        gmData.Code.ByName("gml_Object_oCharacter_Collision_435").ReplaceGMLInCode("if (isCollisionTop(6) == 0)",
            """
            if (!global.hasIBJ)
            {
                if (state == AIRBALL)
                {
                    if (IBJBombSeperateTimer < 0)
                        IBJLaidInAir--;
                }
                else
                {
                    IBJLaidInAir = IBJ_MIDAIR_MAX;
                }
                if (IBJLaidInAir <= 0)
                    exit;
                IBJBombSeperateTimer = IBJ_MAX_BOMB_SEPERATE_TIMER;
            }
            if (isCollisionTop(6) == 0)
            """);
    }
}
