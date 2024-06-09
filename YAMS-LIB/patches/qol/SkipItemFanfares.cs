using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.qol;

public class SkipItemFanfares
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        if (seedObject.Patches.SkipItemFanfares) characterVarsCode.ReplaceGMLInCode("global.skipItemFanfare = 0", "global.skipItemFanfare = 1");

        // Put all items as type one
        gmData.Code.ByName("gml_Object_oItem_Other_10").PrependGMLInCode("if (global.skipItemFanfare) itemtype = 1;");

        // Show popup text only when we skip the cutscene
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("if (itemtype == 1)", "if (global.skipItemFanfare)");

        // Removes cutscenes for type 1's
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("display_itemmsg", "if (!global.skipItemFanfare) display_itemmsg");
    }
}
