using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class EnemyRando
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        foreach(var (roomName, roomEntry) in seedObject.RoomObjects)
        {
            foreach (var (idToSearch, wishedObject) in roomEntry.ChangeInstanceIDs)
            {
                gmData.Rooms.ByName(roomName).GameObjects.First(go => go.InstanceID == idToSearch).ObjectDefinition = gmData.GameObjects.ByName(wishedObject);

            }
        }
    }
}