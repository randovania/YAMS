using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.qol;

public class ShowFullyUnexploredMap
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Replaces all mentions of sMapBlock and sMapCorner with local variables.
        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( "sMapBlock", "blockSprite");
        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( "sMapCorner", "cornerSprite");

        // Redefine the sprite variables to the Unexplored sprites, if map tile hasn't been revealed to player.
        if (seedObject.Cosmetics.ShowUnexploredMap)
            // TODO: don't redefine, but set instead!
            characterVarsCode.ReplaceGMLInCode( "global.unexploredMap = 0", "global.unexploredMap = 1;");

        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( "if (argument8 > 0)", """
            // Variables for swapping map color and corner sprites
            var blockSprite, cornerSprite;

            // Default sprites
            blockSprite = sMapBlock;
            cornerSprite = sMapCorner;

            // Don't draw if map tile is a fast travel pipe
            if (argument7 != "H" && argument7 != "V" && argument7 != "C")
            {
                // Sprite variables changed to unexplored variants if map not revealed
                if (argument8 == 0 && global.unexploredMap)
                {
                    blockSprite = sMapBlockUnexplored;
                    cornerSprite = sMapCornerUnexplored;
                }
                else if (argument8 == 0 && !global.unexploredMap)
                    exit;
            }
            """);

        // Don't ever draw the debug pipe tiles
        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( """
            if (argument7 == "H")
                draw_sprite(sMapSP, 12, argument0, argument1)
            if (argument7 == "V")
                draw_sprite(sMapSP, 13, argument0, argument1)
            if (argument7 == "C")
                draw_sprite(sMapSP, 14, argument0, argument1)
            """, "");

        // Also show item pickups and metroids
        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( "if (argument7 == \"3\" && argument8 == 1)", "if (argument7 == \"3\" && (argument8 == 1 || argument8 == 0))");
        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( "if (argument7 == \"4\" && argument8 == 1)", "if (argument7 == \"4\" && (argument8 == 1 || argument8 == 0))");

        // Add hint icons to minimap
        gmData.Code.ByName("gml_Script_draw_mapblock").AppendGMLInCode( "if (argument7 == \"W\") draw_sprite(sMapSP, 15, argument0, argument1)");

        // Add "M" condition to Metroid alive icon check
        gmData.Code.ByName("gml_Script_draw_mapblock").ReplaceGMLInCode( "if (argument8 == 10)", """if (argument8 == 10 || (argument7 == "M" && global.unexploredMap))""");

        // Draw metroid alive icon on undiscovered map
        gmData.Code.ByName("gml_Script_init_map").AppendGMLInCode( """
        global.map[14, 34] = "10111M0"
        global.map[15, 29] = "10111M0"
        global.map[18, 50] = "10101M0"
        global.map[19, 24] = "00112M0"
        global.map[21, 15] = "10111M0"
        global.map[25, 27] = "10122M0"
        global.map[25, 36] = "10112M0"
        global.map[25, 42] = "10112M0"
        global.map[25, 47] = "10101M0"
        global.map[27, 31] = "11101M0"
        global.map[30, 17] = "10112M0"
        global.map[30, 24] = "11002M0"
        global.map[30, 26] = "11002M0"
        global.map[35, 19] = "11103M0"
        global.map[35, 23] = "21103M0"
        global.map[35, 31] = "11103M0"
        global.map[37, 33] = "11012M0"
        global.map[41, 28] = "11102M0"
        global.map[42, 21] = "10102M0"
        global.map[42, 37] = "11102M0"
        global.map[43, 24] = "10103M0"
        global.map[49, 38] = "01101M0"
        global.map[54, 16] = "10112M0"
        global.map[54, 27] = "10102M0"
        global.map[58, 25] = "10112M0"
        global.map[58, 28] = "12102M0"
        global.map[58, 31] = "00102M0"
        global.map[59, 38] = "11001M0"
        global.map[60, 16] = "11102M0"
        global.map[60, 25] = "11122M0"
        global.map[63, 15] = "11102M0"
        global.map[64, 33] = "01112M0"
        global.map[64, 47] = "10103M0"
        global.map[65, 24] = "01013M0"
        global.map[67, 25] = "10103M0"
        global.map[67, 45] = "10013M0"
        global.map[68, 46] = "01103M0"
        global.map[69, 9] = "11102M0"
        global.map[71, 48] = "11003M0"
        global.map[73, 24] = "11102M0"
        global.map[73, 31] = "11102M0"
        """);

        // Fix BG3 surprise gamma map tile
        gmData.Code.ByName("gml_Object_oMGamma_Alarm_9").SubstituteGMLCode( """
        myposx = floor((x / 320))
        myposy = floor(((y - 8) / 240))
        mapposx = (myposx + global.mapoffsetx)
        mapposy = (myposy + global.mapoffsety)
        if (myid == 20)
        {
            mapposx = 58
            mapposy = 31
        }
        global.dmap[mapposx, mapposy] = 10
        with (oControl)
            event_user(2)
        """);
    }
}
