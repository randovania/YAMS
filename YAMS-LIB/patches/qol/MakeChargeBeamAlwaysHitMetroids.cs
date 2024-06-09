using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.qol;

public class MakeChargeBeamAlwaysHitMetroids
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Make Charge Beam always hit metroids
        foreach (string name in new[]
                 {
                     "gml_Object_oMAlpha_Collision_439", "gml_Object_oMGamma_Collision_439", "gml_Object_oMZeta_Collision_439", "gml_Object_oMZetaBodyMask_Collision_439",
                     "gml_Object_oMOmegaMask2_Collision_439", "gml_Object_oMOmegaMask3_Collision_439"
                 })
        {
            var codeEntry = gmData.Code.ByName(name);
            codeEntry.ReplaceGMLInCode("&& global.missiles == 0 && global.smissiles == 0", "");
            if (codeEntry.GetGMLCode().Contains("""
                    else
                    {
                    """))
            {


                codeEntry.ReplaceGMLInCode("""
                    else
                    {
                    """,
                    """
                    else
                    {
                        if (oBeam.chargebeam) popup_text("Unequip beams to deal Charge damage")
                    """);
            }
            else
            {
                codeEntry.AppendGMLInCode("""
                    else
                    {
                        if (oBeam.chargebeam) popup_text("Unequip beams to deal Charge damage")
                    }
                    """);
            }
        }
    }
}
