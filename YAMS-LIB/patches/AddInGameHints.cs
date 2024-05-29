using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB.patches;

public class AddInGameHints
{
    private static void PatchIceBeamHint(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Make log in lab always appear
        gmData.Code.ByName("gml_Room_rm_a7b04A_Create").ReplaceGMLInCode( "oControl.chozomessage >= 10", "true");
        // Fix hint dissappearing when visiting room right after baby scan
        gmData.Code.ByName("gml_Object_oChozoLogMarker_Step_0").ReplaceGMLInCode( "instance_exists(oNotification)", "instance_exists(oNotification) && oNotification.log == 1");
        // Change text to be hint
        gmData.Code.ByName("gml_Script_load_logs_list").AppendGMLInCode( $"lbl[44] = \"Ice Beam Hint\"; txt[44, 0] = \"{seedObject.Hints[HintLocationEnum.ChozoLabs]}\"; pic[44, 0] = bgLogImg44B");
        // Remove second scanning
        gmData.Code.ByName("gml_Room_rm_a0h01_Create").ReplaceGMLInCode( "scan_log(44, get_text(\"Misc\", \"Translation\"), 180, 1)", "if (false) {}");

        // Change minimap to include hint
        gmData.Code.ByName("gml_Script_map_init_01").ReplaceGMLInCode( "global.map[8, 22] = \"1210300\"", "global.map[8, 22] = \"12103W0\"");
    }

    private static void PatchWisdomSeptoggHints(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Prep work:
        // Increase log array size to 100
        gmData.Code.ByName("gml_Script_sv6_add_logs").ReplaceGMLInCode( "repeat (50)", "repeat (100)");
        gmData.Code.ByName("gml_Script_sv6_get_logs").ReplaceGMLInCode( "repeat (50)", "repeat (100)");
        // Replace reset lists with simplified new array range
        gmData.Code.ByName("gml_Script_reset_logs").SubstituteGMLCode( """
            var i = 99
            repeat (100)
            {
                global.log[i] = 0
                i -= 1
            }
            i = 7
            repeat (8)
            {
                global.trooperlog[i] = 0
                i -= 1
            }
            global.log[0] = 1
            global.log[1] = 1
            global.log[4] = 1
            global.log[5] = 1
            global.log[10] = 1
            global.log[20] = 1
            global.log[30] = 1
            """);
        // Another array extension
        gmData.Code.ByName("gml_Script_reset_logs_list").ReplaceGMLInCode( """
            i = 49
            repeat (50)
            """, """
            i = 99
            repeat (100)
            """);
        // Add 7 new Hint logs to the new category 5
        gmData.Code.ByName("gml_Script_create_log_category").ReplaceGMLInCode( "dlognum = 0", """
            if (argument0 == 5)
            {
                clognum = 7
                min_log = 50
            }
            dlognum = 0
            """);
        // Add categories
        gmData.Code.ByName("gml_Object_oLogScreen_Create_0").ReplaceGMLInCode( "create_log_category(category)", """
            if (global.gotolog >= 50 && global.gotolog < 60)
                category = 5
            create_log_category(category)
            """);
        // This stuff is for the menu. The array thing might not be needed, but did it anyway, increasing by the same amount as the global.log arrays.
        gmData.Code.ByName("gml_Object_oLogScreenControl_Create_0").ReplaceGMLInCode( """
            i = 59
            repeat (60)
            """, """
            i = 109
            repeat (110)
            """);
        gmData.Code.ByName("gml_Object_oLogScreenControl_Create_0").ReplaceGMLInCode( """
            j += 1
            create_log_label(cat[4])
            create_log_entry(41)
            create_log_entry(42)
            create_log_entry(43)
            create_log_entry(45)
            """, """
            if (global.log[41] > 0 || global.log[42] > 0 || global.log[43] > 0 || global.log[45] > 0)
            {
                j += 1
                create_log_label(cat[4])
                create_log_entry(41)
                create_log_entry(42)
                create_log_entry(43)
                create_log_entry(45)
            }
            if (global.log[50] > 0 || global.log[51] > 0 || global.log[52] > 0 || global.log[53] > 0 || global.log[54] > 0 || global.log[55] > 0 || global.log[56] > 0)
            {
                j += 1
                create_log_label(cat[5])
                create_log_entry(50)
                create_log_entry(51)
                create_log_entry(52)
                create_log_entry(53)
                create_log_entry(54)
                create_log_entry(55)
                create_log_entry(56)
            }
            """);
        // Defines the new septogg hint entries
        gmData.Code.ByName("gml_Script_load_logs_list").AppendGMLInCode( $"""
            cat[5] = "DNA Hints"
            lbl[50] = "{seedObject.RoomObjects["rm_a0h13"].RegionName}"
            txt[50, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA0]}"
            pic[50, 0] = bgLogDNA0
            lbl[51] = "{seedObject.RoomObjects["rm_a1b02"].RegionName}"
            txt[51, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA1]}"
            pic[51, 0] = bgLogDNA1
            lbl[52] = "{seedObject.RoomObjects["rm_a2c05"].RegionName}"
            txt[52, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA2]}"
            pic[52, 0] = bgLogDNA2
            lbl[53] = "{seedObject.RoomObjects["rm_a3b10"].RegionName}"
            txt[53, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA3]}"
            pic[53, 0] = bgLogDNA3
            lbl[54] = "{seedObject.RoomObjects["rm_a4h03"].RegionName}"
            txt[54, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA4]}"
            pic[54, 0] = bgLogDNA4
            lbl[55] = "{seedObject.RoomObjects["rm_a5c17"].RegionName}"
            txt[55, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA5]}"
            pic[55, 0] = bgLogDNA5
            lbl[56] = "{seedObject.RoomObjects["rm_a6b02"].RegionName}"
            txt[56, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA6]}"
            pic[56, 0] = bgLogDNA6
            """);

        // Add wisdom septoggs into rooms
        // Create new wisdom septogg object
        UndertaleGameObject oWisdomSeptogg = new UndertaleGameObject
        {
            Name = gmData.Strings.MakeString("oWisdomSeptogg"),
            Sprite = gmData.Sprites.ByName("sWisdomSeptogg"),
            Depth = 90,
        };
        var wisdomSeptoggCreate = oWisdomSeptogg.EventHandlerFor(EventType.Create, gmData.Strings, gmData.Code, gmData.CodeLocals);
        wisdomSeptoggCreate.SubstituteGMLCode("image_speed = 0.1666; origY = y; timer = 0;");
        UndertaleCode wisdomSeptoggStep = oWisdomSeptogg.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, gmData.Strings, gmData.Code, gmData.CodeLocals);
        wisdomSeptoggStep.SubstituteGMLCode("y = origY + (sin((timer) * 0.08) * 2); timer++; if (timer > 9990) timer = 0;");
        gmData.GameObjects.Add(oWisdomSeptogg);

