using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class CustomDarknessLevels
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        gmData.Code.ByName("gml_Script_ApplyLightPreset").ReplaceGMLInCode("global.darkness", "lightLevel");
        StringBuilder sb = new StringBuilder("lightLevelMappings = ds_map_create();");
        foreach ((var roomName, var roomObject)  in seedObject.RoomObjects)
        {
            sb.Append($"\nds_map_add(lightLevelMappings, \"{roomName}\", \"{roomObject.LightLevel}\");");
        }
        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode(sb.ToString());
        gmData.Code.ByName("gml_Script_ApplyLightPreset").PrependGMLInCode(
            """
            var lightLevel = 0;
            var lightLevelFoundValue = ds_map_find_value(oControl.lightLevelMappings, room_get_name(room));
            var lightLevelValue = 0;
            if (is_undefined(lightLevelFoundValue))
            {
              lightLevelValue = global.darkness
            }
            else if (lightLevelFoundValue == "tower")
            {
              if (global.event[200])
                lightLevelValue = 0
              else
                lightLevelValue = 3
            }
            else
            {
              lightLevelValue = real(lightLevelFoundValue)
            }
            lightLevel = lightLevelValue;
            """);
    }
}
