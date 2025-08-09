using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class RandomKnockback
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        if (!seedObject.Patches.RandomKnockback)
            return;
        
        foreach (string codeEntry in new[] { "gml_Script_damage_player", "gml_Script_damage_player_push", "gml_Script_damage_player_knockdown" })
        {
            gmData.Code.ByName(codeEntry).PrependGMLInCode("argument1 *= irandom_range(-20, 20); argument2 *= irandom_range(-10, 10);");

        }
    }
}
