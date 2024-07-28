using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class AdjustSavingScripts
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Modify save scripts to load our new globals / stuff we modified
        // Some variables here depend on other patches (i.e. custom items). These need to be synced.
        // Maybe sometime in the future I can think of a better way to deal with them.
        gmData.Scripts.AddScript("sv6_add_newglobals", """
                                          var list, str_list;
                                          list = ds_list_create()
                                          ds_list_add(list, global.hasBombs)
                                          ds_list_add(list, global.hasPowergrip)
                                          ds_list_add(list, global.hasSpiderball)
                                          ds_list_add(list, global.hasJumpball)
                                          ds_list_add(list, global.hasHijump)
                                          ds_list_add(list, global.hasVaria)
                                          ds_list_add(list, global.hasSpacejump)
                                          ds_list_add(list, global.hasSpeedbooster)
                                          ds_list_add(list, global.hasScrewattack)
                                          ds_list_add(list, global.hasGravity)
                                          ds_list_add(list, global.hasCbeam)
                                          ds_list_add(list, global.hasIbeam)
                                          ds_list_add(list, global.hasWbeam)
                                          ds_list_add(list, global.hasSbeam)
                                          ds_list_add(list, global.hasPbeam)
                                          ds_list_add(list, global.hasMorph)
                                          ds_list_add(list, global.firstMissileCollected)
                                          ds_list_add(list, global.firstSMissileCollected)
                                          ds_list_add(list, global.firstPBombCollected)
                                          ds_list_add(list, global.firstETankCollected)
                                          ds_list_add(list, global.missileLauncher)
                                          ds_list_add(list, global.SMissileLauncher)
                                          ds_list_add(list, global.PBombLauncher)
                                          ds_list_add(list, global.missileLauncherExpansion)
                                          ds_list_add(list, global.SMissileLauncherExpansion)
                                          ds_list_add(list, global.PBombLauncherExpansion)
                                          ds_list_add(list, global.maxhealth)
                                          ds_list_add(list, global.maxmissiles)
                                          ds_list_add(list, global.maxsmissiles)
                                          ds_list_add(list, global.maxpbombs)
                                          ds_list_add(list, global.gameHash)
                                          ds_list_add(list, global.dna)
                                          ds_list_add(list, global.startingSave)
                                          ds_list_add(list, global.flashlightLevel)
                                          ds_list_add(list, global.speedBoosterFramesReduction)
                                          ds_list_add(list, global.showStartingMemo)
                                          ds_list_add(list, global.lastOffworldNumber)
                                          ds_list_add(list, global.collectedIndices)
                                          ds_list_add(list, global.collectedItems)
                                          ds_list_add(list, global.hasWJ)
                                          ds_list_add(list, global.hasIBJ)
                                          ds_list_add(list, global.hasLongBeam)
                                          ds_list_add(list, global.historyLogEntryText)
                                          str_list = ds_list_write(list)
                                          ds_list_clear(list)
                                          return str_list;
                                          """);


        gmData.Scripts.AddScript("sv6_get_newglobals", """
                                          list = ds_list_create()
                                          ds_list_read(list, base64_decode(file_text_read_string(argument0)))
                                          i = 0
                                          global.hasBombs = readline()
                                          global.hasPowergrip = readline()
                                          global.hasSpiderball = readline()
                                          global.hasJumpball = readline()
                                          global.hasHijump = readline()
                                          global.hasVaria = readline()
                                          global.hasSpacejump = readline()
                                          global.hasSpeedbooster = readline()
                                          global.hasScrewattack = readline()
                                          global.hasGravity = readline()
                                          global.hasCbeam = readline()
                                          global.hasIbeam = readline()
                                          global.hasWbeam = readline()
                                          global.hasSbeam = readline()
                                          global.hasPbeam = readline()
                                          global.hasMorph = readline()
                                          global.firstMissileCollected = readline()
                                          global.firstSMissileCollected = readline()
                                          global.firstPBombCollected = readline()
                                          global.firstETankCollected = readline()
                                          global.missileLauncher = readline()
                                          global.SMissileLauncher = readline()
                                          global.PBombLauncher = readline()
                                          global.missileLauncherExpansion = readline()
                                          global.SMissileLauncherExpansion = readline()
                                          global.PBombLauncherExpansion = readline()
                                          global.maxhealth = readline()
                                          global.maxmissiles = readline()
                                          global.maxsmissiles = readline()
                                          global.maxpbombs = readline()
                                          global.gameHash = readline()
                                          global.dna = readline()
                                          global.startingSave = readline()
                                          global.flashlightLevel = readline()
                                          global.speedBoosterFramesReduction = readline();
                                          global.showStartingMemo = readline();
                                          global.lastOffworldNumber = readline();
                                          if (global.lastOffworldNumber == undefined)
                                            global.lastOffworldNumber = 0
                                          global.collectedIndices = readline();
                                          if (global.collectedIndices == undefined || global.collectedIndices == 0)
                                            global.collectedIndices = "locations:"
                                          global.collectedItems = readline();
                                          if (global.collectedItems == undefined || global.collectedItems == 0)
                                            global.collectedItems = "items:"
                                          global.hasWJ = readline()
                                          if (global.hasWJ == undefined)
                                            global.hasWJ = 1
                                          global.hasIBJ = readline()
                                          if (global.hasIBJ == undefined)
                                            global.hasIBJ = 1
                                          global.hasLongBeam = readline()
                                          if (global.hasLongBeam == undefined)
                                            global.hasLongBeam = 1
                                          global.historyLogEntryText = readline()
                                          if (global.historyLogEntryText == undefined)
                                            global.historyLogEntryText = ""
                                          ds_list_clear(list)
                                          """);


        UndertaleCode? sv6Save = gmData.Code.ByName("gml_Script_sv6_save");
        sv6Save.ReplaceGMLInCode("save_str[10] = sv6_add_seed()", "save_str[10] = sv6_add_seed(); save_str[11] = sv6_add_newglobals()");
        sv6Save.ReplaceGMLInCode("V7.0", "RDV V8.0");
        sv6Save.ReplaceGMLInCode("repeat (10)", "repeat (11)");

        UndertaleCode? sv6load = gmData.Code.ByName("gml_Script_sv6_load");
        sv6load.ReplaceGMLInCode("V7.0", "RDV V8.0");
        sv6load.ReplaceGMLInCode("sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid); sv6_get_newglobals(fid);");
        sv6load.ReplaceGMLInCode("global.maxhealth = 99 + (global.etanks * 100 * oControl.mod_etankhealthmult)", "");
        sv6load.ReplaceGMLInCode("""
                                     if (global.difficulty < 2)
                                     {
                                         global.maxmissiles = oControl.mod_Mstartingcount + (global.mtanks * 5);
                                         global.maxsmissiles = global.stanks * 2;
                                         global.maxpbombs = global.ptanks * 2;
                                     }
                                     else
                                     {
                                         global.maxmissiles = oControl.mod_Mstartingcount + (global.mtanks * 2);
                                         global.maxsmissiles = global.stanks;
                                         global.maxpbombs = global.ptanks;
                                     }
                                 """, "");

        //complain if invalid game hash
        sv6load.PrependGMLInCode($"var uniqueGameHash = \"{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash}) (World: {seedObject.Identifier.WorldUUID})\"");
        sv6load.ReplaceGMLInCode("global.playerhealth = global.maxhealth",
            "if (global.gameHash != uniqueGameHash) { " +
            "show_message(\"Save file is from another seed or Multiworld word! (\" + global.gameHash + \")\"); " +
            "file_text_close(fid); file_delete((filename + \"d\")); room_goto(titleroom); exit;" +
            "} global.playerhealth = global.maxhealth");
        // TODO: instead of just show_messsage, have an actual proper in-game solution. Maybe do this after MW
        // reference: https://cdn.discordapp.com/attachments/914294505107251231/1121816654385516604/image.png

        UndertaleCode? sv6loadDetails = gmData.Code.ByName("gml_Script_sv6_load_details");
        sv6loadDetails.ReplaceGMLInCode("V7.0", "RDV V8.0");
        sv6loadDetails.ReplaceGMLInCode("sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid); sv6_get_newglobals(fid);");

        foreach (string code in new[] { "gml_Script_save_stats", "gml_Script_save_stats2", "gml_Script_load_stats", "gml_Script_load_stats2" })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("V7.0", "RDV V8.0");
        }

        // Change to custom save directory
        gmData.GeneralInfo.Name = gmData.Strings.MakeString("AM2R_RDV");
        gmData.GeneralInfo.FileName = gmData.Strings.MakeString("AM2R_RDV");
    }
}
