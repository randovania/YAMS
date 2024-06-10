using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.qol;

public class SaveCutsceneSkip
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        characterVarsCode.AppendGMLInCode($"global.skipSaveCutscene = {(seedObject.Patches.SkipSaveCutscene ? "1" : "0")}");

        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("""
                                                                                 if (statetime == 1)
                                                                                 {
                                                                                     sfx_play(sndSave)
                                                                                     instance_create(x, y, oSaveFX)
                                                                                     instance_create(x, y, oSaveSparks)
                                                                                     popup_text(get_text("Notifications", "GameSaved"))
                                                                                     save_game("save" + (string(global.saveslot + 1)))
                                                                                     refill_heath_ammo()
                                                                                 }
                                                                                 if (statetime == 230)
                                                                                     state = IDLE
                                                                             """, """
                                                                                      if (statetime == 1)
                                                                                      {
                                                                                          sfx_play(sndSave)
                                                                                          if (!global.skipSaveCutscene)
                                                                                          {
                                                                                              instance_create(x, y, oSaveFX)
                                                                                              instance_create(x, y, oSaveSparks)
                                                                                          }
                                                                                          popup_text(get_text("Notifications", "GameSaved"))
                                                                                          save_game("save" + (string(global.saveslot + 1)))
                                                                                          refill_heath_ammo()
                                                                                      }
                                                                                      if ((statetime == 230 && !global.skipSaveCutscene) || (statetime == 10 && global.skipSaveCutscene))
                                                                                          state = IDLE
                                                                                  """);
    }
}
