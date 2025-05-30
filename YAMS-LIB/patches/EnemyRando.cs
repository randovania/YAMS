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
                var gameObject = gmData.Rooms.ByName(roomName).GameObjects.First(go => go.InstanceID == idToSearch);
                gameObject.ObjectDefinition = gmData.GameObjects.ByName(wishedObject);
                if (wishedObject == "oFlitt")
                {
                    var code = gameObject.CreationCode;
                    if (code is null)
                        gameObject.CreationCode = gmData.Code.AddCodeEntry($"gml_RoomCC_{roomName}_{gmData.Code.Count}", "");

                    gameObject.CreationCode.AppendGMLInCode("facing = 1");
                }

            }
        }
    }
}