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


        // Fix spring showing up for a brief moment when killing arachnus
        gmData.Code.ByName("gml_Object_oArachnus_Alarm_11").ReplaceGMLInCode("if (temp_randitem == oItemJumpBall)", "if (false)");

        // Bombs
        UndertaleCode? subscreenMenuStep = gmData.Code.ByName("gml_Object_oSubscreenMenu_Step_0");
        subscreenMenuStep.ReplaceGMLInCode("global.item[0] == 0", "!global.hasBombs");
        UndertaleCode? subscreenMiscDaw = gmData.Code.ByName("gml_Object_oSubScreenMisc_Draw_0");
        subscreenMiscDaw.ReplaceGMLInCode("global.item[0]", "global.hasBombs");

        var rm_a2a06 = gmData.Rooms.ByName("rm_a2a06");
        foreach (string code in new[]
                 {
                     "gml_Script_spawn_rnd_pickup", "gml_Script_spawn_rnd_pickup_at", "gml_Script_spawn_many_powerups",
                     "gml_Script_spawn_many_powerups_tank",
                     rm_a2a06.GameObjects.First(go => go.X == 608 && go.Y == 112 && go.ObjectDefinition.Name.Content == "oBlockBomb").CreationCode.Name.Content,
                     rm_a2a06.GameObjects.First(go => go.X == 624 && go.Y == 48 && go.ObjectDefinition.Name.Content == "oBlockBomb").CreationCode.Name.Content,
                     gmData.Rooms.ByName("rm_a3h03").GameObjects.First(go => go.X == 896 && go.Y == 160 && go.ObjectDefinition.Name.Content == "oBlockBomb").CreationCode.Name.Content,
                     "gml_Room_rm_a3b08_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[0]", "global.hasBombs");
        }

        UndertaleGameObject? elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (var gameObject in new[]
                 {
                     gmData.Rooms.ByName("rm_a0h11").GameObjects.First(go => go.X == 480 && go.Y == 768 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a0h25").GameObjects.First(go => go.X == 120 && go.Y == 816 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a0h25").GameObjects.First(go => go.X == 168 && go.Y == 256 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a0h29").GameObjects.First(go => go.X == 384 && go.Y == 312 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a1h05").GameObjects.First(go => go.X == 1184 && go.Y == 832 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a3h04").GameObjects.First(go => go.X == 528 && go.Y == 1344 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a3h04").GameObjects.First(go => go.X == 1728 && go.Y == 1248 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a3a07").GameObjects.First(go => go.X == 112 && go.Y == 240 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a3b02").GameObjects.First(go => go.X == 192 && go.Y == 896 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a3b08").GameObjects.First(go => go.X == 224 && go.Y == 352 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a0h17").GameObjects.First(go => go.X == 96 && go.Y == 352 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a4b02a").GameObjects.First(go => go.X == 120 && go.Y == 816 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a4b10").GameObjects.First(go => go.X == 144 && go.Y == 624 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a4b10").GameObjects.First(go => go.X == 512 && go.Y == 256 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a4b11").GameObjects.First(go => go.X == 224 && go.Y == 2288 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c13").GameObjects.First(go => go.X == 96 && go.Y == 704 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c14").GameObjects.First(go => go.X == 1056 && go.Y == 288 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c17").GameObjects.First(go => go.X == 192 && go.Y == 288 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c18").GameObjects.First(go => go.X == 128 && go.Y == 192 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c18").GameObjects.First(go => go.X == 480 && go.Y == 192 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c21").GameObjects.First(go => go.X == 160 && go.Y == 384 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                     gmData.Rooms.ByName("rm_a5c21").GameObjects.First(go => go.X == 96 && go.Y == 560 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content),
                 })
        {
            gameObject.CreationCode.ReplaceGMLInCode("global.item[0]", "global.hasBombs");
        }

        // Powergrip
        subscreenMiscDaw.ReplaceGMLInCode("global.item[1]", "global.hasPowergrip");
        subscreenMenuStep.ReplaceGMLInCode("global.item[1] == 0", "!global.hasPowergrip");

        // Spiderball
        subscreenMiscDaw.ReplaceGMLInCode("global.item[2]", "global.hasSpiderball");
        subscreenMenuStep.ReplaceGMLInCode("global.item[2] == 0", "!global.hasSpiderball");
        foreach (UndertaleCode code in gmData.Code.Where(c =>
                     (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && c.Name.Content.Contains('2')) ||
                    c.Name.Content ==  gmData.Rooms.ByName("rm_a0h25").GameObjects.First(go => go.X == 120 && go.Y == 816 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content).CreationCode.Name.Content)
                 )
        {
            code.ReplaceGMLInCode("global.item[2]", "global.hasSpiderball");
        }

        // Jumpball
        subscreenMiscDaw.ReplaceGMLInCode("global.item[3]", "global.hasJumpball");
        subscreenMenuStep.ReplaceGMLInCode("global.item[3] == 0", "!global.hasJumpball");
        rm_a2a06.GameObjects.First(go => go.X == 608 && go.Y == 112 && go.ObjectDefinition.Name.Content == "oBlockBomb").CreationCode.ReplaceGMLInCode("global.item[3] == 0", "!global.hasJumpball");

        // Hijump
        UndertaleCode? subcreenBootsDraw = gmData.Code.ByName("gml_Object_oSubScreenBoots_Draw_0");
        subcreenBootsDraw.ReplaceGMLInCode("global.item[4]", "global.hasHijump");
        subscreenMenuStep.ReplaceGMLInCode("global.item[4] == 0", "!global.hasHijump");
        foreach (UndertaleCode? code in gmData.Code.Where(c =>
                     (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && c.Name.Content.Contains('4')) ||
                     c.Name.Content == "gml_Room_rm_a3b08_Create" ||
                     c == gmData.Rooms.ByName("rm_a5c17").GameObjects.First(go => go.X == 192 && go.Y == 288 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content).CreationCode)
                 )
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
        foreach (UndertaleCode? code in gmData.Code.Where(c =>
                     (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && c.Name.Content.Contains('6')) ||
                     gmData.Rooms.ByName("rm_a5a03").GameObjects.Where(go => go.X is >= 96 and <= 112 && go.Y is >= 240 and <= 288 && go.ObjectDefinition.Name.Content == "oBlockStep").Select(go => go.CreationCode).Contains(c) ||
                     c == gmData.Rooms.ByName("rm_a0h25").GameObjects.First(go => go.X == 120 && go.Y == 816 && go.ObjectDefinition.Name.Content == elderSeptogg.Name.Content).CreationCode)
                 )
        {
            code.ReplaceGMLInCode("global.item[6]", "global.hasSpacejump");
        }

        // Speedbooster
        subcreenBootsDraw.ReplaceGMLInCode("global.item[7]", "global.hasSpeedbooster");
        subscreenMenuStep.ReplaceGMLInCode("global.item[7] == 0", "!global.hasSpeedbooster");
        foreach (UndertaleCode? code in gmData.Code.Where(c =>
                     (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && c.Name.Content.Contains('7')) ||
                     gmData.Rooms.ByName("rm_a5c08").GameObjects.Where(go => go.Y == 32 && go.ObjectDefinition.Name.Content == "oBlockSpeed").Select(go => go.CreationCode).Contains(c))
                 )
        {
            code.ReplaceGMLInCode("global.item[7]", "global.hasSpeedbooster");
        }


        // Screwattack // TODO: revise this, so that we don't set ignoreErrors.
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


        // Gravity // TODO: revise this, so that we don't set ignoreErrors.
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
