using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class DecoupleItemsFromLocations
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        characterVarsCode.PrependGMLInCode("global.hasBombs = 0; global.hasPowergrip = 0; global.hasSpiderball = 0; global.hasJumpball = 0; global.hasHijump = 0;" +
                                           "global.hasVaria = 0; global.hasSpacejump = 0; global.hasSpeedbooster = 0; global.hasScrewattack = 0; global.hasGravity = 0;" +
                                           "global.hasCbeam = 0; global.hasIbeam = 0; global.hasWbeam = 0; global.hasSbeam  = 0; global.hasPbeam = 0; global.hasMorph = 0;");

        // Make all item activation dependant on whether the main item is enabled.
        characterVarsCode.ReplaceGMLInCode("""
                                           global.morphball = 1
                                           global.jumpball = 0
                                           global.powergrip = 1
                                           global.spacejump = 0
                                           global.screwattack = 0
                                           global.hijump = 0
                                           global.spiderball = 0
                                           global.speedbooster = 0
                                           global.bomb = 0
                                           global.ibeam = 0
                                           global.wbeam = 0
                                           global.pbeam = 0
                                           global.sbeam = 0
                                           global.cbeam = 0
                                           """, """
                                                global.morphball = global.hasMorph;
                                                global.jumpball = global.hasJumpball;
                                                global.powergrip = global.hasPowergrip;
                                                global.spacejump = global.hasSpacejump;
                                                global.screwattack = global.hasScrewattack;
                                                global.hijump = global.hasHijump;
                                                global.spiderball = global.hasSpiderball;
                                                global.speedbooster = global.hasSpeedbooster;
                                                global.bomb = global.hasBombs;
                                                global.ibeam = global.hasIbeam;
                                                global.wbeam = global.hasWbeam;
                                                global.pbeam = global.hasPbeam;
                                                global.sbeam = global.hasSbeam;
                                                global.cbeam = global.hasCbeam;
                                                """);
        characterVarsCode.ReplaceGMLInCode("global.currentsuit = 0",
            "global.currentsuit = 0; if (global.hasGravity) global.currentsuit = 2; else if (global.hasVaria) global.currentsuit = 1;");

        // Fix spring showing up for a brief moment when killing arachnus
        gmData.Code.ByName("gml_Object_oArachnus_Alarm_11").ReplaceGMLInCode("if (temp_randitem == oItemJumpBall)", "if (false)");

        // Bombs
        UndertaleCode? subscreenMenuStep = gmData.Code.ByName("gml_Object_oSubscreenMenu_Step_0");
        subscreenMenuStep.ReplaceGMLInCode("global.item[0] == 0", "!global.hasBombs");
        UndertaleCode? subscreenMiscDaw = gmData.Code.ByName("gml_Object_oSubScreenMisc_Draw_0");
        subscreenMiscDaw.ReplaceGMLInCode("global.item[0]", "global.hasBombs");

        foreach (string code in new[]
                 {
                     "gml_Script_spawn_rnd_pickup", "gml_Script_spawn_rnd_pickup_at", "gml_Script_spawn_many_powerups",
                     "gml_Script_spawn_many_powerups_tank", "gml_RoomCC_rm_a2a06_4759_Create", "gml_RoomCC_rm_a2a06_4761_Create",
                     "gml_RoomCC_rm_a3h03_5279_Create", "gml_Room_rm_a3b08_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[0]", "global.hasBombs");
        }

        UndertaleGameObject? elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (UndertaleRoom.GameObject go in room.GameObjects.Where(go => go.ObjectDefinition == elderSeptogg && go.CreationCode is not null))
            {
                go.CreationCode.ReplaceGMLInCode("global.item[0]", "global.hasBombs", true);
            }
        }


        // Powergrip
        subscreenMiscDaw.ReplaceGMLInCode("global.item[1]", "global.hasPowergrip");
        subscreenMenuStep.ReplaceGMLInCode("global.item[1] == 0", "!global.hasPowergrip");

        // Spiderball
        subscreenMiscDaw.ReplaceGMLInCode("global.item[2]", "global.hasSpiderball");
        subscreenMenuStep.ReplaceGMLInCode("global.item[2] == 0", "!global.hasSpiderball");
        foreach (UndertaleCode code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                               c.Name.Content.Contains('2')) || c.Name.Content == "gml_RoomCC_rm_a0h25_4105_Create"))
        {
            code.ReplaceGMLInCode("global.item[2]", "global.hasSpiderball");
        }

        // Jumpball
        subscreenMiscDaw.ReplaceGMLInCode("global.item[3]", "global.hasJumpball");
        subscreenMenuStep.ReplaceGMLInCode("global.item[3] == 0", "!global.hasJumpball");
        gmData.Code.ByName("gml_RoomCC_rm_a2a06_4761_Create").ReplaceGMLInCode("global.item[3] == 0", "!global.hasJumpball");

        // Hijump
        UndertaleCode? subcreenBootsDraw = gmData.Code.ByName("gml_Object_oSubScreenBoots_Draw_0");
        subcreenBootsDraw.ReplaceGMLInCode("global.item[4]", "global.hasHijump");
        subscreenMenuStep.ReplaceGMLInCode("global.item[4] == 0", "!global.hasHijump");
        foreach (UndertaleCode? code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                                c.Name.Content.Contains('4')) || c.Name.Content == "gml_Room_rm_a3b08_Create" ||
                                                               c.Name.Content == "gml_RoomCC_rm_a5c17_7779_Create"))
        {
            code.ReplaceGMLInCode("global.item[4]", "global.hasHijump");
        }

        // Varia
        UndertaleCode? subscreenSuitDraw = gmData.Code.ByName("gml_Object_oSubScreenSuit_Draw_0");
        subscreenSuitDraw.ReplaceGMLInCode("global.item[5]", "global.hasVaria");
        subscreenMenuStep.ReplaceGMLInCode("global.item[5] == 0", "!global.hasVaria");
        foreach (string code in new[]
                 {
                     "gml_Script_characterStepEvent", "gml_Script_damage_player", "gml_Script_damage_player_push", "gml_Script_damage_player_knockdown",
                     "gml_Object_oQueenHead_Step_0"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[5]", "global.hasVaria");
        }

        // Spacejump
        subcreenBootsDraw.ReplaceGMLInCode("global.item[6]", "global.hasSpacejump");
        subscreenMenuStep.ReplaceGMLInCode("global.item[6] == 0", "!global.hasSpacejump");
        foreach (UndertaleCode? code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                                c.Name.Content.Contains('6')) || c.Name.Content.StartsWith("gml_RoomCC_rm_a5a03_") ||
                                                               c.Name.Content == "gml_RoomCC_rm_a0h25_4105_Create"))
        {
            code.ReplaceGMLInCode("global.item[6]", "global.hasSpacejump", true);
        }

        // Speedbooster
        subcreenBootsDraw.ReplaceGMLInCode("global.item[7]", "global.hasSpeedbooster");
        subscreenMenuStep.ReplaceGMLInCode("global.item[7] == 0", "!global.hasSpeedbooster");
        foreach (UndertaleCode? code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                                c.Name.Content.Contains('7')) || c.Name.Content.StartsWith("gml_RoomCC_rm_a5c08_")))
        {
            code.ReplaceGMLInCode("global.item[7]", "global.hasSpeedbooster", true);
        }


        // Screwattack
        subscreenMiscDaw.ReplaceGMLInCode("global.item[8]", "global.hasScrewattack");
        subscreenMenuStep.ReplaceGMLInCode("global.item[8] == 0", "!global.hasScrewattack");
        foreach (string code in new[]
                 {
                     "gml_Script_scr_septoggs_2468", "gml_Script_scr_septoggs_48", "gml_RoomCC_rm_a1a06_4447_Create",
                     "gml_RoomCC_rm_a1a06_4448_Create", "gml_RoomCC_rm_a1a06_4449_Create", "gml_RoomCC_rm_a3a04_5499_Create", "gml_RoomCC_rm_a3a04_5500_Create",
                     "gml_RoomCC_rm_a3a04_5501_Create", "gml_RoomCC_rm_a4a01_6476_Create", "gml_RoomCC_rm_a4a01_6477_Create", "gml_RoomCC_rm_a4a01_6478_Create",
                     "gml_RoomCC_rm_a5c13_7639_Create", "gml_RoomCC_rm_a5c13_7640_Create", "gml_RoomCC_rm_a5c13_7641_Create", "gml_RoomCC_rm_a5c13_7642_Create",
                     "gml_RoomCC_rm_a5c13_7643_Create", "gml_RoomCC_rm_a5c13_7644_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[8]", "global.hasScrewattack");
        }


        // Gravity
        subscreenSuitDraw.ReplaceGMLInCode("global.item[9]", "global.hasGravity");
        subscreenMenuStep.ReplaceGMLInCode("global.item[9] == 0", "!global.hasGravity");

        foreach (string code in new[]
                 {
                     "gml_Script_scr_variasuitswap", "gml_Object_oGravitySuitChangeFX_Step_0", "gml_Object_oGravitySuitChangeFX_Other_10",
                     "gml_RoomCC_rm_a2a06_4759_Create", "gml_RoomCC_rm_a2a06_4761_Create", "gml_RoomCC_rm_a5a03_8631_Create", "gml_RoomCC_rm_a5a03_8632_Create",
                     "gml_RoomCC_rm_a5a03_8653_Create", "gml_RoomCC_rm_a5a03_8654_Create", "gml_RoomCC_rm_a5a03_8655_Create", "gml_RoomCC_rm_a5a03_8656_Create",
                     "gml_RoomCC_rm_a5a03_8657_Create", "gml_RoomCC_rm_a5a03_8674_Create", "gml_RoomCC_rm_a5a05_8701_Create", "gml_RoomCC_rm_a5a06_8704_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[9]", "global.hasGravity");
        }

        // Charge
        UndertaleCode? itemsSwapScript = gmData.Code.ByName("gml_Script_scr_itemsmenu_swap");
        itemsSwapScript.ReplaceGMLInCode("global.item[10]", "global.hasCbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[10] == 0", "!global.hasCbeam");

        // Ice
        itemsSwapScript.ReplaceGMLInCode("global.item[11]", "global.hasIbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[11] == 0", "!global.hasIbeam");
        foreach (string code in new[] { "gml_Object_oEris_Create_0", "gml_Object_oErisBody1_Create_0", "gml_Object_oErisHead_Create_0", "gml_Object_oErisSegment_Create_0" })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[11] == 0", "!global.hasIbeam");
        }

        // Wave
        itemsSwapScript.ReplaceGMLInCode("global.item[12]", "global.hasWbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[12] == 0", "!global.hasWbeam");

        // Spazer
        itemsSwapScript.ReplaceGMLInCode("global.item[13]", "global.hasSbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[13] == 0", "!global.hasSbeam");

        // Plasma
        itemsSwapScript.ReplaceGMLInCode("global.item[14]", "global.hasPbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[14] == 0", "!global.hasPbeam");

        // Morph Ball
        subscreenMiscDaw.ReplaceGMLInCode("""
                                          draw_sprite(sSubScrButton, global.morphball, (x - 28), (y + 16))
                                          draw_text((x - 20), (y + 15 + oControl.subScrItemOffset), morph)
                                          """, """
                                               if (global.hasMorph) {
                                                   draw_sprite(sSubScrButton, global.morphball, (x - 28), (y + 16))
                                                   draw_text((x - 20), ((y + 15) + oControl.subScrItemOffset), morph)
                                               }
                                               """);
        subscreenMenuStep.ReplaceGMLInCode("""
                                           if (global.curropt == 7 && (!global.hasIbeam))
                                                   global.curropt += 1
                                           """, """
                                                if (global.curropt == 7 && (!global.hasIbeam))
                                                        global.curropt += 1
                                                if (global.curropt == 8 && (!global.hasMorph))
                                                        global.curropt += 1
                                                """);
        subscreenMenuStep.ReplaceGMLInCode("""
                                           if (global.curropt == 7 && (!global.hasIbeam))
                                                   global.curropt -= 1
                                           """, """
                                                if (global.curropt == 8 && (!global.hasMorph))
                                                        global.curropt -= 1
                                                if (global.curropt == 7 && (!global.hasIbeam))
                                                        global.curropt -= 1
                                                """);
        subscreenMenuStep.ReplaceGMLInCode("""
                                               else
                                                   global.curropt = 14
                                           """, """
                                                    else
                                                        global.curropt = 14
                                                    if (global.curropt == 8 && (!global.hasMorph))
                                                        global.curropt += 1
                                                    if (global.curropt == 9 && (!global.hasSpiderball))
                                                        global.curropt += 1
                                                    if (global.curropt == 10 && (!global.hasJumpball))
                                                        global.curropt += 1
                                                    if (global.curropt == 11 && (!global.hasBombs))
                                                        global.curropt += 1
                                                    if (global.curropt == 12 && (!global.hasPowergrip))
                                                        global.curropt += 1
                                                    if (global.curropt == 13 && (!global.hasScrewattack))
                                                        global.curropt += 1
                                                """);

        subscreenMenuStep.ReplaceGMLInCode("""
                                               if (global.curropt > 16)
                                                   global.curropt = 8
                                           """, """
                                                    if (global.curropt > 16)
                                                        global.curropt = 8
                                                    if (global.curropt == 8 && (!global.hasMorph))
                                                            global.curropt = 0
                                                """);
    }
}