        // A0
        gmData.Rooms.ByName("rm_a0h13").GameObjects.Add(CreateRoomObject(55, 194, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a0h13_Create").AppendGMLInCode( "create_log_trigger(0, 50, 55, 194, -35, 1)");
        // A1
        gmData.Rooms.ByName("rm_a1b02").GameObjects.Add(CreateRoomObject(144, 670, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a1b02_Create").AppendGMLInCode( "create_log_trigger(0, 51, 144, 670, -35, 1)");
        // A2
        gmData.Rooms.ByName("rm_a2c05").GameObjects.Add(CreateRoomObject(115, 310, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a2c05_Create").AppendGMLInCode( "create_log_trigger(0, 52, 115, 310, -35, 1)");
        // A3
        gmData.Rooms.ByName("rm_a3b10").GameObjects.Add(CreateRoomObject(768, 396, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a3b10_Create").AppendGMLInCode( "create_log_trigger(0, 53, 768, 396, -35, 1)");
        // A4
        gmData.Rooms.ByName("rm_a4h03").GameObjects.Add(CreateRoomObject(88, 523, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a4h03_Create").AppendGMLInCode( "create_log_trigger(0, 54, 88, 523, -35, 1)");
        // A5
        gmData.Rooms.ByName("rm_a5c17").GameObjects.Add(CreateRoomObject(246, 670, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a5c17_Create").AppendGMLInCode( "create_log_trigger(0, 55, 246, 670, -35, 1)");
        // A6
        gmData.Rooms.ByName("rm_a6b02").GameObjects.Add(CreateRoomObject(240, 400, oWisdomSeptogg));
        gmData.Code.ByName("gml_Room_rm_a6b02_Create").AppendGMLInCode( "create_log_trigger(0, 56, 240, 400, -35, 1)");


        // Change minimap to include hint
        // A0
        gmData.Code.ByName("gml_Script_map_init_09").ReplaceGMLInCode( "global.map[41, 24] = \"2201100\"", "global.map[41, 24] = \"22011W0\"");

        // A1
        gmData.Code.ByName("gml_Script_map_init_12").ReplaceGMLInCode( "global.map[58, 16] = \"0212200\"", "global.map[58, 16] = \"02122W0\"");

        // A2
        gmData.Code.ByName("gml_Script_map_init_04").ReplaceGMLInCode( "global.map[24, 26] = \"0101200\"", "global.map[24, 26] = \"01012W0\"");

        // A3
        gmData.Code.ByName("gml_Script_map_init_14").ReplaceGMLInCode( "global.map[63, 28] = \"0011200\"", "global.map[63, 28] = \"00112W0\"");

        // A4
        gmData.Code.ByName("gml_Script_map_init_05").ReplaceGMLInCode( "global.map[32, 29] = \"0021200\"", "global.map[32, 29] = \"00212W0\"");

        // A5
        gmData.Code.ByName("gml_Script_map_init_16").ReplaceGMLInCode( "global.map[69, 45] = \"0112300\"", "global.map[69, 45] = \"01123W0\"");

        // A6
        gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode( "global.map[20, 40] = \"0112100\"", "global.map[20, 40] = \"01121W0\"");
    }

    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        PatchIceBeamHint(gmData, decompileContext, seedObject);
        PatchWisdomSeptoggHints(gmData, decompileContext, seedObject);
    }
}
