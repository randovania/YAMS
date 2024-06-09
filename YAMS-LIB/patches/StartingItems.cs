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
        foreach ((ItemEnum item, int quantity) in seedObject.StartingItems)
        {
            int finalQuantity = quantity;
            switch (item)
            {
                case ItemEnum.EnergyTank:
                    characterVarsCode.AppendGMLInCode($"global.etanks = {quantity};");
                    characterVarsCode.AppendGMLInCode($"global.playerhealth = {seedObject.Patches.EnergyPerTank + seedObject.Patches.EnergyPerTank * quantity - 1};");
                    break;
                case ItemEnum.LockedMissile:
                case ItemEnum.Missile:
                    if (alreadyAddedMissiles) break;

                    if (item == ItemEnum.Missile && seedObject.StartingItems.TryGetValue(ItemEnum.LockedMissile, out int lockedMissileQuantity))
                    {
                        finalQuantity += lockedMissileQuantity;
                    }

                    if (item == ItemEnum.LockedMissile && seedObject.StartingItems.TryGetValue(ItemEnum.Missile, out int missileQuantity)) finalQuantity += missileQuantity;

                    characterVarsCode.AppendGMLInCode($"global.missiles = {finalQuantity};");
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

                    characterVarsCode.AppendGMLInCode($"global.smissiles = {finalQuantity};");
                    alreadyAddedSupers = true;
                    break;

                case ItemEnum.LockedPBomb:
                case ItemEnum.PBomb:
                    if (alreadyAddedPBombs) break;

                    if (item == ItemEnum.PBomb && seedObject.StartingItems.TryGetValue(ItemEnum.LockedPBomb, out int lockedPBombQuantity)) finalQuantity += lockedPBombQuantity;

                    if (item == ItemEnum.LockedPBomb && seedObject.StartingItems.TryGetValue(ItemEnum.PBomb, out int pBombQuantity)) finalQuantity += pBombQuantity;

                    characterVarsCode.AppendGMLInCode($"global.pbombs = {finalQuantity};");
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
                    characterVarsCode.AppendGMLInCode($"global.hasBombs = {quantity};");
                    break;
                case ItemEnum.Powergrip:
                    characterVarsCode.AppendGMLInCode($"global.hasPowergrip = {quantity};");
                    break;
                case ItemEnum.Spiderball:
                    characterVarsCode.AppendGMLInCode($"global.hasSpiderball = {quantity};");
                    break;
                case ItemEnum.Springball:
                    characterVarsCode.AppendGMLInCode($"global.hasJumpball = {quantity};");
                    break;
                case ItemEnum.Hijump:
                    characterVarsCode.AppendGMLInCode($"global.hasHijump = {quantity};");
                    break;
                case ItemEnum.Varia:
                    characterVarsCode.AppendGMLInCode($"global.hasVaria = {quantity};");
                    break;
                case ItemEnum.Spacejump:
                    characterVarsCode.AppendGMLInCode($"global.hasSpacejump = {quantity};");
                    break;
                case ItemEnum.ProgressiveJump:
                    if (quantity >= 1) characterVarsCode.AppendGMLInCode("global.hasHijump = 1;");

                    if (quantity >= 2) characterVarsCode.AppendGMLInCode("global.hasSpacejump = 1;");

                    break;
                case ItemEnum.Speedbooster:
                    characterVarsCode.AppendGMLInCode($"global.hasSpeedbooster = {quantity};");
                    break;
                case ItemEnum.Screwattack:
                    characterVarsCode.AppendGMLInCode($"global.hasScrewattack = {quantity};");
                    break;
                case ItemEnum.Gravity:
                    characterVarsCode.AppendGMLInCode($"global.hasGravity = {quantity};");
                    break;
                case ItemEnum.ProgressiveSuit:
                    if (quantity >= 1) characterVarsCode.AppendGMLInCode("global.hasVaria = 1;");

                    if (quantity >= 2) characterVarsCode.AppendGMLInCode( "global.hasGravity = 1;");

                    break;
                case ItemEnum.Power:
                    // Stubbed for now, may get a purpose in the future
                    break;
                case ItemEnum.Charge:
                    characterVarsCode.AppendGMLInCode($"global.hasCbeam = {quantity};");
                    break;
                case ItemEnum.Ice:
                    characterVarsCode.AppendGMLInCode($"global.hasIbeam = {quantity};");
                    break;
                case ItemEnum.Wave:
                    characterVarsCode.AppendGMLInCode($"global.hasWbeam = {quantity};");
                    break;
                case ItemEnum.Spazer:
                    characterVarsCode.AppendGMLInCode($"global.hasSbeam = {quantity};");
                    break;
                case ItemEnum.Plasma:
                    characterVarsCode.AppendGMLInCode($"global.hasPbeam = {quantity};");
                    break;
                case ItemEnum.Morphball:
                    characterVarsCode.AppendGMLInCode($"global.hasMorph = {quantity};");
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
