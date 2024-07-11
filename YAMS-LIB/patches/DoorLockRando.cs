using System.Net.Http.Headers;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB.patches;

public class DoorLockRando
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject, bool isHorde)
    {
        var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Adjust global event array to be 900
        characterVarsCode.ReplaceGMLInCode( $$"""
            i = {{(!isHorde ? "350" : "400")}}
            repeat ({{(!isHorde ? "350" : "400")}})
            {
                i -= 1
                global.event[i] = 0
            }
            """, """
            i = 900
            repeat (900)
            {
                i -= 1
                global.event[i] = 0
            }
            """);
         gmData.Code.ByName("gml_Script_sv6_add_events").ReplaceGMLInCode( "350", "900");
         gmData.Code.ByName("gml_Script_sv6_get_events").ReplaceGMLInCode( "350", "900");

         // Fix Tower activation unlocking right door for door lock rando
         var rightTowerDoorID = gmData.Rooms.ByName("rm_a4a04").GameObjects.First(go => go.X == 616 && go.Y == 80 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
         if (seedObject.DoorLocks.ContainsKey(rightTowerDoorID)) gmData.Code.ByName("gml_Object_oArea4PowerSwitch_Step_0").ReplaceGMLInCode("lock = 0", "lock = lock;");

         // Make water turbine generic where it can be shuffled
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Create_0").PrependGMLInCode("facingDirection = 1; if (image_xscale < 0) facingDirection = -1; wasAlreadyDestroyed = 0;");
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Create_0").ReplaceGMLInCode("""
                                                                                 if (global.event[101] > 0)
                                                                                     instance_destroy()
                                                                                 """,
            """
            eventToSet = 101;
            if (((((global.targetx - (32 * facingDirection)) == x) && ((global.targety - 64) == y))) ||
                (room == rm_a2h02 && x == 912 && y == 1536 && global.event[101] != 0))
            {
                if (global.event[eventToSet] < 1)
                    global.event[eventToSet] = 1;
                wasAlreadyDestroyed = 1;
                instance_destroy();
            }
            """);
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Create_0").ReplaceGMLInCode("wall = instance_create((x + 16), y, oSolid1x4)",
            "var xWallOffset = 16; if (facingDirection == -1) xWallOffset = -32; wall = instance_create(x + xWallOffset, y, oSolid1x4);");
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Other_11").ReplaceGMLInCode(
            """
            o = instance_create(x, y, oMoveWater)
            o.targety = 1552
            o.delay = 2
            global.event[101] = 1
            instance_create((x - 120), y, oBubbleSpawner)
            """,
            """
            global.event[eventToSet] = 1;
            if (room == rm_a2h02 && x == 912 && y == 1536 && global.event[101] == 1)
            {
                o = instance_create(x, y, oMoveWater)
                o.targety = 1552
                o.delay = 2
                instance_create((x - 120), y, oBubbleSpawner)
            }
            """);

        // Replace every normal, a4 and a8 door with an a5 door for consistency
        var a5Door = gmData.GameObjects.ByName("oDoorA5");
        foreach (var room in gmData.Rooms)
        {
            foreach (var door in room.GameObjects.Where(go => go.ObjectDefinition is not null && go.ObjectDefinition.Name.Content.StartsWith("oDoor")))
            {
                door.ObjectDefinition = a5Door;
            }
        }
        // Also fix depth value for them
        a5Door.Depth = -99;

        // Remove foreground tiles in water turbine station and the alpha room to make doors more readable, and then fix the black tile holes
        var pipes3 = gmData.Backgrounds.ByName("tlPipes3");
        foreach (string roomName in new[] {"rm_a2a08", "rm_a2a09"})
        {
            var room = gmData.Rooms.ByName(roomName);
            var tilesToRemove = roomName switch
            {
                "rm_a2a08" => room.Tiles.Where(t => t.BackgroundDefinition == pipes3 && (t.X is 288 or 336)),
                "rm_a2a09" => room.Tiles.Where(t => t.BackgroundDefinition == pipes3 && t.X == 0),
                _ => throw new InvalidOperationException("Source code was changed to not account for new rooms?")
            };
            foreach (var tile in tilesToRemove.ToList())
            {
                room.Tiles.Remove(tile);
            }

            if (roomName == "rm_a2a08")
            {
                room.Tiles.Add(CreateRoomTile(288, 384, 100, pipes3, 0, 48, 16, 64));
            }
            else if (roomName == "rm_a2a09")
            {
                room.Tiles.Add(CreateRoomTile(0, 144, 100, pipes3, 0, 48, 16, 64));
            }
        }


        var doorEventIndex = 350;
        foreach ((var id, var doorEntry) in seedObject.DoorLocks)
        {
            bool found = false;
            foreach (var room in gmData.Rooms)
            {
                foreach (var gameObject in room.GameObjects)
                {
                    if (gameObject.InstanceID != id) continue;

                    UndertaleGameObject goToRoomObject = gmData.GameObjects.ByName("oGotoRoom");
                    bool isGotoObject = gameObject.ObjectDefinition == goToRoomObject;
                    bool isResearchHatch = gameObject.ObjectDefinition.Name.Content == "oA3LabDoor";
                    if (isGotoObject && !doorEntry.isDock)
                    {
                        throw new NotSupportedException($"The instance id {id} is a GotoRoom object, but the setting whether this instance is a dock is set to false!");
                    }

                    if (!gameObject.ObjectDefinition.Name.Content.StartsWith("oDoor") && gameObject.ObjectDefinition.Name.Content != "oA2BigTurbine" && !isGotoObject &&
                        !isResearchHatch)
                    {
                        throw new NotSupportedException($"The 'door' instance {id} is not actually a door!");
                    }

                    UndertaleRoom.GameObject door = gameObject;
                    if (isGotoObject)
                    {
                        // Place tiles
                        int tileDepth = -80;
                        var doorTileset = gmData.Backgrounds.ByName("tlDoorsExtended");
                        room.Tiles.Add(CreateRoomTile(gameObject.X - (doorEntry.FacingDirection == DoorFacingDirection.Left ? 32 : 0), gameObject.Y-64, tileDepth, doorTileset, doorEntry.FacingDirection == DoorFacingDirection.Left ? (uint)0 : 128, 96, 32, 64));

                        // Extend the tiles if goto object is on the edge of room or on special cases
                        // Top transition in waterfalls entryway
                        uint waterfallID = gmData.Rooms.ByName("rm_a6b17").GameObjects.First(go => go.X == 320 && go.Y == 144 && go.ObjectDefinition == goToRoomObject).InstanceID;
                        // Top transition in hideout alpha nest
                        uint hideoutAlphaID = gmData.Rooms.ByName("rm_a6a09").GameObjects.First(go => go.X == 1280 && go.Y == 176 && go.ObjectDefinition == goToRoomObject)
                            .InstanceID;
                        // Bottom transition in Skreek Street
                        uint skreekStreetID = gmData.Rooms.ByName("rm_a0h30").GameObjects.First(go => go.X == 960 && go.Y == 848 && go.ObjectDefinition == goToRoomObject)
                            .InstanceID;
                        // Bottom transition in Grave Grotto
                        uint graveGrottoID = gmData.Rooms.ByName("rm_a0h07").GameObjects.First(go => go.X == 640 && go.Y == 1600 && go.ObjectDefinition == goToRoomObject)
                            .InstanceID;

                        bool shouldExtendTiles = false;
                        if (gameObject.X == 0 || gameObject.X == room.Width)
                            shouldExtendTiles = true;
                        else if (door.InstanceID == waterfallID || door.InstanceID == hideoutAlphaID || door.InstanceID == skreekStreetID || door.InstanceID == graveGrottoID)
                            shouldExtendTiles = true;

                        int maxIteration = 5;
                        if (door.InstanceID == waterfallID)
                            maxIteration = 8;

                        if (shouldExtendTiles)
                        {
                            for (int i = 1; i <= maxIteration; i++)
                            {
                                int tilesetCounter = i + (doorEntry.FacingDirection == DoorFacingDirection.Right ? 0 : 1);
                                room.Tiles.Add(CreateRoomTile(gameObject.X - (doorEntry.FacingDirection == DoorFacingDirection.Right ? 0 : 32) - (16 * i * (doorEntry.FacingDirection == DoorFacingDirection.Right ? 1 : -1)), gameObject.Y-64, tileDepth, doorTileset, tilesetCounter % 2 == 0 ? (uint)32 : 16, 96, 16, 80));
                            }
                        }

                        // Place door
                        door = CreateRoomObject(gameObject.X - ((doorEntry.FacingDirection == DoorFacingDirection.Left ? 1 : -1) * 24), gameObject.Y-64, gmData.GameObjects.ByName("oDoorA5"), null, doorEntry.FacingDirection == DoorFacingDirection.Left ? -1 : 1);
                        room.GameObjects.Add(door);
                    }

                    found = true;
                    if (door.CreationCode is null)
                    {
                        var code = new UndertaleCode() { Name = gmData.Strings.MakeString($"gml_RoomCC_{room.Name.Content}_{id}_Create") };
                        gmData.Code.Add(code);
                        door.CreationCode = code;
                    }

                    var doorObject = gmData.GameObjects.ByName("oDoorA5");
                    var waterTurbineObject = gmData.GameObjects.ByName("oA2BigTurbine");
                    var researchHatchObject = gmData.GameObjects.ByName("oA3LabDoor");

                    string codeText = doorEntry.Lock switch
                    {
                        DoorLockType.Normal => "lock = 0; event = -1;",
                        DoorLockType.Missile => $"lock = 1; originalLock = lock; event = {doorEventIndex};",
                        DoorLockType.SuperMissile => $"lock = 2; originalLock = lock; event = {doorEventIndex};",
                        DoorLockType.PBomb => $"lock = 3; originalLock = lock; event = {doorEventIndex};",
                        DoorLockType.TempLocked => $"lock = 4; originalLock = lock; event = -1;",
                        DoorLockType.Charge => $"lock = 5; originalLock = lock; event = -1;",
                        DoorLockType.Wave => $"lock = 6; originalLock = lock; event = -1;",
                        DoorLockType.Spazer => $"lock = 7; originalLock = lock; event = -1;",
                        DoorLockType.Plasma => $"lock = 8; originalLock = lock; event = -1;",
                        DoorLockType.Ice => $"lock = 9; originalLock = lock; event = -1;",
                        DoorLockType.Bomb => "lock = 10; originalLock = lock; event = -1;",
                        DoorLockType.Spider => "lock = 11; originalLock = lock; event = -1;",
                        DoorLockType.Screw => "lock = 12; originalLock = lock; event = -1;",
                        DoorLockType.TowerEnabled => "lock = 13; originalLock = lock; event = -1;",
                        DoorLockType.TesterDead => "lock = 14; originalLock = lock; event = -1;",
                        DoorLockType.GuardianDead => "lock = 15; originalLock = lock; event = -1;",
                        DoorLockType.ArachnusDead => "lock = 16; originalLock = lock; event = -1;",
                        DoorLockType.TorizoDead => "lock = 17; originalLock = lock; event = -1;",
                        DoorLockType.SerrisDead => "lock = 18; originalLock = lock; event = -1;",
                        DoorLockType.GenesisDead => "lock = 19; originalLock = lock; event = -1;",
                        DoorLockType.QueenDead => "lock = 20; originalLock = lock; event = -1;",
                        DoorLockType.EMPActivated => "lock = 21; originalLock = lock; event = -1;",
                        DoorLockType.EMPA1 => "lock = 22; originalLock = lock; event = -1;",
                        DoorLockType.EMPA2 => "lock = 23; originalLock = lock; event = -1;",
                        DoorLockType.EMPA3 => "lock = 24; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5Tutorial => "lock = 25; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5RobotHome => "lock = 26; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5NearZeta => "lock = 27; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5BulletHell => "lock = 28; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5PipeHub => "lock = 29; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5RightExterior => "lock = 30; originalLock = lock; event = -1;",
                        DoorLockType.Locked => "lock = 31; originalLock = lock; event = -1;",
                        DoorLockType.A2WaterTurbine => $"eventToSet = {doorEventIndex};" +
                                                       $"if (global.event[eventToSet] > 0)" +
                                                       $"{{ if (!wasAlreadyDestroyed) {{ with (wall) instance_destroy(); }} instance_destroy();}} " +
                                                       $"if (wasAlreadyDestroyed && global.event[eventToSet] < 1) global.event[eventToSet] = 1;",
                        DoorLockType.ResearchHatch => door.ObjectDefinition != researchHatchObject ? $"facing = {(door.ScaleX >= 0 ? 1 : -1)}" : door.CreationCode.GetGMLCode(),
                        _ => throw new NotSupportedException($"Door {id} has an unsupported door lock ({doorEntry.Lock})!")
                    };

                    if (door.ObjectDefinition == researchHatchObject && doorEntry.Lock != DoorLockType.ResearchHatch)
                    {
                        var hatchCode = door.CreationCode.GetGMLCode();
                        bool flipped = hatchCode.Contains("facing = -1");
                        if (flipped)
                        {
                            door.ScaleX *= -1;
                            door.X += 16;
                        }

                        // Move door to be more visible and place tiles
                        door.X -= 16 * (flipped ? 1 : -1);
                        var doorTileset = gmData.Backgrounds.ByName("tlDoor");
                        room.Tiles.Add(CreateRoomTile(gameObject.X - (flipped ? 8 : 24), gameObject.Y, -95, doorTileset, flipped ? (uint)64 : 96, 0, 32, 64));

                    }

                    if (door.ObjectDefinition != researchHatchObject && doorEntry.Lock == DoorLockType.ResearchHatch)
                    {
                        if (door.ScaleX < 0)
                        {
                            door.ScaleX = Math.Abs(door.ScaleX);
                            door.X -= 16;
                        }
                    }

                    if (door.ObjectDefinition == waterTurbineObject && doorEntry.Lock != DoorLockType.A2WaterTurbine)
                    {
                        door.X += (24 * (int)door.ScaleX);
                        door.ScaleX *= -1;
                        bool leftFacing = door.ScaleX < 0;
                        room.Tiles.Add(CreateRoomTile(door.X - (leftFacing ? 8 : 24), door.Y, -110, gmData.Backgrounds.ByName("tlDoor"), (leftFacing ? 0u : 32u), 0, 32, 64));
                        var tilesToDelete = room.Tiles.Where((t => (t is { X: 912, Y: 1584, SourceX: 48, SourceY: 304 } or { X: 928, Y: 1536, SourceX: 96, SourceY: 304 }))).ToList();
                        foreach (var tile in tilesToDelete)
                            room.Tiles.Remove(tile);
                    }

                    if (door.ObjectDefinition != waterTurbineObject && doorEntry.Lock == DoorLockType.A2WaterTurbine)
                    {
                        int movingOffset = door.ObjectDefinition == researchHatchObject ? 32 : 24;
                        door.X += (movingOffset * (int)door.ScaleX);
                        door.ScaleX *= -1;
                        if ((door.X - 48) == 0)
                            room.GameObjects.Add(CreateRoomObject(door.X-72, door.Y, gmData.GameObjects.ByName("oSolid1x4")));
                        else if ((door.X + 48) == room.Width)
                            room.GameObjects.Add(CreateRoomObject(door.X+72, door.Y, gmData.GameObjects.ByName("oSolid1x4")));

                    }

                    if (doorEntry.Lock == DoorLockType.ResearchHatch)
                        door.ObjectDefinition = researchHatchObject;
                    else if (doorEntry.Lock == DoorLockType.A2WaterTurbine)
                        door.ObjectDefinition = waterTurbineObject;
                    else
                        door.ObjectDefinition = doorObject;

                    door.CreationCode.AppendGMLInCode(codeText);
                    doorEventIndex++;
                    break;
                }

                if (found) break;
            }

            if (!found)
                throw new NotSupportedException($"There is no door with ID {id}!");
        }
    }
}
