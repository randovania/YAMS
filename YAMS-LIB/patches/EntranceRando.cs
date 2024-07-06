using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class EntranceRando
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        foreach (var entrance in seedObject.EntranceObjects)
        {
            foreach (UndertaleRoom? room in gmData.Rooms)
            {
                foreach (UndertaleRoom.GameObject? sourceEntrance in room.GameObjects)
                {
                    if (sourceEntrance.InstanceID != entrance.Key) continue;

                    if (entrance.Value.Direction == DoorFacingDirection.Invalid) throw new NotSupportedException("Entrance must have a valid direction");

                    var targetRoom = gmData.Rooms.ByName(entrance.Value.Room);
                    var targetEntrance = targetRoom.GameObjects.ByInstanceID(entrance.Value.DestEntranceID);

                    int xMod = 0;
                    int yMod = 0;
                    int finalDirection = 0;
                    int camYMod = 0;
                    int transX = 0;
                    int transY = 0;

                    switch (entrance.Value.Direction)
                    {
                        case DoorFacingDirection.Right:
                            xMod = 16;
                            finalDirection = 0;
                            camYMod = 120;
                            transX = (targetEntrance.X + 4) % 320;
                            transY = ((targetEntrance.Y) % 240);
                            break;
                        case DoorFacingDirection.Left:
                            xMod = -16;
                            finalDirection = 180;
                            camYMod = -120;
                            transX = (targetEntrance.X - 4) % 320;
                            transY = ((targetEntrance.Y) % 240);
                            break;
                        // At one point:tm: also implement up and down
                    }

                    int targetX = targetEntrance.X + xMod;
                    int targetY = targetEntrance.Y + yMod;

                    int finalHeight = 64; // TODO: this is a broad assumption for now, change this later.

                    int camStartX = targetEntrance.X + (xMod * 10);

                    // if less than y = 136, use y = 120, otherwise use y
                    var camStartY = targetEntrance.Y < 136 ? 120 : targetEntrance.Y;

                    sourceEntrance.CreationCode.SubstituteGMLCode($$"""
                    targetroom = {{targetRoom.Name.Content}}
                    targetx = {{targetX}}
                    targety = {{targetY}}
                    height = {{finalHeight}}
                    direction = {{finalDirection}}
                    camstartx = {{camStartX}}
                    camstarty = {{camStartY}}
                    transitionx = {{transX}}
                    transitiony = {{transY}}
                    """);
                }
            }
        }
    }
}
