using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches.geometry;

public class SoftlockPrevention
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Softlock prevention blocks
        if (seedObject.Patches.SoftlockPrevention) characterVarsCode.ReplaceGMLInCode("global.softlockPrevention = 0", "global.softlockPrevention = 1;");

        // gml_Room_rm_a3b08_Create - some shot / solid blocks in BG3
        // Also change these to chain bomb blocks
        foreach (UndertaleRoom.GameObject? go in gmData.Rooms.ByName("rm_a3b08").GameObjects.Where(go => go.ObjectDefinition.Name.Content == "oBlockShootChain"))
        {
            go.ObjectDefinition = gmData.GameObjects.ByName("oBlockBombChain");
        }

        gmData.Code.ByName("gml_Room_rm_a3b08_Create").ReplaceGMLInCode("""
                                                                            if (oControl.mod_septoggs_bombjumps_easy == 0 && global.hasBombs == 1)
                                                                            {
                                                                                with (121234)
                                                                                    instance_destroy()
                                                                                with (121235)
                                                                                    instance_destroy()
                                                                                with (121236)
                                                                                    instance_destroy()
                                                                            }
                                                                            else if (global.item[2] == 1 || global.item[6] == 1 || global.hasHijump == 1)
                                                                            {
                                                                                with (121234)
                                                                                    instance_destroy()
                                                                                with (121235)
                                                                                    instance_destroy()
                                                                                with (121236)
                                                                                    instance_destroy()
                                                                            }
                                                                            else
                                                                            {
                                                                                with (121151)
                                                                                    instance_destroy()
                                                                                tile_layer_delete_at(-105, 848, 192)
                                                                            }
                                                                        """, """
                                                                             if (global.softlockPrevention)
                                                                             {
                                                                                 with (121151)
                                                                                     instance_destroy()
                                                                                 tile_layer_delete_at(-105, 848, 192)
                                                                             }
                                                                             else
                                                                             {
                                                                                 with (121234)
                                                                                     instance_destroy()
                                                                                 with (121235)
                                                                                     instance_destroy()
                                                                                 with (121236)
                                                                                     instance_destroy()
                                                                             }
                                                                             """);

        // speed booster blocks near a5 activation
        UndertaleRoom? a5c08 = gmData.Rooms.ByName("rm_a5c08");
        foreach (UndertaleRoom.GameObject? gameObject in a5c08.GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockSpeed"))
        {
            // Y 32 is the top row of speed blocks. we need to remove am2random behaviour from them
            if (gameObject.Y == 32)
            {
                gameObject.CreationCode.ReplaceGMLInCode("""
                                                         if (oControl.mod_randomgamebool == 1 && global.hasSpeedbooster == 0)
                                                             instance_destroy()
                                                         """, "");
            }

            // X 960 are the right pillars which we want to remove.
            if (gameObject.X >= 960) gameObject.CreationCode.AppendGMLInCode("if (global.softlockPrevention) instance_destroy();");
        }

        // screw blocks in bullet hell room
        foreach (UndertaleRoom.GameObject? gameObject in gmData.Rooms.ByName("rm_a5c22").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockScrew"))
        {
            if (gameObject.X is 48 or 64)
            {
                gameObject.CreationCode.ReplaceGMLInCode("oControl.mod_previous_room == 268 && global.screwattack == 0 && global.item[scr_itemchange(8)] == 1",
                    "global.softlockPrevention");
            }
        }

        // Crumble blocks and shoot block before Ice chamber
        foreach (UndertaleRoom.GameObject? gameObject in gmData.Rooms.ByName("rm_a5c31").GameObjects.Where(o => o.ObjectDefinition.Name.Content is "oBlockStep" or "oBlockShoot"))
        {
            gameObject.CreationCode.ReplaceGMLInCode("oControl.mod_previous_room == 277 && global.ibeam == 0 && global.item[scr_itemchange(11)] == 1",
                "global.softlockPrevention");
        }

        // Crumble blocks in gravity area one way room
        foreach (UndertaleRoom.GameObject? gameObject in gmData.Rooms.ByName("rm_a5a03").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockStep"))
        {
            if (gameObject.X is 96 or 112)
            {
                gameObject.CreationCode.ReplaceGMLInCode("oControl.mod_previous_room == 298 && (global.hasGravity == 0 || global.hasSpacejump == 0)",
                    "global.softlockPrevention");
            }
        }


        // Gravity chamber access, have bottom bomb block be open
        foreach (UndertaleRoom.GameObject? gameObject in gmData.Rooms.ByName("rm_a5a06").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockBombChain"))
        {
            // Top bomb block
            if (gameObject.Y == 64)
            {
                gameObject.CreationCode.ReplaceGMLInCode("""
                                                         if (oControl.mod_randomgamebool == 1 && oControl.mod_previous_room == 301 && global.hasGravity == 0 && global.item[oControl.mod_gravity] == 1 && global.ptanks == 0)
                                                             instance_destroy()
                                                         else
                                                         """, "");
            }

            // Bottom bomb block
            if (gameObject.Y == 176) gameObject.CreationCode.AppendGMLInCode("if (global.softlockPrevention) instance_destroy();");
        }

        // Crumble blocks in plasma chamber
        gmData.Code.ByName("gml_Room_rm_a4a10_Create").AppendGMLInCode("if (global.softlockPrevention) { with (oBlockStep) instance_destroy(); }");
    }
}
