using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class RequiredMains
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject, bool isHorde)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Add main (super) missile / PB launcher
        // missileLauncher, SMissileLauncher, PBombLauncher
        // also add an item for them + amount of expansions they give

        characterVarsCode.PrependGMLInCode("global.missileLauncher = 0; global.SMissileLauncher = 0; global.PBombLauncher = 0;" +
                                           "global.missileLauncherExpansion = 30; global.SMissileLauncherExpansion = 2; global.PBombLauncherExpansion = 2;");


        // Make expansion set to default values
        characterVarsCode.ReplaceGMLInCode("global.missiles = oControl.mod_Mstartingcount", "global.missiles = 0;");
        characterVarsCode.ReplaceGMLInCode("global.maxmissiles = oControl.mod_Mstartingcount", "global.maxmissiles = global.missiles;");
        characterVarsCode.ReplaceGMLInCode("global.smissiles = 0", "global.smissiles = 0;");
        characterVarsCode.ReplaceGMLInCode("global.maxsmissiles = 0", "global.maxsmissiles = global.smissiles;");
        characterVarsCode.ReplaceGMLInCode("global.pbombs = 0", "global.pbombs = 0;");
        characterVarsCode.ReplaceGMLInCode("global.maxpbombs = 0", "global.maxpbombs = global.pbombs;");
        characterVarsCode.ReplaceGMLInCode("global.maxhealth = 99", "global.maxhealth = global.playerhealth;");

        // Make main (super) missile / PB launcher required for firing
        UndertaleCode? drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
        UndertaleCode? shootMissileCode = gmData.Code.ByName("gml_Script_shoot_missile");
        shootMissileCode.ReplaceGMLInCode(
            "if ((global.currentweapon == 1 && global.missiles > 0) || (global.currentweapon == 2 && global.smissiles > 0))",
            "if ((global.currentweapon == 1 && global.missiles > 0 && global.missileLauncher) || (global.currentweapon == 2 && global.smissiles > 0 && global.SMissileLauncher))");
        UndertaleCode? chStepFireCode = gmData.Code.ByName("gml_Script_chStepFire");
        chStepFireCode.ReplaceGMLInCode("&& global.pbombs > 0", "&& global.pbombs > 0 && global.PBombLauncher");

        // Change GUI For toggle, use a red item sprite instead of green, for hold use a red instead of yellow. For not selected, use a crossed out one.
        // Replace Missile GUI
        if (isHorde)
            drawGuiCode.ReplaceGMLInCode("mslspr = sGUIIceMissile", "mslspr = sGUIMissile");
        string mslSprite = !isHorde ? "sGUIMissile" : "mslspr";
        drawGuiCode.ReplaceGMLInCode(
            $$"""
                        if (global.currentweapon != 1 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball)
                        {
                            draw_sprite({{mslSprite}}, 0, 0 + xoff + 1, 4);
                        }
            """,
            """
                        if (((global.currentweapon != 1 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && (!global.missileLauncher)))
                            draw_sprite(sGUIMissile, 4, (0 + xoff + 1), 4)
                        else if (((global.currentweapon != 1 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && (global.missileLauncher)))
                            draw_sprite(sGUIMissile, 0, (0 + xoff + 1), 4)

            """);
        drawGuiCode.ReplaceGMLInCode($$"""
                                                     if (oCharacter.armmsl == 0)
                                                     {
                                                         draw_sprite({{mslSprite}}, 1, 0 + xoff + 1, 4);
                                                     }
                                     """, """
                                                          if (oCharacter.armmsl == 0 && global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 1, (0 + xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 0 && !global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 5, (0 + xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode($$"""
                                                     if (oCharacter.armmsl == 1)
                                                     {
                                                         draw_sprite({{mslSprite}}, 2, 0 + xoff + 1, 4);
                                                     }
                                     """, """
                                                          if (oCharacter.armmsl == 1 && global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 2, (0 + xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 1 && !global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 3, (0 + xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode($$"""
                                                 if (global.currentweapon == 1)
                                                 {
                                                     draw_sprite({{mslSprite}}, 1, 0 + xoff + 1, 4);
                                                 }
                                     """, """
                                                      if (global.currentweapon == 1 && global.missileLauncher)
                                                          draw_sprite(sGUIMissile, 1, (0 + xoff + 1), 4)
                                                      else if (global.currentweapon == 1 && !global.missileLauncher)
                                                          draw_sprite(sGUIMissile, 3, (0 + xoff + 1), 4)
                                                      else if (global.currentweapon != 1 && !global.missileLauncher)
                                                          draw_sprite(sGUIMissile, 4, (0 + xoff + 1), 4)
                                          """);

        // Replace Super GUI
        drawGuiCode.ReplaceGMLInCode(
            """
                        if (global.currentweapon != 2 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball)
                        {
                            draw_sprite(sGUISMissile, 0, xoff + 1, 4);
                        }
            """,
            """
                        if ((global.currentweapon != 2 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && !global.SMissileLauncher)
                            draw_sprite(sGUISMissile, 4, (xoff + 1), 4)
                        else if ((global.currentweapon != 2 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && global.SMissileLauncher)
                            draw_sprite(sGUISMissile, 0, (xoff + 1), 4)

            """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 0)
                                                     {
                                                         draw_sprite(sGUISMissile, 1, xoff + 1, 4);
                                                     }
                                     """, """
                                                          if (oCharacter.armmsl == 0 && global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 0 && !global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 5, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 1)
                                                     {
                                                         draw_sprite(sGUISMissile, 2, xoff + 1, 4);
                                                     }
                                     """, """
                                                          if (oCharacter.armmsl == 1 && global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 2, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 1 && !global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 3, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                 if (global.currentweapon == 2)
                                                 {
                                                     draw_sprite(sGUISMissile, 1, xoff + 1, 4);
                                                 }
                                     """, """
                                                      if (global.currentweapon == 2 && global.SMissileLauncher)
                                                          draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
                                                      else if (global.currentweapon == 2 && !global.SMissileLauncher)
                                                          draw_sprite(sGUISMissile, 3, (xoff + 1), 4)
                                                      else if (global.currentweapon != 2 && !global.SMissileLauncher)
                                                          draw_sprite(sGUISMissile, 4, (xoff + 1), 4)
                                          """);

        // Replace PB GUI
        drawGuiCode.ReplaceGMLInCode(
            """
                        if (oCharacter.state != 23 && oCharacter.state != 24 && oCharacter.state != 27 && oCharacter.state != 54 && oCharacter.state != 55 && oCharacter.sjball == 0)
                        {
                            draw_sprite(sGUIPBomb, 0, xoff + 1, 4);
                        }
            """,
            """
                        if ((global.PBombLauncher) && oCharacter.state != 23 && oCharacter.state != 24 && oCharacter.state != 27 && oCharacter.state != 54 && oCharacter.state != 55 && oCharacter.sjball == 0)
                            draw_sprite(sGUIPBomb, 0, (xoff + 1), 4)
                        else if ((!global.PBombLauncher) && oCharacter.state != 23 && oCharacter.state != 24 && oCharacter.state != 27 && oCharacter.state != 54 && oCharacter.state != 55 && oCharacter.sjball == 0)
                            draw_sprite(sGUIPBomb, 4, (xoff + 1), 4)
            """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 0)
                                                     {
                                                         draw_sprite(sGUIPBomb, 1, xoff + 1, 4);
                                                     }
                                     """, """
                                                          if (oCharacter.armmsl == 0 && global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 0 && !global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 5, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 1)
                                                     {
                                                         draw_sprite(sGUIPBomb, 2, xoff + 1, 4);
                                                     }
                                     """, """
                                                          if (oCharacter.armmsl == 1 && global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 2, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 1 && !global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 3, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                 if (global.currentweapon == 3)
                                                 {
                                                     draw_sprite(sGUIPBomb, 1, xoff + 1, 4);
                                                 }
                                     """, """
                                                      if (global.currentweapon == 3 && global.PBombLauncher)
                                                          draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
                                                      else if (global.currentweapon == 3 && !global.PBombLauncher)
                                                          draw_sprite(sGUIPBomb, 3, (xoff + 1), 4)
                                                      else if (global.currentweapon != 3 && !global.PBombLauncher)
                                                          draw_sprite(sGUIPBomb, 4, (xoff + 1), 4)
                                          """);

        // Fix weapon selection with toggle
        UndertaleCode? chStepControlCode = gmData.Code.ByName("gml_Script_chStepControl");
        chStepControlCode.ReplaceGMLInCode("if (kMissile && kMissilePushedSteps == 1 && global.maxmissiles > 0", "if (kMissile && kMissilePushedSteps == 1");
        chStepControlCode.ReplaceGMLInCode("if (global.currentweapon == 1 && global.missiles == 0)",
            "if (global.currentweapon == 1 && (global.maxmissiles == 0 || global.missiles == 0))");

        // Fix weapon selection cancel with toggle
        chStepControlCode.ReplaceGMLInCode("if (kSelect && kSelectPushedSteps == 0 && global.maxmissiles > 0 && global.currentweapon != 0)",
            "if (kSelect && kSelectPushedSteps == 0 && (global.missiles > 0 || global.smissiles > 0 || global.pbombs > 0) && global.currentweapon != 0)");

        // Fix weapon selection with hold
        chStepControlCode.ReplaceGMLInCode("""
                                               if (global.currentweapon == 0)
                                               {
                                                   global.currentweapon = 1;
                                               }
                                           """, """
                                                if (global.currentweapon == 0)
                                                {
                                                    if (global.maxmissiles > 0) global.currentweapon = 1;
                                                    else if (global.maxsmissiles > 0) global.currentweapon = 2;
                                                }
                                                """);
        chStepControlCode.ReplaceGMLInCode("if (global.maxmissiles > 0 && (state", "if ((state");
    }
}
