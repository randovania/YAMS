using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class CosmeticRotation
{
    private static readonly string[]  Tilesets =
    [
        "tlA1OutsideBG.png",
        "tlA1OutsideNB.png",
        "tlA1Structures.png",
        "tlA2Bridge.png",
        "tlA2Outside.png",
        "tlA3Machines.png",
        "tlA4BDoorDestr.png",
        "tlA4Statues.png",
        "tlA5Outside.png",
        "tlA5Pipes.png",
        "tlA5Switch.png",
        "tlA8Bridge2.png",
        "tlA8Bridge.png",
        "tlA8Outside.png",
        "tlA8ShipFront.png",
        "tlArea1A.png",
        "tlArea1BG.png",
        "tlArea3Breed.png",
        "tlArea4BG.png",
        "tlArea4Pipes.png",
        "tlArea4PowerSwitch.png",
        "tlArea4Spikes.png",
        "tlArea4Tech2.png",
        "tlArea4TechDestroyed.png",
        "tlArea4Tech.png",
        "tlArea4Tower.png",
        "tlArea5A.png",
        "tlArea5BG.png",
        "tlArea5B.png",
        "tlArea5C.png",
        "tlArea5MetalBG.png",
        "tlArea5Metal.png",
        "tlArea5Moss.png",
        "tlArea7InsideBG.png",
        "tlArea7Inside.png",
        "tlArea7Outside.png",
        "tlArea8A.png",
        "tlArea8B.png",
        "tlArtifact2.png",
        "tlArtifact3.png",
        "tlArtifact4.png",
        "tlArtifact5.png",
        "tlArtifactBG1.png",
        "tlArtifact.png",
        "tlBlueRuins2.png",
        "tlBlueRuins.png",
        "tlBrick1BG.png",
        "tlBrick1.png",
        "tlBrick2.png",
        "tlBubbleBG.png",
        "tlBubbles2.png",
        "tlBubbles.png",
        "tlChozoStatue1.png",
        "tlChozoTemple2.png",
        "tlChozoTemple3B.png",
        "tlChozoTemple3C.png",
        "tlChozoTemple3.png",
        "tlDoor.png",
        "tlGenesisFighters.png",
        "tlGravityBG.png",
        "tlGreenCrystals.png",
        "tlIce.png",
        "tlMachine1.png",
        "tlMap.png",
        "tlPipes1.png",
        "tlPipes2.png",
        "tlPipes3.png",
        "tlPipes4.png",
        "tlPlant1NB.png",
        "tlPlant2NB.png",
        "tlRescueTeam.png",
        "tlResearchBase.png",
        "tlRock1AN.png",
        "tlRock1BG.png",
        "tlRock2BG.png",
        "tlRock2NB.png",
        "tlRock3A.png",
        "tlRock3BG.png",
        "tlRock3B.png",
        "tlRock4A.png",
        "tlRock4BG.png",
        "tlRock4B.png",
        "tlRock5A.png",
        "tlRock5BG.png",
        "tlRock6A.png",
        "tlRock6BG.png",
        "tlRock7A.png",
        "tlRock7BG.png",
        "tlRock7B.png",
        "tlStatue1.png",
        "tlSurface1Night.png",
        "tlSurface1.png",
        "tlSurface1Twilight.png",
        "tlSurface2.png",
        "tlSurfaceBGNight.png",
        "tlSurfaceBG.png",
        "tlSurfaceBGTwilight.png",
        "tlWarpPipes.png",
    ];

    private static readonly string[] Backgrounds =
    [
        "bgA0Cave1BG.png",
        "bgA0Cave1FG.png",
        "bgA0Cave2BG.png",
        "bgA0Cave2FG.png",
        "bgA0Cave3BG.png",
        "bgA0Cave3FG.png",
        "bgA0Cave4BG.png",
        "bgA0Cave4FG.png",
        "bgA0MountainNight.png",
        "bgA0Mountain.png",
        "bgA0MountainTwilight.png",
        "bgA0SkyNight.png",
        "bgA0SkyNight_wide.png",
        "bgA0Sky.png",
        "bgA0SkyTwilight.png",
        "bgA0SkyTwilight_wide.png",
        "bgA0Sky_wide.png",
        "bgA1Breed.png",
        "bgA1Brick.png",
        "bgA1CanyonNight.png",
        "bgA1Canyon.png",
        "bgA1CanyonTwilight.png",
        "bgA1Save.png",
        "bgA1Temple.png",
        "bgA2Breed.png",
        "bgA2Brick.png",
        "bgA2PipesH.png",
        "bgA2PipesV.png",
        "bgA2Save.png",
        "bgA2WaterfallBG.png",
        "bgA3Brick.png",
        "bgA3Columns.png",
        "bgA3FactoryBG.png",
        "bgA3FactoryFG.png",
        "bgA3LabCave.png",
        "bgA3MinesCave.png",
        "bgA4PowerSwitch.png",
        "bgA4TowerL1.png",
        "bgA4TowerL2.png",
        "bgA4TowerL3.png",
        "bgA4TowerL4.png",
        "bgA4TowerL5.png",
        "bgA4Tower.png",
        "bgA5ActivationLights.png",
        "bgA5Activation.png",
        "bgA5Cave.png",
        "bgA5Outside.png",
        "bgA5Vertical.png",
        "bgA6Cave.png",
        "bgA7CaveBG.png",
        "bgA7CaveFG.png",
        "bgA7Cave.png",
        "bgA7Lab.png",
        "bgA8CommsScreens.png",
        "bgA8Corridor1.png",
        "bgA8Corridor2.png",
        "bgA8Corridor3.png",
        "bgA8DropShip.png",
        "bgA8Elevator.png",
        "bgA8Lab.png",
        "bgA8LoungeWindows.png",
        "bgA8Storage.png",
        "bgController.png",
        "bgDisclaimer.png",
        "bgEnding1.png",
        "bgEnding2.png",
        "bgEnding3.png",
        "bgFog.png",
        "bgGalTN1.png",
        "bgGalTN2.png",
        "bgGalTN3.png",
        "bgGameOver.png",
        "bgGUIMapBG.png",
        "bgIntroSC1.png",
        "bgIntroSC2.png",
        "bgIntroText1.png",
        "bgIntroText2.png",
        "bgIntroText3.png",
        "bgLava0.png",
        "bgLava1.png",
        "bgLava2.png",
        "bgLava3.png",
        "bgLava4.png",
        "bgLoading.png",
        "bgLogImg00.png",
        "bgLogImg01.png",
        "bgLogImg03.png",
        "bgLogImg04B.png",
        "bgLogImg05B.png",
        "bgLogImg06.png",
        "bgLogImg10.png",
        "bgLogImg11.png",
        "bgLogImg12.png",
        "bgLogImg13.png",
        "bgLogImg14.png",
        "bgLogImg15.png",
        "bgLogImg16.png",
        "bgLogImg20.png",
        "bgLogImg21.png",
        "bgLogImg22.png",
        "bgLogImg23.png",
        "bgLogImg24.png",
        "bgLogImg25.png",
        "bgLogImg26A.png",
        "bgLogImg26B.png",
        "bgLogImg27.png",
        "bgLogImg28A.png",
        "bgLogImg28B.png",
        "bgLogImg29.png",
        "bgLogImg30.png",
        "bgLogImg31.png",
        "bgLogImg32.png",
        "bgLogImg33.png",
        "bgLogImg34.png",
        "bgLogImg35.png",
        "bgLogImg36.png",
        "bgLogImg37.png",
        "bgLogImg38.png",
        "bgLogImg40.png",
        "bgLogImg41.png",
        "bgLogImg42.png",
        "bgLogImg43.png",
        "bgLogImg44A.png",
        "bgLogImg44B.png",
        "bgLogImg45.png",
        "bgMapScreenBG.png",
        "bgOptions.png",
        "bgScoreScreenBG.png",
        "bgScoreScreenPlayer_fusion.png",
        "bgScoreScreenPlayerGlow1.png",
        "bgScoreScreenPlayerGlow2.png",
        "bgScoreScreenPlayerGlow3.png",
        "bgScoreScreenPlayerGlow4.png",
        "bgScoreScreenPlayer.png",
        "bgShipBoosterMid.png",
        "bgShipBoosterSide.png",
        "bgWater0.png",
        "bgWater1.png",
        "bgWater2.png",
        "bgWaterfall.png",
        "bgWFilter0B.png",
        "bgWFilter0.png",
        "bgWFilter1.png",
        "bgWFilter2.png",
    ];

    static void RotateTextureAndSaveToTexturePage(UndertaleEmbeddedTexture texture, List<Tuple<Rectangle, int>> rectangleRotationTuple)
    {
        using Image texturePage = Image.Load(texture.TextureData.TextureBlob);
        foreach ((var rectangle, var rotation) in rectangleRotationTuple)
        {
            texturePage.Mutate(im => im.Hue(rotation, rectangle));
        }

        using MemoryStream ms = new MemoryStream();
        texturePage.Save(ms, PngFormat.Instance);
        texture.TextureData.TextureBlob = ms.ToArray();
    }


    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var textureDict = new Dictionary<UndertaleEmbeddedTexture, List<Tuple<Rectangle, int>>>();

        // TODO: less copypaste
        // Hue shift etanks
        if (seedObject.Cosmetics.EtankHUDRotation != 0)
        {
            foreach (UndertaleSprite.TextureEntry? textureEntry in gmData.Sprites.ByName("sGUIETank").Textures)
            {
                var texture = textureEntry.Texture;
                bool wasInDict = textureDict.TryGetValue(texture.TexturePage, out var tupleList);
                if (tupleList is null)
                    tupleList = new List<Tuple<Rectangle, int>>();
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight),
                    seedObject.Cosmetics.EtankHUDRotation));
                if (!wasInDict)
                    textureDict.Add(texture.TexturePage, null);

                textureDict[texture.TexturePage] = tupleList;
            }
        }

        // Hue shift health numbers
        if (seedObject.Cosmetics.HealthHUDRotation != 0)
        {
            foreach (UndertaleSprite.TextureEntry? textureEntry in gmData.Sprites.ByName("sGUIFont1").Textures.Concat(gmData.Sprites.ByName("sGUIFont1A").Textures))
            {
                var texture = textureEntry.Texture;
                bool wasInDict = textureDict.TryGetValue(texture.TexturePage, out var tupleList);
                if (tupleList is null)
                    tupleList = new List<Tuple<Rectangle, int>>();
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight),
                    seedObject.Cosmetics.HealthHUDRotation));
                if (!wasInDict)
                    textureDict.Add(texture.TexturePage, null);

                textureDict[texture.TexturePage] = tupleList;
            }
        }

        // Hue shift dna icon
        if (seedObject.Cosmetics.DNAHUDRotation != 0)
        {
            foreach (UndertaleBackground bg in new List<UndertaleBackground> { gmData.Backgrounds.ByName("bgGUIMetCountBG1"), gmData.Backgrounds.ByName("bgGUIMetCountBG2ELM") })
            {
                var texture = bg.Texture;
                bool wasInDict = textureDict.TryGetValue(texture.TexturePage, out var tupleList);
                if (tupleList is null)
                    tupleList = new List<Tuple<Rectangle, int>>();
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight),
                    seedObject.Cosmetics.DNAHUDRotation));
                if (!wasInDict)
                    textureDict.Add(texture.TexturePage, null);

                textureDict[texture.TexturePage] = tupleList;
            }
        }

        // Hue shift tilesets
        if (seedObject.Cosmetics.TilesetRotation != 0)
        {
            foreach (UndertaleBackground bg in gmData.Backgrounds.Where(bg => Tilesets.Contains(bg.Name.Content)))
            {
                var texture = bg.Texture;
                bool wasInDict = textureDict.TryGetValue(texture.TexturePage, out var tupleList);
                if (tupleList is null)
                    tupleList = new List<Tuple<Rectangle, int>>();
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight),
                    seedObject.Cosmetics.TilesetRotation));
                if (!wasInDict)
                    textureDict.Add(texture.TexturePage, null);

                textureDict[texture.TexturePage] = tupleList;
            }
        }

        // Hue shift tilesets
        if (seedObject.Cosmetics.BackgroundRotation != 0)
        {
            foreach (UndertaleBackground bg in gmData.Backgrounds.Where(bg => Backgrounds.Contains(bg.Name.Content)))
            {
                var texture = bg.Texture;
                bool wasInDict = textureDict.TryGetValue(texture.TexturePage, out var tupleList);
                if (tupleList is null)
                    tupleList = new List<Tuple<Rectangle, int>>();
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight),
                    seedObject.Cosmetics.BackgroundRotation));
                if (!wasInDict)
                    textureDict.Add(texture.TexturePage, null);

                textureDict[texture.TexturePage] = tupleList;
            }
        }

        foreach ((var texturePage, var rectangles) in textureDict)
        {
            RotateTextureAndSaveToTexturePage(texturePage, rectangles);
        }
    }
}
