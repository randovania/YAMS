using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.geometry;

public class MandatoryGeometryChanges
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // A4 exterior top, always remove the bomb blocks when coming from that entrance
        foreach (var codeEntry in gmData.Rooms.ByName("rm_a4h03").GameObjects.Where(go => go.Y == 624 && go.ObjectDefinition.Name.Content == "oBlockBomb"))
        {
            codeEntry.CreationCode.ReplaceGMLInCode("oControl.mod_previous_room == 214 && global.spiderball == 0", "global.targetx == 416");
        }
        
        // When going down from thoth, make PB blocks disabled
        gmData.Code.ByName("gml_Room_rm_a0h13_Create").PrependGMLInCode("if (global.targety == 16) {global.event[176] = 1; with (oBlockPBombChain) event_user(0); }");

        // When coming from right side in Drill, always make drill event done
        gmData.Code.ByName("gml_Room_rm_a0h17e_Create").PrependGMLInCode("if (global.targety == 160) global.event[172] = 3");
    }
}
