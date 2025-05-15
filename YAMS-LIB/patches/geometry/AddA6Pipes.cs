using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;
namespace YAMS_LIB.patches.geometry;

public class AddA6Pipes
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        const uint PipeInHideoutID = 400003;
        const uint PipeInDepthsLowerID = 400004;
        const uint PipeInDepthsUpperID = 400005;
        const uint PipeInWaterfallsID = 400006;

        // Hideout
        UndertaleRoom? hideoutPipeRoom = gmData.Rooms.ByName("rm_a6a11");
        UndertaleBackground? hideoutPipeTileset = gmData.Backgrounds.ByName("tlWarpDepthsEntrance");
        UndertaleBackground? depthsEntrancePipeTileset = gmData.Backgrounds.ByName("tlWarpHideout");
        UndertaleBackground? depthsExitPipeTileset = gmData.Backgrounds.ByName("tlWarpWaterfall");
        UndertaleBackground? waterfallsPipeTileset = gmData.Backgrounds.ByName("tlWarpDepthsExit");
        UndertaleBackground? pipeBGTileset = gmData.Backgrounds.ByName("tlWarpPipes");
        UndertaleGameObject? solidObject = gmData.GameObjects.ByName("oSolid1");
        UndertaleGameObject? pipeObject = gmData.GameObjects.ByName("oWarpPipeTrigger");
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 176, 100, hideoutPipeTileset, 0, 48, 48, 48));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 176, -101, hideoutPipeTileset, 32, 0));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(368, 176, -101, hideoutPipeTileset, 48, 32));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(384, 176, -101, hideoutPipeTileset, 16, 0));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 192, -101, hideoutPipeTileset, 0, 32));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 208, -101, hideoutPipeTileset, 32, 16));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(368, 208, -101, hideoutPipeTileset, 48, 48));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(384, 208, -101, hideoutPipeTileset, 16, 16));
        hideoutPipeRoom.Tiles.Add(CreateRoomTile(360, 80, 100, pipeBGTileset, 0, 32, 32, 96));

        hideoutPipeRoom.GameObjects.Add(CreateRoomObject(352, 176, solidObject, null, 3));
        hideoutPipeRoom.GameObjects.Add(CreateRoomObject(352, 192, solidObject));
        hideoutPipeRoom.GameObjects.Add(CreateRoomObject(352, 208, solidObject, null, 3));

        var hideoutPipeCode = gmData.Code.AddCodeEntry("gml_RoomCC_rm_a6a11_pipe_Create", "targetroom = 327; targetx = 216; targety = 400; direction = 90;");
        hideoutPipeRoom.GameObjects.Add(CreateRoomObject(368, 192, pipeObject, hideoutPipeCode, 1, 1, PipeInHideoutID));
        hideoutPipeRoom.CreationCodeId.AppendGMLInCode("global.darkness = 0; mus_change(mus_get_main_song());");

        // Nest
        UndertaleRoom? nestPipeRoom = gmData.Rooms.ByName("rm_a6b03");
        nestPipeRoom.Tiles.Add(CreateRoomTile(192, 368, 100, depthsEntrancePipeTileset, 0, 48, 48, 48));
        nestPipeRoom.Tiles.Add(CreateRoomTile(192, 368, -101, depthsEntrancePipeTileset, 0, 0));
        nestPipeRoom.Tiles.Add(CreateRoomTile(208, 368, -101, depthsEntrancePipeTileset, 48, 32));
        nestPipeRoom.Tiles.Add(CreateRoomTile(224, 368, -101, depthsEntrancePipeTileset, 48, 0));
        nestPipeRoom.Tiles.Add(CreateRoomTile(224, 384, -101, depthsEntrancePipeTileset, 16, 32));
        nestPipeRoom.Tiles.Add(CreateRoomTile(192, 400, -101, depthsEntrancePipeTileset, 0, 16));
        nestPipeRoom.Tiles.Add(CreateRoomTile(208, 400, -101, depthsEntrancePipeTileset, 48, 48));
        nestPipeRoom.Tiles.Add(CreateRoomTile(224, 400, -101, depthsEntrancePipeTileset, 48, 16));
        //nestPipeRoom.Tiles.Add(CreateRoomTile(360, 80, 100, pipeBGTileset, 0, 32, 32, 96));

        nestPipeRoom.GameObjects.Add(CreateRoomObject(192, 368, solidObject, null, 3));
        nestPipeRoom.GameObjects.Add(CreateRoomObject(224, 384, solidObject));
        nestPipeRoom.GameObjects.Add(CreateRoomObject(192, 400, solidObject, null, 3));

        nestPipeRoom.CreationCodeId.AppendGMLInCode("mus_change(musArea6A)");

        UndertaleCode nestPipeCode = gmData.Code.AddCodeEntry("gml_RoomCC_rm_a6b03_pipe_Create", "targetroom = 317; targetx = 376; targety = 208; direction = 270;");
        nestPipeRoom.GameObjects.Add(CreateRoomObject(208, 384, pipeObject, nestPipeCode, 1, 1, PipeInDepthsLowerID));

        // Change slope to solid to prevent oob issue
        nestPipeRoom.GameObjects.First(o => o.X == 176 && o.Y == 416).ObjectDefinition = solidObject;

        // Add shortcut between Depths and Waterfalls
        // Depths
        UndertaleRoom? depthsPipeRoom = gmData.Rooms.ByName("rm_a6b11");
        depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 160, 100, depthsExitPipeTileset, 0, 48, 48, 48));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 160, -101, depthsExitPipeTileset, 32, 0));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(96, 160, -101, depthsExitPipeTileset, 48, 32));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(112, 160, -101, depthsExitPipeTileset, 16, 0));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 176, -101, depthsExitPipeTileset, 0, 32));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 192, -101, depthsExitPipeTileset, 32, 16));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(96, 192, -101, depthsExitPipeTileset, 48, 48));
        depthsPipeRoom.Tiles.Add(CreateRoomTile(112, 192, -101, depthsExitPipeTileset, 16, 16));
        //depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 80, 100, pipeBGTileset, 0, 32, 32, 96));

        // Clean up some tiles/collision
        depthsPipeRoom.Tiles.Remove(depthsPipeRoom.Tiles.First(t => t.X == 112 && t.Y == 160));
        depthsPipeRoom.Tiles.Remove(depthsPipeRoom.Tiles.First(t => t.X == 80 && t.Y == 192));
        depthsPipeRoom.GameObjects.First(o => o.X == 96 && o.Y == 208).ObjectDefinition = solidObject;

        depthsPipeRoom.GameObjects.Add(CreateRoomObject(80, 160, solidObject, null, 3));
        depthsPipeRoom.GameObjects.Add(CreateRoomObject(80, 176, solidObject));
        depthsPipeRoom.GameObjects.Add(CreateRoomObject(80, 192, solidObject, null, 3));

        depthsPipeRoom.CreationCodeId.AppendGMLInCode("mus_change(musArea6A);");

        UndertaleCode depthsPipeCode = gmData.Code.AddCodeEntry("gml_RoomCC_rm_a6b11_pipe_Create", "targetroom = 348; targetx = 904; targety = 208; direction = 180;");
        depthsPipeRoom.GameObjects.Add(CreateRoomObject(96, 176, pipeObject, depthsPipeCode, 1, 1, PipeInDepthsUpperID));

        // Waterfalls
        UndertaleRoom? waterfallsPipeRoom = gmData.Rooms.ByName("rm_a7a07");
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 176, 100, waterfallsPipeTileset, 0, 48, 48, 48));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 176, -101, waterfallsPipeTileset, 0, 0));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(896, 176, -101, waterfallsPipeTileset, 48, 32));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(912, 176, -101, waterfallsPipeTileset, 48, 0));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(912, 192, -101, waterfallsPipeTileset, 16, 32));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 208, -101, waterfallsPipeTileset, 0, 16));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(896, 208, -101, waterfallsPipeTileset, 48, 48));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(912, 208, -101, waterfallsPipeTileset, 48, 16));
        //nestPipeRoom.Tiles.Add(CreateRoomTile(360, 80, 100, pipeBGTileset, 0, 32, 32, 96));

        // Clean up some tiles/collision
        waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 912 && t.Y == 192));
        waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 912 && t.Y == 208));
        waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 896 && t.Y == 192));
        waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 880 && t.Y == 192));
        waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 224, -100, gmData.Backgrounds.ByName("tlRock7A"), 0, 32, 32));

        waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(880, 176, solidObject, null, 3));
        waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(912, 192, solidObject));
        waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(880, 208, solidObject, null, 3));

        UndertaleCode waterfallsPipeCode = gmData.Code.AddCodeEntry("gml_RoomCC_rm_a7a07_pipe_Create", "targetroom = 335; targetx = 104; targety = 192; direction = 0;");
        waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(896, 192, pipeObject, waterfallsPipeCode, 1, 1, PipeInWaterfallsID));

        waterfallsPipeRoom.CreationCodeId.AppendGMLInCode("global.darkness = 0");

        // Modify minimap for new pipes and purple in nest and waterfalls too
        // Hideout
        gmData.Code.ByName("gml_Script_map_init_04").ReplaceGMLInCode(@"global.map[21, 53] = ""1210100""", @"global.map[21, 53] = ""12104U0""");
        gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[20, 53] = ""1012100""", @"global.map[20, 53] = ""1012400""");
        // Depths lower
        gmData.Code.ByName("gml_Script_map_init_04").ReplaceGMLInCode("global.map[21, 44] = \"1102100\"\nglobal.map[21, 45] = \"0112100\"",
            "global.map[21, 44] = \"1102400\"\nglobal.map[21, 45] = \"01124D0\"");
        // Depths upper
        gmData.Code.ByName("gml_Script_map_init_02").ReplaceGMLInCode(@"global.map[16, 34] = ""1012100""", @"global.map[16, 34] = ""10124L0""");
        gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[17, 34] = ""1010100""", @"global.map[17, 34] = ""1010400""");
        gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[18, 34] = ""1020100""", @"global.map[18, 34] = ""1020400""");
        gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[19, 34] = ""1010100""", @"global.map[19, 34] = ""1010400""");
        gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[20, 34] = ""1210100""", @"global.map[20, 34] = ""1210400""");
        // Waterfalls
        gmData.Code.ByName("gml_Script_map_init_01").ReplaceGMLInCode(@"global.map[7, 34] = ""1012200""", @"global.map[7, 34] = ""1012400""");
        gmData.Code.ByName("gml_Script_map_init_17").ReplaceGMLInCode(@"global.map[8, 34] = ""1010200""", @"global.map[8, 34] = ""1010400""");
        gmData.Code.ByName("gml_Script_map_init_01").ReplaceGMLInCode(@"global.map[9, 34] = ""1010200""", @"global.map[9, 34] = ""10104R0""");
        gmData.Code.ByName("gml_Script_map_init_02").ReplaceGMLInCode(@"global.map[10, 34] = ""1210200""", @"global.map[10, 34] = ""1210400""");
    }
}
