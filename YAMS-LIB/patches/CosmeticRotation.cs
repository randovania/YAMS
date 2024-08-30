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
        "tlA1OutsideBG",
        "tlA1OutsideNB",
        "tlA1Structures",
        "tlA2Bridge",
        "tlA2Outside",
        "tlA3Machines",
        "tlA4BDoorDestr",
        "tlA4Statues",
        "tlA5Outside",
        "tlA5Pipes",
        "tlA5Switch",
        "tlA8Bridge2",
        "tlA8Bridge",
        "tlA8Outside",
        "tlA8ShipFront",
        "tlArea1A",
        "tlArea1BG",
        "tlArea3Breed",
        "tlArea4BG",
        "tlArea4Pipes",
        "tlArea4PowerSwitch",
        "tlArea4Spikes",
        "tlArea4Tech2",
        "tlArea4TechDestroyed",
        "tlArea4Tech",
        "tlArea4Tower",
        "tlArea5A",
        "tlArea5BG",
        "tlArea5B",
        "tlArea5C",
        "tlArea5MetalBG",
        "tlArea5Metal",
        "tlArea5Moss",
        "tlArea7InsideBG",
        "tlArea7Inside",
        "tlArea7Outside",
        "tlArea8A",
        "tlArea8B",
        "tlArtifact2",
        "tlArtifact3",
        "tlArtifact4",
        "tlArtifact5",
        "tlArtifactBG1",
        "tlArtifact",
        "tlBlueRuins2",
        "tlBlueRuins",
        "tlBrick1BG",
        "tlBrick1",
        "tlBrick2",
        "tlBubbleBG",
        "tlBubbles2",
        "tlBubbles",
        "tlChozoStatue1",
        "tlChozoTemple2",
        "tlChozoTemple3B",
        "tlChozoTemple3C",
        "tlChozoTemple3",
        "tlDoor",
        "tlGenesisFighters",
        "tlGravityBG",
        "tlGreenCrystals",
        "tlIce",
        "tlMachine1",
        "tlMap",
        "tlPipes1",
        "tlPipes2",
        "tlPipes3",
        "tlPipes4",
        "tlPlant1NB",
        "tlPlant2NB",
        "tlRescueTeam",
        "tlResearchBase",
        "tlRock1AN",
        "tlRock1BG",
        "tlRock2BG",
        "tlRock2NB",
        "tlRock3A",
        "tlRock3BG",
        "tlRock3B",
        "tlRock4A",
        "tlRock4BG",
        "tlRock4B",
        "tlRock5A",
        "tlRock5BG",
        "tlRock6A",
        "tlRock6BG",
        "tlRock7A",
        "tlRock7BG",
        "tlRock7B",
        "tlStatue1",
        "tlSurface1Night",
        "tlSurface1",
        "tlSurface1Twilight",
        "tlSurface2",
        "tlSurfaceBGNight",
        "tlSurfaceBG",
        "tlSurfaceBGTwilight",
        "tlWarpPipes",
    ];

    private static readonly string[] Backgrounds =
    [
        "bgA0Cave1BG",
        "bgA0Cave1FG",
        "bgA0Cave2BG",
        "bgA0Cave2FG",
        "bgA0Cave3BG",
        "bgA0Cave3FG",
        "bgA0Cave4BG",
        "bgA0Cave4FG",
        "bgA0MountainNight",
        "bgA0Mountain",
        "bgA0MountainTwilight",
        "bgA0SkyNight",
        "bgA0SkyNight_wide",
        "bgA0Sky",
        "bgA0SkyTwilight",
        "bgA0SkyTwilight_wide",
        "bgA0Sky_wide",
        "bgA1Breed",
        "bgA1Brick",
        "bgA1CanyonNight",
        "bgA1Canyon",
        "bgA1CanyonTwilight",
        "bgA1Save",
        "bgA1Temple",
        "bgA2Breed",
        "bgA2Brick",
        "bgA2PipesH",
        "bgA2PipesV",
        "bgA2Save",
        "bgA2WaterfallBG",
        "bgA3Brick",
        "bgA3Columns",
        "bgA3FactoryBG",
        "bgA3FactoryFG",
        "bgA3LabCave",
        "bgA3MinesCave",
        "bgA4PowerSwitch",
        "bgA4TowerL1",
        "bgA4TowerL2",
        "bgA4TowerL3",
        "bgA4TowerL4",
        "bgA4TowerL5",
        "bgA4Tower",
        "bgA5ActivationLights",
        "bgA5Activation",
        "bgA5Cave",
        "bgA5Outside",
        "bgA5Vertical",
        "bgA6Cave",
        "bgA7CaveBG",
        "bgA7CaveFG",
        "bgA7Cave",
        "bgA7Lab",
        "bgA8CommsScreens",
        "bgA8Corridor1",
        "bgA8Corridor2",
        "bgA8Corridor3",
        "bgA8DropShip",
        "bgA8Elevator",
        "bgA8Lab",
        "bgA8LoungeWindows",
        "bgA8Storage",
        "bgController",
        "bgDisclaimer",
        "bgFog",
        "bgGameOver",
        "bgGUIMapBG",
        "bgIntroSC1",
        "bgIntroSC2",
        "bgIntroText1",
        "bgIntroText2",
        "bgIntroText3",
        "bgLava0",
        "bgLava1",
        "bgLava2",
        "bgLava3",
        "bgLava4",
        "bgLoading",
        "bgLogImg00",
        "bgLogImg01",
        "bgLogImg03",
        "bgLogImg04B",
        "bgLogImg05B",
        "bgLogImg06",
        "bgLogImg10",
        "bgLogImg11",
        "bgLogImg12",
        "bgLogImg13",
        "bgLogImg14",
        "bgLogImg15",
        "bgLogImg16",
        "bgLogImg20",
        "bgLogImg21",
        "bgLogImg22",
        "bgLogImg23",
        "bgLogImg24",
        "bgLogImg25",
        "bgLogImg26A",
        "bgLogImg26B",
        "bgLogImg27",
        "bgLogImg28A",
        "bgLogImg28B",
        "bgLogImg29",
        "bgLogImg30",
        "bgLogImg31",
        "bgLogImg32",
        "bgLogImg33",
        "bgLogImg34",
        "bgLogImg35",
        "bgLogImg36",
        "bgLogImg37",
        "bgLogImg38",
        "bgLogImg40",
        "bgLogImg41",
        "bgLogImg42",
        "bgLogImg43",
        "bgLogImg44A",
        "bgLogImg44B",
        "bgLogImg45",
        "bgMapScreenBG",
        "bgOptions",
        "bgScoreScreenBG",
        "bgScoreScreenPlayer_fusion",
        "bgScoreScreenPlayerGlow1",
        "bgScoreScreenPlayerGlow2",
        "bgScoreScreenPlayerGlow3",
        "bgScoreScreenPlayerGlow4",
        "bgScoreScreenPlayer",
        "bgShipBoosterMid",
        "bgShipBoosterSide",
        "bgWater0",
        "bgWater1",
        "bgWater2",
        "bgWaterfall",
        "bgWFilter0B",
        "bgWFilter0",
        "bgWFilter1",
        "bgWFilter2",
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

        // Hue shift backgrounds
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
