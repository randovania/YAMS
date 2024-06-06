using System.Globalization;
using System.Reflection;
using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using YAMS_LIB.patches;
using YAMS_LIB.patches.decompilerWorkarounds;
using YAMS_LIB.patches.geometry;
using YAMS_LIB.patches.misc;
using YAMS_LIB.patches.qol;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB;

public class Patcher
{
    public static string Version = CreateVersionString();
    internal static UndertaleData? gmData;
    internal static GlobalDecompileContext? decompileContext;

    private static string CreateVersionString()
    {
        Version? assembly = Assembly.GetExecutingAssembly().GetName().Version;
        if (assembly is null) return "";

        return $"{assembly.Major}.{assembly.Minor}.{assembly.Build}";
    }

    public static void Main(string am2rPath, string outputAm2rPath, string jsonPath)
    {
        // TODO: go through all the patches, remove *all* instance/creation code ID references

        // Change this to not have to deal with floating point madness
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        SeedObject? seedObject = JsonSerializer.Deserialize<SeedObject>(File.ReadAllText(jsonPath));

        // TODO: lots of code cleanup and sanity checking

        // TODO: make insanity save stations enabled again by using jes' code

        // Read 1.5.x data
        gmData = new UndertaleData();

        using (FileStream fs = new FileInfo(am2rPath).OpenRead())
        {
            gmData = UndertaleIO.Read(fs);
        }

        Console.WriteLine("Read data file.");
        decompileContext = new GlobalDecompileContext(gmData, false);

        // Check for 1.5.5 before doing *anything*
        string controlCreate = gmData.Code.ByName("gml_Object_oControl_Create_0").GetGMLCode();
        bool useAnyVersion = Environment.GetEnvironmentVariable("YAMS_USE_ANY_AM2R_VERSION") == "true";
        if (!useAnyVersion && !controlCreate.Contains("global.am2r_version = \"V1.5.5\"")) throw new InvalidAM2RVersionException("The selected game is not AM2R 1.5.5!");

        // Important invasive modifications done first
        // Fixes character step event for further modification
        FixCharacterStepEvent.Apply(gmData, decompileContext, seedObject);

        // Decouple the item locations from the actual items
        DecoupleItemsFromLocations.Apply(gmData, decompileContext, seedObject);

        // Import new Sprites
        Sprites.Apply(gmData, decompileContext, seedObject);
        // Apply cosmetic patches - TODO: run this in parallel?
        CosmeticHud.Apply(gmData, decompileContext, seedObject);
        // Shuffle Music - TODO: run this in parallel?
        MusicShuffle.ShuffleMusic(Path.GetDirectoryName(outputAm2rPath), seedObject.Cosmetics.MusicShuffleDict);

        // Fix songs that break if they're too long
        FixOverlappingSongs.Apply(gmData, decompileContext, seedObject);


        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Remove other game modes, rename "normal" to "Randovania"
        UndertaleCode? gameSelMenuStepCode = gmData.Code.ByName("gml_Object_oGameSelMenu_Step_0");
        gameSelMenuStepCode.ReplaceGMLInCode("if (global.mod_gamebeaten == 1)", "if (false)");
        gmData.Code.ByName("gml_Object_oSlotMenu_normal_only_Create_0").ReplaceGMLInCode(
            "d0str = get_text(\"Title-Additions\", \"GameSlot_NewGame_NormalGame\")", "d0str = \"Randovania\";");

        // Add Credits
        gmData.Code.ByName("gml_Object_oCreditsText_Create_0").ReplaceGMLInCode("/Japanese Community;;;;",
            "/Japanese Community;;;*AM2R Randovania Credits;;*Development;Miepee=JesRight;;*Logic Database;Miepee=JeffGainsNGames;/Esteban 'DruidVorse' Criado;;*Art;ShirtyScarab=AbyssalCreature;;/With contributions from many others;;;");

        // Fix crashes where rooms try to access these vars in the starting room
        gmData.Code.ByName("gml_Script_set_start_location").AppendGMLInCode("global.targetx = global.save_x; global.targety = global.save_y;");

        // Fix visual edge case discrepancy with time of day
        gmData.Code.ByName("gml_Room_rm_a8h01_Create").PrependGMLInCode("global.timeofday = 1;");

        // Unlock fusion etc. by default
        UndertaleCode? unlockStuffCode = gmData.Code.ByName("gml_Object_oControl_Other_2");
        unlockStuffCode.AppendGMLInCode("global.mod_fusion_unlocked = 1; global.mod_gamebeaten = 1;");
        gmData.Code.ByName("gml_Object_oSS_Fg_Create_0").AppendGMLInCode("itemcollunlock = 1;");

        // Make fusion only a damage multiplier, leaving the fusion stuff up to a setting
        gmData.Code.ByName("gml_Object_oControl_Step_0").PrependGMLInCode("mod_fusion = 0;");

        // Fix varia cutscene
        gmData.Code.ByName("gml_Object_oSuitChangeFX_Step_0").ReplaceGMLInCode("bg1alpha = 0", "bg1alpha = 0; instance_create(x, y, oSuitChangeFX2);");
        gmData.Code.ByName("gml_Object_oSuitChangeFX2_Create_0").ReplaceGMLInCode("image_index = 1133", "sprite_index = sSuitChangeFX2_fusion");

        DontDespawnWeaponsOffscreen.Apply(gmData, decompileContext, seedObject);

        // Fix arachnus event value doing x coordinate BS
        gmData.Code.ByName("gml_Object_oArachnus_Alarm_11").ReplaceGMLInCode("global.event[103] = x", "global.event[103] = 1");
        // Make arachnus item location always spawn in center
        gmData.Code.ByName("gml_Room_rm_a2a04_Create").ReplaceGMLInCode("instance_create(global.event[103]", "instance_create(room_width / 2");


        // Killing queen should not lock you out of the rest of the game
        gmData.Code.ByName("gml_RoomCC_rm_a0h01_3762_Create").AppendGMLInCode("instance_destroy()");
        gmData.Code.ByName("gml_Room_rm_a0h01_Create").AppendGMLInCode("tile_layer_delete(-119)");

        // Killing Queen spawns pickups (helps prevent softlocks with DLR)
        gmData.Code.ByName("gml_Object_oQueen_Other_20").AppendGMLInCode("spawn_many_powerups(x, y-70, 100, 60)");

        // Make Logbook colored
        ColoredLogBook.Apply(gmData, decompileContext, seedObject);

        // For pause menu, draw now the same as equipment menu because doing determining what max total health/missiles/etc. are would be spoilery and insane to figure out
        UndertaleCode? ssDraw = gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0");
        ssDraw.ReplaceGMLInCode("(string(global.etanks) + \"/10\")", "( string(ceil(global.playerhealth)) + \"/\" + string(global.maxhealth) )");
        ssDraw.ReplaceGMLInCode("(string(global.mtanks) + \"/44\")", "( string(global.missiles) + \"/\" + string(global.maxmissiles) )");
        ssDraw.ReplaceGMLInCode("(string(global.stanks) + \"/10\")", "( string(global.smissiles) + \"/\" + string(global.maxsmissiles) )");
        ssDraw.ReplaceGMLInCode(" (string(global.ptanks) + \"/10\")", "( string(global.pbombs) + \"/\" + string(global.maxpbombs) )");
        foreach (string? code in new[] { ssDraw.Name.Content, "gml_Script_scr_SubScrTop_swap", "gml_Script_scr_SubScrTop_swap2" })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.stanks > 0", "true");
            gmData.Code.ByName(code).ReplaceGMLInCode("global.ptanks > 0", "true");
        }

        // Make doors automatically free their event when passing through them!...
        gmData.Code.ByName("gml_Object_oDoor_Alarm_0").ReplaceGMLInCode("event_user(2)",
            "{ event_user(2); if(event > 0 && lock < 4) global.event[event] = 1; }");
        // ...But don't make them automatically opened for non-ammo doors!
        gmData.Code.ByName("gml_Object_oDoor_Alarm_0").ReplaceGMLInCode("lock = 0", "if (lock < 4) lock = 0;");

        // Make doors when unlocked, go to the type they were before except for ammo doors
        gmData.Code.ByName("gml_Object_oDoor_Create_0").AppendGMLInCode("originalLock = lock;");
        gmData.Code.ByName("gml_Object_oDoor_Other_13").ReplaceGMLInCode("lock = 0", "lock = originalLock; if (originalLock < 4) lock = 0");

        // Fix doors unlocking in arachnus/torizo/tester/serris/genesis
        gmData.Code.ByName("gml_Room_rm_a2a04_Create").AppendGMLInCode("if (!global.event[103]) {with (oDoor) lock = 4;}");
        gmData.Code.ByName("gml_Room_rm_a3a01_Create").AppendGMLInCode("if (!global.event[152]) {with (oDoor) lock = 4;}");
        gmData.Code.ByName("gml_Room_rm_a4a05_Create").AppendGMLInCode("if (!global.event[207]) {with (oDoor) lock = 4;}");
        gmData.Code.ByName("gml_Object_oErisBossTrigger_Create_0").AppendGMLInCode("else { with (oDoor) lock = 4; }");
        gmData.Code.ByName("gml_Room_rm_a8a11_Create").AppendGMLInCode("if (!global.event[307]) {with (oDoor) lock = 4;}");

        // Fix doors in tester to be always blue
        foreach (string codeName in new[] { "gml_RoomCC_rm_a4a05_6510_Create", "gml_RoomCC_rm_a4a05_6511_Create" })
        {
            gmData.Code.ByName(codeName).SubstituteGMLCode("lock = 0;");
        }

        // Fix Tower activation unlocking right door for door lock rando
        if (seedObject.DoorLocks.ContainsKey(127890)) gmData.Code.ByName("gml_Object_oArea4PowerSwitch_Step_0").ReplaceGMLInCode("lock = 0", "lock = lock;");

        // Fix tester being fought in darkness / proboscums being disabled on not activated tower
        gmData.Code.ByName("gml_Object_oTesterBossTrigger_Other_10").PrependGMLInCode(
            "global.darkness = 0; with (oLightEngine) instance_destroy(); with (oFlashlight64); instance_destroy()");
        gmData.Code.ByName("gml_Object_oProboscum_Create_0").AppendGMLInCode("active = true; image_index = 0;");

        // Fix tester events sharing an event with tower activated - moved tester to 207
        gmData.Code.ByName("gml_RoomCC_rm_a4a04_6496_Create").ReplaceGMLInCode("global.event[200] < 2", "!global.event[207]");
        gmData.Code.ByName("gml_Object_oTesterBossTrigger_Create_0").ReplaceGMLInCode("global.event[200] != 1", "global.event[207]");
        gmData.Code.ByName("gml_Object_oTester_Step_0").ReplaceGMLInCode("global.event[200] = 2", "global.event[207] = 1;");

        // Force drops into rooms if ammo is low
        gmData.Code.ByName("gml_Room_rm_a3h02_Create").AppendGMLInCode("if (global.smissiles == 0 && global.maxsmissiles > 0) instance_create(32, 128, oSMPickup)");
        gmData.Code.ByName("gml_Room_rm_a8a08_Create").AppendGMLInCode("if (global.pbombs == 0 && global.maxpbombs > 0) instance_create(536, 140, oPBPickup)");
        gmData.Code.ByName("gml_Room_rm_a8a12_Create").AppendGMLInCode("if (global.pbombs == 0 && global.maxpbombs > 0) instance_create(496, 168, oPBPickup)");

        // Make Doors shine more in the dark
        gmData.Code.ByName("gml_Object_oLightEngine_Other_11").ReplaceGMLInCode("1, 0.4", "0.7, 1.4");
        gmData.Code.ByName("gml_Object_oLightEngine_Other_11").ReplaceGMLInCode("1, -0.4", "0.7, -1.4");

