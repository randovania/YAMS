using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class CustomSuitDamageReduction
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        if (Math.Abs(seedObject.Patches.OneSuitDamageMultiplier - 0.5) < 0.01 && Math.Abs(seedObject.Patches.TwoSuitsDamageMultiplier - 0.25) < 0.01)
        {
            // Vanilla values, don't modify anything
            return;
        }

        foreach (string codeEntry in new[] { "gml_Script_damage_player", "gml_Script_damage_player_push", "gml_Script_damage_player_knockdown" })
        {
            gmData.Code.ByName(codeEntry).ReplaceGMLInCode(
                """
                    if (global.currentsuit == 1)
                        damage_taken = (ceil(argument0 * 0.5)) * oControl.mod_diffmult
                    if (global.currentsuit == 2)
                    {
                        if (global.hasVaria == 0)
                            damage_taken = (ceil(argument0 * 0.5)) * oControl.mod_diffmult
                        else
                            damage_taken = (ceil(argument0 * 0.25)) * oControl.mod_diffmult
                    }
                """,
                $$"""
                    if (global.currentsuit == 1)
                        damage_taken = (ceil(argument0 * {{seedObject.Patches.OneSuitDamageMultiplier}})) * oControl.mod_diffmult
                    if (global.currentsuit == 2)
                    {
                        if (global.hasVaria == 0)
                            damage_taken = (ceil(argument0 * {{seedObject.Patches.OneSuitDamageMultiplier}})) * oControl.mod_diffmult
                        else
                            damage_taken = (ceil(argument0 * {{seedObject.Patches.TwoSuitsDamageMultiplier}})) * oControl.mod_diffmult
                    }
                """
                );
        }
    }
}
