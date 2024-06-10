using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.qol;

public class GameplayCutsceneSkip
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Skip most cutscenes when enabled
        characterVarsCode.AppendGMLInCode($"global.skipCutscenes = {(seedObject.Patches.SkipCutscenes ? "1" : "0")}");

        // Skip Intro cutscene instantly
        gmData.Code.ByName("gml_Object_oIntroCutscene_Create_0").PrependGMLInCode("room_change(15, 0)");
        // First Alpha cutscene - event 0
        characterVarsCode.AppendGMLInCode("global.event[0] = global.skipCutscenes");
        // Gamma mutation cutscene - event 109
        gmData.Code.ByName("gml_Object_oMGammaFirstTrigger_Collision_267").PrependGMLInCode("""
                                                                                            if (global.skipCutscenes)
                                                                                            {
                                                                                                global.event[109] = 1;
                                                                                                mus_current_fadeout();
                                                                                                mutat = instance_create(144, 96, oMGammaMutate);
                                                                                                mutat.state = 3;
                                                                                                mutat.statetime = 90;
                                                                                                instance_destroy();
                                                                                                exit;
                                                                                            }
                                                                                            """);
        // Zeta mutation cutscene - event 205
        characterVarsCode.AppendGMLInCode("global.event[205] = global.skipCutscenes");
        // Omega Mutation cutscene - event 300
        characterVarsCode.AppendGMLInCode("global.event[300] = global.skipCutscenes");
        // Also still increase the metroid counters from the hatchling cutscene
        gmData.Code.ByName("gml_Object_oEggTrigger_Create_0").PrependGMLInCode("""
                                                                               if (global.skipCutscenes && !global.event[302])
                                                                               {
                                                                                    if (oControl.mod_monstersextremecheck == 1)
                                                                                       oControl.mod_monstersextreme = 1
                                                                                   global.event[302] = 1
                                                                                   global.monstersleft = 9
                                                                                   if (global.difficulty == 2)
                                                                                       global.monstersleft = 16
                                                                                   if (oControl.mod_fusion == 1)
                                                                                       global.monstersleft = 21
                                                                                   if (oControl.mod_monstersextreme == 1)
                                                                                       global.monstersleft = 47
                                                                                   if (!instance_exists(oScanMonster))
                                                                                   {
                                                                                       scan = instance_create(0, 0, oScanMonster)
                                                                                       scan.ammount = 9
                                                                                       if (global.difficulty == 2)
                                                                                           scan.ammount = 16
                                                                                       if (oControl.mod_fusion == 1)
                                                                                           scan.ammount = 21
                                                                                       if (oControl.mod_monstersextreme == 1)
                                                                                           scan.ammount = 47
                                                                                       scan.eventno = 700
                                                                                       scan.alarm[0] = 15
                                                                                   }
                                                                               }
                                                                               """);
        // Drill cutscene - event 172 to 3
        characterVarsCode.AppendGMLInCode("global.event[172] = global.skipCutscenes * 3");
        // 1 Orb cutscene
        gmData.Code.ByName("gml_Object_oClawOrbFirst_Other_11")
            .AppendGMLInCode(
                "if (global.skipCutscenes) {with (ecam) instance_destroy(); global.enablecontrol = 1; view_object[0] = oCamera; block2 = instance_create(768, 48, oSolid2x2); block2.material = 3; with (oA1MovingPlatform2) with (myblock) instance_destroy()}");
        // 3 Orb cutscene
        gmData.Code.ByName("gml_Object_oClawPuzzle_Alarm_0")
            .AppendGMLInCode(
                "if (global.skipCutscenes) {with (ecam) instance_destroy(); global.enablecontrol = 1; view_object[0] = oCamera; block2 = instance_create(608, 112, oSolid2x2); block2.material = 3; with (oA1MovingPlatform) with (myblock) instance_destroy()}");
        // Fix audio for the orb cutscenes
        gmData.Code.ByName("gml_Object_oMusicV2_Other_4").AppendGMLInCode("sfx_stop(sndStoneLoop)");
        // Skip baby collected cutscene
        gmData.Code.ByName("gml_Object_oHatchlingTrigger_Collision_267")
            .PrependGMLInCode("if (global.skipCutscenes) { global.event[304] = 1; instance_create(x, y, oHatchling); instance_destroy(); exit; }");
        // Skip A5 activation cutscene to not have to wait a long time
        gmData.Code.ByName("gml_Object_oA5MainSwitch_Step_0").ReplaceGMLInCode("""
                                                                                       if (oCharacter.x < 480)
                                                                                       {
                                                                                           with (oCharacter)
                                                                                               x += 1
                                                                                       }
                                                                               """, """
                                                                                            if (oCharacter.x < 480)
                                                                                            {
                                                                                                with (oCharacter)
                                                                                                    x += 1
                                                                                            }
                                                                                            if (oCharacter.x == 480 && global.skipCutscenes)
                                                                                                statetime = 119
                                                                                    """);
        gmData.Code.ByName("gml_Object_oA5MainSwitch_Step_0").ReplaceGMLInCode("instance_create(x, y, oA5BotSpawnCutscene)",
            "instance_create(x, y, oA5BotSpawnCutscene); if (global.skipCutscenes) statetime = 319");
    }
}
