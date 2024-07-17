using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.misc;

public class FlippedGame
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode($"global.flipGameVertically = {(seedObject.Patches.FlipVertically ? 1 : 0)}; global.flipGameHorizontally = {(seedObject.Patches.FlipHorizontally ? 1 : 0)};");

        gmData.Scripts.AddScript("draw_surface_ext_flipped",
            """
            var surf = argument0;
            var xCoord = argument1;
            var yCoord = argument2;
            var xScale = argument3;
            var yScale = argument4;
            var rot = argument5;
            var col = argument6;
            var alpha = argument7;

            if (!(room == rm_loading || room == rm_controller || room == titleroom || room == rm_gameplay || room == rm_options || room == rm_gallery || room == rm_credits || (room == rm_transition && global.transitiontype != 1)))
            {
                // if flipped horizontal
                if (global.flipGameHorizontally)
                {
                    xCoord = (surface_get_width(surf) * xScale) + xCoord;
                    xScale *= -1;
                }
                // if flipped vertical
                if (global.flipGameVertically)
                {
                    yCoord = (surface_get_height(surf) * yScale) + yCoord;
                    yScale *= -1;
                }
            }
            draw_surface_ext(surf, xCoord, yCoord, xScale, yScale, rot, col, alpha);
            """);

        gmData.Code.ByName("gml_Script_draw_game_surface").
            ReplaceGMLInCode("draw_surface_ext(application_surface, argument0, argument1, argument2, argument2, 0, c_white, 1)",
                "draw_surface_ext_flipped(application_surface, argument0, argument1, argument2, argument2, 0, c_white, 1)");

        gmData.Code.ByName("gml_Object_oControl_Draw_77")
            .ReplaceGMLInCode("draw_surface_ext(widescreen_surface, (displayx + xShake), (displayy + yShake), display_scale, display_scale, 0, c_white, 1)",
            "draw_surface_ext_flipped(widescreen_surface, (displayx + xShake), (displayy + yShake), display_scale, display_scale, 0, c_white, 1)");

        // Make logbook text look normal
        gmData.Code.ByName("gml_Object_oLogScreenControl_Draw_0")
            .ReplaceGMLInCode("draw_surface(surf, (view_wview[0] / 2 + 150 + widescreen_space), (view_yview[0] + 52))",
                "draw_surface_ext_flipped(surf, (((view_wview[0] / 2) + 150) + widescreen_space), (view_yview[0] + 52), 1, 1, 0, c_white, 1);");

        // Make popup text look normal
        gmData.Code.ByName("gml_Object_oPopupText_Create_0").AppendGMLInCode("surf = surface_create(320 + 53 * oControl.widescreen, 240);");
        gmData.GameObjects.ByName("oPopupText").EventHandlerFor(EventType.Destroy, gmData).SubstituteGMLCode("surface_free(surf)");
        gmData.Code.ByName("gml_Object_oPopupText_Step_0").AppendGMLInCode("""
        if (!surface_exists(surf))
          surf = surface_create(320 + 53 * oControl.widescreen, 240);
        surface_set_target(surf);
        draw_set_font(global.fontGUI2);
        draw_set_halign(fa_right);
        draw_cool_text((316 + 53 * oControl.widescreen), (226), text, c_black, c_white, c_white, image_alpha);
        draw_set_halign(fa_left);
        surface_reset_target();
        """);
        gmData.Code.ByName("gml_Object_oPopupText_Draw_0").SubstituteGMLCode(
            """
            var flipAdjustment = 0
            if (global.flipGameHorizontally)
                flipAdjustment = -1
            draw_surface_ext_flipped(surf, view_xview[0] + 53 * oControl.widescreen * flipAdjustment, view_yview[0], 1, 1, 0, c_white, 1);
            """
            );

        // Make item cutscene look normal
        gmData.Code.ByName("gml_Object_oItemCutscene_Create_0").PrependGMLInCode("item_surf = surface_create(320 + 53 * oControl.widescreen, 240);");
        gmData.GameObjects.ByName("oItemCutscene").EventHandlerFor(EventType.Destroy, gmData).SubstituteGMLCode("surface_free(item_surf)");
        gmData.Code.ByName("gml_Object_oItemCutscene_Draw_0").ReplaceGMLInCode("draw_set_alpha(0.8)",
            """
            if (!surface_exists(item_surf))
                item_surf = surface_create(320 + 53 * oControl.widescreen, 240);
            surface_set_target(item_surf)
            draw_set_alpha(0.8)
            """
            );
        gmData.Code.ByName("gml_Object_oItemCutscene_Draw_0").ReplaceGMLInCode("draw_set_alpha(1)", """
            draw_set_alpha(1);
            surface_reset_target();
            var flipAdjustment = 0
            if (global.flipGameHorizontally)
                flipAdjustment = -1
            draw_surface_ext_flipped(item_surf, view_xview[0] + 53 * oControl.widescreen * flipAdjustment, view_yview[0], 1, 1, 0, c_white, 1)
            """);

        // Flip minimap but keep in place
        gmData.Code.ByName("gml_Object_oControl_Create_0").ReplaceGMLInCode("gui_surface = 0", "gui_surface = 0; map_surface = 0;");
        gmData.Code.ByName("gml_Object_oControl_Step_0").ReplaceGMLInCode(
            """
            if surface_exists(gui_surface)
            {
                surface_set_target(gui_surface)
            """,
            """
            if surface_exists(map_surface)
            {
                if (surface_get_width(map_surface) != (320 + widescreen_space))
                    surface_free(map_surface)
            }
            if (!surface_exists(map_surface))
                map_surface = surface_create((320 + widescreen_space), 240)
            if surface_exists(gui_surface)
            {
                surface_set_target(gui_surface)
            """
            );
        gmData.Code.ByName("gml_Script_draw_gui").ReplaceGMLInCode("draw_gui_map((276 + widescreen_space), 0)", "{ surface_set_target(map_surface); draw_gui_map((276 + widescreen_space), 0); surface_reset_target(); }");
        gmData.Code.ByName("gml_Object_oControl_Draw_64").ReplaceGMLInCode("d = application_get_position()", """
            d = application_get_position();
            if surface_exists(map_surface)
            {
                surface_set_target(gui_surface)
                var xPos = 0;
                var yPos = 0;
                var xScale = 1;
                var yScale = 1;
                if (global.flipGameHorizontally)
                {
                    xPos = ((320 + widescreen_space) * 2 - 48);
                    xScale = -1;
                }
                if (global.flipGameVertically)
                {
                    yPos = 32;
                    yScale = -1;
                }
                draw_surface_ext(map_surface, xPos, yPos, xScale, yScale, 0, c_white, 1)
                surface_reset_target()
            }
            """);

        // Invert controls if in inverted rooms.
        gmData.Code.ByName("gml_Object_oControl_Step_0").ReplaceGMLInCode("global_control()",
            """
            {
                if (!(room == rm_loading || room == rm_controller || room == titleroom || room == rm_gameplay || room == rm_options || room == rm_gallery || room == rm_credits))
                {
                    // if flipped horizontal
                    var tempCtrl = 0;
                    if (global.flipGameHorizontally)
                    {
                        tempCtrl = ctrl_Left;
                        ctrl_Left = ctrl_Right;
                        ctrl_Right = tempCtrl;
                    }
                    // if flipped vertical
                    if (global.flipGameVertically)
                    {
                        tempCtrl = ctrl_Up;
                        ctrl_Up = ctrl_Down;
                        ctrl_Down = tempCtrl;
                    }
                }
                global_control();
            }
            """);
    }
}