        // Fix doors in labs, by making them always blue, and the metroid listener lock/unlock them
        foreach (string codeName in new[]
                 {
                     "gml_RoomCC_rm_a7b05_9400_Create", "gml_RoomCC_rm_a7b06_9413_Create", "gml_RoomCC_rm_a7b06_9414_Create",
                     "gml_RoomCC_rm_a7b06A_9421_Create", "gml_RoomCC_rm_a7b06A_9420_Create", "gml_RoomCC_rm_a7b07_9437_Create", "gml_RoomCC_rm_a7b07_9438_Create",
                     "gml_RoomCC_rm_a7b08_9455_Create", "gml_RoomCC_rm_a7b08_9454_Create", "gml_RoomCC_rm_a7b08A_9467_Create", "gml_RoomCC_rm_a7b08A_9470_Create"
                 })
        {
            gmData.Code.ByName(codeName).SubstituteGMLCode("");
        }

        gmData.Code.ByName("gml_Object_oMonsterDoorControl_Alarm_0").SubstituteGMLCode("if (instance_number(oMonster) > 0) { with (oDoor) lock = 4 }");

        // Have option for missile doors to not open by supers
        gmData.Code.ByName("gml_Object_oDoor_Collision_438")
            .ReplaceGMLInCode("lock == 1", "((lock == 1 && !other.smissile) || (lock == 1 && other.smissile && global.canUseSupersOnMissileDoors))");

        // Implement new beam doors (charge = 5, wave = 6, spazer = 7, plasma = 8, ice = 9)
        gmData.Code.ByName("gml_Object_oDoor_Collision_439").ReplaceGMLInCode("lock == 0", "(lock == 0) || (lock == 5 && other.chargebeam) ||" +
                                                                                           "(lock == 6 && other.wbeam) || (lock == 7 && other.sbeam) || " +
                                                                                           "(lock == 7 && other.sbeam) || (lock == 8 && other.pbeam) || " +
                                                                                           "(lock == 9 && other.ibeam)");


        // Implement other weapon doors (bomb = 10, spider = 11, screw = 12)
        gmData.Code.ByName("gml_Object_oDoor_Collision_435").ReplaceGMLInCode("lock == 0", "(lock == 0 || lock == 10 )");
        // 267 is oCharacter ID
        UndertaleCode doorSamusCollision = gmData.GameObjects.ByName("oDoor").EventHandlerFor(EventType.Collision, 267, gmData.Strings, gmData.Code, gmData.CodeLocals);
        doorSamusCollision.SubstituteGMLCode("if (!open && ((lock == 11 && other.state == other.SPIDERBALL) || " +
                                             "(lock == 12 && global.screwattack && other.state == other.JUMPING && !other.vjump && !other.walljumping && (!other.inwater || global.currentsuit >= 2))))" +
                                             "event_user(1)");

        // Implement tower activated (13), tester dead doors (14), guardian doors (15), arachnus (16), torizo (17), serris (18), genesis (19), queen (20)
        // Also implement emp events - emp active (21), emp a1 (22), emp a2 (23), emp a3 (24), emp tutorial (25), emp robot home (26), emp near zeta (27),
        // emp near bullet hell (28), emp near pipe hub (29), emp near right exterior (30).
        // perma locked is doesnt have a number and is never set here as being openable.
        string newDoorReplacementText = "(lock == 0) || (global.event[200] && lock == 13)" +
                                        "|| (global.event[207] && lock == 14) || (global.event[51] && lock == 15)" +
                                        "|| (global.event[103] && lock == 16) || (global.event[152] && lock == 17)" +
                                        "|| (global.event[261] && lock == 18) || (global.event[307] && lock == 19)" +
                                        "|| (global.event[303] && lock == 20) || (global.event[250] && lock == 21)" +
                                        "|| (global.event[57] && lock == 22) || (global.event[110] && lock == 23)" +
                                        "|| (global.event[163] && lock == 24) || (global.event[251] && lock == 25) || (global.event[252] && lock == 26) || (global.event[253] && lock == 27)" +
                                        "|| (global.event[256] && lock == 28) || (global.event[254] && lock == 29) || (global.event[262] && lock == 30)";
        // beams, missile explosion, pbomb explosion, bomb explosion
        foreach (string codeName in new[]
                     { "gml_Object_oDoor_Collision_439", "gml_Object_oDoor_Collision_438", "gml_Object_oDoor_Collision_437", "gml_Object_oDoor_Collision_435" })
        {
            gmData.Code.ByName(codeName).ReplaceGMLInCode("lock == 0", newDoorReplacementText);
        }

        // Make EMP slots activate doors instantly, rather than having to wait 1.5 seconds
        gmData.Code.ByName("gml_Object_oBattery_Collision_187").ReplaceGMLInCode("alarm[0] = 90", "alarm[0] = 1");

        // Fix Emp devices unlocking all doors automatically!
        string empBatteryCellCondition = "false";
        foreach (uint doorID in new uint[] { 108539, 111778, 115149, 133836, 133903, 133914, 133911, 134711, 134426, 135330 })
        {
            if (!seedObject.DoorLocks.ContainsKey(doorID)) empBatteryCellCondition += $" || id == {doorID}";
        }

        gmData.Code.ByName("gml_Object_oBatterySlot_Alarm_0").ReplaceGMLInCode("""
                                                                               with (oDoor)
                                                                                   event_user(3)
                                                                               """,
            $"with (oDoor) {{ if ({empBatteryCellCondition}) event_user(3) }}");
        gmData.Code.ByName("gml_Object_oBatterySlot_Alarm_1").ReplaceGMLInCode("""
                                                                                   with (oDoor)
                                                                                       lock = 0
                                                                               """,
            $"with (oDoor) {{ if ({empBatteryCellCondition}) lock = 0 }}");

        string a5ActivateCondition = "false";
        foreach (uint doorID in new uint[] { 133732, 133731 })
        {
            if (!seedObject.DoorLocks.ContainsKey(doorID)) a5ActivateCondition += $" || id == {doorID}";
        }

        gmData.Code.ByName("gml_Object_oA5MainSwitch_Step_0").ReplaceGMLInCode("""
                                                                                       with (oDoor)
                                                                                           event_user(3)
                                                                               """,
            $"with (oDoor) {{ if ({a5ActivateCondition}) event_user(3) }}");
        gmData.Code.ByName("gml_Object_oA5MainSwitch_Alarm_0").ReplaceGMLInCode("""
                                                                                    with (oDoor)
                                                                                        lock = 0
                                                                                """,
            $"with (oDoor) {{ if ({a5ActivateCondition}) lock = 0 }}");


        //Destroy turbines and set the event to fully complete if entering "Water Turbine Station" at bottom doors and to "water should be here" if entering from the top.
        gmData.Code.ByName("gml_Room_rm_a2a08_Create").PrependGMLInCode("""
                                                                        if (global.targety == 160 && global.event[101] < 1)
                                                                            global.event[101] = 1;
                                                                        else if (global.targety > 240)
                                                                        {
                                                                            with (oA2SmallTurbine)
                                                                                instance_destroy();
                                                                            global.event[101] = 4;
                                                                        }
                                                                        """);
        //Remove setting of turbine event from adjacent rooms
        gmData.Code.ByName("gml_Room_rm_a2a09_Create").ReplaceGMLInCode("global.event[101] = 4", "");
        gmData.Code.ByName("gml_Room_rm_a2a19_Create").ReplaceGMLInCode("global.event[101] = 4", "");

        // Fix plasma chamber having a missile door instead of normal after tester dead
        gmData.Code.ByName("gml_RoomCC_rm_a4a09_6582_Create").ReplaceGMLInCode("lock = 1", "lock = 0;");

        // Fix lab log not displaying progress bar
        gmData.Code.ByName("gml_Room_rm_a7b04A_Create").ReplaceGMLInCode("create_log_trigger(0, 44, 440, 111, 0, 0)", "create_log_trigger(0, 44, 438, 111, -60, 1)");

        // Fix skreek street not actually having skreeks
        gmData.Code.ByName("gml_Script_scr_skreeks_destroy").PrependGMLInCode("exit");

        // Rename "fusion" difficulty to brutal, in order to be less confusing
        foreach (string codeName in new[] { "gml_Object_oMenuSaveSlot_Other_10", "gml_Object_oSlotMenu_Fusion_Create_0" })
        {
            gmData.Code.ByName(codeName).ReplaceGMLInCode(@"get_text(""Title-Additions"", ""GameSlot_NewGame_Fusion"")", "\"Brutal\"");
        }

        // Implement a fix, where every save shows "Brutal" as the difficulty when global.mod_fusion is enabled
        gmData.Code.ByName("gml_Object_oGameSelMenu_Other_12").ReplaceGMLInCode("if (oControl.mod_fusion == 1)", "if (oControl.mod_diffmult == 4)");

        // Make the popup text display during the pause for item acquisitions for less awkwardness
        gmData.Code.ByName("gml_Object_oItemCutscene_Create_0").ReplaceGMLInCode("sfx_play(sndMessage)",
            "popup_text(global.itmtext1); sfx_play(sndMessage);");


        // Add doors so that all doors are always two-way
        AddMissingDoors.Apply(gmData, decompileContext, seedObject);


        // Implement dna item
        UndertaleGameObject? enemyObject = gmData.GameObjects.ByName("oItem");
        for (int i = 350; i <= 395; i++)
        {
            UndertaleGameObject go = new UndertaleGameObject();
            go.Name = gmData.Strings.MakeString("oItemDNA_" + i);
            go.ParentId = enemyObject;
            // Add create event
            UndertaleCode create = new UndertaleCode();
            create.Name = gmData.Strings.MakeString($"gml_Object_oItemDNA_{i}_Create_0");
            create.SubstituteGMLCode("event_inherited(); itemid = " + i + ";");
            gmData.Code.Add(create);
            var createEventList = go.Events[0];
            UndertaleGameObject.EventAction action = new UndertaleGameObject.EventAction();
            action.CodeId = create;
            UndertaleGameObject.Event gEvent = new UndertaleGameObject.Event();
            gEvent.Actions.Add(action);
            createEventList.Add(gEvent);

            UndertaleCode collision = new UndertaleCode();
            collision.Name = gmData.Strings.MakeString($"gml_Object_oItemDNA_{i}_Collision_267");
            gmData.Code.Add(collision);
            var collisionEventList = go.Events[4];
            action = new UndertaleGameObject.EventAction();
            action.CodeId = collision;
            gEvent = new UndertaleGameObject.Event();
            gEvent.EventSubtype = 267; // 267 is oCharacter ID
            gEvent.Actions.Add(action);
            collisionEventList.Add(gEvent);
            gmData.GameObjects.Add(go);
        }

        // Adjust global item array to be 400
        characterVarsCode.ReplaceGMLInCode("""
                                           i = 350
                                           repeat (350)
                                           {
                                               i -= 1
                                               global.item[i] = 0
                                           }
                                           """, """
                                                i = 400
                                                repeat (400)
                                                {
                                                    i -= 1
                                                    global.item[i] = 0
                                                }
                                                """);
        gmData.Code.ByName("gml_Script_sv6_add_items").ReplaceGMLInCode("350", "400");
        gmData.Code.ByName("gml_Script_sv6_get_items").ReplaceGMLInCode("350", "400");

