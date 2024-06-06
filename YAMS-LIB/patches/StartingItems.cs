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
        foreach ((ItemEnum item, int quantity) in seedObject.StartingItems)
        {
            int finalQuantity = quantity;
            switch (item)
            {
                case ItemEnum.EnergyTank:
                    characterVarsCode.ReplaceGMLInCode("global.etanks = 0", $"global.etanks = {quantity};");
                    characterVarsCode.ReplaceGMLInCode($"global.playerhealth = {seedObject.Patches.EnergyPerTank - 1}",
                        $"global.playerhealth = {seedObject.Patches.EnergyPerTank + seedObject.Patches.EnergyPerTank * quantity - 1};");
                    break;
                case ItemEnum.LockedMissile:
                case ItemEnum.Missile:
                    if (alreadyAddedMissiles) break;

                    if (item == ItemEnum.Missile && seedObject.StartingItems.TryGetValue(ItemEnum.LockedMissile, out int lockedMissileQuantity))
                    {
                        finalQuantity += lockedMissileQuantity;
                    }

                    if (item == ItemEnum.LockedMissile && seedObject.StartingItems.TryGetValue(ItemEnum.Missile, out int missileQuantity)) finalQuantity += missileQuantity;

                    characterVarsCode.ReplaceGMLInCode("global.missiles = 0", $"global.missiles = {finalQuantity};");
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

                    characterVarsCode.ReplaceGMLInCode("global.smissiles = 0", $"global.smissiles = {finalQuantity};");
                    alreadyAddedSupers = true;
                    break;

                case ItemEnum.LockedPBomb:
                case ItemEnum.PBomb:
                    if (alreadyAddedPBombs) break;

                    if (item == ItemEnum.PBomb && seedObject.StartingItems.TryGetValue(ItemEnum.LockedPBomb, out int lockedPBombQuantity)) finalQuantity += lockedPBombQuantity;

                    if (item == ItemEnum.LockedPBomb && seedObject.StartingItems.TryGetValue(ItemEnum.PBomb, out int pBombQuantity)) finalQuantity += pBombQuantity;

                    characterVarsCode.ReplaceGMLInCode("global.pbombs = 0", $"global.pbombs = {finalQuantity};");
                    alreadyAddedPBombs = true;
                    break;
                case ItemEnum.MissileLauncher:
                case ItemEnum.SuperMissileLauncher:
                case ItemEnum.PBombLauncher:
                    // Are handled further down
                    break;

                case var x when x.ToString().StartsWith("DNA"):
                    characterVarsCode.ReplaceGMLInCode("global.dna =", "global.dna = 1 +");
                    break;

                case ItemEnum.Bombs:
                    characterVarsCode.ReplaceGMLInCode("global.hasBombs = 0", $"global.hasBombs = {quantity};");
                    break;
                case ItemEnum.Powergrip:
                    characterVarsCode.ReplaceGMLInCode("global.hasPowergrip = 0", $"global.hasPowergrip = {quantity};");
                    break;
                case ItemEnum.Spiderball:
                    characterVarsCode.ReplaceGMLInCode("global.hasSpiderball = 0", $"global.hasSpiderball = {quantity};");
                    break;
                case ItemEnum.Springball:
                    characterVarsCode.ReplaceGMLInCode("global.hasJumpball = 0", $"global.hasJumpball = {quantity};");
                    break;
                case ItemEnum.Hijump:
                    characterVarsCode.ReplaceGMLInCode("global.hasHijump = 0", $"global.hasHijump = {quantity};");
                    break;
                case ItemEnum.Varia:
                    characterVarsCode.ReplaceGMLInCode("global.hasVaria = 0", $"global.hasVaria = {quantity};");
                    break;
                case ItemEnum.Spacejump:
                    characterVarsCode.ReplaceGMLInCode("global.hasSpacejump = 0", $"global.hasSpacejump = {quantity};");
                    break;
                case ItemEnum.ProgressiveJump:
                    if (quantity >= 1) characterVarsCode.ReplaceGMLInCode("global.hasHijump = 0", "global.hasHijump = 1;");

                    if (quantity >= 2) characterVarsCode.ReplaceGMLInCode("global.hasSpacejump = 0", "global.hasSpacejump = 1;");

                    break;
                case ItemEnum.Speedbooster:
                    characterVarsCode.ReplaceGMLInCode("global.hasSpeedbooster = 0", $"global.hasSpeedbooster = {quantity};");
                    break;
                case ItemEnum.Screwattack:
                    characterVarsCode.ReplaceGMLInCode("global.hasScrewattack = 0", $"global.hasScrewattack = {quantity};");
                    break;
                case ItemEnum.Gravity:
                    characterVarsCode.ReplaceGMLInCode("global.hasGravity = 0", $"global.hasGravity = {quantity};");
                    break;
                case ItemEnum.ProgressiveSuit:
                    if (quantity >= 1) characterVarsCode.ReplaceGMLInCode("global.hasVaria = 0", "global.hasVaria = 1;");

                    if (quantity >= 2) characterVarsCode.ReplaceGMLInCode("global.hasGravity = 0", "global.hasGravity = 1;");

                    break;
                case ItemEnum.Power:
                    // Stubbed for now, may get a purpose in the future
                    break;
                case ItemEnum.Charge:
                    characterVarsCode.ReplaceGMLInCode("global.hasCbeam = 0", $"global.hasCbeam = {quantity};");
                    break;
                case ItemEnum.Ice:
                    characterVarsCode.ReplaceGMLInCode("global.hasIbeam = 0", $"global.hasIbeam = {quantity};");
                    break;
                case ItemEnum.Wave:
                    characterVarsCode.ReplaceGMLInCode("global.hasWbeam = 0", $"global.hasWbeam = {quantity};");
                    break;
                case ItemEnum.Spazer:
                    characterVarsCode.ReplaceGMLInCode("global.hasSbeam = 0", $"global.hasSbeam = {quantity};");
                    break;
                case ItemEnum.Plasma:
                    characterVarsCode.ReplaceGMLInCode("global.hasPbeam = 0", $"global.hasPbeam = {quantity};");
                    break;
                case ItemEnum.Morphball:
                    characterVarsCode.ReplaceGMLInCode("global.hasMorph = 0", $"global.hasMorph = {quantity};");
                    break;
                case ItemEnum.Flashlight:
                    characterVarsCode.ReplaceGMLInCode("global.flashlightLevel = 0", $"global.flashlightLevel = {quantity};");
                    break;
                case ItemEnum.Blindfold:
                    characterVarsCode.ReplaceGMLInCode("global.flashlightLevel = 0", $"global.flashlightLevel = -{quantity};");
                    break;
                case ItemEnum.SpeedBoosterUpgrade:
                    characterVarsCode.ReplaceGMLInCode("global.speedBoosterFramesReduction = 0", $"global.speedBoosterFramesReduction = {quantity}");
                    break;
                case ItemEnum.WalljumpBoots:
                    characterVarsCode.ReplaceGMLInCode("global.hasWJ = 0", $"global.hasWJ = {quantity}");
                    break;
                case ItemEnum.InfiniteBombPropulsion:
                    characterVarsCode.ReplaceGMLInCode("global.hasIBJ = 0", $"global.hasIBJ = {quantity}");
                    break;
                case ItemEnum.LongBeam:
                    characterVarsCode.ReplaceGMLInCode("global.hasLongBeam = 0", $"global.hasLongBeam = {quantity}");
                    break;
                case ItemEnum.Nothing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            characterVarsCode.AppendGMLInCode($"global.collectedItems += \"{item.GetEnumMemberValue()}|{quantity},\"");
        }
        // After we have gotten our starting items, adjust the DNA counter
        characterVarsCode.ReplaceGMLInCode("global.dna = ", $"global.dna = (46 - {seedObject.Patches.RequiredDNAmount}) + ");

        // Check whether option has been set for non-main launchers or if starting with them, if yes enable the main launchers in character var
        if (!seedObject.Patches.RequireMissileLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.MissileLauncher))
        {
            characterVarsCode.ReplaceGMLInCode("global.missileLauncher = 0", "global.missileLauncher = 1");
        }

        if (!seedObject.Patches.RequireSuperLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.SuperMissileLauncher))
        {
            characterVarsCode.ReplaceGMLInCode("global.SMissileLauncher = 0", "global.SMissileLauncher = 1");
        }

        if (!seedObject.Patches.RequirePBLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.PBombLauncher))
        {
            characterVarsCode.ReplaceGMLInCode("global.PBombLauncher = 0", "global.PBombLauncher = 1");
        }
    }
}
