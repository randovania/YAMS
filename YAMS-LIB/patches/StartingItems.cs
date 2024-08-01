using System.Text;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class StartingItems
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        StringBuilder textToAppend = new StringBuilder();

        // Set starting items
        bool alreadyAddedMissiles = false;
        bool alreadyAddedSupers = false;
        bool alreadyAddedPBombs = false;
        textToAppend.AppendLine("global.collectedItems = \"items:\";");
        int howMuchDna = 0;
        foreach ((ItemEnum item, int quantity) in seedObject.StartingItems)
        {
            int finalQuantity = quantity;
            switch (item)
            {
                case ItemEnum.EnergyTank:
                    textToAppend.AppendLine($"global.etanks = {quantity};");
                    textToAppend.AppendLine($"global.playerhealth = {seedObject.Patches.EnergyPerTank + seedObject.Patches.EnergyPerTank * quantity - 1};");
                    break;
                case ItemEnum.LockedMissile:
                case ItemEnum.Missile:
                    if (alreadyAddedMissiles) break;

                    if (item == ItemEnum.Missile && seedObject.StartingItems.TryGetValue(ItemEnum.LockedMissile, out int lockedMissileQuantity))
                    {
                        finalQuantity += lockedMissileQuantity;
                    }

                    if (item == ItemEnum.LockedMissile && seedObject.StartingItems.TryGetValue(ItemEnum.Missile, out int missileQuantity)) finalQuantity += missileQuantity;

                    textToAppend.AppendLine($"global.missiles = {finalQuantity};");
                    alreadyAddedMissiles = true;
                    break;
                case ItemEnum.LockedSuperMissile:
                case ItemEnum.SuperMissile:
                    if (alreadyAddedSupers) break;

                    if (item == ItemEnum.SuperMissile && seedObject.StartingItems.TryGetValue(ItemEnum.LockedSuperMissile, out int lockedSuperQuantity))
                    {
                        finalQuantity += lockedSuperQuantity;
                    }

                    if (item == ItemEnum.LockedSuperMissile && seedObject.StartingItems.TryGetValue(ItemEnum.SuperMissile, out int superQuantity)) finalQuantity += superQuantity;

                    textToAppend.AppendLine($"global.smissiles = {finalQuantity};");
                    alreadyAddedSupers = true;
                    break;

                case ItemEnum.LockedPBomb:
                case ItemEnum.PBomb:
                    if (alreadyAddedPBombs) break;

                    if (item == ItemEnum.PBomb && seedObject.StartingItems.TryGetValue(ItemEnum.LockedPBomb, out int lockedPBombQuantity)) finalQuantity += lockedPBombQuantity;

                    if (item == ItemEnum.LockedPBomb && seedObject.StartingItems.TryGetValue(ItemEnum.PBomb, out int pBombQuantity)) finalQuantity += pBombQuantity;

                    textToAppend.AppendLine($"global.pbombs = {finalQuantity};");
                    alreadyAddedPBombs = true;
                    break;
                case ItemEnum.MissileLauncher:
                case ItemEnum.SuperMissileLauncher:
                case ItemEnum.PBombLauncher:
                    // Are handled further down
                    break;

                case var x when x.ToString().StartsWith("DNA"):
                    // Accumulate how much dna should be added so that we can do that in one call.
                    howMuchDna++;
                    break;

                case ItemEnum.Bombs:
                    textToAppend.AppendLine($"global.hasBombs = {quantity};");
                    break;
                case ItemEnum.Powergrip:
                    textToAppend.AppendLine($"global.hasPowergrip = {quantity};");
                    break;
                case ItemEnum.Spiderball:
                    textToAppend.AppendLine($"global.hasSpiderball = {quantity};");
                    break;
                case ItemEnum.Springball:
                    textToAppend.AppendLine($"global.hasJumpball = {quantity};");
                    break;
                case ItemEnum.Hijump:
                    textToAppend.AppendLine($"global.hasHijump = {quantity};");
                    break;
                case ItemEnum.Varia:
                    textToAppend.AppendLine($"global.hasVaria = {quantity};");
                    break;
                case ItemEnum.Spacejump:
                    textToAppend.AppendLine($"global.hasSpacejump = {quantity};");
                    break;
                case ItemEnum.ProgressiveJump:
                    if (quantity >= 1) textToAppend.AppendLine("global.hasHijump = 1;");

                    if (quantity >= 2) textToAppend.AppendLine("global.hasSpacejump = 1;");

                    break;
                case ItemEnum.Speedbooster:
                    textToAppend.AppendLine($"global.hasSpeedbooster = {quantity};");
                    break;
                case ItemEnum.Screwattack:
                    textToAppend.AppendLine($"global.hasScrewattack = {quantity};");
                    break;
                case ItemEnum.Gravity:
                    textToAppend.AppendLine($"global.hasGravity = {quantity};");
                    break;
                case ItemEnum.ProgressiveSuit:
                    if (quantity >= 1) textToAppend.AppendLine("global.hasVaria = 1;");

                    if (quantity >= 2) textToAppend.AppendLine( "global.hasGravity = 1;");

                    break;
                case ItemEnum.Power:
                    // Stubbed for now, may get a purpose in the future
                    break;
                case ItemEnum.Charge:
                    textToAppend.AppendLine($"global.hasCbeam = {quantity};");
                    break;
                case ItemEnum.Ice:
                    textToAppend.AppendLine($"global.hasIbeam = {quantity};");
                    break;
                case ItemEnum.Wave:
                    textToAppend.AppendLine($"global.hasWbeam = {quantity};");
                    break;
                case ItemEnum.Spazer:
                    textToAppend.AppendLine($"global.hasSbeam = {quantity};");
                    break;
                case ItemEnum.Plasma:
                    textToAppend.AppendLine($"global.hasPbeam = {quantity};");
                    break;
                case ItemEnum.Morphball:
                    textToAppend.AppendLine($"global.hasMorph = {quantity};");
                    break;
                case ItemEnum.Flashlight:
                    textToAppend.AppendLine($"global.flashlightLevel = {quantity};");
                    break;
                case ItemEnum.Blindfold:
                    textToAppend.AppendLine($"global.flashlightLevel = -{quantity};");
                    break;
                case ItemEnum.SpeedBoosterUpgrade:
                    textToAppend.AppendLine($"global.speedBoosterFramesReduction = {quantity};");
                    break;
                case ItemEnum.WalljumpBoots:
                    textToAppend.AppendLine($"global.hasWJ = {quantity};");
                    break;
                case ItemEnum.InfiniteBombPropulsion:
                    textToAppend.AppendLine($"global.hasIBJ = {quantity};");
                    break;
                case ItemEnum.LongBeam:
                    textToAppend.AppendLine($"global.hasLongBeam = {quantity};");
                    break;
                case ItemEnum.Nothing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            textToAppend.AppendLine($"global.collectedItems += \"{item.GetEnumMemberValue()}|{quantity},\";");
        }
        // After we have gotten our starting items, adjust the DNA counter
        characterVarsCode.PrependGMLInCode($"global.dna = (46 - {seedObject.Patches.RequiredDNAmount}) + {howMuchDna};");

        // Check whether option has been set for non-main launchers or if starting with them, if yes enable the main launchers in character var
        if (!seedObject.Patches.RequireMissileLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.MissileLauncher))
        {
            textToAppend.AppendLine("global.missileLauncher = 1;");
        }

        if (!seedObject.Patches.RequireSuperLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.SuperMissileLauncher))
        {
            textToAppend.AppendLine("global.SMissileLauncher = 1;");
        }

        if (!seedObject.Patches.RequirePBLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.PBombLauncher))
        {
            textToAppend.AppendLine("global.PBombLauncher = 1;");
        }

        // Make all item activation dependent on whether the main item is enabled.
        textToAppend.AppendLine("""
                                                global.morphball = global.hasMorph;
                                                global.jumpball = global.hasJumpball;
                                                global.powergrip = global.hasPowergrip;
                                                global.spacejump = global.hasSpacejump;
                                                global.screwattack = global.hasScrewattack;
                                                global.hijump = global.hasHijump;
                                                global.spiderball = global.hasSpiderball;
                                                global.speedbooster = global.hasSpeedbooster;
                                                global.bomb = global.hasBombs;
                                                global.ibeam = global.hasIbeam;
                                                global.wbeam = global.hasWbeam;
                                                global.pbeam = global.hasPbeam;
                                                global.sbeam = global.hasSbeam;
                                                global.cbeam = global.hasCbeam;

                                                global.maxhealth = global.playerhealth;
                                                global.maxmissiles = global.missiles;
                                                global.maxsmissiles = global.smissiles;
                                                global.maxpbombs = global.pbombs;
                                                """);
        textToAppend.AppendLine("global.currentsuit = 0; if (global.hasGravity) global.currentsuit = 2; else if (global.hasVaria) global.currentsuit = 1;");

        characterVarsCode.AppendGMLInCode(textToAppend.ToString());
    }
}
