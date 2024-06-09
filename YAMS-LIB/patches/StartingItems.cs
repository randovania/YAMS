using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class StartingItems
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Set starting items
        bool alreadyAddedMissiles = false;
        bool alreadyAddedSupers = false;
        bool alreadyAddedPBombs = false;
        characterVarsCode.AppendGMLInCode("global.collectedItems = \"items:\"");
        int howMuchDna = 0;
        // TODO: move the "has bla" stuff into a seperate append call at the very end rather than copy pasting for each entry?
        foreach ((ItemEnum item, int quantity) in seedObject.StartingItems)
        {
            int finalQuantity = quantity;
            switch (item)
            {
                case ItemEnum.EnergyTank:
                    characterVarsCode.AppendGMLInCode($"global.etanks = {quantity};");
                    characterVarsCode.AppendGMLInCode($"global.playerhealth = {seedObject.Patches.EnergyPerTank + seedObject.Patches.EnergyPerTank * quantity - 1};");
                    characterVarsCode.AppendGMLInCode("global.maxhealth = global.playerhealth");
                    break;
                case ItemEnum.LockedMissile:
                case ItemEnum.Missile:
                    if (alreadyAddedMissiles) break;

                    if (item == ItemEnum.Missile && seedObject.StartingItems.TryGetValue(ItemEnum.LockedMissile, out int lockedMissileQuantity))
                    {
                        finalQuantity += lockedMissileQuantity;
                    }

                    if (item == ItemEnum.LockedMissile && seedObject.StartingItems.TryGetValue(ItemEnum.Missile, out int missileQuantity)) finalQuantity += missileQuantity;

                    characterVarsCode.AppendGMLInCode($"global.missiles = {finalQuantity}; global.maxmissiles = global.missiles");
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

                    characterVarsCode.AppendGMLInCode($"global.smissiles = {finalQuantity}; global.maxsmissiles = global.smissiles");
                    alreadyAddedSupers = true;
                    break;

                case ItemEnum.LockedPBomb:
                case ItemEnum.PBomb:
                    if (alreadyAddedPBombs) break;

                    if (item == ItemEnum.PBomb && seedObject.StartingItems.TryGetValue(ItemEnum.LockedPBomb, out int lockedPBombQuantity)) finalQuantity += lockedPBombQuantity;

                    if (item == ItemEnum.LockedPBomb && seedObject.StartingItems.TryGetValue(ItemEnum.PBomb, out int pBombQuantity)) finalQuantity += pBombQuantity;

                    characterVarsCode.AppendGMLInCode($"global.pbombs = {finalQuantity}; global.maxpbombs = global.pbombs");
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
                    characterVarsCode.AppendGMLInCode($"global.hasBombs = {quantity}; global.bomb = global.hasBombs");
                    break;
                case ItemEnum.Powergrip:
                    characterVarsCode.AppendGMLInCode($"global.hasPowergrip = {quantity}; global.powergrip = global.hasPowergrip");
                    break;
                case ItemEnum.Spiderball:
                    characterVarsCode.AppendGMLInCode($"global.hasSpiderball = {quantity}; global.spiderball = global.hasSpiderball");
                    break;
                case ItemEnum.Springball:
                    characterVarsCode.AppendGMLInCode($"global.hasJumpball = {quantity}; global.jumpball = global.hasJumpball");
                    break;
                case ItemEnum.Hijump:
                    characterVarsCode.AppendGMLInCode($"global.hasHijump = {quantity}; global.hijump = global.hasHijump");
                    break;
                case ItemEnum.Varia:
                    characterVarsCode.AppendGMLInCode($"global.hasVaria = {quantity}; global.currentsuit = 1");
                    break;
                case ItemEnum.Spacejump:
                    characterVarsCode.AppendGMLInCode($"global.hasSpacejump = {quantity}; global.spacejump = global.hasSpacejump");
                    break;
                case ItemEnum.ProgressiveJump:
                    if (quantity >= 1) characterVarsCode.AppendGMLInCode("global.hasHijump = 1; global.hijump = global.hasHijump");

                    if (quantity >= 2) characterVarsCode.AppendGMLInCode("global.hasSpacejump = 1; global.spacejump = global.hasSpacejump");

                    break;
                case ItemEnum.Speedbooster:
                    characterVarsCode.AppendGMLInCode($"global.hasSpeedbooster = {quantity}; global.speedbooster = global.hasSpeedbooster");
                    break;
                case ItemEnum.Screwattack:
                    characterVarsCode.AppendGMLInCode($"global.hasScrewattack = {quantity}; global.screwattack = global.hasScrewattack");
                    break;
                case ItemEnum.Gravity:
                    characterVarsCode.AppendGMLInCode($"global.hasGravity = {quantity}; global.currentsuit = 2");
                    break;
                case ItemEnum.ProgressiveSuit:
                    if (quantity >= 1) characterVarsCode.AppendGMLInCode("global.hasVaria = 1; global.currentsuit = 1");

                    if (quantity >= 2) characterVarsCode.AppendGMLInCode( "global.hasGravity = 1; global.currentsuit = 2");

                    break;
                case ItemEnum.Power:
                    // Stubbed for now, may get a purpose in the future
                    break;
                case ItemEnum.Charge:
                    characterVarsCode.AppendGMLInCode($"global.hasCbeam = {quantity}; global.cbeam = global.hasCbeam");
                    break;
                case ItemEnum.Ice:
                    characterVarsCode.AppendGMLInCode($"global.hasIbeam = {quantity}; global.ibeam = global.hasIbeam");
                    break;
                case ItemEnum.Wave:
                    characterVarsCode.AppendGMLInCode($"global.hasWbeam = {quantity}; global.wbeam = global.hasWbeam");
                    break;
                case ItemEnum.Spazer:
                    characterVarsCode.AppendGMLInCode($"global.hasSbeam = {quantity}; global.sbeam = global.hasSbeam");
                    break;
                case ItemEnum.Plasma:
                    characterVarsCode.AppendGMLInCode($"global.hasPbeam = {quantity}; global.pbeam = global.hasPbeam");
                    break;
                case ItemEnum.Morphball:
                    characterVarsCode.AppendGMLInCode($"global.hasMorph = {quantity}; global.morphball = global.hasMorph");
                    break;
                case ItemEnum.Flashlight:
                    characterVarsCode.AppendGMLInCode($"global.flashlightLevel = {quantity};");
                    break;
                case ItemEnum.Blindfold:
                    characterVarsCode.AppendGMLInCode($"global.flashlightLevel = -{quantity};");
                    break;
                case ItemEnum.SpeedBoosterUpgrade:
                    characterVarsCode.AppendGMLInCode($"global.speedBoosterFramesReduction = {quantity}");
                    break;
                case ItemEnum.WalljumpBoots:
                    characterVarsCode.AppendGMLInCode($"global.hasWJ = {quantity}");
                    break;
                case ItemEnum.InfiniteBombPropulsion:
                    characterVarsCode.AppendGMLInCode($"global.hasIBJ = {quantity}");
                    break;
                case ItemEnum.LongBeam:
                    characterVarsCode.AppendGMLInCode($"global.hasLongBeam = {quantity}");
                    break;
                case ItemEnum.Nothing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            characterVarsCode.AppendGMLInCode($"global.collectedItems += \"{item.GetEnumMemberValue()}|{quantity},\"");
        }
        // After we have gotten our starting items, adjust the DNA counter
        characterVarsCode.AppendGMLInCode($"global.dna = (46 - {seedObject.Patches.RequiredDNAmount}) + {howMuchDna}");

        // Check whether option has been set for non-main launchers or if starting with them, if yes enable the main launchers in character var
        if (!seedObject.Patches.RequireMissileLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.MissileLauncher))
        {
            characterVarsCode.AppendGMLInCode("global.missileLauncher = 1");
        }

        if (!seedObject.Patches.RequireSuperLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.SuperMissileLauncher))
        {
            characterVarsCode.AppendGMLInCode("global.SMissileLauncher = 1");
        }

        if (!seedObject.Patches.RequirePBLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.PBombLauncher))
        {
            characterVarsCode.AppendGMLInCode("global.PBombLauncher = 1");
        }
    }
}