        // Metroid ID to DNA map
        UndertaleCode scrDNASpawn = new UndertaleCode();
        scrDNASpawn.Name = gmData.Strings.MakeString("gml_Script_scr_DNASpawn");
        scrDNASpawn.SubstituteGMLCode("""
                                      if (argument0 == 0)
                                          return oItemDNA_350;
                                      if (argument0 == 1)
                                          return oItemDNA_351;
                                      if (argument0 == 2)
                                          return oItemDNA_352;
                                      if (argument0 == 3)
                                          return oItemDNA_353;
                                      if (argument0 == 4)
                                          return oItemDNA_354;
                                      if (argument0 == 5)
                                          return oItemDNA_355;
                                      if (argument0 == 6)
                                          return oItemDNA_358;
                                      if (argument0 == 7)
                                          return oItemDNA_357;
                                      if (argument0 == 8)
                                          return oItemDNA_356;
                                      if (argument0 == 9)
                                          return oItemDNA_359;
                                      if (argument0 == 10)
                                          return oItemDNA_361;
                                      if (argument0 == 11)
                                          return oItemDNA_360;
                                      if (argument0 == 12)
                                          return oItemDNA_373;
                                      if (argument0 == 13)
                                          return oItemDNA_375;
                                      if (argument0 == 14)
                                          return oItemDNA_362;
                                      if (argument0 == 15)
                                          return oItemDNA_376;
                                      if (argument0 == 16)
                                          return oItemDNA_377;
                                      if (argument0 == 17)
                                          return oItemDNA_378;
                                      if (argument0 == 18)
                                          return oItemDNA_363;
                                      if (argument0 == 19)
                                          return oItemDNA_379;
                                      if (argument0 == 20)
                                          return oItemDNA_380;
                                      if (argument0 == 21)
                                          return oItemDNA_381;
                                      if (argument0 == 22)
                                          return oItemDNA_374;
                                      if (argument0 == 23)
                                          return oItemDNA_364;
                                      if (argument0 == 24)
                                          return oItemDNA_365;
                                      if (argument0 == 25)
                                          return oItemDNA_366;
                                      if (argument0 == 26)
                                          return oItemDNA_382;
                                      if (argument0 == 27)
                                          return oItemDNA_389;
                                      if (argument0 == 28)
                                          return oItemDNA_383;
                                      if (argument0 == 29)
                                          return oItemDNA_384;
                                      if (argument0 == 30)
                                          return oItemDNA_390;
                                      if (argument0 == 31)
                                          return oItemDNA_385;
                                      if (argument0 == 32)
                                          return oItemDNA_388;
                                      if (argument0 == 33)
                                          return oItemDNA_391;
                                      if (argument0 == 34)
                                          return oItemDNA_370;
                                      if (argument0 == 35)
                                          return oItemDNA_368;
                                      if (argument0 == 36)
                                          return oItemDNA_367;
                                      if (argument0 == 37)
                                          return oItemDNA_371;
                                      if (argument0 == 38)
                                          return oItemDNA_369;
                                      if (argument0 == 39)
                                          return oItemDNA_386;
                                      if (argument0 == 40)
                                          return oItemDNA_387;
                                      if (argument0 == 41)
                                          return oItemDNA_372;
                                      if (argument0 == 42)
                                          return oItemDNA_392;
                                      if (argument0 == 43)
                                          return oItemDNA_394;
                                      if (argument0 == 44)
                                          return oItemDNA_393;
                                      if (argument0 == 45)
                                          return oItemDNA_395;
                                      """);
        gmData.Code.Add(scrDNASpawn);
        gmData.Scripts.Add(new UndertaleScript { Name = gmData.Strings.MakeString("scr_DNASpawn"), Code = scrDNASpawn });

        // Make DNA count show on map
        ssDraw.ReplaceGMLInCode("draw_text((view_xview[0] + 18), ((view_yview[0] + 198) + rectoffset), timetext)",
            "draw_text((view_xview[0] + 18), ((view_yview[0] + 198) + rectoffset), timetext); draw_text((view_xview[0] + 158), ((view_yview[0] + 198) + rectoffset), string(global.dna) + \"/46\")");
        ssDraw.ReplaceGMLInCode("draw_text((view_xview[0] + 17), ((view_yview[0] + 197) + rectoffset), timetext)",
            "draw_text((view_xview[0] + 17), ((view_yview[0] + 197) + rectoffset), timetext); draw_text((view_xview[0] + 157), ((view_yview[0] + 197) + rectoffset), string(global.dna) + \"/46\")");

        // Fix item percentage now that more items have been added
        foreach (string name in new[]
                 {
                     "gml_Object_oGameSelMenu_Other_12", "gml_Object_oSS_Fg_Draw_0", "gml_Object_oScoreScreen_Create_0", "gml_Object_oScoreScreen_Other_10",
                     "gml_Object_oIGT_Step_0"
                 })
        {
            gmData.Code.ByName(name).ReplaceGMLInCode("/ 88", "/ 134");
        }

        // Make Charge Beam always hit metroids
        foreach (string name in new[]
                 {
                     "gml_Object_oMAlpha_Collision_439", "gml_Object_oMGamma_Collision_439", "gml_Object_oMZeta_Collision_439", "gml_Object_oMZetaBodyMask_Collision_439",
                     "gml_Object_oMOmegaMask2_Collision_439", "gml_Object_oMOmegaMask3_Collision_439"
                 })
        {
            var codeEntry = gmData.Code.ByName(name);
            codeEntry.ReplaceGMLInCode("&& global.missiles == 0 && global.smissiles == 0", "");
            if (codeEntry.GetGMLCode().Contains("""
                    else
                    {
                    """))
            {


                codeEntry.ReplaceGMLInCode("""
                    else
                    {
                    """,
                    """
                    else
                    {
                        if (oBeam.chargebeam) popup_text("Unequip beams to deal Charge damage")
                    """);
            }
            else
            {
                codeEntry.AppendGMLInCode("""
                else
                {
                    if (oBeam.chargebeam) popup_text("Unequip beams to deal Charge damage")
                }
                """);
            }
        }

