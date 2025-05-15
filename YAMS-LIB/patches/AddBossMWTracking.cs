using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class AddBossMWTracking
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Guardian
        gmData.Code.ByName("gml_Object_oBoss1Head_Step_0").ReplaceGMLInCode("sfx_play(194)", "sfx_play(sndBoss1Death); global.collectedItems += \"Guardian|1,\"; send_location_and_inventory_packet();");
        gmData.Code.ByName("gml_Object_oBoss1Head_Collision_382").ReplaceGMLInCode("sfx_play(194)", "sfx_play(sndBoss1Death); global.collectedItems += \"Guardian|1,\"; send_location_and_inventory_packet();");

        // Arachnus
        gmData.Code.ByName("gml_Object_oArachnus_Step_0").ReplaceGMLInCode("sfx_play(219)", "sfx_play(sndArachnusDeath); global.collectedItems += \"Arachnus|1,\"; send_location_and_inventory_packet();");

        // Torizo
        gmData.Code.ByName("gml_Object_oTorizo2_Step_0").ReplaceGMLInCode("sfx_play(249)", "{ sfx_play(sndTorizoDeath); global.collectedItems += \"Torizo|1,\"; send_location_and_inventory_packet(); }");

        // Tester
        gmData.Code.ByName("gml_Object_oTester_Step_0").ReplaceGMLInCode("sfx_play(309)", "sfx_play(sndTesterDeath); global.collectedItems += \"Tester|1,\"; send_location_and_inventory_packet();");

        // Serris
        gmData.Code.ByName("gml_Object_oErisHead_Step_0").ReplaceGMLInCode("sfx_play(327)", "sfx_play(sndErisDeath); global.collectedItems += \"Serris|1,\"; send_location_and_inventory_packet();");

        // Genesis
        gmData.Code.ByName("gml_Object_oGenesis_Step_0").ReplaceGMLInCode("sfx_play(341)", "sfx_play(sndGenesisDeath); global.collectedItems += \"Genesis|1,\"; send_location_and_inventory_packet();");
    }
}
