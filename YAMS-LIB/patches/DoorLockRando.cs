using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB.patches;

public class DoorLockRando
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Adjust global event array to be 700
        characterVarsCode.ReplaceGMLInCode( """
            i = 350
            repeat (350)
            {
                i -= 1
                global.event[i] = 0
            }
            """, """
            i = 700
            repeat (700)
            {
                i -= 1
                global.event[i] = 0
            }
            """);
         gmData.Code.ByName("gml_Script_sv6_add_events").ReplaceGMLInCode( "350", "700");
         gmData.Code.ByName("gml_Script_sv6_get_events").ReplaceGMLInCode( "350", "700");

        // Replace every normal, a4 and a8 door with an a5 door for consistency
        var a5Door = gmData.GameObjects.ByName("oDoorA5");
        foreach (var room in gmData.Rooms)
        {
            foreach (var door in room.GameObjects.Where(go => go.ObjectDefinition.Name.Content.StartsWith("oDoor")))
            {
                door.ObjectDefinition = a5Door;
            }
        }
        // Also fix depth value for them
        a5Door.Depth = -99;


        var doorEventIndex = 350;
        foreach ((var id, var doorLock) in seedObject.DoorLocks)
        {
            bool found = false;
            foreach (var room in gmData.Rooms)
            {
                foreach (var gameObject in room.GameObjects)
                {
                    if (gameObject.InstanceID != id) continue;

                    if (!gameObject.ObjectDefinition.Name.Content.StartsWith("oDoor") && gameObject.ObjectDefinition.Name.Content != "oA2BigTurbine")
                        throw new NotSupportedException($"The 'door' instance {id} is not actually a door!");

                    found = true;
                    if (gameObject.CreationCode is null)
                    {
                        var code = new UndertaleCode() { Name = gmData.Strings.MakeString($"gml_RoomCC_{room.Name.Content}_{id}_Create") };
                        gmData.Code.Add(code);
                        gameObject.CreationCode = code;
                    }

                    string codeText = doorLock.Lock switch
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
                        _ => throw new NotSupportedException($"Door {id} has an unsupported door lock ({doorLock.Lock})!")
                    };

                    var waterTurbineObject = gmData.GameObjects.ByName("oA2BigTurbine");
                    if (gameObject.ObjectDefinition == waterTurbineObject && doorLock.Lock != DoorLockType.A2WaterTurbine)
                    {
                        gameObject.ObjectDefinition = gmData.GameObjects.ByName("oDoorA5");
                        gameObject.X += (24 * (int)gameObject.ScaleX);
                        gameObject.ScaleX *= -1;
                        bool leftFacing = gameObject.ScaleX < 0;
                        room.Tiles.Add(CreateRoomTile(gameObject.X - (leftFacing ? 8 : 24), gameObject.Y, -110, gmData.Backgrounds.ByName("tlDoor"), (leftFacing ? 0u : 32u), 0, 32, 64));
                        var tilesToDelete = room.Tiles.Where((t => (t is { X: 912, Y: 1584, SourceX: 48, SourceY: 304 } or { X: 928, Y: 1536, SourceX: 96, SourceY: 304 }))).ToList();
                        foreach (var tile in tilesToDelete)
                            room.Tiles.Remove(tile);
                    }

                    if (gameObject.ObjectDefinition != waterTurbineObject && doorLock.Lock == DoorLockType.A2WaterTurbine)
                    {
                        gameObject.ObjectDefinition = waterTurbineObject;
                        gameObject.X += (24 * (int)gameObject.ScaleX);
                        gameObject.ScaleX *= -1;
                        if ((gameObject.X - 48) == 0)
                            room.GameObjects.Add(CreateRoomObject(gameObject.X-72, gameObject.Y, gmData.GameObjects.ByName("oSolid1x4")));
                        else if ((gameObject.X + 48) == room.Width)
                            room.GameObjects.Add(CreateRoomObject(gameObject.X+72, gameObject.Y, gmData.GameObjects.ByName("oSolid1x4")));

                    }

                    gameObject.CreationCode.AppendGMLInCode( codeText);
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
