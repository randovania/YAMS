using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class ModifyItems
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? missileCharacterEvent = gmData.Code.ByName("gml_Script_scr_missile_character_event");
        UndertaleCode? superMissileCharacterEvent = gmData.Code.ByName("gml_Script_scr_supermissile_character_event");
        UndertaleCode? pBombCharacterEvent = gmData.Code.ByName("gml_Script_scr_powerbomb_character_event");


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
        gmData.Scripts.AddScript("get_arm_cannon", "global.hasArmCannon = 1;");
        gmData.Scripts.AddScript("get_alpha_lure", "global.hasAlphaLure = 1;");
        gmData.Scripts.AddScript("get_gamma_lure", "global.hasGammaLure = 1;");
        gmData.Scripts.AddScript("get_zeta_lure", "global.hasZetaLure = 1;");
        gmData.Scripts.AddScript("get_omega_lure", "global.hasOmegaLure = 1;");


        // Modify every location item, to give the wished item, spawn the wished text and the wished sprite
        foreach ((string pickupName, PickupObject pickup) in seedObject.PickupObjects)
        {
            UndertaleGameObject? gmObject = gmData.GameObjects.ByName(pickupName);
            gmObject.Sprite = gmData.Sprites.ByName(pickup.SpriteDetails.Name);
            if (gmObject.Sprite is null && !String.IsNullOrWhiteSpace(pickup.SpriteDetails.Name))
            {
                throw new NotSupportedException($"The sprite for {pickupName} ({gmObject.Name.Content}) cannot be null! (Sprite name was \"{pickup.SpriteDetails.Name}\")");
            }

            // First 0 is for creation event
            UndertaleCode? createCode = gmObject.Events[0][0].Actions[0].CodeId;
            createCode.AppendGMLInCode($"image_speed = {pickup.SpriteDetails.Speed}; text1 = \"{pickup.Text.Header}\"; text2 = \"{pickup.Text.Description}\"; btn1_name = \"\"; btn2_name = \"\"; itemName = \"{pickup.ItemEffect.GetEnumMemberValue()}\"; itemQuantity = {pickup.Quantity};");
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
                ItemEnum.ArmCannon => "event_inherited(); if (active) { get_arm_cannon(); }",
                ItemEnum.AlphaLure => "event_inherited(); if (active) {get_alpha_lure(); }",
                ItemEnum.GammaLure => "event_inherited(); if (active) {get_gamma_lure(); }",
                ItemEnum.ZetaLure => "event_inherited(); if (active) {get_zeta_lure(); }",
                ItemEnum.OmegaLure => "event_inherited(); if (active) {get_omega_lure(); }",
                ItemEnum.Nothing => "event_inherited();",
                _ => throw new NotSupportedException("Unsupported item! " + pickup.ItemEffect),
            };

            collisionCode.SubstituteGMLCode(collisionCodeToBe);
        }

        // Modify how much expansions give
        missileCharacterEvent.ReplaceGMLInCode("""
                                                   if (global.difficulty < 2)
                                                   {
                                                       global.maxmissiles += 5;
                                                   }
                                                   if (global.difficulty == 2)
                                                   {
                                                       global.maxmissiles += 2;
                                                   }
                                               """, $"""
                                                         global.maxmissiles += argument0
                                                     """);

        superMissileCharacterEvent.ReplaceGMLInCode("""
                                                        if (global.difficulty < 2)
                                                        {
                                                            global.maxsmissiles += 2;
                                                        }
                                                        if (global.difficulty == 2)
                                                        {
                                                            global.maxsmissiles += 1;
                                                        }
                                                    """, $"""
                                                              global.maxsmissiles += argument0
                                                          """);

        pBombCharacterEvent.ReplaceGMLInCode("""
                                                 if (global.difficulty < 2)
                                                 {
                                                     global.maxpbombs += 2;
                                                 }
                                                 if (global.difficulty == 2)
                                                 {
                                                     global.maxpbombs += 1;
                                                 }
                                             """, $"""
                                                       global.maxpbombs += argument0
                                                   """);
    }
}
