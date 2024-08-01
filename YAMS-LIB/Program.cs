using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using SixLabors.ImageSharp.Processing;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using YAMS_LIB.patches;
using YAMS_LIB.patches.geometry;
using YAMS_LIB.patches.misc;
using System.Diagnostics;
using YAMS_LIB.patches.qol;

namespace YAMS_LIB;

public class Patcher
{
    public static string Version = CreateVersionString();
    internal static UndertaleData? gmData;
    internal static GlobalDecompileContext? decompileContext;
    internal static bool isHorde = false;

    internal static Dictionary<UndertaleCode, string> CodeCache = new Dictionary<UndertaleCode, string>(1024);

    private static string CreateVersionString()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        string? version = fileVersionInfo.ProductVersion;

        if (version is null) return "";

        string[] split = version.Split('.');
        string major = split[0];
        string minor = split[1];
        string build = split[2];
        if (build.Contains('+'))
            build = build[..build.IndexOf('+')];
        build = build.Replace('-', '.');
        if (build[2..].StartsWith("rc"))
            build = build[0] + build[2..];

        return $"{major}.{minor}.{build}";
    }

    public static void Main(string am2rPath, string outputAm2rPath, string jsonPath)
    {
        // Change this to not have to deal with floating point madness
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        SeedObject? seedObject = JsonSerializer.Deserialize<SeedObject>(File.ReadAllText(jsonPath));

        Stopwatch sw = new Stopwatch();
        sw.Start();

        // Read 1.5.x data
        using (FileStream fs = new FileInfo(am2rPath).OpenRead())
        {
            gmData = UndertaleIO.Read(fs);
        }
        sw.Stop();
        var afterRead = sw.Elapsed;
        sw.Start();

        Console.WriteLine("Read data file.");
        decompileContext = new GlobalDecompileContext(gmData, false);

        // Check for 1.5.5 before doing *anything*
        string controlCreate = gmData.Code.ByName("gml_Object_oControl_Create_0").GetGMLCode();
        bool useAnyVersion = Environment.GetEnvironmentVariable("YAMS_USE_ANY_AM2R_VERSION") == "true";
        if (!useAnyVersion && !controlCreate.Contains("global.am2r_version = \"V1.5.5\"")) throw new InvalidAM2RVersionException("The selected game is not AM2R 1.5.5!");

        isHorde = gmData.Code.ByName("gml_Object_oDrawTitleBG_Create_0").GetGMLCode().Contains("hordeversion = \"The Horde");

        // Important invasive modifications done first

        // Decouple the item locations from the actual items
        DecoupleItemsFromLocations.Apply(gmData, decompileContext, seedObject);

        // Use custom darkness levels
        CustomDarknessLevels.Apply(gmData, decompileContext, seedObject);

        // Use custom liquid info
        CustomWaterLevel.Apply(gmData, decompileContext, seedObject);

        // Run these in parallel to speed up performance slightly
        List<Task> nonCodeTasks = new List<Task>();

        // Import new Sprites (can't run it parallel, 'cause some edits rely on this being done first.)
        Sprites.Apply(gmData, decompileContext, seedObject);
        // Apply cosmetic patches
        nonCodeTasks.Add(Task.Run(() => CosmeticRotation.Apply(gmData, decompileContext, seedObject)));
        // Shuffle Music
        nonCodeTasks.Add(Task.Run(() => MusicShuffle.ShuffleMusic(Path.GetDirectoryName(outputAm2rPath), seedObject.Cosmetics.MusicShuffleDict)));

        // TODO: move this further down, when we actually need the results? Dunno if that's actually better, would rely on non-thread safe utmt stuff.
        Task.WhenAll(nonCodeTasks).GetAwaiter().GetResult();

        // Fix songs that break if they're too long
        FixOverlappingSongs.Apply(gmData, decompileContext, seedObject);


        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Apply randovania themed stuff ("randovania" mode, credits, extras unlocked etc.)
        RandovaniaStuff.Apply(gmData, decompileContext, seedObject);


        // Fix crashes where rooms try to access these vars in the starting room
        gmData.Code.ByName("gml_Script_set_start_location").AppendGMLInCode("global.targetx = global.save_x; global.targety = global.save_y;");

        // Fix visual edge case discrepancy with time of day
        gmData.Code.ByName("gml_Room_rm_a8h01_Create").PrependGMLInCode("global.timeofday = 1;");



        // Make fusion only a damage multiplier, leaving the fusion stuff up to a setting
        gmData.Code.ByName("gml_Object_oControl_Step_0").PrependGMLInCode("mod_fusion = 0;");

        // Fix varia cutscene
        gmData.Code.ByName("gml_Object_oSuitChangeFX_Step_0").ReplaceGMLInCode("bg1alpha = 0", "bg1alpha = 0; instance_create(x, y, oSuitChangeFX2);");
        gmData.Code.ByName("gml_Object_oSuitChangeFX2_Create_0").ReplaceGMLInCode("image_index = 1133", "sprite_index = sSuitChangeFX2_fusion");


        if (!isHorde)
        {
            // Don't respawn weapons when offscreen
            DontDespawnWeaponsOffscreen.Apply(gmData, decompileContext, seedObject);

            // Fix arachnus event value doing x coordinate BS
            gmData.Code.ByName("gml_Object_oArachnus_Alarm_11").ReplaceGMLInCode("global.event[103] = x", "global.event[103] = 1");
            // Make arachnus item location always spawn in center
            gmData.Code.ByName("gml_Room_rm_a2a04_Create").ReplaceGMLInCode("instance_create(global.event[103]", "instance_create(room_width / 2");

        }

        // Killing queen should not lock you out of the rest of the game
        gmData.Rooms.ByName("rm_a0h01").GameObjects.Remove(gmData.Rooms.ByName("rm_a0h01").GameObjects.First(go => go.X == 4432 && go.Y == 992 && (Math.Abs(go.ScaleY - 4.0) < 0.1)));
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
        gmData.Code.ByName("gml_Object_oDoor_Alarm_0").ReplaceGMLInCode("lock = 0", "if (lock <= 4) lock = 0;");

        // Make doors when unlocked, go to the type they were. Only go to ammo doors, if they haven't been destroyed yet
        gmData.Code.ByName("gml_Object_oDoor_Create_0").AppendGMLInCode("originalLock = lock; wasPassedThrough = false;");
        gmData.Code.ByName("gml_Object_oDoor_Alarm_0").ReplaceGMLInCode("event_user(2)", "event_user(2); wasPassedThrough = true;");
        gmData.Code.ByName("gml_Object_oDoor_Other_13").ReplaceGMLInCode("lock = 0", "lock = originalLock; if (originalLock < 4 && wasPassedThrough) lock = 0");

        // Fix doors unlocking in arachnus/torizo/tester/serris/genesis
        gmData.Code.ByName("gml_Room_rm_a2a04_Create").AppendGMLInCode("if (!global.event[103]) {with (oDoor) lock = 4;}");
        gmData.Code.ByName("gml_Room_rm_a3a01_Create").AppendGMLInCode("if (!global.event[152]) {with (oDoor) lock = 4;}");
        gmData.Code.ByName("gml_Room_rm_a4a05_Create").AppendGMLInCode("if (!global.event[207]) {with (oDoor) lock = 4;}");
        gmData.GameObjects.ByName("oErisBossTrigger").EventHandlerFor(EventType.Step, gmData).SubstituteGMLCode("with (oDoor) lock = 4;");
        gmData.Code.ByName("gml_Room_rm_a8a11_Create").AppendGMLInCode("if (!global.event[307]) {with (oDoor) lock = 4;}");

        // Fix doors in tester to be always blue
        foreach (var door in gmData.Rooms.ByName("rm_a4a05").GameObjects.Where(go => go.ObjectDefinition.Name.Content == "oDoorA4"))
        {
            door.CreationCode.SubstituteGMLCode("lock = 0;");
        }

        // Fix tester being fought in darkness / proboscums being disabled on not activated tower
        gmData.Code.ByName("gml_Object_oTesterBossTrigger_Other_10").PrependGMLInCode(
            "global.darkness = 0; with (oLightEngine) instance_destroy(); with (oFlashlight64); instance_destroy()");
        gmData.Code.ByName("gml_Object_oProboscum_Create_0").AppendGMLInCode("active = true; image_index = 0;");

        // Fix tester events sharing an event with tower activated - moved tester to 207
        gmData.Rooms.ByName("rm_a4a04").GameObjects.First(go => go.X == 24 && go.Y == 80 && go.ObjectDefinition.Name.Content == "oDoorA4").CreationCode.ReplaceGMLInCode("global.event[200] < 2", "!global.event[207]");
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
        foreach (var roomName in new[] { "rm_a7b05", "rm_a7b06", "rm_a7b06A", "rm_a7b07", "rm_a7b08", "rm_a7b08A" })
        {
            foreach (var door in gmData.Rooms.ByName(roomName).GameObjects.Where(go => go.ObjectDefinition.Name.Content == "oDoor"))
            {
                door.CreationCode.SubstituteGMLCode("");
            }
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
        // perma locked doesn't have a number and is never set here as being open-able.
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

        // Fix Emp devices unlocking all doors automatically! - TODO: move these into door lock rando patch
        string empBatteryCellCondition = "false";
        var a1EMPID = gmData.Rooms.ByName("rm_a1a06").GameObjects.First(go => go.X == 296 && go.Y == 1104 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var a2EMPID = gmData.Rooms.ByName("rm_a2c01").GameObjects.First(go => go.X == 24 && go.Y == 592 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var a3EMPID = gmData.Rooms.ByName("rm_a3h08").GameObjects.First(go => go.X == 24 && go.Y == 368 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var nearEscapeEMPID = gmData.Rooms.ByName("rm_a5c09").GameObjects.First(go => go.X == 24 && go.Y == 624 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var robotHomeEMPID = gmData.Rooms.ByName("rm_a5c10").GameObjects.First(go => go.X == 24 && go.Y == 80 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var a5RightTowerLeftDoorEMPID = gmData.Rooms.ByName("rm_a5c11").GameObjects.First(go => go.X == 24 && go.Y == 608 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var a5RightTowerRightDoorEMPID = gmData.Rooms.ByName("rm_a5c11").GameObjects.First(go => go.X == 296 && go.Y == 608 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var pipeHubEMPID = gmData.Rooms.ByName("rm_a5c15").GameObjects.First(go => go.X == 24 && go.Y == 128 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var nearSAEMPID = gmData.Rooms.ByName("rm_a5c21").GameObjects.First(go => go.X == 616 && go.Y == 320 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        var nearA5ExteriorEMPID = gmData.Rooms.ByName("rm_a5c33").GameObjects.First(go => go.X == 24 && go.Y == 128 && go.ObjectDefinition.Name.Content.StartsWith("oDoor")).InstanceID;
        foreach (uint doorID in new uint[] { a1EMPID, a2EMPID, a3EMPID, nearEscapeEMPID, robotHomeEMPID, a5RightTowerLeftDoorEMPID, a5RightTowerRightDoorEMPID, pipeHubEMPID, nearSAEMPID, nearA5ExteriorEMPID })
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
        foreach (var doorObject in gmData.Rooms.ByName("rm_a5c07").GameObjects.Where(go => go.ObjectDefinition.Name.Content == "oDoorA5"))
        {
            if (!seedObject.DoorLocks.ContainsKey(doorObject.InstanceID)) a5ActivateCondition += $" || id == {doorObject.InstanceID}";
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


        // Destroy turbines and set the event to fully complete if entering "Water Turbine Station" at bottom doors and to "water should be here" if entering from the top.
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
        gmData.Rooms.ByName("rm_a4a09").GameObjects.First(go => go.X == 24 && go.Y == 80 && go.ObjectDefinition.Name.Content == "oDoorA4").CreationCode.ReplaceGMLInCode("lock = 1", "lock = 0;");

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

        // Implement dna as an item and all its dependencies
        AddDNAItem.Apply(gmData, decompileContext, seedObject);

        // In vanilla, you need empty ammo for charge beam to work. fix that.
        if (!isHorde)
            MakeChargeBeamAlwaysHitMetroids.Apply(gmData, decompileContext, seedObject);

        // Add shortcut between nest and hideout
        if (seedObject.Patches.NestPipes)
        {
            AddA6Pipes.Apply(gmData, decompileContext, seedObject);
        }

        // Move alpha in nest
        if (!isHorde)
            gmData.Rooms.ByName("rm_a6a09").GameObjects.First(go => go.X == 800 && go.Y == 368 && go.ObjectDefinition.Name.Content == "oMalpha3TriggerProx").CreationCode.ReplaceGMLInCode("if (global.lavastate > 8)", "y = 320; if (false)");
        else
        {
            gmData.Rooms.ByName("rm_a6a09").GameObjects.First(go => go.ObjectDefinition.Name.Content == "oMZeta_Cocoon").CreationCode.ReplaceGMLInCode("if (global.lavastate > 8)", "y = 320; if (false)");
        }

        // Lock these blocks behind a setting because they can make for some interesting changes
        gmData.Code.ByName("gml_Room_rm_a0h07_Create").ReplaceGMLInCode(
            "if (oControl.mod_purerandombool == 1 || oControl.mod_splitrandom == 1 || global.gamemode == 2)",
            $"if ({(!seedObject.Patches.GraveGrottoBlocks).ToString().ToLower()})");

        // Enable randomizer to be always on
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

        // Add required launcher mains
        RequiredMains.Apply(gmData, decompileContext, seedObject, isHorde);

        // Have new variables for certain events because they are easier to debug via a switch than changing a ton of values
        // TODO: move these all into their seperate patches.
        characterVarsCode.PrependGMLInCode("global.canUseSupersOnMissileDoors = 0;");

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

        // Add a "load from start" option
        LoadFromStart.Apply(gmData, decompileContext, seedObject);

        // Add new custom items
        FlashLightItem.Apply(gmData, decompileContext, seedObject);
        IBJItem.Apply(gmData, decompileContext, seedObject);
        LongBeamItem.Apply(gmData, decompileContext, seedObject);
        SpeedBoosterUpgradeItem.Apply(gmData, decompileContext, seedObject);
        WallJumpBootsItem.Apply(gmData, decompileContext, seedObject);

        // Modify save scripts to load our new globals / stuff we modified
        AdjustSavingScripts.Apply(gmData, decompileContext, seedObject);

        // Change starting health and energy per tank - TODO: should get into startingitems patch
        characterVarsCode.ReplaceGMLInCode("global.playerhealth = 99", $"global.playerhealth = {seedObject.Patches.EnergyPerTank - 1};");
        eTankCharacterEvent.ReplaceGMLInCode("global.maxhealth += (100 * oControl.mod_etankhealthmult)", $"global.maxhealth += {seedObject.Patches.EnergyPerTank}");

        // Set starting items
        StartingItems.Apply(gmData, decompileContext, seedObject);

        // Set starting location - TODO: allow custom x/y position in rooms.
        characterVarsCode.ReplaceGMLInCode("global.startingSave = 0", $"global.startingSave = {seedObject.StartingLocation.SaveRoom}");
        characterVarsCode.ReplaceGMLInCode("global.save_room = 0", $"global.save_room = {seedObject.StartingLocation.SaveRoom}");

        // Door locks
        DoorLockRando.Apply(gmData, decompileContext, seedObject, isHorde);

        // Modify every location item, to give the wished item, spawn the wished text and the wished sprite
        ModifyItems.Apply(gmData, decompileContext, seedObject);


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
        UndertaleCode? drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
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
        RemoveHelperSeptoggs.Apply(gmData, decompileContext, seedObject);

        // Options to turn off the random room geometry changes!
        MandatoryGeometryChanges.Apply(gmData, decompileContext, seedObject);
        ScrewPipeBlocks.Apply(gmData, decompileContext, seedObject);
        BombBeforeA3.Apply(gmData, decompileContext, seedObject);
        SoftlockPrevention.Apply(gmData, decompileContext, seedObject, isHorde);
        DontRespawnBombBlocks.Apply(gmData, decompileContext, seedObject);
        EnforceScrewForBG2.Apply(gmData, decompileContext, seedObject);


        // On start, make all rooms show being "unexplored" similar to prime/super rando
        ShowFullyUnexploredMap.Apply(gmData, decompileContext, seedObject, isHorde);

        // Force all breakables (except the hidden super blocks) to be visible
        ShowUnveiledBreakables.Apply(gmData, decompileContext, seedObject);

        // Skip cutscenes and fanfares
        GameplayCutsceneSkip.Apply(gmData, decompileContext, seedObject);
        SaveCutsceneSkip.Apply(gmData, decompileContext, seedObject, isHorde);
        SkipItemFanfares.Apply(gmData, decompileContext, seedObject);

        // Patch to add room name display near health
        DisplayRoomNameOnHUD.Apply(gmData, decompileContext, seedObject);

        // Make the septogg in BG3 spawn directly when killing the Gamma
        BG3MakeSeptoggSpawnImmediately.Apply(gmData, decompileContext, seedObject);

        // Set fusion mode value
        gmData.Code.ByName("gml_Object_oControl_Step_0").ReplaceGMLInCode("mod_fusion = 0", $"mod_fusion = {(seedObject.Patches.FusionMode ? 1 : 0)}");

        // Display Seed hash on title and credits
        gmData.Code.ByName("gml_Object_oGameSelMenu_Draw_0").AppendGMLInCode($"""
                                                                              draw_set_font(global.fontGUI2)
                                                                              draw_set_halign(fa_center)
                                                                              draw_cool_text(160, 5, "{seedObject.Identifier.RDVVersion} - {seedObject.Identifier.PatcherVersion}", c_black, c_white, c_white, 1)
                                                                              draw_cool_text(160, 15, "{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})", c_black, c_white, c_white, 1)
                                                                              draw_set_halign(fa_left)
                                                                              """);

        gmData.Code.ByName("gml_Object_oScoreScreen_Draw_0").ReplaceGMLInCode("draw_text(tx1x, (tx1y + 52), text2a)", $"draw_text(tx1x, (tx1y + 52), text2a); draw_text(tx1x, (tx1y + 80), \"{seedObject.Identifier.RDVVersion}\"); draw_text(tx1x, (tx1y + 92), \"{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})\")");

        // Set option on whether supers can destroy missile doors
        if (seedObject.Patches.CanUseSupersOnMissileDoors) characterVarsCode.ReplaceGMLInCode("global.canUseSupersOnMissileDoors = 0", "global.canUseSupersOnMissileDoors = 1");

        // Add in-game Hints
        AddInGameHints.Apply(gmData, decompileContext, seedObject);

        // Flip game
        FlippedGame.Apply(gmData, decompileContext, seedObject);

        // Add history log entry
        HistoryLogEntry.Apply(gmData, decompileContext, seedObject);

        // Pipe rando
        PipeRando.Apply(gmData, decompileContext, seedObject);

        // Entrance rando
        EntranceRando.Apply(gmData, decompileContext, seedObject);

        // Make Bosses now spawns PB drops on death
        gmData.Code.ByName("gml_Script_spawn_many_powerups").ReplaceGMLInCode("if ((global.hasBombs == 0 && global.maxpbombs > 0) || (oControl.mod_insanitymode == 1 && global.maxpbombs > 0))", "if (global.maxpbombs > 0)");

        // Add patch to see room names on minimap
        DisplayRoomNamesOnMap.Apply(gmData, decompileContext, seedObject);

        // Apply custom suit damage reductions
        CustomSuitDamageReduction.Apply(gmData, decompileContext, seedObject);

        // Add spoiler log in credits when finished game normally
        gmData.Code.ByName("gml_Object_oCreditsText_Create_0")
            .ReplaceGMLInCode("TEXT_ROWS = ", $"if (!global.creditsmenuopt) text = \"{seedObject.CreditsSpoiler}\" + text;\n TEXT_ROWS = ");

        // Multiworld stuff
        Multiworld.Apply(gmData, decompileContext, seedObject);
        AddBossMWTracking.Apply(gmData, decompileContext, seedObject);


        // Write back to disk
        ExtensionMethods.FlushCode();
        sw.Stop();
        var beforeWrite = sw.Elapsed;
        sw.Start();
        using (FileStream fs = new FileInfo(outputAm2rPath).OpenWrite())
        {
            UndertaleIO.Write(fs, gmData, Console.WriteLine);
        }
        sw.Stop();
        Console.WriteLine($"Total Time: {sw.Elapsed}");
        Console.WriteLine($"Patching Time Only: {beforeWrite-afterRead}");
    }
}
