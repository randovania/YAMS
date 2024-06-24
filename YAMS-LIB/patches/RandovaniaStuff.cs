using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class RandovaniaStuff
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Remove other game modes, rename "normal" to "Randovania"
        UndertaleCode? gameSelMenuStepCode = gmData.Code.ByName("gml_Object_oGameSelMenu_Step_0");
        gameSelMenuStepCode.ReplaceGMLInCode("if (global.mod_gamebeaten == 1)", "if (false)");
        gmData.Code.ByName("gml_Object_oSlotMenu_normal_only_Create_0").ReplaceGMLInCode(
            "d0str = get_text(\"Title-Additions\", \"GameSlot_NewGame_NormalGame\")", "d0str = \"Randovania\";");

        // Add Credits
        gmData.Code.ByName("gml_Object_oCreditsText_Create_0").ReplaceGMLInCode("/Japanese Community;;",
            "/Japanese Community;;;*AM2R Randovania Credits;;*Development;Miepee=JesRight;;*Logic Database;Miepee=JeffGainsNGames;/Esteban 'DruidVorse' Criado;;*Art;ShirtyScarab=AbyssalCreature;;/With contributions from many others;;;");

        // Unlock fusion etc. by default
        UndertaleCode? unlockStuffCode = gmData.Code.ByName("gml_Object_oControl_Other_2");
        unlockStuffCode.AppendGMLInCode("global.mod_fusion_unlocked = 1; global.mod_gamebeaten = 1;");
        gmData.Code.ByName("gml_Object_oSS_Fg_Create_0").AppendGMLInCode("itemcollunlock = 1;");
    }
}
