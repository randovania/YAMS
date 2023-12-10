using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.qol;

public class DisplayRoomNameOnHUD
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        StringBuilder roomNameDSMapBuilder = new StringBuilder();
        foreach ((var roomName, var roomData) in seedObject.RoomObjects)
            roomNameDSMapBuilder.Append($"ds_map_add(roomNames, \"{roomName}\", \"{roomData.DisplayName}\");\n");
        string roomNameDSMapString = roomNameDSMapBuilder.ToString();

        var roomNameHudCC = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Create_0") };
        roomNameHudCC.SubstituteGMLCode( $"""
        rnh_surface = surface_create((320 + oControl.widescreen_space), 240)
        fadeout = 0
        roomtime = 0
        textToDisplay = ""
        image_alpha = 0
        offsetY = 0
        displayMode = {(int)seedObject.Cosmetics.RoomNameHud}
        textBGColor = 0
        textColor0 = c_white
        textColor1 = c_silver
        roomNames = ds_map_create()
        {roomNameDSMapString}
        """);
        var roomNameHudDestroy = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Destroy_0") };
        roomNameHudDestroy.SubstituteGMLCode( "surface_free(rnh_surface); ds_map_destroy(roomNames)");
        var roomNameHudDrawGui = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Draw_64") };
        roomNameHudDrawGui.SubstituteGMLCode( """
        var d;
        d = application_get_position()
        if (room != rm_transition && instance_exists(oCharacter) && global.ingame && displayMode != 0 && global.opshowhud)
            draw_surface_ext(rnh_surface, (oControl.displayx - d[0]), (oControl.displayy - d[1]), oControl.display_scale, oControl.display_scale, 0, -1, 1)
        """);
        var roomNameHudRoomStart = new UndertaleCode() {Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Other_4")};
        roomNameHudRoomStart.SubstituteGMLCode( """
        var newRoomName = ds_map_find_value(roomNames, room_get_name(room))
        if (global.ingame && textToDisplay != newRoomName)
        {
            fadeout = 0
            roomtime = 0
            image_alpha = 0
            textToDisplay = newRoomName
        }
        """);
        var roomNameHudStep = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Step_0") };
        roomNameHudStep.SubstituteGMLCode( """
        if (displayMode == 0)
            exit;
        offsetY = 0
        if instance_exists(oIGT)
            offsetY = 11
        if global.ingame
        {
            if (!fadeout)
            {
                if (image_alpha < 1)
                    image_alpha += 0.2
                else
                    fadeout = 1
            }
            else if (displayMode == 1 && roomtime > 200 && image_alpha > 0)
                image_alpha -= 0.02
            roomtime++
        }
        if (surface_exists(rnh_surface) && surface_get_width(rnh_surface) < (320 + oControl.widescreen_space))
            surface_free(rnh_surface)
        if (!surface_exists(rnh_surface))
            rnh_surface = surface_create((320 + oControl.widescreen_space), 240)
        if surface_exists(rnh_surface)
        {
            surface_set_target(rnh_surface)
            draw_clear_alpha(c_black, 0)
            draw_set_font(global.fontGUI2)
            draw_set_halign(fa_left)
            draw_set_alpha(1)
            draw_set_color(c_white)
            draw_cool_text(4, (16 + offsetY), textToDisplay, textBGColor, textColor0, textColor1, image_alpha)
            surface_reset_target()
        }
        """);
        gmData.Code.Add(roomNameHudCC);
        gmData.Code.Add(roomNameHudDestroy);
        gmData.Code.Add(roomNameHudDrawGui);
        gmData.Code.Add(roomNameHudRoomStart);
        gmData.Code.Add(roomNameHudStep);
        // Create Object and add events

        var roomHudObject = new UndertaleGameObject() { Name = gmData.Strings.MakeString("oRoomNameHUD"), Depth = -9999999, Persistent = true};
        gmData.GameObjects.Add(roomHudObject);
        gmData.Code.ByName("gml_Script_load_character_vars").AppendGMLInCode( "if (!instance_exists(oRoomNameHUD))\ninstance_create(0, 0, oRoomNameHUD)");
        // Create
        var roomHudCollisionList = roomHudObject.Events[0];
        var roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudCC;
        var roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 0;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Destroy
        roomHudCollisionList = roomHudObject.Events[1];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudDestroy;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 0;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Step
        roomHudCollisionList = roomHudObject.Events[3];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudStep;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 0;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Room start
        roomHudCollisionList = roomHudObject.Events[7];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudRoomStart;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 4;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Draw GUI
        roomHudCollisionList = roomHudObject.Events[8];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudDrawGui;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 64;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
    }
}
