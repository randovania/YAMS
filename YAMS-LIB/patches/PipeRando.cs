using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class PipeRando
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // TODO: optimization could be made here, by letting rdv provide the room where the instance id is, thus not neeeding to crawl over every room.
        // TODO: for this (And for entrance rando) i need to go through each room, and set the correct global.darkness, global.water and music value.
        // FIXME: temporary hack to change darkness, waterlevel and music value for all pipe rooms.

        foreach (var roomName in new[]
                 {
                     "rm_a1a11",
                     "rm_a2a18",
                     "rm_a2a19",
                     "rm_a3a27",
                     "rm_a4h16",
                     "rm_a4a14",
                     "rm_a5c14",
                     "rm_a5b03",
                     "rm_a5b08",
                     "rm_a5a01",
                     "rm_a5a09",
                     "rm_a5c26",
                     "rm_a5c31",
                     "rm_a5c25",
                     "rm_a5c13",
                     "rm_a5c17",
                     "rm_a7b03B",
                     "rm_a6a11",
                     "rm_a6b03",
                     "rm_a6b11",
                     "rm_a7a07"
                 })
        {
            string musicCode = roomName switch
            {
                var s when s.StartsWith("rm_a5b") || (s.StartsWith("rm_a5c") && s != "rm_a5c14") => "if (global.event[250] > 0) mus_change(musArea5B) else mus_change(musArea5A);",
                "rm_a6b03" or "rm_a6b11" or "rm_a7a07" => "mus_change(musArea6A);",
                "rm_a6a11" => "mus_change(mus_get_main_song());",
                _ => "mus_change(musItemAmb);",
            };

            gmData.Rooms.ByName(roomName).CreationCodeId.PrependGMLInCode($"global.darkness = 0; global.waterlevel = 0; global.watertype = 0; {musicCode}");
        }


        foreach (var pipe in seedObject.PipeObjects)
        {
            foreach (UndertaleRoom? room in gmData.Rooms)
            {
                foreach (UndertaleRoom.GameObject? gameObject in room.GameObjects)
                {
                    if (gameObject.InstanceID != pipe.Key) continue;

                    gameObject.CreationCode.AppendGMLInCode($"targetx = {pipe.Value.XPosition}; targety = {pipe.Value.YPosition}; targetroom = {pipe.Value.Room};");
                }
            }
        }
    }
}
