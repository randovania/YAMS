using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB.patches;

public class AddMissingDoors
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        const uint ThothBridgeLeftDoorID = 400000;
        const uint ThothBridgeRightDoorID = 400001;
        const uint A2WaterTurbineLeftDoorID = 400002;

        // Add doors to gfs thoth bridge
        UndertaleCode thothLeftDoorCC = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_thothLeftDoor_Create") };
        UndertaleCode thothRightDoorCC = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_thothRightDoor_Create") };
        gmData.Code.Add(thothLeftDoorCC);
        gmData.Code.Add(thothRightDoorCC);
        var a8door = gmData.GameObjects.ByName("oDoorA8");
        gmData.Rooms.ByName("rm_a8a03").GameObjects.Add(CreateRoomObject(24, 96, a8door, thothLeftDoorCC, 1, 1, ThothBridgeLeftDoorID));
        gmData.Rooms.ByName("rm_a8a03").GameObjects.Add(CreateRoomObject(616, 96, a8door, thothRightDoorCC, 1, 1, ThothBridgeRightDoorID));
        // Make doors appear in front, so you can see them in door lock rando
        gmData.Code.ByName("gml_Room_rm_a8a03_Create").AppendGMLInCode("with (oDoor) depth = -200");


        // Add door from water turbine station to hydro station exterior
        UndertaleCode waterTurbineDoorCC = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_waterStationDoor_Create") };
        gmData.Code.Add(waterTurbineDoorCC);
        UndertaleRoom? rm_a2a08 = gmData.Rooms.ByName("rm_a2a08");
        rm_a2a08.GameObjects.Add(CreateRoomObject(24, 96, gmData.GameObjects.ByName("oDoor"), waterTurbineDoorCC, 1, 1, A2WaterTurbineLeftDoorID));
        UndertaleBackground? doorTileset = gmData.Backgrounds.ByName("tlDoor");
        rm_a2a08.Tiles.Add(CreateRoomTile(0, 96, -103, doorTileset, 96, 0, 32, 64));
    }
}