        // Replace Metroids counters with DNA counters
        UndertaleCode? drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
        drawGuiCode.ReplaceGMLInCode("global.monstersleft", "global.dna");
        drawGuiCode.ReplaceGMLInCode("global.monstersarea", $"(max((46 - global.dna), 0))");
        gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_14").ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter\")", "\"DNA Counter\"");
        gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_10").ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter\")", "\"DNA Counter\"");
        gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_13").ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Tip\")",
            "\"Switches the type of the HUD DNA Counter\"");
        UndertaleCode? optionsDisplayUser2 = gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_12");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Local\")", "\"Until Labs\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Global\")", "\"Current\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Disabled_Tip\")", "\"Don't show the DNA Counter\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Local_Tip\")",
            "\"Show the remaining DNA until you can access the Genetics Laboratory\"");
        optionsDisplayUser2.ReplaceGMLInCode("get_text(\"OptionsDisplay\", \"MonsterCounter_Global_Tip\")", "\"Show the currently collected DNA\"");
        gmData.Code.ByName("gml_Object_oGameSelMenu_Other_12").ReplaceGMLInCode("global.monstersleft", "global.dna");

        // Add shortcut between nest and hideout
        if (seedObject.Patches.NestPipes)
        {
            AddA6Pipes.Apply(gmData, decompileContext, seedObject);
        }

        // Make metroids drop an item onto you on death and increase music timer to not cause issues
        gmData.Code.ByName("gml_Object_oMAlpha_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[1] >= 0) alarm[1] = 120; }");
        gmData.Code.ByName("gml_Object_oMGamma_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[2] >= 0) alarm[2] = 120; }");
        gmData.Code.ByName("gml_Object_oMZeta_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[3] >= 0) alarm[3] = 120; }");
        gmData.Code.ByName("gml_Object_oMOmega_Other_10").ReplaceGMLInCode("check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; changeOnMap = false} with (oMusicV2) { if (alarm[4] >= 0) alarm[4] = 120; }");

        // Make new global.lavastate 11 that requires 46 dna to be collected
        gmData.Code.ByName("gml_Script_check_areaclear")
            .SubstituteGMLCode(
                "var spawnQuake = is_undefined(argument0); if (global.lavastate == 11) { if (global.dna >= 46) { if (spawnQuake) instance_create(0, 0, oBigQuake); global.lavastate = 12; } }");

        // Check lavastate at labs
        UndertaleRoom? labsRoom = gmData.Rooms.ByName("rm_a7b04A");
        UndertaleRoom.GameObject labBlock = new UndertaleRoom.GameObject();
        labBlock.X = 64;
        labBlock.Y = 96;
        labBlock.ScaleX = 2;
        labBlock.ScaleY = 4;
        labBlock.InstanceID = gmData.GeneralInfo.LastObj++;
        labBlock.ObjectDefinition = gmData.GameObjects.ByName("oSolid1");
        UndertaleCode labBlockCode = new UndertaleCode();
        labBlockCode.Name = gmData.Strings.MakeString("gml_RoomCC_rm_a7b04A_labBlock_Create");
        labBlockCode.SubstituteGMLCode("if (global.lavastate > 11) {  tile_layer_delete(-99); instance_destroy(); }");
        gmData.Code.Add(labBlockCode);
        labBlock.CreationCode = labBlockCode;
        labsRoom.GameObjects.Add(labBlock);
        var tla7Outside = gmData.Backgrounds.ByName("tlArea7Outside");
        labsRoom.Tiles.Add(CreateRoomTile(64, 96, -99, tla7Outside, 0, 208, 32, 32));
        labsRoom.Tiles.Add(CreateRoomTile(64, 128, -99, tla7Outside, 0, 208, 32, 32));

        // Move alpha in nest
        gmData.Code.ByName("gml_RoomCC_rm_a6a09_8945_Create").ReplaceGMLInCode("if (global.lavastate > 8)", "y = 320; if (false)");

        // Lock these blocks behind a setting because they can make for some interesting changes
        gmData.Code.ByName("gml_Room_rm_a0h07_Create").ReplaceGMLInCode(
            "if (oControl.mod_purerandombool == 1 || oControl.mod_splitrandom == 1 || global.gamemode == 2)",
            $"if ({(!seedObject.Patches.GraveGrottoBlocks).ToString().ToLower()})");

        // enable randomizer to be always on
        UndertaleCode? newGameCode = gmData.Code.ByName("gml_Script_scr_newgame");
        newGameCode.ReplaceGMLInCode("oControl.mod_randomgamebool = 0", "oControl.mod_randomgamebool = 1");

        // Fix local metroids
        newGameCode.ReplaceGMLInCode("global.monstersleft = 47", "global.monstersleft = 47; global.monstersarea = 44");

        // Fix larvas dropping either missiles or supers instead of what's needed
        gmData.Code.ByName("gml_Object_oMonster_Other_10").ReplaceGMLInCode("pickup == 1", "true");
        gmData.Code.ByName("gml_Object_oMonster_Other_10").ReplaceGMLInCode("pickup == 0", "true");

        // Make it in oItem, that itemtype one's automatically spawn a popup
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("global.itemtype = itemtype",
            "if (itemtype == 1) {popup_text(text1);} global.itemtype = itemtype");

        // Add main (super) missile / PB launcher
        // missileLauncher, SMissileLauncher, PBombLauncher
        // also add an item for them + amount of expansions they give

        characterVarsCode.PrependGMLInCode("global.missileLauncher = 0; global.SMissileLauncher = 0; global.PBombLauncher = 0;" +
                                           "global.missileLauncherExpansion = 30; global.SMissileLauncherExpansion = 2; global.PBombLauncherExpansion = 2;");


        // Make expansion set to default values
        characterVarsCode.ReplaceGMLInCode("global.missiles = oControl.mod_Mstartingcount", "global.missiles = 0;");
        characterVarsCode.ReplaceGMLInCode("global.maxmissiles = oControl.mod_Mstartingcount", "global.maxmissiles = global.missiles;");
        characterVarsCode.ReplaceGMLInCode("global.smissiles = 0", "global.smissiles = 0;");
        characterVarsCode.ReplaceGMLInCode("global.maxsmissiles = 0", "global.maxsmissiles = global.smissiles;");
        characterVarsCode.ReplaceGMLInCode("global.pbombs = 0", "global.pbombs = 0;");
        characterVarsCode.ReplaceGMLInCode("global.maxpbombs = 0", "global.maxpbombs = global.pbombs;");
        characterVarsCode.ReplaceGMLInCode("global.maxhealth = 99", "global.maxhealth = global.playerhealth;");

        // Make main (super) missile / PB launcher required for firing
        UndertaleCode? shootMissileCode = gmData.Code.ByName("gml_Script_shoot_missile");
        shootMissileCode.ReplaceGMLInCode(
            "if ((global.currentweapon == 1 && global.missiles > 0) || (global.currentweapon == 2 && global.smissiles > 0))",
            "if ((global.currentweapon == 1 && global.missiles > 0 && global.missileLauncher) || (global.currentweapon == 2 && global.smissiles > 0 && global.SMissileLauncher))");
        UndertaleCode? chStepFireCode = gmData.Code.ByName("gml_Script_chStepFire");
        chStepFireCode.ReplaceGMLInCode("&& global.pbombs > 0", "&& global.pbombs > 0 && global.PBombLauncher");

        // Change GUI For toggle, use a red item sprite instead of green, for hold use a red instead of yellow. For not selected, use a crossed out one.
        // Replace Missile GUI
        drawGuiCode.ReplaceGMLInCode(
            """
                        if (global.currentweapon != 1 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball)
                            draw_sprite(sGUIMissile, 0, ((0 + xoff) + 1), 4)
            """,
            """
                        if (((global.currentweapon != 1 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && (!global.missileLauncher)))
                            draw_sprite(sGUIMissile, 4, ((0 + xoff) + 1), 4)
                        else if (((global.currentweapon != 1 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && (global.missileLauncher)))
                            draw_sprite(sGUIMissile, 0, ((0 + xoff) + 1), 4)

            """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 0)
                                                         draw_sprite(sGUIMissile, 1, ((0 + xoff) + 1), 4)
                                     """, """
                                                          if (oCharacter.armmsl == 0 && global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 1, ((0 + xoff) + 1), 4)
                                                          else if (oCharacter.armmsl == 0 && !global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 5, ((0 + xoff) + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 1)
                                                         draw_sprite(sGUIMissile, 2, ((0 + xoff) + 1), 4)
                                     """, """
                                                          if (oCharacter.armmsl == 1 && global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 2, ((0 + xoff) + 1), 4)
                                                          else if (oCharacter.armmsl == 1 && !global.missileLauncher)
                                                              draw_sprite(sGUIMissile, 3, ((0 + xoff) + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                 if (global.currentweapon == 1)
                                                     draw_sprite(sGUIMissile, 1, ((0 + xoff) + 1), 4)
                                     """, """
                                                      if (global.currentweapon == 1 && global.missileLauncher)
                                                          draw_sprite(sGUIMissile, 1, ((0 + xoff) + 1), 4)
                                                      else if (global.currentweapon == 1 && !global.missileLauncher)
                                                          draw_sprite(sGUIMissile, 3, ((0 + xoff) + 1), 4)
                                                      else if (global.currentweapon != 1 && !global.missileLauncher)
                                                          draw_sprite(sGUIMissile, 4, ((0 + xoff) + 1), 4)
                                          """);

        // Replace Super GUI
        drawGuiCode.ReplaceGMLInCode(
            """
                        if (global.currentweapon != 2 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball)
                            draw_sprite(sGUISMissile, 0, (xoff + 1), 4)
            """,
            """
                        if ((global.currentweapon != 2 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && !global.SMissileLauncher)
                            draw_sprite(sGUISMissile, 4, (xoff + 1), 4)
                        else if ((global.currentweapon != 2 || oCharacter.state == 23 || oCharacter.state == 24 || oCharacter.state == 27 || oCharacter.state == 54 || oCharacter.state == 55 || oCharacter.sjball) && global.SMissileLauncher)
                            draw_sprite(sGUISMissile, 0, (xoff + 1), 4)

            """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 0)
                                                         draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
                                     """, """
                                                          if (oCharacter.armmsl == 0 && global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 0 && !global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 5, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 1)
                                                         draw_sprite(sGUISMissile, 2, (xoff + 1), 4)
                                     """, """
                                                          if (oCharacter.armmsl == 1 && global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 2, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 1 && !global.SMissileLauncher)
                                                              draw_sprite(sGUISMissile, 3, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                 if (global.currentweapon == 2)
                                                     draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
                                     """, """
                                                      if (global.currentweapon == 2 && global.SMissileLauncher)
                                                          draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
                                                      else if (global.currentweapon == 2 && !global.SMissileLauncher)
                                                          draw_sprite(sGUISMissile, 3, (xoff + 1), 4)
                                                      else if (global.currentweapon != 2 && !global.SMissileLauncher)
                                                          draw_sprite(sGUISMissile, 4, (xoff + 1), 4)
                                          """);

        // Replace PB GUI
        drawGuiCode.ReplaceGMLInCode(
            """
                        if (oCharacter.state != 23 && oCharacter.state != 24 && oCharacter.state != 27 && oCharacter.state != 54 && oCharacter.state != 55 && oCharacter.sjball == 0)
                            draw_sprite(sGUIPBomb, 0, (xoff + 1), 4)
            """,
            """
                        if ((global.PBombLauncher) && oCharacter.state != 23 && oCharacter.state != 24 && oCharacter.state != 27 && oCharacter.state != 54 && oCharacter.state != 55 && oCharacter.sjball == 0)
                            draw_sprite(sGUIPBomb, 0, (xoff + 1), 4)
                        else if ((!global.PBombLauncher) && oCharacter.state != 23 && oCharacter.state != 24 && oCharacter.state != 27 && oCharacter.state != 54 && oCharacter.state != 55 && oCharacter.sjball == 0)
                            draw_sprite(sGUIPBomb, 4, (xoff + 1), 4)
            """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 0)
                                                         draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
                                     """, """
                                                          if (oCharacter.armmsl == 0 && global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 0 && !global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 5, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                     if (oCharacter.armmsl == 1)
                                                         draw_sprite(sGUIPBomb, 2, (xoff + 1), 4)
                                     """, """
                                                          if (oCharacter.armmsl == 1 && global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 2, (xoff + 1), 4)
                                                          else if (oCharacter.armmsl == 1 && !global.PBombLauncher)
                                                              draw_sprite(sGUIPBomb, 3, (xoff + 1), 4)
                                          """);
        drawGuiCode.ReplaceGMLInCode("""
                                                 if (global.currentweapon == 3)
                                                     draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
                                     """, """
                                                      if (global.currentweapon == 3 && global.PBombLauncher)
                                                          draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
                                                      else if (global.currentweapon == 3 && !global.PBombLauncher)
                                                          draw_sprite(sGUIPBomb, 3, (xoff + 1), 4)
                                                      else if (global.currentweapon != 3 && !global.PBombLauncher)
                                                          draw_sprite(sGUIPBomb, 4, (xoff + 1), 4)
                                          """);

        // Fix weapon selection with toggle
        UndertaleCode? chStepControlCode = gmData.Code.ByName("gml_Script_chStepControl");
        chStepControlCode.ReplaceGMLInCode("if (kMissile && kMissilePushedSteps == 1 && global.maxmissiles > 0", "if (kMissile && kMissilePushedSteps == 1");
        chStepControlCode.ReplaceGMLInCode("if (global.currentweapon == 1 && global.missiles == 0)",
            "if (global.currentweapon == 1 && (global.maxmissiles == 0 || global.missiles == 0))");

        // Fix weapon selection cancel with toggle
        chStepControlCode.ReplaceGMLInCode("if (kSelect && kSelectPushedSteps == 0 && global.maxmissiles > 0 && global.currentweapon != 0)",
            "if (kSelect && kSelectPushedSteps == 0 && (global.missiles > 0 || global.smissiles > 0 || global.pbombs > 0) && global.currentweapon != 0)");

        // Fix weapon selection with hold
        chStepControlCode.ReplaceGMLInCode("""
                                               if (global.currentweapon == 0)
                                                   global.currentweapon = 1
                                           """, """
                                                if (global.currentweapon == 0)
                                                {
                                                    if (global.maxmissiles > 0) global.currentweapon = 1;
                                                    else if (global.maxsmissiles > 0) global.currentweapon = 2;
                                                }
                                                """);
        chStepControlCode.ReplaceGMLInCode("if (global.maxmissiles > 0 && (state", "if ((state");

        // Have new variables for certain events because they are easier to debug via a switch than changing a ton of values
        characterVarsCode.PrependGMLInCode(
            "global.septoggHelpers = 0; global.skipCutscenes = 0; global.skipSaveCutscene = 0; global.skipItemFanfare = 0; global.respawnBombBlocks = 0; global.screwPipeBlocks = 0;" +
            "global.a3Block = 0; global.softlockPrevention = 0; global.unexploredMap = 0; global.unveilBlocks = 0; global.canUseSupersOnMissileDoors = 0;");

        // Set geothermal reactor to always be exploded
        characterVarsCode.AppendGMLInCode("global.event[203] = 9");

        // Set a bunch of metroid events to already be scanned
        characterVarsCode.AppendGMLInCode("global.event[301] = 1; global.event[305] = 1; global.event[306] = 1;");

        // Move Geothermal PB
        MoveGeothermalPB.Apply(gmData, decompileContext, seedObject);

        // Set lava state and the metroid scanned events
        characterVarsCode.AppendGMLInCode("global.lavastate = 11; global.event[4] = 1; global.event[56] = 1;" +
                                          " global.event[155] = 1; global.event[173] = 1; global.event[204] = 1; global.event[259] = 1; check_areaclear(1)");

        // Improve when expansions trigger big pickup text and popup_text
        characterVarsCode.PrependGMLInCode("global.firstMissileCollected = 0; global.firstSMissileCollected = 0; " +
                                           "global.firstPBombCollected = 0; global.firstETankCollected = 0;");
        UndertaleCode? missileCharacterEvent = gmData.Code.ByName("gml_Script_scr_missile_character_event");
        missileCharacterEvent.ReplaceGMLInCode("""
                                                   if (global.maxmissiles == oControl.mod_Mstartingcount)
                                                       event_inherited()
                                               """, """
                                                        if (!global.firstMissileCollected) {
                                                            event_inherited();
                                                            global.firstMissileCollected = 1;
                                                        }
                                                    """);
        missileCharacterEvent.ReplaceGMLInCode("popup_text(get_text(\"Notifications\", \"MissileTank\"))", "");

        UndertaleCode? superMissileCharacterEvent = gmData.Code.ByName("gml_Script_scr_supermissile_character_event");
        superMissileCharacterEvent.ReplaceGMLInCode("""
                                                        if (global.maxsmissiles == 0)
                                                            event_inherited()
                                                    """, """
                                                             if (!global.firstSMissileCollected) {
                                                                 event_inherited();
                                                                 global.firstSMissileCollected = 1;
                                                             }
                                                         """);
        superMissileCharacterEvent.ReplaceGMLInCode("popup_text(get_text(\"Notifications\", \"SuperMissileTank\"))", "");

        UndertaleCode? pBombCharacterEvent = gmData.Code.ByName("gml_Script_scr_powerbomb_character_event");
        pBombCharacterEvent.ReplaceGMLInCode("""
                                                 if (global.maxpbombs == 0)
                                                     event_inherited()
                                             """, """
                                                      if (!global.firstPBombCollected) {
                                                          event_inherited();
                                                          global.firstPBombCollected = 1;
                                                      }
                                                  """);
        pBombCharacterEvent.ReplaceGMLInCode("popup_text(get_text(\"Notifications\", \"PowerBombTank\"))", "");

        UndertaleCode? eTankCharacterEvent = gmData.Code.ByName("gml_Script_scr_energytank_character_event");
        eTankCharacterEvent.ReplaceGMLInCode("""
                                                 if (global.maxhealth < 100)
                                                     event_inherited()
                                             """, """
                                                      if (!global.firstETankCollected) {
                                                          event_inherited();
                                                          global.firstETankCollected = 1;
                                                      }
                                                  """);
        eTankCharacterEvent.ReplaceGMLInCode("popup_text(get_text(\"Notifications\", \"EnergyTank\"))", "");

        // Add speedbooster reduction
        characterVarsCode.PrependGMLInCode("global.speedBoosterFramesReduction = 0;");
        gmData.Code.ByName("gml_Script_characterStepEvent")
            .ReplaceGMLInCode("speedboost_steps > 75", "speedboost_steps >= 1 && speedboost_steps > (75 - global.speedBoosterFramesReduction)");
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("dash == 30", "dash >= 1 && dash >= (30 - (max(global.speedBoosterFramesReduction, 76)-76))");
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("""
                                                                                 speedboost = 1
                                                                                 canturn = 0
                                                                                 sjball = 0
                                                                                 charge = 0
                                                                                 sfx_play(sndSBStart)
                                                                                 alarm[2] = 30
                                                                             """, """
                                                                                      dash = 30
                                                                                      speedboost = 1
                                                                                      canturn = 0
                                                                                      sjball = 0
                                                                                      charge = 0
                                                                                      sfx_play(sndSBStart)
                                                                                      alarm[2] = 30
                                                                                  """);

        // Add starting equipment memo
        characterVarsCode.PrependGMLInCode("global.showStartingMemo = 1; global.startingHeader = \"\"; global.startingText = \"\";");
        gmData.Code.ByName("gml_Object_oCharacter_Create_0").AppendGMLInCode("if (!global.showStartingMemo) display_itemmsg(global.startingHeader, global.startingText, \"\", \"\");");
        gmData.Code.ByName("gml_Object_oItemCutscene_Create_0").ReplaceGMLInCode("mus_play_once(musItemGet)", "if (global.showStartingMemo) mus_play_once(musItemGet); global.showStartingMemo = 1;");
        if (seedObject.Identifier.StartingMemoText is not null)
        {
            characterVarsCode.ReplaceGMLInCode("global.showStartingMemo = 1", "global.showStartingMemo = 0");
            characterVarsCode.ReplaceGMLInCode("global.startingHeader = \"\"", $"global.startingHeader = \"{seedObject.Identifier.StartingMemoText.Header}\"");
            characterVarsCode.ReplaceGMLInCode("global.startingText = \"\"", $"global.startingText = \"{seedObject.Identifier.StartingMemoText.Description}\"");
        }

        // TODO: put this somewhere Decouple Major items from item locations
        characterVarsCode.PrependGMLInCode("global.dna = 0;");

        // Add Long Beam as an item
        characterVarsCode.PrependGMLInCode("global.hasLongBeam = 0;");
        gmData.Code.ByName("gml_Object_oBeam_Create_0").AppendGMLInCode("existingTimer = 12;");
        gmData.Code.ByName("gml_Object_oBeam_Step_0").PrependGMLInCode("if (existingTimer > 0) existingTimer--; if (existingTimer <= 0 && !global.hasLongBeam) instance_destroy()");

        // Add WJ as item
        characterVarsCode.PrependGMLInCode("global.hasWJ = 0;");
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode(
            "if (state == JUMPING && statetime > 4 && position_meeting(x, (y + 8), oSolid) == 0 && justwalljumped == 0 && walljumping == 0 && monster_drain == 0)",
            "if (state == JUMPING && statetime > 4 && position_meeting(x, (y + 8), oSolid) == 0 && justwalljumped == 0 && walljumping == 0 && monster_drain == 0 && global.hasWJ)");

        // Add IBJ as item
        characterVarsCode.PrependGMLInCode("global.hasIBJ = 0;");
        gmData.Code.ByName("gml_Script_characterCreateEvent").AppendGMLInCode("IBJ_MIDAIR_MAX = 5; IBJLaidInAir = IBJ_MIDAIR_MAX; IBJ_MAX_BOMB_SEPERATE_TIMER = 4; IBJBombSeperateTimer = -1;");
        gmData.Code.ByName("gml_Object_oCharacter_Step_0").AppendGMLInCode("if (IBJBombSeperateTimer >= 0) IBJBombSeperateTimer-- if (!platformCharacterIs(IN_AIR)) IBJLaidInAir = IBJ_MIDAIR_MAX;");
        gmData.Code.ByName("gml_Object_oCharacter_Collision_435").ReplaceGMLInCode("if (isCollisionTop(6) == 0)",
            """
            if (!global.hasIBJ)
            {
                if (state == AIRBALL)
                {
                    if (IBJBombSeperateTimer < 0)
                        IBJLaidInAir--;
                }
                else
                {
                    IBJLaidInAir = IBJ_MIDAIR_MAX;
                }
                if (IBJLaidInAir <= 0)
                    exit;
                IBJBombSeperateTimer = IBJ_MAX_BOMB_SEPERATE_TIMER;
            }
            if (isCollisionTop(6) == 0)
            """);

        // Save current hash seed, so we can compare saves later
        characterVarsCode.PrependGMLInCode($"global.gameHash = \"{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash}) (World: {seedObject.Identifier.WorldUUID})\"");

        // modify gravity pod room to *always* spawn an item
        gmData.Code.ByName("gml_Room_rm_a5a07_Create").ReplaceGMLInCode("if (oControl.mod_gravity != 9)", "");
        gmData.Code.ByName("gml_Object_oGravityPodTrigger_Create_0").SubstituteGMLCode("instance_destroy()");
        gmData.Code.ByName("gml_Object_oGravityPod_Create_0").AppendGMLInCode("closed = 1; xoff = 0;");

        // Always enable long range activation, for consistent zips
        gmData.Code.ByName("gml_Object_oCharacter_Step_1").ReplaceGMLInCode("global.objdeactivate", "false");

        gmData.Code.ByName("gml_Script_start_new_game").AppendGMLInCode("global.targetx = global.save_x; global.targetx = global.save_y;");

        // Make new game not hardcode separate starting values
        characterVarsCode.PrependGMLInCode("global.startingSave = 0;");
        UndertaleCode? startNewGame = gmData.Code.ByName("gml_Script_start_new_game");
        startNewGame.ReplaceGMLInCode("""
                                      global.start_room = 21
                                      global.save_x = 3408
                                      global.save_y = 1184
                                      """, "load_character_vars(); global.save_room = global.startingSave; set_start_location();");

        // Modify main menu to have a "restart from starting save" option
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Other_10").SubstituteGMLCode("""
                                                                                      op1 = instance_create(x, y, oPauseOption)
                                                                                      op1.optionid = 0
                                                                                      op1.label = get_text("PauseMenu", "Resume")
                                                                                      op2 = instance_create(x, (y + 16), oPauseOption)
                                                                                      op2.optionid = 1
                                                                                      op2.label = get_text("PauseMenu", "Restart")
                                                                                      op3 = instance_create(x, (y + 32), oPauseOption)
                                                                                      op3.optionid = 2
                                                                                      op3.label = "Restart from Start Location"
                                                                                      op4 = instance_create(x, (y + 48), oPauseOption)
                                                                                      op4.optionid = 3
                                                                                      op4.label = get_text("PauseMenu", "Options")
                                                                                      op5 = instance_create(x, (y + 64), oPauseOption)
                                                                                      op5.optionid = 4
                                                                                      op5.label = get_text("PauseMenu", "Quit")
                                                                                      """);
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Create_0").ReplaceGMLInCode("lastitem = 3", "lastitem = 4;");
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Create_0").AppendGMLInCode("""
                                                                                    tip[0] = get_text("PauseMenu", "Resume_Tip");
                                                                                    tip[1] = get_text("PauseMenu", "Restart_Tip");
                                                                                    tip[2] = "Abandon the current game and load from Starting Area";
                                                                                    tip[3] = get_text("PauseMenu", "Options_Tip");
                                                                                    tip[4] = get_text("PauseMenu", "Quit_Tip");
                                                                                    global.tiptext = tip[global.curropt];
                                                                                    """);
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Step_0").ReplaceGMLInCode("""
                                                                                           if (global.curropt == 1)
                                                                                           {
                                                                                               instance_create(50, 92, oOptionsReload)
                                                                                               instance_destroy()
                                                                                           }
                                                                                           if (global.curropt == 2)
                                                                                           {
                                                                                               instance_create(50, 92, oOptionsMain)
                                                                                               instance_destroy()
                                                                                           }
                                                                                           if (global.curropt == 3)
                                                                                           {
                                                                                               instance_create(50, 92, oOptionsQuit)
                                                                                               instance_destroy()
                                                                                           }
                                                                                   """, """
                                                                                                if (global.curropt == 1)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsReload)
                                                                                                    global.shouldLoadFromStart = 0;
                                                                                                    instance_destroy()
                                                                                                }
                                                                                                if (global.curropt == 2)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsReload)
                                                                                                    global.shouldLoadFromStart = 1;
                                                                                                    instance_destroy()
                                                                                                }
                                                                                                if (global.curropt == 3)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsMain)
                                                                                                    instance_destroy()
                                                                                                }
                                                                                                if (global.curropt == 4)
                                                                                                {
                                                                                                    instance_create(50, 92, oOptionsQuit)
                                                                                                    instance_destroy()
                                                                                                }
                                                                                        """);
        gmData.Code.ByName("gml_Object_oPauseMenuOptions_Other_11").AppendGMLInCode("""
                                                                                    if instance_exists(op5)
                                                                                    {
                                                                                        with (op5)
                                                                                            instance_destroy()
                                                                                    }

                                                                                    """);
        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode("global.shouldLoadFromStart = 0;");
        gmData.Code.ByName("gml_Object_oLoadGame_Other_10").AppendGMLInCode("""
                                                                            if (global.shouldLoadFromStart)
                                                                            {
                                                                              global.save_room = global.startingSave;
                                                                              set_start_location();
                                                                              room_change(global.start_room, 1)
                                                                              global.shouldLoadFromStart = 0;
                                                                            }
                                                                            """);
        gmData.Code.ByName("gml_Object_oOptionsReload_Step_0").ReplaceGMLInCode("instance_create(50, 92, oPauseMenuOptions)",
            "instance_create(50, 92, oPauseMenuOptions); global.shouldLoadFromStart = 0;");

        // Modify save scripts to load our new globals / stuff we modified
        UndertaleCode saveGlobalsCode = new UndertaleCode();
        saveGlobalsCode.Name = gmData.Strings.MakeString("gml_Script_sv6_add_newglobals");
        saveGlobalsCode.SubstituteGMLCode("""
                                          var list, str_list;
                                          list = ds_list_create()
                                          ds_list_add(list, global.hasBombs)
                                          ds_list_add(list, global.hasPowergrip)
                                          ds_list_add(list, global.hasSpiderball)
                                          ds_list_add(list, global.hasJumpball)
                                          ds_list_add(list, global.hasHijump)
                                          ds_list_add(list, global.hasVaria)
                                          ds_list_add(list, global.hasSpacejump)
                                          ds_list_add(list, global.hasSpeedbooster)
                                          ds_list_add(list, global.hasScrewattack)
                                          ds_list_add(list, global.hasGravity)
                                          ds_list_add(list, global.hasCbeam)
                                          ds_list_add(list, global.hasIbeam)
                                          ds_list_add(list, global.hasWbeam)
                                          ds_list_add(list, global.hasSbeam)
                                          ds_list_add(list, global.hasPbeam)
                                          ds_list_add(list, global.hasMorph)
                                          ds_list_add(list, global.firstMissileCollected)
                                          ds_list_add(list, global.firstSMissileCollected)
                                          ds_list_add(list, global.firstPBombCollected)
                                          ds_list_add(list, global.firstETankCollected)
                                          ds_list_add(list, global.missileLauncher)
                                          ds_list_add(list, global.SMissileLauncher)
                                          ds_list_add(list, global.PBombLauncher)
                                          ds_list_add(list, global.missileLauncherExpansion)
                                          ds_list_add(list, global.SMissileLauncherExpansion)
                                          ds_list_add(list, global.PBombLauncherExpansion)
                                          ds_list_add(list, global.maxhealth)
                                          ds_list_add(list, global.maxmissiles)
                                          ds_list_add(list, global.maxsmissiles)
                                          ds_list_add(list, global.maxpbombs)
                                          ds_list_add(list, global.gameHash)
                                          ds_list_add(list, global.dna)
                                          ds_list_add(list, global.startingSave)
                                          ds_list_add(list, global.flashlightLevel)
                                          ds_list_add(list, global.speedBoosterFramesReduction)
                                          ds_list_add(list, global.showStartingMemo)
                                          ds_list_add(list, global.lastOffworldNumber)
                                          ds_list_add(list, global.collectedIndices)
                                          ds_list_add(list, global.collectedItems)
                                          ds_list_add(list, global.hasWJ)
                                          ds_list_add(list, global.hasIBJ)
                                          ds_list_add(list, global.hasLongBeam)
                                          str_list = ds_list_write(list)
                                          ds_list_clear(list)
                                          return str_list;
                                          """);
        gmData.Code.Add(saveGlobalsCode);
        gmData.Scripts.Add(new UndertaleScript { Name = gmData.Strings.MakeString("sv6_add_newglobals"), Code = saveGlobalsCode });

        UndertaleCode loadGlobalsCode = new UndertaleCode();
        loadGlobalsCode.Name = gmData.Strings.MakeString("gml_Script_sv6_get_newglobals");
        loadGlobalsCode.SubstituteGMLCode("""
                                          list = ds_list_create()
                                          ds_list_read(list, base64_decode(file_text_read_string(argument0)))
                                          i = 0
                                          global.hasBombs = readline()
                                          global.hasPowergrip = readline()
                                          global.hasSpiderball = readline()
                                          global.hasJumpball = readline()
                                          global.hasHijump = readline()
                                          global.hasVaria = readline()
                                          global.hasSpacejump = readline()
                                          global.hasSpeedbooster = readline()
                                          global.hasScrewattack = readline()
                                          global.hasGravity = readline()
                                          global.hasCbeam = readline()
                                          global.hasIbeam = readline()
                                          global.hasWbeam = readline()
                                          global.hasSbeam = readline()
                                          global.hasPbeam = readline()
                                          global.hasMorph = readline()
                                          global.firstMissileCollected = readline()
                                          global.firstSMissileCollected = readline()
                                          global.firstPBombCollected = readline()
                                          global.firstETankCollected = readline()
                                          global.missileLauncher = readline()
                                          global.SMissileLauncher = readline()
                                          global.PBombLauncher = readline()
                                          global.missileLauncherExpansion = readline()
                                          global.SMissileLauncherExpansion = readline()
                                          global.PBombLauncherExpansion = readline()
                                          global.maxhealth = readline()
                                          global.maxmissiles = readline()
                                          global.maxsmissiles = readline()
                                          global.maxpbombs = readline()
                                          global.gameHash = readline()
                                          global.dna = readline()
                                          global.startingSave = readline()
                                          global.flashlightLevel = readline()
                                          global.speedBoosterFramesReduction = readline();
                                          global.showStartingMemo = readline();
                                          global.lastOffworldNumber = readline();
                                          if (global.lastOffworldNumber == undefined)
                                            global.lastOffworldNumber = 0
                                          global.collectedIndices = readline();
                                          if (global.collectedIndices == undefined || global.collectedIndices == 0)
                                            global.collectedIndices = "locations:"
                                          global.collectedItems = readline();
                                          if (global.collectedItems == undefined || global.collectedItems == 0)
                                            global.collectedItems = "items:"
                                          global.hasWJ = readline()
                                          if (global.hasWJ == undefined)
                                            global.hasWJ = 1
                                          global.hasIBJ = readline()
                                          if (global.hasIBJ == undefined)
                                            global.hasIBJ = 1
                                          global.hasLongBeam = readline()
                                          if (global.hasLongBeam == undefined)
                                            global.hasLongBeam = 1
                                          ds_list_clear(list)
                                          """);
        gmData.Code.Add(loadGlobalsCode);
        gmData.Scripts.Add(new UndertaleScript { Name = gmData.Strings.MakeString("sv6_get_newglobals"), Code = loadGlobalsCode });

        UndertaleCode? sv6Save = gmData.Code.ByName("gml_Script_sv6_save");
        sv6Save.ReplaceGMLInCode("save_str[10] = sv6_add_seed()", "save_str[10] = sv6_add_seed(); save_str[11] = sv6_add_newglobals()");
        sv6Save.ReplaceGMLInCode("V7.0", "RDV V8.0");
        sv6Save.ReplaceGMLInCode("repeat (10)", "repeat (11)");

        UndertaleCode? sv6load = gmData.Code.ByName("gml_Script_sv6_load");
        sv6load.ReplaceGMLInCode("V7.0", "RDV V8.0");
        sv6load.ReplaceGMLInCode("sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid); sv6_get_newglobals(fid);");
        sv6load.ReplaceGMLInCode("global.maxhealth = (99 + ((global.etanks * 100) * oControl.mod_etankhealthmult))", "");
        sv6load.ReplaceGMLInCode("""
                                     if (global.difficulty < 2)
                                     {
                                         global.maxmissiles = (oControl.mod_Mstartingcount + (global.mtanks * 5))
                                         global.maxsmissiles = (global.stanks * 2)
                                         global.maxpbombs = (global.ptanks * 2)
                                     }
                                     else
                                     {
                                         global.maxmissiles = (oControl.mod_Mstartingcount + (global.mtanks * 2))
                                         global.maxsmissiles = global.stanks
                                         global.maxpbombs = global.ptanks
                                     }
                                 """, "");

        //complain if invalid game hash
        sv6load.PrependGMLInCode($"var uniqueGameHash = \"{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash}) (World: {seedObject.Identifier.WorldUUID})\"");
        sv6load.ReplaceGMLInCode("global.playerhealth = global.maxhealth",
            "if (global.gameHash != uniqueGameHash) { " +
            "show_message(\"Save file is from another seed or Multiworld word! (\" + global.gameHash + \")\"); " +
            "file_text_close(fid); file_delete((filename + \"d\")); room_goto(titleroom); exit;" +
            "} global.playerhealth = global.maxhealth");
        // TODO: instead of just show_messsage, have an actual proper in-game solution. Maybe do this after MW
        // reference: https://cdn.discordapp.com/attachments/914294505107251231/1121816654385516604/image.png

        UndertaleCode? sv6loadDetails = gmData.Code.ByName("gml_Script_sv6_load_details");
        sv6loadDetails.ReplaceGMLInCode("V7.0", "RDV V8.0");
        sv6loadDetails.ReplaceGMLInCode("sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid); sv6_get_newglobals(fid);");

        foreach (string code in new[] { "gml_Script_save_stats", "gml_Script_save_stats2", "gml_Script_load_stats", "gml_Script_load_stats2" })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("V7.0", "RDV V8.0");
        }

        // Change to custom save directory
        gmData.GeneralInfo.Name = gmData.Strings.MakeString("AM2R_RDV");
        gmData.GeneralInfo.FileName = gmData.Strings.MakeString("AM2R_RDV");

        // Change starting health and energy per tank
        characterVarsCode.ReplaceGMLInCode("global.playerhealth = 99", $"global.playerhealth = {seedObject.Patches.EnergyPerTank - 1};");
        eTankCharacterEvent.ReplaceGMLInCode("global.maxhealth += (100 * oControl.mod_etankhealthmult)", $"global.maxhealth += {seedObject.Patches.EnergyPerTank}");

        // Flashlight
        characterVarsCode.PrependGMLInCode("global.flashlightLevel = 0;");
        gmData.Code.ByName("gml_Script_ApplyLightPreset").ReplaceGMLInCode("global.darkness", "lightLevel");
        gmData.Code.ByName("gml_Script_ApplyLightPreset").PrependGMLInCode(
            """
            var lightLevel = 0
            lightLevel = global.darkness - global.flashlightLevel
            if (lightLevel < 0)
                lightLevel = 0
            if (lightLevel > 4)
                lightLevel = 4
            """);

        // Set starting items
        StartingItems.Apply(gmData, decompileContext, seedObject);

        // Set starting location - TODO: allow custom x/y position in rooms.
        characterVarsCode.ReplaceGMLInCode("global.startingSave = 0", $"global.startingSave = {seedObject.StartingLocation.SaveRoom}");
        characterVarsCode.ReplaceGMLInCode("global.save_room = 0", $"global.save_room = {seedObject.StartingLocation.SaveRoom}");




        // Make items spawned from metroids not change map
        gmData.Code.ByName("gml_Object_oItem_Create_0").AppendGMLInCode("changeOnMap = true");
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("if (distance_to_object(oItem) > 180)",
            "if ((distance_to_object(oItem) > 180) && changeOnMap)");

        // Door locks
        DoorLockRando.Apply(gmData, decompileContext, seedObject);

        // Add new item name variable for oItem, necessary for MW
        gmData.Code.ByName("gml_Object_oItem_Create_0").AppendGMLInCode("itemName = \"\"; itemQuantity = 0;");

        // Add new item scripts
        gmData.Scripts.AddScript("get_etank", "scr_energytank_character_event()");
        gmData.Scripts.AddScript("get_missile_expansion", $"if (!global.missileLauncher) {{ text1 = \"{seedObject.Patches.LockedMissileText.Header}\"; " +
                                                          $"text2 = \"{seedObject.Patches.LockedMissileText.Description}\" }} if (active) global.collectedItems += (\"{ItemEnum.Missile.GetEnumMemberValue()}\" + \"|\" + string(argument0) + \",\"); scr_missile_character_event(argument0)");
        gmData.Scripts.AddScript("get_missile_launcher", $"global.missileLauncher = 1; global.maxmissiles += argument0; global.missiles = global.maxmissiles; if (active) global.collectedItems += (\"{ItemEnum.Missile.GetEnumMemberValue()}\" + \"|\" + string(argument0) + \",\")");
        gmData.Scripts.AddScript("get_super_missile_expansion", $"if (!global.SMissileLauncher) {{ text1 = \"{seedObject.Patches.LockedSuperText.Header}\"; " +
                                                                $"text2 = \"{seedObject.Patches.LockedSuperText.Description}\" }} if (active) global.collectedItems += (\"{ItemEnum.SuperMissile.GetEnumMemberValue()}\" + \"|\" + string(argument0) + \",\"); scr_supermissile_character_event(argument0)");
        gmData.Scripts.AddScript("get_super_missile_launcher", $"global.SMissileLauncher = 1; global.maxsmissiles += argument0; global.smissiles = global.maxsmissiles; if (active) global.collectedItems += (\"{ItemEnum.SuperMissile.GetEnumMemberValue()}\" + \"|\" + string(argument0) + \",\")");
        gmData.Scripts.AddScript("get_pb_expansion", $"if (!global.PBombLauncher) {{ text1 = \"{seedObject.Patches.LockedPBombText.Header}\"; " +
                                                     $"text2 = \"{seedObject.Patches.LockedPBombText.Description}\" }} if (active) global.collectedItems += (\"{ItemEnum.PBomb.GetEnumMemberValue()}\" + \"|\" + string(argument0) + \",\"); scr_powerbomb_character_event(argument0)");
        gmData.Scripts.AddScript("get_pb_launcher", $"global.PBombLauncher = 1; global.maxpbombs += argument0; global.pbombs = global.maxpbombs; if (active) global.collectedItems += (\"{ItemEnum.PBomb.GetEnumMemberValue()}\" + \"|\" + string(argument0) + \",\")");
        gmData.Scripts.AddScript("get_dna", "global.dna++; check_areaclear(); ");
        gmData.Scripts.AddScript("get_bombs", "global.bomb = 1; global.hasBombs = 1;");
        gmData.Scripts.AddScript("get_power_grip", "global.powergrip = 1; global.hasPowergrip = 1;");
        gmData.Scripts.AddScript("get_spider_ball", "global.spiderball = 1; global.hasSpiderball = 1;");
        gmData.Scripts.AddScript("get_spring_ball", "global.jumpball = 1; global.hasJumpball = 1; ");
        gmData.Scripts.AddScript("get_screw_attack", "global.screwattack = 1; global.hasScrewattack = 1; with (oCharacter) sfx_stop(spinjump_sound);");
        gmData.Scripts.AddScript("get_varia", """
                                              global.SuitChange = !global.skipItemFanfare;
                                              // If any Metroid exists, force suit cutscene to be off
                                              if (!((instance_number(oMAlpha) <= 0) && (instance_number(oMGamma) <= 0) && (instance_number(oMZeta) <= 0) && (instance_number(oMOmega) <= 0)))
                                                  global.SuitChange = 0;
                                              if collision_line((x + 8), (y - 8), (x + 8), (y - 32), oSolid, false, true)
                                                  global.SuitChange = 0;
                                              if (!(collision_point((x + 8), (y + 8), oSolid, 0, 1)))
                                                  global.SuitChange = 0;
                                              if (room == rm_transition || room == rm_loading || room == itemroom || object_index == oControl)
                                                  global.SuitChange = 0;
                                              global.SuitChangeX = x;
                                              global.SuitChangeY = y;
                                              global.SuitChangeGravity = 0;

                                              var hasOCharacter = false;
                                              global.hasVaria = 1;
                                              with (oCharacter)
                                              {
                                                  hasOCharacter = true
                                                  alarm[1] = 1;
                                              }
                                              if (!hasOCharacter) global.currentsuit = 1
                                              """);
        gmData.Scripts.AddScript("get_space_jump", "global.spacejump = 1; global.hasSpacejump = 1; with (oCharacter) sfx_stop(spinjump_sound);");
        gmData.Scripts.AddScript("get_speed_booster", "global.speedbooster = 1; global.hasSpeedbooster = 1;");
        gmData.Scripts.AddScript("get_hijump", "global.hijump = 1; global.hasHijump = 1;");
        gmData.Scripts.AddScript("get_progressive_jump", $"if (global.hasSpacejump) exit; else if (global.hasHijump) {{ global.spacejump = 1; global.hasSpacejump = 1; with (oCharacter) sfx_stop(spinjump_sound); itemName = \"{ItemEnum.Spacejump.GetEnumMemberValue()}\";}} else {{ global.hijump = 1; global.hasHijump = 1; itemName = \"{ItemEnum.Hijump.GetEnumMemberValue()}\";}} ");
        gmData.Scripts.AddScript("get_gravity", """
                                                global.SuitChange = !global.skipItemFanfare;
                                                // If any Metroid exists, force suit cutscene to be off
                                                if (!((instance_number(oMAlpha) <= 0) && (instance_number(oMGamma) <= 0) && (instance_number(oMZeta) <= 0) && (instance_number(oMOmega) <= 0)))
                                                    global.SuitChange = 0;
                                                if (collision_line((x + 8), (y - 8), (x + 8), (y - 32), oSolid, false, true))
                                                    global.SuitChange = 0;
                                                if (!(collision_point((x + 8), (y + 8), oSolid, 0, 1)))
                                                    global.SuitChange = 0;
                                                if (room == rm_transition || room == rm_loading || room == itemroom || object_index == oControl)
                                                    global.SuitChange = 0;
                                                global.SuitChangeX = x;
                                                global.SuitChangeY = y;
                                                global.SuitChangeGravity = 1;

                                                global.hasGravity = 1;
                                                var hasOCharacter = false;
                                                with (oCharacter)
                                                {
                                                    hasOCharacter = true
                                                    alarm[4] = 1;
                                                }
                                                if (!hasOCharacter) global.currentsuit = 2
                                                """);
        gmData.Scripts.AddScript("get_progressive_suit", $$"""
                                                         global.SuitChange = !global.skipItemFanfare;
                                                         // If any Metroid exists, force suit cutscene to be off
                                                         if (!((instance_number(oMAlpha) <= 0) && (instance_number(oMGamma) <= 0) && (instance_number(oMZeta) <= 0) && (instance_number(oMOmega) <= 0)))
                                                             global.SuitChange = 0;
                                                         if (collision_line((x + 8), (y - 8), (x + 8), (y - 32), oSolid, false, true))
                                                             global.SuitChange = 0;
                                                         if (!(collision_point((x + 8), (y + 8), oSolid, 0, 1)))
                                                             global.SuitChange = 0;
                                                         if (room == rm_transition || room == rm_loading || room == itemroom || object_index == oControl)
                                                             global.SuitChange = 0;
                                                         global.SuitChangeX = x;
                                                         global.SuitChangeY = y;

                                                         if (global.hasGravity) exit
                                                         else if (global.hasVaria)
                                                         {
                                                             global.hasGravity = 1;
                                                             global.SuitChangeGravity = 1;
                                                             itemName = "{{ItemEnum.Gravity.GetEnumMemberValue()}}"
                                                         }
                                                         else
                                                         {
                                                             global.hasVaria = 1;
                                                             global.SuitChangeGravity = 0;
                                                             itemName = "{{ItemEnum.Varia.GetEnumMemberValue()}}"
                                                         }
                                                         var hasOCharacter = false;
                                                         with (oCharacter)
                                                         {
                                                             hasOCharacter = true;
                                                             if (global.hasGravity)
                                                                 alarm[4] = 1;
                                                             else if (global.hasVaria)
                                                                 alarm[1] = 1;
                                                         }
                                                         if (!hasOCharacter)
                                                         {
                                                             if (global.hasGravity)
                                                                 global.currentsuit = 2;
                                                             else if (global.hasVaria)
                                                                 global.currentsuit = 1;
                                                         }
                                                         """);
        gmData.Scripts.AddScript("get_charge_beam", "global.cbeam = 1; global.hasCbeam = 1;");
        gmData.Scripts.AddScript("get_ice_beam", "global.ibeam = 1; global.hasIbeam = 1; ");
        gmData.Scripts.AddScript("get_wave_beam", "global.wbeam = 1; global.hasWbeam = 1;");
        gmData.Scripts.AddScript("get_spazer_beam", "global.sbeam = 1; global.hasSbeam = 1;");
        gmData.Scripts.AddScript("get_plasma_beam", "global.pbeam = 1; global.hasPbeam = 1;");
        gmData.Scripts.AddScript("get_morph_ball", "global.morphball = 1; global.hasMorph = 1; ");
        gmData.Scripts.AddScript("get_small_health_drop", "global.playerhealth += argument0; if (global.playerhealth > global.maxhealth) global.playerhealth = global.maxhealth");
        gmData.Scripts.AddScript("get_big_health_drop", "global.playerhealth += argument0; if (global.playerhealth > global.maxhealth) global.playerhealth = global.maxhealth");
        gmData.Scripts.AddScript("get_missile_drop", "global.missiles += argument0; if (global.missiles > global.maxmissiles) global.missiles = global.maxmissiles");
        gmData.Scripts.AddScript("get_super_missile_drop", "global.smissiles += argument0; if (global.smissiles > global.maxsmissiles) global.smissiles = global.maxsmissiles ");
        gmData.Scripts.AddScript("get_power_bomb_drop", "global.pbombs += argument0; if (global.pbombs > global.maxpbombs) global.pbombs = global.maxpbombs");
        gmData.Scripts.AddScript("get_flashlight", "global.flashlightLevel += argument0; with (oLightEngine) instance_destroy(); with (oFlashlight64) instance_destroy(); if (instance_exists(oCharacter)) ApplyLightPreset();");
        gmData.Scripts.AddScript("get_blindfold", "global.flashlightLevel -= argument0; with (oLightEngine) instance_destroy(); with (oFlashlight64) instance_destroy(); if (instance_exists(oCharacter)) ApplyLightPreset();");
        gmData.Scripts.AddScript("get_speed_booster_upgrade", "global.speedBoosterFramesReduction += argument0;");
        gmData.Scripts.AddScript("get_walljump_upgrade", "global.hasWJ = 1;");
        gmData.Scripts.AddScript("get_IBJ_upgrade", "global.hasIBJ = 1;");
        gmData.Scripts.AddScript("get_long_beam", "global.hasLongBeam = 1;");

        // Modify every location item, to give the wished item, spawn the wished text and the wished sprite
        foreach ((string pickupName, PickupObject pickup) in seedObject.PickupObjects)
        {
            UndertaleGameObject? gmObject = gmData.GameObjects.ByName(pickupName);
            gmObject.Sprite = gmData.Sprites.ByName(pickup.SpriteDetails.Name);
            if (gmObject.Sprite is null && !String.IsNullOrWhiteSpace(pickup.SpriteDetails.Name))
            {
                throw new NotSupportedException($"The sprite for {pickupName} ({gmObject.Name.Content}) cannot be null!");
            }

            // First 0 is for creation event
            UndertaleCode? createCode = gmObject.Events[0][0].Actions[0].CodeId;
            createCode.AppendGMLInCode($"image_speed = {pickup.SpriteDetails.Speed}; text1 = \"{pickup.Text.Header}\"; text2 = \"{pickup.Text.Description}\";" +
                                        $"btn1_name = \"\"; btn2_name = \"\"; itemName = \"{pickup.ItemEffect.GetEnumMemberValue()}\"; itemQuantity = {pickup.Quantity};");
            // First 4 is for Collision event
            UndertaleCode? collisionCode = gmObject.Events[4][0].Actions[0].CodeId;
            string collisionCodeToBe = pickup.ItemEffect switch
            {
                ItemEnum.EnergyTank => "get_etank()",
                ItemEnum.MissileExpansion => $"get_missile_expansion({pickup.Quantity})",
                ItemEnum.MissileLauncher => "if (active) " +
                                            $"{{ get_missile_launcher({pickup.Quantity}) }} event_inherited(); ",
                ItemEnum.SuperMissileExpansion => $"get_super_missile_expansion({pickup.Quantity})",
                ItemEnum.SuperMissileLauncher => "if (active) " +
                                                 $"{{ get_super_missile_launcher({pickup.Quantity}) }} event_inherited(); ",
                ItemEnum.PBombExpansion => $"get_pb_expansion({pickup.Quantity})",
                ItemEnum.PBombLauncher => "if (active) " +
                                          $"{{ get_pb_launcher({pickup.Quantity}) }} event_inherited(); ",
                var x when Enum.GetName(x).StartsWith("DNA") => "event_inherited(); if (active) { get_dna() }",
                ItemEnum.Bombs => "btn1_name = \"Fire\"; event_inherited(); if (active) { get_bombs(); }",
                ItemEnum.Powergrip => "event_inherited(); if (active) { get_power_grip() }",
                ItemEnum.Spiderball => "btn1_name = \"Aim\"; event_inherited(); if (active) { get_spider_ball() }",
                ItemEnum.Springball => "btn1_name = \"Jump\"; event_inherited(); if (active) { get_spring_ball() }",
                ItemEnum.Screwattack => "event_inherited(); if (active) { get_screw_attack() } ",
                ItemEnum.Varia => """
                                      event_inherited()
                                      if (active)
                                      {
                                          get_varia()
                                      }
                                  """,
                ItemEnum.Spacejump => "event_inherited(); if (active) { get_space_jump()  } ",
                ItemEnum.Speedbooster => "event_inherited(); if (active) { get_speed_booster() }",
                ItemEnum.Hijump => "event_inherited(); if (active) { get_hijump() }",
                ItemEnum.ProgressiveJump =>
                    "if (active) { get_progressive_jump() }; event_inherited(); ",
                ItemEnum.Gravity => """
                                        event_inherited();
                                        if (active)
                                        {
                                            get_gravity()
                                        }
                                    """,
                ItemEnum.ProgressiveSuit => """
                                                if (active)
                                                {
                                                    get_progressive_suit()
                                                };
                                                event_inherited();
                                            """,
                ItemEnum.Charge => "btn1_name = \"Fire\"; event_inherited(); if (active) { get_charge_beam() }",
                ItemEnum.Ice => "event_inherited(); if (active) { get_ice_beam() }",
                ItemEnum.Wave => "event_inherited(); if (active) { get_wave_beam() }",
                ItemEnum.Spazer => "event_inherited(); if (active) { get_spazer_beam() }",
                ItemEnum.Plasma => "event_inherited(); if (active) { get_plasma_beam() }",
                ItemEnum.Morphball => "event_inherited(); if (active) { get_morph_ball() }",
                ItemEnum.SmallHealthDrop =>
                    $"event_inherited(); if (active) {{ get_small_health_drop({pickup.Quantity}); }}",
                ItemEnum.BigHealthDrop =>
                    $"event_inherited(); if (active) {{ get_big_health_drop({pickup.Quantity}); }}",
                ItemEnum.MissileDrop =>
                    $"event_inherited(); if (active) {{ get_missile_drop({pickup.Quantity}); }}",
                ItemEnum.SuperMissileDrop =>
                    $"event_inherited(); if (active) {{ get_super_missile_drop({pickup.Quantity}); }}",
                ItemEnum.PBombDrop =>
                    $"event_inherited(); if (active) {{ get_power_bomb_drop({pickup.Quantity}); }}",
                ItemEnum.Flashlight =>
                    $"event_inherited(); if (active) {{ get_flashlight({pickup.Quantity}); }}",
                ItemEnum.Blindfold =>
                    $"event_inherited(); if (active) {{ get_blindfold({pickup.Quantity}); }}",
                ItemEnum.SpeedBoosterUpgrade => $"event_inherited(); if (active) {{ get_speed_booster_upgrade({pickup.Quantity}); }}",
                ItemEnum.WalljumpBoots => "event_inherited(); if (active) { get_walljump_upgrade(); }",
                ItemEnum.InfiniteBombPropulsion => "event_inherited(); if (active) { get_IBJ_upgrade(); }",
                ItemEnum.LongBeam => "event_inherited(); if (active) { get_long_beam(); }",
                ItemEnum.Nothing => "event_inherited();",
                _ => throw new NotSupportedException("Unsupported item! " + pickup.ItemEffect)
            };
            collisionCode.SubstituteGMLCode(collisionCodeToBe);
        }

        // Modify how much expansions give
        missileCharacterEvent.ReplaceGMLInCode("""
                                                   if (global.difficulty < 2)
                                                       global.maxmissiles += 5
                                                   if (global.difficulty == 2)
                                                       global.maxmissiles += 2
                                               """, $"""
                                                         global.maxmissiles += argument0
                                                     """);

        superMissileCharacterEvent.ReplaceGMLInCode("""
                                                        if (global.difficulty < 2)
                                                            global.maxsmissiles += 2
                                                        if (global.difficulty == 2)
                                                            global.maxsmissiles += 1
                                                    """, $"""
                                                              global.maxsmissiles += argument0
                                                          """);

        pBombCharacterEvent.ReplaceGMLInCode("""
                                                 if (global.difficulty < 2)
                                                     global.maxpbombs += 2
                                                 if (global.difficulty == 2)
                                                     global.maxpbombs += 1
                                             """, $"""
                                                       global.maxpbombs += argument0
                                                   """);


        // Also change how gui health is drawn
        gmData.Code.ByName("gml_Script_gui_health").ReplaceGMLInCode("""
                                                                     if (ceil(guih) == 100)
                                                                         guih = 99
                                                                     """, $"""
                                                                           guih = ceil((global.playerhealth % {seedObject.Patches.EnergyPerTank}));
                                                                           if (ceil(guih) == {seedObject.Patches.EnergyPerTank})
                                                                               guih = {seedObject.Patches.EnergyPerTank - 1};
                                                                           """);

        // Draw_gui has a huge fucking block that does insane etank shenanigans
        // because i dont want to copypaste the whole thing into here, i'll get the index where it starts, where it ends, and replace that section with my own
        string? drawGuiText = Decompiler.Decompile(drawGuiCode, decompileContext);
        int drawStartIndex = drawGuiText.IndexOf("if (global.etanks >= 1)");
        int drawEndIndex = drawGuiText.IndexOf("draw_set_font(global.guifont2)");
        string etankSnippet = drawGuiText.Substring(drawStartIndex, drawEndIndex - drawStartIndex);
        drawGuiCode.ReplaceGMLInCode(etankSnippet, $$"""
                                                     var isHealthPerTankOverHundo = {{(seedObject.Patches.EnergyPerTank > 100).ToString().ToLower()}}
                                                     if (isHealthPerTankOverHundo)
                                                     {
                                                         xoff += 12
                                                         etankxoff += 12
                                                     }
                                                     for (var i = 1; i<= 30; i++ )
                                                     {
                                                       if (global.etanks < i) break;
                                                       var etankIndex = 0
                                                       if (global.playerhealth > ({{seedObject.Patches.EnergyPerTank - 0.01}} + ((i-1)*{{seedObject.Patches.EnergyPerTank}})))
                                                         etankIndex = 1;
                                                       var drawXOff = (floor((i-1)/2) * 6) + (floor((i-1) / 10) * 3)
                                                       var drawYOff = 4;
                                                       if (i % 2 == 0) drawYOff = 10
                                                       draw_sprite(sGUIETank, etankIndex, (0+etankxoff+drawXOff), drawYOff)
                                                     }

                                                     """);

        // Turn off Septoggs if the wished configuration
        if (seedObject.Patches.SeptoggHelpers) characterVarsCode.ReplaceGMLInCode("global.septoggHelpers = 0", "global.septoggHelpers = 1");

        foreach (UndertaleCode? code in gmData.Code.Where(c => c.Name.Content.StartsWith("gml_Script_scr_septoggs_")))
        {
            code.PrependGMLInCode("if (!global.septoggHelpers) return true; else return false;");
        }

        UndertaleGameObject? elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (UndertaleRoom.GameObject go in room.GameObjects.Where(go => go.ObjectDefinition == elderSeptogg && go.CreationCode is not null))
            {
                go.CreationCode.ReplaceGMLInCode("oControl.mod_septoggs_bombjumps_easy == 0 && global.hasBombs == 1",
                    "!global.septoggHelpers", true);
            }
        }

        gmData.Code.ByName("gml_RoomCC_rm_a0h25_4105_Create").ReplaceGMLInCode("else if (global.hasBombs == 1 || global.hasSpiderball == 1 || global.hasSpacejump == 1)",
            "else if (!global.septoggHelpers)");
        // Make these septoggs always appear instead of only when coming from certain room
        gmData.Code.ByName("gml_RoomCC_rm_a2a13_5007_Create").ReplaceGMLInCode("&& oControl.mod_previous_room == 103", "");
        gmData.Code.ByName("gml_RoomCC_rm_a3a07_5533_Create").ReplaceGMLInCode("&& oControl.mod_previous_room == 136", "");
        gmData.Code.ByName("gml_RoomCC_rm_a5a05_8701_Create").ReplaceGMLInCode("&& oControl.mod_previous_room == 300", "");


        // Options to turn off the random room geometry changes!
        MandatoryGeometryChanges.Apply(gmData, decompileContext, seedObject);
        ScrewPipeBlocks.Apply(gmData, decompileContext, seedObject);
        BombBeforeA3.Apply(gmData, decompileContext, seedObject);
        SoftlockPrevention.Apply(gmData, decompileContext, seedObject);
        DontRespawnBombBlocks.Apply(gmData, decompileContext, seedObject);


        // On start, make all rooms show being "unexplored" similar to prime/super rando
        ShowFullyUnexploredMap.Apply(gmData, decompileContext, seedObject);

        // Force all breakables (except the hidden super blocks) to be visible
        if (seedObject.Cosmetics.UnveilBlocks) characterVarsCode.ReplaceGMLInCode("global.unveilBlocks = 0", "global.unveilBlocks = 1");

        gmData.Code.ByName("gml_Object_oSolid_Alarm_5").AppendGMLInCode("if (global.unveilBlocks && sprite_index >= sBlockShoot && sprite_index <= sBlockSand)\n" +
                                                                        "{ event_user(1); visible = true; }");

        GameplayCutsceneSkip.Apply(gmData, decompileContext, seedObject);

        // Shorten save animation
        if (seedObject.Patches.SkipSaveCutscene) characterVarsCode.ReplaceGMLInCode("global.skipSaveCutscene = 0", "global.skipSaveCutscene = 1");

        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("""
                                                                                 if (statetime == 1)
                                                                                 {
                                                                                     sfx_play(sndSave)
                                                                                     instance_create(x, y, oSaveFX)
                                                                                     instance_create(x, y, oSaveSparks)
                                                                                     popup_text(get_text("Notifications", "GameSaved"))
                                                                                     save_game(("save" + string((global.saveslot + 1))))
                                                                                     refill_heath_ammo()
                                                                                 }
                                                                                 if (statetime == 230)
                                                                                     state = IDLE
                                                                             """, """
                                                                                      if (statetime == 1)
                                                                                      {
                                                                                          sfx_play(sndSave)
                                                                                          if (!global.skipSaveCutscene)
                                                                                          {
                                                                                              instance_create(x, y, oSaveFX)
                                                                                              instance_create(x, y, oSaveSparks)
                                                                                          }
                                                                                          popup_text(get_text("Notifications", "GameSaved"))
                                                                                          save_game(("save" + string((global.saveslot + 1))))
                                                                                          refill_heath_ammo()
                                                                                      }
                                                                                      if ((statetime == 230 && !global.skipSaveCutscene) || (statetime == 10 && global.skipSaveCutscene))
                                                                                          state = IDLE
                                                                                  """);

        // Skip Item acquisition fanfares
        if (seedObject.Patches.SkipItemFanfares) characterVarsCode.ReplaceGMLInCode("global.skipItemFanfare = 0", "global.skipItemFanfare = 1");

        // Put all items as type one
        gmData.Code.ByName("gml_Object_oItem_Other_10").PrependGMLInCode("if (global.skipItemFanfare) itemtype = 1;");

        // Show popup text only when we skip the cutscene
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("if (itemtype == 1)", "if (global.skipItemFanfare)");
        // Removes cutscenes for type 1's
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("display_itemmsg", "if (!global.skipItemFanfare) display_itemmsg");

        // Add DNA check to Baby trigger
        gmData.Code.ByName("gml_Object_oHatchlingTrigger_Collision_267").PrependGMLInCode(
            """
            if (global.dna < 46)
            {
                popup_text("Collect all the DNA to hatch the Baby")
                instance_destroy()
                exit
            }
            """);

        // Patch to add room name display near health
        DisplayRoomNameOnHUD.Apply(gmData, decompileContext, seedObject);

        // Set fusion mode value
        gmData.Code.ByName("gml_Object_oControl_Step_0").ReplaceGMLInCode("mod_fusion = 0", $"mod_fusion = {(seedObject.Patches.FusionMode ? 1 : 0)}");

        // Display Seed hash
        gmData.Code.ByName("gml_Object_oGameSelMenu_Draw_0").AppendGMLInCode($"""
                                                                              draw_set_font(global.fontGUI2)
                                                                              draw_set_halign(fa_center)
                                                                              draw_cool_text(160, 5, "{seedObject.Identifier.RDVVersion} - {seedObject.Identifier.PatcherVersion}", c_black, c_white, c_white, 1)
                                                                              draw_cool_text(160, 15, "{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})", c_black, c_white, c_white, 1)
                                                                              draw_set_halign(fa_left)
                                                                              """);

        // Set option on whether supers can destroy missile doors
        if (seedObject.Patches.CanUseSupersOnMissileDoors) characterVarsCode.ReplaceGMLInCode("global.canUseSupersOnMissileDoors = 0", "global.canUseSupersOnMissileDoors = 1");

        // TODO: For the future, with room rando, go through each door and modify where it leads to

        // Ad in-game Hints
        AddInGameHints.Apply(gmData, decompileContext, seedObject);

        // Pipe rando
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

        // Make Bosses now spawns PB drops on death
        gmData.Code.ByName("gml_Script_spawn_many_powerups").ReplaceGMLInCode("if ((global.hasBombs == 0 && global.maxpbombs > 0) || (oControl.mod_insanitymode == 1 && global.maxpbombs > 0))", "if (global.maxpbombs > 0)");

        // Add patch to see room names on minimap
        DisplayRoomNamesOnMap.Apply(gmData, decompileContext, seedObject);

        // Adjust pause screen text to mention room names
        RoomFeatureMapText.Apply(gmData, decompileContext);


        // Add spoiler log in credits when finished game normally
        gmData.Code.ByName("gml_Object_oCreditsText_Create_0")
            .ReplaceGMLInCode("TEXT_ROWS = ", $"if (!global.creditsmenuopt) text = \"{seedObject.CreditsSpoiler}\" + text;\n TEXT_ROWS = ");

        // Multiworld stuff
        Multiworld.Apply(gmData, decompileContext, seedObject);

        // Write back to disk
        using (FileStream fs = new FileInfo(outputAm2rPath).OpenWrite())
        {
            UndertaleIO.Write(fs, gmData, Console.WriteLine);
        }
    }
}
