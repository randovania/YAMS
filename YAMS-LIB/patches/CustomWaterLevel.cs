using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class CustomWaterLevel
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        StringBuilder sb = new StringBuilder("liquidInfoMappings = ds_map_create();");
        foreach ((var roomName, var roomObject)  in seedObject.RoomObjects)
        {
            sb.Append($"\nds_map_add(liquidInfoMappings, \"{roomName}\", \"{roomObject.LiquidInfo.FormatForDSMap()}\");");
        }
        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode(sb.ToString());
        gmData.Scripts.AddScript("ApplyWaterLevel",
            """
            var liquidInfo = ds_map_find_value(oControl.liquidInfoMappings, room_get_name(room))
            if (is_undefined(liquidInfo))
            {
                exit;
            }

            var splitBy = "|"
            var splitted;
            var i = 0
            var total = string_count(splitBy, liquidInfo)
            while (i<=total)
            {
                var limit = string_length(liquidInfo)+1
                if (string_count(splitBy, liquidInfo) > 0)
                    limit = string_pos("|", liquidInfo)
                var element = string_copy(liquidInfo, 1, limit-1)
                splitted[i] = real(element)
                liquidInfo = string_copy(liquidInfo, limit+1, string_length(liquidInfo)-limit)
                i++
            }

            // Water level 0 is considered by the game as "doesnt exist".
            if (splitted[1] == 0)
                exit;

            with (oWater)
                instance_destroy()
            with (oWaterFXV2)
                instance_destroy()
            with (oLavaSurface)
                instance_destroy()
            with (oLavaBGFX)
                instance_destroy()

            make_liquid(splitted[0], splitted[1], splitted[2], splitted[3], splitted[4], splitted[5], splitted[6]);
            """);

        gmData.Code.ByName("gml_Object_oControl_Other_4").ReplaceGMLInCode("ApplyLightPreset()", "ApplyLightPreset(); ApplyWaterLevel();");
    }
}
