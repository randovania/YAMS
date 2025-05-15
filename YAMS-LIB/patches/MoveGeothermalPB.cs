using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class MoveGeothermalPB
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Move Geothermal PB to big shaft
        gmData.Rooms.ByName("rm_a4b02a").CreationCodeId.AppendGMLInCode("instance_create(272, 400, scr_itemsopen(oControl.mod_253));");
        gmData.Rooms.ByName("rm_a4b02b").CreationCodeId.ReplaceGMLInCode("instance_create(314, 192, scr_itemsopen(oControl.mod_253))", "");

        // Modify minimap for power plant because of pb movement
        gmData.Code.ByName("gml_Script_map_init_07").ReplaceGMLInCode("""
                                                                      global.map[35, 43] = "0112300"
                                                                      global.map[35, 44] = "1210300"
                                                                      global.map[35, 45] = "1210300"
                                                                      """, """
                                                                           global.map[35, 43] = "0101330"
                                                                           global.map[35, 44] = "0101300"
                                                                           global.map[35, 45] = "0101300"
                                                                           """);
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("&& itemid == 253", "&& false");
        // Removes map tiles of the inaccessible A4 basement/Reactor Core/Power Plant.
        gmData.Code.ByName("gml_Script_init_map").AppendGMLInCode("""
                                                                  // Remove A4 reactor core map tiles
                                                                  // Upper left
                                                                  i = 31
                                                                  repeat (4)
                                                                  {
                                                                      j = 43
                                                                      repeat (3)
                                                                      {
                                                                          global.map[i, j] = "0"
                                                                          j++
                                                                      }
                                                                      i++
                                                                  }

                                                                  // Mid section
                                                                  global.map[36, 44] = "0"
                                                                  global.map[36, 45] = "0"
                                                                  global.map[36, 46] = "0"
                                                                  global.map[37, 46] = "0"

                                                                  i = 34
                                                                  repeat (4)
                                                                  {
                                                                      j = 47
                                                                      repeat (6)
                                                                      {
                                                                          global.map[i, j] = "0"
                                                                          j++
                                                                      }
                                                                      i++
                                                                  }

                                                                  // Below A6
                                                                  i = 31
                                                                  repeat (8)
                                                                  {
                                                                      j = 54
                                                                      repeat (6)
                                                                      {
                                                                          global.map[i, j] = "0"
                                                                          j++
                                                                      }
                                                                      i++
                                                                  }
                                                                  """);
    }
}
