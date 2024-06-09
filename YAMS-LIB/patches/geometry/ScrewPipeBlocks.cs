using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.geometry;

public class ScrewPipeBlocks
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // screw+pipes related
        if (seedObject.Patches.ScrewPipeBlocks) characterVarsCode.ReplaceGMLInCode("global.screwPipeBlocks = 0", "global.screwPipeBlocks = 1");

        // Screw blocks before normal pipe rooms
        foreach (string codeName in new[]
                 {
                     "gml_Room_rm_a1a06_Create", "gml_Room_rm_a2a08_Create", "gml_Room_rm_a2a09_Create", "gml_Room_rm_a2a12_Create", "gml_Room_rm_a3a04_Create",
                     "gml_Room_rm_a4h01_Create", "gml_Room_rm_a4a01_Create"
                 })
        {
            gmData.Code.ByName(codeName).AppendGMLInCode("if (!global.screwPipeBlocks) {with (oBlockScrew) instance_destroy();}");
        }

        foreach (string roomName in new[] { "rm_a1a06", "rm_a3a04", "rm_a4a01" })
        {
            foreach (UndertaleRoom.GameObject? gameObject in gmData.Rooms.ByName(roomName).GameObjects.Where(g => g.ObjectDefinition.Name.Content == "oBlockScrew"))
            {
                if (gameObject.CreationCode is null) continue;

                gameObject.CreationCode.ReplaceGMLInCode("global.hasScrewattack == 0", "false");
            }
        }

        // A bunch of tiles in a5c13 - screw blocks before pipe hub
        foreach (var codeEntry in gmData.Rooms.ByName("rm_a5c13").GameObjects
                     .Where(go => go.Y is >= 144 and <= 176 && go.X is >= 240 and <= 256 && go.ObjectDefinition.Name.Content == "oBlockScrew"))
        {
            codeEntry.CreationCode.SubstituteGMLCode("if (!global.screwPipeBlocks) instance_destroy();");
        }
    }
}
