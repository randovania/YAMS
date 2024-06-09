using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB.patches;

public class AddDNAItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? ssDraw = gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0");
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Implement DNA item
        UndertaleGameObject? enemyObject = gmData.GameObjects.ByName("oItem");
        for (int i = 350; i <= 395; i++)
        {
            UndertaleGameObject go = new UndertaleGameObject();
            go.Name = gmData.Strings.MakeString($"oItemDNA_{i}");
            go.ParentId = enemyObject;
            // Add create event
            UndertaleCode create = go.EventHandlerFor(EventType.Create, gmData);
            create.SubstituteGMLCode("event_inherited(); itemid = " + i + ";");

            // Add collision event with Samus
            go.EventHandlerFor(EventType.Collision, 267u, gmData.Strings, gmData.Code, gmData.CodeLocals);
            gmData.GameObjects.Add(go);
        }

        // Adjust global item array to be 400
        characterVarsCode.ReplaceGMLInCode("""
                                           i = 350
                                           repeat (350)
                                           {
                                               i -= 1
                                               global.item[i] = 0
                                           }
                                           """, """
                                                i = 400
                                                repeat (400)
                                                {
                                                    i -= 1
                                                    global.item[i] = 0
                                                }
                                                """);
        gmData.Code.ByName("gml_Script_sv6_add_items").ReplaceGMLInCode("350", "400");
        gmData.Code.ByName("gml_Script_sv6_get_items").ReplaceGMLInCode("350", "400");

        // Metroid ID to DNA map
        gmData.Scripts.AddScript("gml_Script_scr_DNASpawn", """
            if (argument0 == 0)
                return oItemDNA_350;
            if (argument0 == 1)
                return oItemDNA_351;
            if (argument0 == 2)
                return oItemDNA_352;
            if (argument0 == 3)
                return oItemDNA_353;
            if (argument0 == 4)
                return oItemDNA_354;
            if (argument0 == 5)
                return oItemDNA_355;
            if (argument0 == 6)
                return oItemDNA_358;
            if (argument0 == 7)
                return oItemDNA_357;
            if (argument0 == 8)
                return oItemDNA_356;
            if (argument0 == 9)
                return oItemDNA_359;
            if (argument0 == 10)
                return oItemDNA_361;
            if (argument0 == 11)
                return oItemDNA_360;
            if (argument0 == 12)
                return oItemDNA_373;
            if (argument0 == 13)
                return oItemDNA_375;
            if (argument0 == 14)
                return oItemDNA_362;
            if (argument0 == 15)
                return oItemDNA_376;
            if (argument0 == 16)
                return oItemDNA_377;
            if (argument0 == 17)
                return oItemDNA_378;
            if (argument0 == 18)
                return oItemDNA_363;
            if (argument0 == 19)
                return oItemDNA_379;
            if (argument0 == 20)
                return oItemDNA_380;
            if (argument0 == 21)
                return oItemDNA_381;
            if (argument0 == 22)
                return oItemDNA_374;
            if (argument0 == 23)
                return oItemDNA_364;
            if (argument0 == 24)
                return oItemDNA_365;
            if (argument0 == 25)
                return oItemDNA_366;
            if (argument0 == 26)
                return oItemDNA_382;
            if (argument0 == 27)
                return oItemDNA_389;
            if (argument0 == 28)
                return oItemDNA_383;
            if (argument0 == 29)
                return oItemDNA_384;
            if (argument0 == 30)
                return oItemDNA_390;
            if (argument0 == 31)
                return oItemDNA_385;
            if (argument0 == 32)
                return oItemDNA_388;
            if (argument0 == 33)
                return oItemDNA_391;
            if (argument0 == 34)
                return oItemDNA_370;
            if (argument0 == 35)
                return oItemDNA_368;
            if (argument0 == 36)
                return oItemDNA_367;
            if (argument0 == 37)
                return oItemDNA_371;
            if (argument0 == 38)
                return oItemDNA_369;
            if (argument0 == 39)
                return oItemDNA_386;
            if (argument0 == 40)
                return oItemDNA_387;
            if (argument0 == 41)
                return oItemDNA_372;
            if (argument0 == 42)
                return oItemDNA_392;
            if (argument0 == 43)
                return oItemDNA_394;
            if (argument0 == 44)
                return oItemDNA_393;
            if (argument0 == 45)
                return oItemDNA_395;
            """);

        // Make DNA count show on map
        ssDraw.ReplaceGMLInCode("draw_text((view_xview[0] + 18), (view_yview[0] + 198 + rectoffset), timetext)",
            "draw_text((view_xview[0] + 18), (view_yview[0] + 198 + rectoffset), timetext); draw_text((view_xview[0] + 158), (view_yview[0] + 198 + rectoffset), string(global.dna) + \"/46\")");
        ssDraw.ReplaceGMLInCode("draw_text((view_xview[0] + 17), (view_yview[0] + 197 + rectoffset), timetext)",
            "draw_text((view_xview[0] + 17), (view_yview[0] + 197 + rectoffset), timetext); draw_text((view_xview[0] + 157), (view_yview[0] + 197 + rectoffset), string(global.dna) + \"/46\")");

        // Fix item percentage now that more items have been added
        foreach (string name in new[]
                 {
                     "gml_Object_oGameSelMenu_Other_12", "gml_Object_oSS_Fg_Draw_0", "gml_Object_oScoreScreen_Create_0", "gml_Object_oScoreScreen_Other_10",
                     "gml_Object_oIGT_Step_0"
                 })
        {
            gmData.Code.ByName(name).ReplaceGMLInCode("/ 88", "/ 134");
        }

        // Replace Metroids counters with DNA counters
        UndertaleCode? drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
        drawGuiCode.ReplaceGMLInCode("global.monstersleft", "global.dna");
        drawGuiCode.ReplaceGMLInCode("global.monstersarea", $"(max((46 - global.dna), 0))");
        gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_14").ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter\")", "\"DNA Counter\"");
        gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_10").ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter\")", "\"DNA Counter\"");
        gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_13").ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Tip\")",
            "\"Switches the type of the HUD DNA Counter\"");
        UndertaleCode? optionsDisplayUser2 = gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_12");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Local\")", "\"Until Labs\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Global\")", "\"Current\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Disabled_Tip\")", "\"Don't show the DNA Counter\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Local_Tip\")",
            "\"Show the remaining DNA until you can access the Genetics Laboratory\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Global_Tip\")", "\"Show the currently collected DNA\"");
        gmData.Code.ByName("gml_Object_oGameSelMenu_Other_12").ReplaceGMLInCode("global.monstersleft", "global.dna");

        // Make metroids drop an item onto you on death and increase music timer to not cause issues
        gmData.Code.ByName("gml_Object_oMAlpha_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[1] >= 0) alarm[1] = 120; }");
        gmData.Code.ByName("gml_Object_oMGamma_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[2] >= 0) alarm[2] = 120; }");
        gmData.Code.ByName("gml_Object_oMZeta_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[3] >= 0) alarm[3] = 120; }");
        gmData.Code.ByName("gml_Object_oMOmega_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[4] >= 0) alarm[4] = 120; }");

        // Make items spawned from metroids not change map
        gmData.Code.ByName("gml_Object_oItem_Create_0").AppendGMLInCode("changeOnMap = true");
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("if (distance_to_object(oItem) > 180)",
            "if ((distance_to_object(oItem) > 180) && changeOnMap)");

        // Make new global.lavastate 11 that requires 46 dna to be collected
        gmData.Code.ByName("gml_Script_check_areaclear")
            .SubstituteGMLCode(
                "var spawnQuake = is_undefined(argument0); if (global.lavastate == 11) { if (global.dna >= 46) { if (spawnQuake) instance_create(0, 0, oBigQuake); global.lavastate = 12; } }");

        // Check lavastate at labs
        UndertaleRoom? labsRoom = gmData.Rooms.ByName("rm_a7b04A");
        gmData.Code.AddCodeEntry("gml_RoomCC_rm_a7b04A_labBlock_Create", "if (global.lavastate > 11) {  tile_layer_delete(-99); instance_destroy(); }");
        labsRoom.GameObjects.Add(CreateRoomObject(64, 96, gmData.GameObjects.ByName("oSolid1"), gmData.Code.ByName("gml_RoomCC_rm_a7b04A_labBlock_Create"), 2, 4));
        var tla7Outside = gmData.Backgrounds.ByName("tlArea7Outside");
        labsRoom.Tiles.Add(CreateRoomTile(64, 96, -99, tla7Outside, 0, 208, 32, 32));
        labsRoom.Tiles.Add(CreateRoomTile(64, 128, -99, tla7Outside, 0, 208, 32, 32));

        // Add DNA check to Baby trigger
        gmData.Code.ByName("gml_Object_oHatchlingTrigger_Collision_267").PrependGMLInCode(
            """
            if (global.dna < 46)
            {
                popup_text("Collect all the DNA to hatch the Baby")
                instance_destroy()
                exit
            }
            """);
    }
}
