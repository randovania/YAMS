using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.misc;

public class RemoveHelperSeptoggs
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        // Turn off Septoggs if the wished configuration
        characterVarsCode.AppendGMLInCode($"global.septoggHelpers = {(seedObject.Patches.SeptoggHelpers ? "1" : "0")}");

        foreach (UndertaleCode? code in gmData.Code.Where(c => c.Name.Content.StartsWith("gml_Script_scr_septoggs_")))
        {
            code.PrependGMLInCode("if (!global.septoggHelpers) return true; else return false;");
        }

        UndertaleGameObject? elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (var septogg in new[]
                 {
                     gmData.Rooms.ByName("rm_a0h11").GameObjects.First(go => go.X == 480 && go.Y == 768 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a0h29").GameObjects.First(go => go.X == 384 && go.Y == 312 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a1h05").GameObjects.First(go => go.X == 1184 && go.Y == 832 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a3h04").GameObjects.First(go => go.X == 528 && go.Y == 1344 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a3h04").GameObjects.First(go => go.X == 1728 && go.Y == 1248 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a3a07").GameObjects.First(go => go.X == 112 && go.Y == 240 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a3b02").GameObjects.First(go => go.X == 192 && go.Y == 896 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a3b08").GameObjects.First(go => go.X == 224 && go.Y == 352 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a0h17").GameObjects.First(go => go.X == 96 && go.Y == 352 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a4b02a").GameObjects.First(go => go.X == 120 && go.Y == 816 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a4b10").GameObjects.First(go => go.X == 144 && go.Y == 624 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a4b10").GameObjects.First(go => go.X == 512 && go.Y == 256 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a4b11").GameObjects.First(go => go.X == 224 && go.Y == 2288 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c13").GameObjects.First(go => go.X == 96 && go.Y == 704 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c14").GameObjects.First(go => go.X == 1056 && go.Y == 288 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c17").GameObjects.First(go => go.X == 192 && go.Y == 288 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c18").GameObjects.First(go => go.X == 128 && go.Y == 192 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c18").GameObjects.First(go => go.X == 480 && go.Y == 192 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c21").GameObjects.First(go => go.X == 160 && go.Y == 384 && go.ObjectDefinition == elderSeptogg),
                     gmData.Rooms.ByName("rm_a5c21").GameObjects.First(go => go.X == 96 && go.Y == 560 && go.ObjectDefinition == elderSeptogg),
                 })
        {
            septogg.CreationCode.ReplaceGMLInCode("oControl.mod_septoggs_bombjumps_easy == 0 && global.hasBombs == 1",
                "!global.septoggHelpers");
        }

        gmData.Rooms.ByName("rm_a0h25").GameObjects.First(go => go.X == 120 && go.Y == 816 && go.ObjectDefinition == elderSeptogg).CreationCode.ReplaceGMLInCode(
            "else if (global.hasBombs == 1 || global.hasSpiderball == 1 || global.hasSpacejump == 1)",
            "else if (!global.septoggHelpers)");
        // Make these septoggs always appear instead of only when coming from certain room
        gmData.Rooms.ByName("rm_a2a13").GameObjects.First(go => go.ObjectDefinition == elderSeptogg).CreationCode.ReplaceGMLInCode("&& oControl.mod_previous_room == 103", "");
        gmData.Rooms.ByName("rm_a3a07").GameObjects.First(go => go.ObjectDefinition == elderSeptogg).CreationCode.ReplaceGMLInCode("&& oControl.mod_previous_room == 136", "");
        gmData.Rooms.ByName("rm_a5a05").GameObjects.First(go => go.ObjectDefinition == elderSeptogg).CreationCode.ReplaceGMLInCode("&& oControl.mod_previous_room == 300", "");
    }
}
