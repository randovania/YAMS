using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class HistoryLogEntry
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        StringBuilder dsMapCordBuilder = new StringBuilder("roomNameDict = ds_map_create()");
        foreach ((string roomName, RoomObject value) in seedObject.RoomObjects)
        {
            dsMapCordBuilder.Append($"ds_map_add(roomNameDict, \"{roomName}\", \"{value.RegionName} - {value.DisplayName}\");\n");
        }
        string DSMapCoordRoomname = dsMapCordBuilder.ToString();


        gmData.Code.ByName("gml_Object_oControl_Create_0").AppendGMLInCode(DSMapCoordRoomname);

        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        characterVarsCode.PrependGMLInCode($"global.historyLogEntryText = \"{(seedObject.Identifier.StartingMemoText is null ? "" : seedObject.Identifier.StartingMemoText.Description + " as extra starting items.#")}\"");
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("instance_destroy()", "var roomName = ds_map_find_value(oControl.roomNameDict, room_get_name(room)); if (roomName == undefined) roomName = \"ERROR!!!\"; global.historyLogEntryText = (\"{yellow}\" + string(itemName) + \"{white} was found in {fuchsia}\" + roomName + \"{white}#\") + global.historyLogEntryText; instance_destroy();");

        gmData.Code.ByName("gml_Script_load_logs_list").ReplaceGMLInCode("get_text(\"Logs\", \"Briefing\")", "\"Item History\"");
        gmData.Code.ByName("gml_Script_load_logs_list").ReplaceGMLInCode("get_text(\"Logs\", \"Briefing_Text\")", "\"These are the items you have found so far:#\" + global.historyLogEntryText");
    }
}
