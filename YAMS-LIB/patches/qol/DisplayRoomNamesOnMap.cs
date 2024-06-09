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
        gmData.Code.ByName("gml_Object_oSS_Fg_Create_0").AppendGMLInCode( DSMapCoordRoomname);
        gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0").ReplaceGMLInCode( """
            draw_text((view_xview[0] + 161), (view_yview[0] + 30 - rectoffset), maptext)
            draw_set_color(c_white)
            draw_text((view_xview[0] + 160), (view_yview[0] + 29 - rectoffset), maptext)
        ""","""
            draw_set_font(global.fontMenuSmall2)
            var titleText = maptext;
            if (instance_exists(oMapCursor) && oMapCursor.active && oMapCursor.state == 1)
            {
                titleText = ds_map_find_value(room_names_coords, string(global.mapmarkerx) + "|" + string(global.mapmarkery));
                if (titleText == undefined)
                    titleText = maptext;
            }
            draw_text((view_xview[0] + 161), ((view_yview[0] + 29) - rectoffset), titleText);
            draw_set_color(c_white);
            draw_text((view_xview[0] + 160), ((view_yview[0] + 28) - rectoffset), titleText);
            draw_set_font(global.fontGUI2)
        """);
        var ssFGDestroyCode = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oSS_Fg_Destroy_0") };
        ssFGDestroyCode.SubstituteGMLCode( "ds_map_destroy(room_names_coords)");
        gmData.Code.Add(ssFGDestroyCode);
        var ssFGCollisionList = gmData.GameObjects.ByName("oSS_Fg").Events[1];
        var ssFGAction = new UndertaleGameObject.EventAction();
        ssFGAction.CodeId = ssFGDestroyCode;
        var ssFGEvent = new UndertaleGameObject.Event();
        ssFGEvent.EventSubtype = 0;
        ssFGEvent.Actions.Add(ssFGAction);
        ssFGCollisionList.Add(ssFGEvent);
    }
}
