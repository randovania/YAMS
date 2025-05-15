using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.qol;

public class DontDespawnWeaponsOffscreen
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Make beams not instantly despawn when out of screen
        gmData.Code.ByName("gml_Object_oBeam_Step_0").ReplaceGMLInCode(
            "if (x < (view_xview[0] - 48 - (oControl.widescreen_space / 2)) || x > (view_xview[0] + view_wview[0] + 48 + (oControl.widescreen_space / 2)) || y < (view_yview[0] - 48) || y > (view_yview[0] + view_hview[0] + 48))",
            "if (x > (room_width + 80) || x < -80 || y > (room_height + 80) || y < -160)");

        // Make Missiles not instantly despawn when out of screen
        gmData.Code.ByName("gml_Object_oMissile_Step_0").ReplaceGMLInCode(
            "if (x < (view_xview[0] - 48 - (oControl.widescreen_space / 2)) || x > (view_xview[0] + view_wview[0] + 48 + (oControl.widescreen_space / 2)) || y < (view_yview[0] - 48) || y > (view_yview[0] + view_hview[0] + 48))",
            "if (x > (room_width + 80) || x < -80 || y > (room_height + 80) || y < -160)");

        // No more Out of Bounds oSmallsplash crashes
        gmData.Code.ByName("gml_Object_oSmallSplash_Step_0").ReplaceGMLInCode("if (global.watertype == 0)", "if (global.watertype == 0 && instance_exists(oWater))");
    }
}
