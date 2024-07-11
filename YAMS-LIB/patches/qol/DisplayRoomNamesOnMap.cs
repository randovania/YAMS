using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.qol;

public class DisplayRoomNamesOnMap
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        StringBuilder dsMapCordBuilder = new StringBuilder("room_names_coords = ds_map_create()");
        foreach ((string key, RoomObject value) in seedObject.RoomObjects)
        {
            foreach (Coordinate coord in value.MinimapData)
            {
                dsMapCordBuilder.Append($"ds_map_add(room_names_coords, \"{coord.X}|{coord.Y}\", \"{value.RegionName} - {value.DisplayName}\");\n");
            }
        }
        string DSMapCoordRoomname = dsMapCordBuilder.ToString();


        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode(DSMapCoordRoomname);

        gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0").ReplaceGMLInCode("""
            draw_text((view_xview[0] + 161), (view_yview[0] + 30 - rectoffset), maptext)
            draw_set_color(c_white)
            draw_text((view_xview[0] + 160), (view_yview[0] + 29 - rectoffset), maptext)
        ""","""
            draw_set_font(global.fontMenuSmall2)
            var titleText = maptext;
            if (instance_exists(oMapCursor) && oMapCursor.active && oMapCursor.state == 1)
            {
                titleText = ds_map_find_value(oControl.room_names_coords, string(global.mapmarkerx) + "|" + string(global.mapmarkery));
                if (titleText == undefined)
                    titleText = maptext;
            }
            draw_text((view_xview[0] + 161), ((view_yview[0] + 29) - rectoffset), titleText);
            draw_set_color(c_white);
            draw_text((view_xview[0] + 160), ((view_yview[0] + 28) - rectoffset), titleText);
            draw_set_font(global.fontGUI2)
        """);

        var ssFGDestroyCode = gmData.GameObjects.ByName("oSS_Fg").EventHandlerFor(EventType.Destroy, gmData);
        ssFGDestroyCode.SubstituteGMLCode("ds_map_destroy(oControl.room_names_coords)");

        // Adjust pause screen text to mention room names
        gmData.Code.ByName("gml_Object_oSS_Fg_Create_0").ReplaceGMLInCode( "tip2text = get_text(\"Subscreen\", \"Marker_Tip\")", "tip2text = \"| - Marker & Room Names\"");
    }
}
