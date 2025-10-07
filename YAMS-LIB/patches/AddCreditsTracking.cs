using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class AddCreditsTracking
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Send credits item if ship is about to fly off.
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("instance_create(3296, 1088, oShipOutro);", "global.collectedItems += \"Credits|1,\"; send_location_and_inventory_packet(); instance_create(3296, 1088, oShipOutro);");
    }
}
