using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class LoadFromStart
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Modify main menu to have a "restart from starting save" option
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Other_10").SubstituteGMLCode("""
                                                                                      op1 = instance_create(x, y, oPauseOption)
                                                                                      op1.optionid = 0
                                                                                      op1.label = get_text("PauseMenu", "Resume")
                                                                                      op2 = instance_create(x, (y + 16), oPauseOption)
                                                                                      op2.optionid = 1
                                                                                      op2.label = get_text("PauseMenu", "Restart")
                                                                                      op3 = instance_create(x, (y + 32), oPauseOption)
                                                                                      op3.optionid = 2
                                                                                      op3.label = "Restart from Start Location"
                                                                                      op4 = instance_create(x, (y + 48), oPauseOption)
                                                                                      op4.optionid = 3
                                                                                      op4.label = get_text("PauseMenu", "Options")
                                                                                      op5 = instance_create(x, (y + 64), oPauseOption)
                                                                                      op5.optionid = 4
                                                                                      op5.label = get_text("PauseMenu", "Quit")
                                                                                      """);
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Create_0").ReplaceGMLInCode("lastitem = 3", "lastitem = 4;");
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Create_0").AppendGMLInCode("""
                                                                                    tip[0] = get_text("PauseMenu", "Resume_Tip");
                                                                                    tip[1] = get_text("PauseMenu", "Restart_Tip");
                                                                                    tip[2] = "Abandon the current game and load from Starting Area";
                                                                                    tip[3] = get_text("PauseMenu", "Options_Tip");
                                                                                    tip[4] = get_text("PauseMenu", "Quit_Tip");
                                                                                    global.tiptext = tip[global.curropt];
                                                                                    """);
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Step_0").ReplaceGMLInCode("""
                                                                                           if (global.curropt == 1)
                                                                                           {
                                                                                               instance_create(50, 92, oOptionsReload)
                                                                                               instance_destroy()
                                                                                           }
                                                                                           if (global.curropt == 2)
                                                                                           {
                                                                                               instance_create(50, 92, oOptionsMain)
                                                                                               instance_destroy()
                                                                                           }
                                                                                           if (global.curropt == 3)
                                                                                           {
                                                                                               instance_create(50, 92, oOptionsQuit)
                                                                                               instance_destroy()
                                                                                           }
                                                                                   """, """
                                                                                                if (global.curropt == 1)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsReload)
                                                                                                    global.shouldLoadFromStart = 0;
                                                                                                    instance_destroy()
                                                                                                }
                                                                                                if (global.curropt == 2)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsReload)
                                                                                                    global.shouldLoadFromStart = 1;
                                                                                                    instance_destroy()
                                                                                                }
                                                                                                if (global.curropt == 3)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsMain)
                                                                                                    instance_destroy()
                                                                                                }
                                                                                                if (global.curropt == 4)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsQuit)
                                                                                                    instance_destroy()
                                                                                                }
                                                                                        """);
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Other_11").AppendGMLInCode("""
                                                                                    if instance_exists(op5)
                                                                                    {
                                                                                        with (op5)
                                                                                            instance_destroy()
                                                                                    }

                                                                                    """);
        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode("global.shouldLoadFromStart = 0;");
        gmData.Code.ByName("gml_Object_oLoadGame_Other_10").AppendGMLInCode("""
                                                                            if (global.shouldLoadFromStart)
                                                                            {
                                                                              global.save_room = global.startingSave;
                                                                              set_start_location();
                                                                              room_change(global.start_room, 1)
                                                                              global.shouldLoadFromStart = 0;
                                                                            }
                                                                            """);
        gmData.Code.ByName("gml_Object_oOptionsReload_Step_0").ReplaceGMLInCode("instance_create(50, 92, oPauseMenuOptions)",
            "instance_create(50, 92, oPauseMenuOptions); global.shouldLoadFromStart = 0;");
    }
}
