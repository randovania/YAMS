using System.Globalization;
using System.Reflection;
using System.Text.Json;
using NaturalSort.Extension;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using YAMS_LIB.patches;
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
        // TODO: import jes tester display to make tester fight better

        const uint ThothBridgeLeftDoorID = 400000;
        const uint ThothBridgeRightDoorID = 400001;
        const uint A2WaterTurbineLeftDoorID = 400002;
        const uint PipeInHideoutID = 400003;
        const uint PipeInDepthsLowerID = 400004;
        const uint PipeInDepthsUpperID = 400005;
        const uint PipeInWaterfallsID = 400006;

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
        if (!controlCreate.Contains("global.am2r_version = \"V1.5.5\"")) throw new InvalidAM2RVersionException("The selected game is not AM2R 1.5.5!");


        // Import new Sprites
        var nameToPageItemDict = new Dictionary<string, int>();
        const int pageDimension = 1024;
        int lastUsedX = 0, lastUsedY = 0, currentShelfHeight = 0;
        var newTexturePage = new Image<Rgba32>(pageDimension, pageDimension);
        UndertaleEmbeddedTexture? utTexturePage = new UndertaleEmbeddedTexture();
        utTexturePage.TextureHeight = utTexturePage.TextureWidth = pageDimension;
        gmData.EmbeddedTextures.Add(utTexturePage);

        void AddAllSpritesFromDir(string dirPath)
        {
            // Recursively add sprites from subdirs
            foreach (string subDir in Directory.GetDirectories(dirPath))
            {
                AddAllSpritesFromDir(subDir);
            }

            foreach (string filePath in Directory.GetFiles(dirPath))
            {
                string extension = new FileInfo(filePath).Extension;
                if (String.IsNullOrWhiteSpace(extension) || extension == ".md" || extension == ".txt") continue;

                Image sprite = Image.Load(filePath);
                currentShelfHeight = Math.Max(currentShelfHeight, sprite.Height);
                if (lastUsedX + sprite.Width > pageDimension)
                {
                    lastUsedX = 0;
                    lastUsedY += currentShelfHeight;
                    currentShelfHeight = sprite.Height + 1; // One pixel padding

                    if (sprite.Width > pageDimension)
                    {
                        throw new NotSupportedException($"Currently a sprite ({filePath}) is bigger than the max size of a {pageDimension} texture page!");
                    }
                }

                if (lastUsedY + sprite.Height > pageDimension) throw new NotSupportedException($"Currently all the sprites would be above a {pageDimension} texture page!");

                int xCoord = lastUsedX;
                int yCoord = lastUsedY;
                newTexturePage.Mutate(i => i.DrawImage(sprite, new Point(xCoord, yCoord), 1));
                UndertaleTexturePageItem pageItem = new UndertaleTexturePageItem();
                pageItem.SourceX = (ushort)xCoord;
                pageItem.SourceY = (ushort)yCoord;
                pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = (ushort)sprite.Width;
                pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = (ushort)sprite.Height;
                pageItem.TexturePage = utTexturePage;
                gmData.TexturePageItems.Add(pageItem);
                lastUsedX += sprite.Width + 1; //One pixel padding
                nameToPageItemDict.Add(Path.GetFileNameWithoutExtension(filePath), gmData.TexturePageItems.Count - 1);
            }
        }

        AddAllSpritesFromDir(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/sprites");
        using (MemoryStream ms = new MemoryStream())
        {
            newTexturePage.Save(ms, PngFormat.Instance);
            utTexturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
        }

        // Replace A4 doors
        {
            UndertaleTexturePageItem? a4DoorTex = gmData.TexturePageItems[nameToPageItemDict["newA4Doors"]];
            Image a4DoorImage = Image.Load(a4DoorTex.TexturePage.TextureData.TextureBlob);
            a4DoorImage.Mutate(i => i.Crop(new Rectangle(a4DoorTex.SourceX, a4DoorTex.SourceY, a4DoorTex.SourceWidth, a4DoorTex.SourceHeight)));
            UndertaleTexturePageItem? a4Tex = gmData.Backgrounds.ByName("tlArea4Tech").Texture;
            Image a4PageImage = Image.Load(a4Tex.TexturePage.TextureData.TextureBlob);
            a4PageImage.Mutate(i => i.DrawImage(a4DoorImage, new Point(a4Tex.SourceX + 104, a4Tex.SourceY), 1));
            using (MemoryStream ms = new MemoryStream())
            {
                a4PageImage.Save(ms, PngFormat.Instance);
                a4Tex.TexturePage.TextureData.TextureBlob = ms.ToArray();
            }

            UndertaleTexturePageItem? a4door2Tex = gmData.TexturePageItems[nameToPageItemDict["newA4Doors2"]];
            Image a4Door2Image = Image.Load(a4door2Tex.TexturePage.TextureData.TextureBlob);
            a4Door2Image.Mutate(i => i.Crop(new Rectangle(a4door2Tex.SourceX, a4door2Tex.SourceY, a4door2Tex.SourceWidth, a4door2Tex.SourceHeight)));
            UndertaleTexturePageItem? a4Tex2 = gmData.Backgrounds.ByName("tlArea4Tech2").Texture;
            Image a4Page2Image = Image.Load(a4Tex2.TexturePage.TextureData.TextureBlob);
            a4Page2Image.Mutate(i => i.DrawImage(a4Door2Image, new Point(a4Tex2.SourceX + 104, a4Tex2.SourceY), 1));
            using (MemoryStream ms = new MemoryStream())
            {
                a4Page2Image.Save(ms, PngFormat.Instance);
                a4Tex2.TexturePage.TextureData.TextureBlob = ms.ToArray();
            }
        }

        UndertaleSimpleList<UndertaleSprite.TextureEntry> GetTexturePageItemsForSpriteName(string name)
        {
            var list = new UndertaleSimpleList<UndertaleSprite.TextureEntry>();
            foreach (string key in nameToPageItemDict.Keys.OrderBy(k => k, StringComparison.OrdinalIgnoreCase.WithNaturalSort()))
            {
                if (key.StartsWith(name)) list.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict[key]] });
            }

            if (!list.Any())
                throw new Exception("Could not find any sprites for " + name);

            return list;
        }

        gmData.Backgrounds.ByName("bg_MapBottom2").Texture = gmData.TexturePageItems[nameToPageItemDict["bg_MapBottom2"]];
        gmData.Backgrounds.ByName("bgGUIMetCountBG1").Texture = gmData.TexturePageItems[nameToPageItemDict["bgGUIMetCountBG2"]];
        gmData.Backgrounds.ByName("bgGUIMetCountBG2").Texture = gmData.TexturePageItems[nameToPageItemDict["bgGUIMetCountBG2"]];
        gmData.Backgrounds.ByName("bgGUIMetCountBG2ELM").Texture = gmData.TexturePageItems[nameToPageItemDict["bgGUIMetCountBG2ELM"]];
        gmData.Backgrounds.ByName("bgLogImg44B").Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogIce"]];
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA0"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA0"]] });
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA1"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA1"]] });
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA2"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA2"]] });
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA3"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA3"]] });
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA4"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA4"]] });
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA5"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA5"]] });
        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("bgLogDNA6"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA6"]] });

        gmData.Backgrounds.Add(
            new UndertaleBackground { Name = gmData.Strings.MakeString("tlWarpHideout"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpHideout"]] });
        gmData.Backgrounds.Add(new UndertaleBackground
            { Name = gmData.Strings.MakeString("tlWarpDepthsEntrance"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpDepthsEntrance"]] });
        gmData.Backgrounds.Add(new UndertaleBackground
            { Name = gmData.Strings.MakeString("tlWarpDepthsExit"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpDepthsExit"]] });
        gmData.Backgrounds.Add(new UndertaleBackground
            { Name = gmData.Strings.MakeString("tlWarpWaterfall"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpWaterfall"]] });


        gmData.Sprites.ByName("sGUIMissile").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIMissileSelected"]] });
        gmData.Sprites.ByName("sGUISMissile").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUISMissileSelected"]] });
        gmData.Sprites.ByName("sGUIPBomb").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIPBombSelected"]] });
        gmData.Sprites.ByName("sGUIMissile").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIMissileNormal"]] });
        gmData.Sprites.ByName("sGUISMissile").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUISMissileNormal"]] });
        gmData.Sprites.ByName("sGUIPBomb").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIPBombNormal"]] });
        gmData.Sprites.ByName("sGUIMissile").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIMissileNormalGreen"]] });
        gmData.Sprites.ByName("sGUISMissile").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUISMissileNormalGreen"]] });
        gmData.Sprites.ByName("sGUIPBomb").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIPBombNormalGreen"]] });

        // Replace existing door sprites
        gmData.Sprites.ByName("sDoorA5Locks").Textures[0].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorBlue"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[1].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorMissile"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[2].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSuper"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[3].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorPBomb"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[4].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTempLocked"]];

        // Add new sprites for doors
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorChargeBeam"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorWaveBeam"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSpazerBeam"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorPlasmaBeam"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorIceBeam"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorBomb"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSpider"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorScrew"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTowerEnabled"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTester"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorGuardian"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorArachnus"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTorizo"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSerris"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorGenesis"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorQueen"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPActivated"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPA1"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPA2"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPA3"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearTotem"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPRobotHome"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearSave"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearBulletHell"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearPipeHub"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPRightExterior"]] });
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorLocked"]] });

        // New sprites for door animation
        gmData.Sprites.ByName("sDoorA5").Textures.Clear();
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_1"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_2"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_3"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_4"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_5"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_6"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_7"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_8"]] });
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_9"]] });

        void CreateAndAddItemSprite(string name)
        {
            gmData.Sprites.Add(new UndertaleSprite
            {
                Name = gmData.Strings.MakeString(name), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
                Textures = GetTexturePageItemsForSpriteName(name + "_")
            });
        }

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemShinyMissile"), Height = 16, Width = 16,
            MarginLeft = 3, MarginRight = 12, MarginBottom = 12, MarginTop = 1, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemShinyMissile_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemSmallHealthDrop"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemSmallHealthDrop_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemBigHealthDrop"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemBigHealthDrop_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemMissileDrop"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemMissileDrop_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemSMissileDrop"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemSMissileDrop_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemPBombDrop"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemPBombDrop_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemFlashlight"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemFlashlight_")
        });
        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemBlindfold"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemBlindfold_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemSpeedBoosterUpgrade"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemSpeedBoosterUpgrade_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            // TODO: sprite is offset by a bit? Double check whether thats still the case
            Name = gmData.Strings.MakeString("sItemNothing"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemNothing_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemUnknown"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemUnknown_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemShinyNothing"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemShinyNothing_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemShinyScrewAttack"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemScrewAttacker_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemShinyIceBeam"), Height = 16, Width = 16,
            MarginLeft = 3, MarginRight = 12, MarginBottom = 12, MarginTop = 1, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemShinyIceBeam_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemShinyHijump"), Height = 16, Width = 16,
            MarginLeft = 3, MarginRight = 12, MarginBottom = 12, MarginTop = 1, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemShinyHijump_")
        });

        gmData.Sprites.ByName("sItemPowergrip").Textures.Clear();
        gmData.Sprites.ByName("sItemPowergrip").Textures = GetTexturePageItemsForSpriteName("sItemPowergrip_");

        gmData.Sprites.ByName("sItemMorphBall").Textures.Clear();
        gmData.Sprites.ByName("sItemMorphBall").Textures = GetTexturePageItemsForSpriteName("sItemMorphBall_");

        gmData.Sprites.ByName("sMapSP").Textures.Add(new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapHint"]] });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sMapBlockUnexplored"), Height = 8, Width = 8,
            Textures =
            {
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] }
            }
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sMapCornerUnexplored"), Height = 8, Width = 8,
            Textures =
            {
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[0].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[1].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[2].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[3].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[4].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[5].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[6].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[7].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[8].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[9].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[10].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[11].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[12].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[13].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[14].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[15].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.Sprites.ByName("sMapCorner").Textures[16].Texture },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_0"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_1"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_0"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_1"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_0"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_1"]] },

                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_2"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_3"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_4"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_5"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_2"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_3"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_4"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_5"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_2"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_3"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_4"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_5"]] },

                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_6"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_7"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_8"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_9"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_6"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_7"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_8"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_9"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_6"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_7"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_8"]] },
                new UndertaleSprite.TextureEntry { Texture = gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_9"]] }
            }
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemMissileLauncher"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemMissileLauncher_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemSMissileLauncher"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemSMissileLauncher_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemPBombLauncher"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemPBombLauncher_")
        });

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sItemDNA"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures = GetTexturePageItemsForSpriteName("sItemDNA_")
        });

        // New sprites for dna septogg
        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sWisdomSeptogg"), Height = 35, Width = 47, MarginLeft = 14, MarginRight = 32, MarginBottom = 11, MarginTop = 6, OriginX = 23,
            OriginY = 35,
            Textures = GetTexturePageItemsForSpriteName("sWisdomSeptogg_")
        });

        #region MW sprites

        CreateAndAddItemSprite("sItemRDV");

        #region Prime1
        CreateAndAddItemSprite("sItemArtifactPrime");
        CreateAndAddItemSprite("sItemBombsPrime");
        CreateAndAddItemSprite("sItemBoostBallPrime");
        CreateAndAddItemSprite("sItemCombatVisorPrime");
        CreateAndAddItemSprite("sItemFlamethrowerPrime");
        CreateAndAddItemSprite("sItemGrappleBeamPrime");
        CreateAndAddItemSprite("sItemGravitySuitPrime");
        CreateAndAddItemSprite("sItemIceBeamPrime");
        CreateAndAddItemSprite("sItemIceSpreaderPrime");
        CreateAndAddItemSprite("sItemMorphBallPrime");
        CreateAndAddItemSprite("sItemPhazonSuitPrime");
        CreateAndAddItemSprite("sItemPlasmaBeamPrime");
        CreateAndAddItemSprite("sItemPowerBeamPrime");
        CreateAndAddItemSprite("sItemPowerBombLauncherPrime");
        CreateAndAddItemSprite("sItemScanVisorPrime");
        CreateAndAddItemSprite("sItemSuperMissilePrime");
        CreateAndAddItemSprite("sItemThermalVisorPrime");
        CreateAndAddItemSprite("sItemVariaSuitPrime");
        CreateAndAddItemSprite("sItemWaveBeamPrime");
        CreateAndAddItemSprite("sItemWaveBusterPrime");
        CreateAndAddItemSprite("sItemXrayVisorPrime");
        #endregion
        #region Prime 2 Echoes
        CreateAndAddItemSprite("sItemAmberEchoes");
        CreateAndAddItemSprite("sItemAnnihilatorEchoes");
        CreateAndAddItemSprite("sItemBeamAmmoEchoes");
        CreateAndAddItemSprite("sItemCannonBallEchoes");
        CreateAndAddItemSprite("sItemCobaltEchoes");
        CreateAndAddItemSprite("sItemDarkAgonKeyEchoes");
        CreateAndAddItemSprite("sItemDarkAmmoEchoes");
        CreateAndAddItemSprite("sItemDarkBeamEchoes");
        CreateAndAddItemSprite("sItemDarkSuitEchoes");
        CreateAndAddItemSprite("sItemDarkTorvusKeyEchoes");
        CreateAndAddItemSprite("sItemDarkVisorEchoes");
        CreateAndAddItemSprite("sItemDarkburstEchoes");
        CreateAndAddItemSprite("sItemEchoVisorEchoes");
        CreateAndAddItemSprite("sItemEmeraldEchoes");
        CreateAndAddItemSprite("sItemIngHiveKeyEchoes");
        CreateAndAddItemSprite("sItemLightAmmoEchoes");
        CreateAndAddItemSprite("sItemLightBeamEchoes");
        CreateAndAddItemSprite("sItemLightSuitEchoes");
        CreateAndAddItemSprite("sItemPowerBombLauncherEchoes");
        CreateAndAddItemSprite("sItemProgressiveSuitEchoes");
        CreateAndAddItemSprite("sItemScrewAttackEchoes");
        CreateAndAddItemSprite("sItemSeekerMissileEchoes");
        CreateAndAddItemSprite("sItemSkyTempleKeyEchoes");
        CreateAndAddItemSprite("sItemSuperMissileEchoes");
        CreateAndAddItemSprite("sItemSonicBoomEchoes");
        CreateAndAddItemSprite("sItemVioletEchoes");
        #endregion

        #region Metroid Dread
        CreateAndAddItemSprite("sItemCrossBombsDread");
        CreateAndAddItemSprite("sItemEPartDread");
        CreateAndAddItemSprite("sItemGrappleBeamDread");
        CreateAndAddItemSprite("sItemIceMissilesDread");
        CreateAndAddItemSprite("sItemMorphBallDread");
        CreateAndAddItemSprite("sItemSpeedBoosterDread");
        CreateAndAddItemSprite("sItemSpiderMagnetDread");
        CreateAndAddItemSprite("sItemSpinBoostDread");
        CreateAndAddItemSprite("sItemStormMissilesDread");
        #endregion
        #endregion

        void RotateTextureAndSaveToTexturePage(int rotation, UndertaleTexturePageItem texture)
        {
            using Image texturePage = Image.Load(texture.TexturePage.TextureData.TextureBlob);
            texturePage.Mutate(im => im.Hue(rotation, new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight)));

            using MemoryStream ms = new MemoryStream();
            texturePage.Save(ms, PngFormat.Instance);
            texture.TexturePage.TextureData.TextureBlob = ms.ToArray();
        }

        // Hue shift etanks
        if (seedObject.Cosmetics.EtankHUDRotation != 0)
        {
            foreach (UndertaleSprite.TextureEntry? textureEntry in gmData.Sprites.ByName("sGUIETank").Textures)
            {
                RotateTextureAndSaveToTexturePage(seedObject.Cosmetics.EtankHUDRotation, textureEntry.Texture);
            }
        }

        // Hue shift health numbers
        if (seedObject.Cosmetics.HealthHUDRotation != 0)
        {
            foreach (UndertaleSprite.TextureEntry? textureEntry in gmData.Sprites.ByName("sGUIFont1").Textures.Concat(gmData.Sprites.ByName("sGUIFont1A").Textures))
            {
                RotateTextureAndSaveToTexturePage(seedObject.Cosmetics.HealthHUDRotation, textureEntry.Texture);
            }
        }

        // Hue shift dna icon
        if (seedObject.Cosmetics.DNAHUDRotation != 0)
        {
            foreach (UndertaleBackground bg in new List<UndertaleBackground> { gmData.Backgrounds.ByName("bgGUIMetCountBG1"), gmData.Backgrounds.ByName("bgGUIMetCountBG2ELM") })
            {
                RotateTextureAndSaveToTexturePage(seedObject.Cosmetics.DNAHUDRotation, bg.Texture);
            }
        }

        // Sabre's new skippy design for Skippy the Bot
        if (seedObject.Patches.SabreSkippy)
        {
            foreach (string spriteName in new[] { "sAutoadP", "sAutoadPFang", "sAutoadPClaw" })
            {
                UndertaleSprite? sprite = gmData.Sprites.ByName(spriteName);
                sprite.Textures[0].Texture = gmData.TexturePageItems[nameToPageItemDict[spriteName]];
            }
        }

        // Shuffle Music
        MusicShuffle.ShuffleMusic(Path.GetDirectoryName(outputAm2rPath), seedObject.Cosmetics.MusicShuffleDict);
        // Fix annoying overlapping songs when fanfare is long song.
        gmData.Code.ByName("gml_Object_oMusicV2_Alarm_0").PrependGMLInCode("if (sfx_isplaying(musFanfare)) audio_stop_sound(musFanfare)");
        gmData.Code.ByName("gml_Script_mus_intro_fanfare").ReplaceGMLInCode("alarm[0] = 60", "alarm[0] = 330");


        // Create new wisdom septogg object
        UndertaleGameObject oWisdomSeptogg = new UndertaleGameObject
        {
            Name = gmData.Strings.MakeString("oWisdomSeptogg"),
            Sprite = gmData.Sprites.ByName("sWisdomSeptogg"),
            Depth = 90
        };
        UndertaleCode wisdomSeptoggCreate = new UndertaleCode { Name = gmData.Strings.MakeString("gml_Object_oWisdomSeptogg_Create_0") };
        wisdomSeptoggCreate.SubstituteGMLCode("image_speed = 0.1666; origY = y; timer = 0;");
        gmData.Code.Add(wisdomSeptoggCreate);
        var wisdomSeptoggCreateList = oWisdomSeptogg.Events[0];
        UndertaleGameObject.EventAction wisdomSeptoggAction = new UndertaleGameObject.EventAction();
        wisdomSeptoggAction.CodeId = wisdomSeptoggCreate;
        UndertaleGameObject.Event wisdomSeptoggEvent = new UndertaleGameObject.Event();
        wisdomSeptoggEvent.EventSubtype = 0;
        wisdomSeptoggEvent.Actions.Add(wisdomSeptoggAction);
        wisdomSeptoggCreateList.Add(wisdomSeptoggEvent);
        UndertaleCode wisdomSeptoggStep = new UndertaleCode { Name = gmData.Strings.MakeString("gml_Object_oWisdomSeptogg_Step_0") };
        wisdomSeptoggStep.SubstituteGMLCode("y = origY + (sin((timer) * 0.08) * 2); timer++; if (timer > 9990) timer = 0;");
        gmData.Code.Add(wisdomSeptoggStep);
        var wisdomSeptoggStepList = oWisdomSeptogg.Events[3];
        wisdomSeptoggAction = new UndertaleGameObject.EventAction();
        wisdomSeptoggAction.CodeId = wisdomSeptoggStep;
        wisdomSeptoggEvent = new UndertaleGameObject.Event();
        wisdomSeptoggEvent.EventSubtype = 0;
        wisdomSeptoggEvent.Actions.Add(wisdomSeptoggAction);
        wisdomSeptoggStepList.Add(wisdomSeptoggEvent);
        gmData.GameObjects.Add(oWisdomSeptogg);

        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");

        // Fix power grip sprite
        gmData.Sprites.ByName("sItemPowergrip").OriginX = 0;
        gmData.Sprites.ByName("sItemPowergrip").OriginY = 16;

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

        // Make beams not instantly despawn when out of screen
        gmData.Code.ByName("gml_Object_oBeam_Step_0").ReplaceGMLInCode(
            "if (x < ((view_xview[0] - 48) - (oControl.widescreen_space / 2)) || x > (((view_xview[0] + view_wview[0]) + 48) + (oControl.widescreen_space / 2)) || y < (view_yview[0] - 48) || y > ((view_yview[0] + view_hview[0]) + 48))",
            "if (x > (room_width + 80) || x < -80 || y > (room_height + 80) || y < -160)");

        // Make Missiles not instantly despawn when out of screen
        gmData.Code.ByName("gml_Object_oMissile_Step_0").ReplaceGMLInCode(
            "if (x < ((view_xview[0] - 48) - (oControl.widescreen_space / 2)) || x > (((view_xview[0] + view_wview[0]) + 48) + (oControl.widescreen_space / 2)) || y < (view_yview[0] - 48) || y > ((view_yview[0] + view_hview[0]) + 48))",
            "if (x > (room_width + 80) || x < -80 || y > (room_height + 80) || y < -160)");

        // Fix arachnus event value doing x coordinate BS
        gmData.Code.ByName("gml_Object_oArachnus_Alarm_11").ReplaceGMLInCode("global.event[103] = x", "global.event[103] = 1");
        // Make arachnus item location always spawn in center
        gmData.Code.ByName("gml_Room_rm_a2a04_Create").ReplaceGMLInCode("instance_create(global.event[103]", "instance_create(room_width / 2");

        //No more Out of Bounds oSmallsplash crashes
        gmData.Code.ByName("gml_Object_oSmallSplash_Step_0").ReplaceGMLInCode("if (global.watertype == 0)", "if (global.watertype == 0 && instance_exists(oWater))");

        // Killing queen should not lock you out of the rest of the game
        gmData.Code.ByName("gml_RoomCC_rm_a0h01_3762_Create").AppendGMLInCode("instance_destroy()");
        gmData.Code.ByName("gml_Room_rm_a0h01_Create").AppendGMLInCode("tile_layer_delete(-119)");

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

        // Make water turbine generic where it can be shuffled
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Create_0").PrependGMLInCode("facingDirection = 1; if (image_xscale < 0) facingDirection = -1; wasAlreadyDestroyed = 0;");
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Create_0").ReplaceGMLInCode("""
                                                                                 if (global.event[101] > 0)
                                                                                     instance_destroy()
                                                                                 """,
            """
            eventToSet = 101;
            if (((((global.targetx - (32 * facingDirection)) == x) && ((global.targety - 64) == y))) ||
                (room == rm_a2h02 && x == 912 && y == 1536 && global.event[101] != 0))
            {
                if (global.event[eventToSet] < 1)
                    global.event[eventToSet] = 1;
                wasAlreadyDestroyed = 1;
                instance_destroy();
            }
            """);
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Create_0").ReplaceGMLInCode("wall = instance_create((x + 16), y, oSolid1x4)",
            "var xWallOffset = 16; if (facingDirection == -1) xWallOffset = -32; wall = instance_create(x + xWallOffset, y, oSolid1x4);");
        gmData.Code.ByName("gml_Object_oA2BigTurbine_Other_11").ReplaceGMLInCode(
            """
            o = instance_create(x, y, oMoveWater)
            o.targety = 1552
            o.delay = 2
            global.event[101] = 1
            instance_create((x - 120), y, oBubbleSpawner)
            """,
            """
            global.event[eventToSet] = 1;
            if (room == rm_a2h02 && x == 912 && y == 1536 && global.event[101] == 1)
            {
                o = instance_create(x, y, oMoveWater)
                o.targety = 1552
                o.delay = 2
                instance_create((x - 120), y, oBubbleSpawner)
            }
            """);

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
        UndertaleCode doorSamusCollision = new UndertaleCode();
        doorSamusCollision.Name = gmData.Strings.MakeString("gml_Object_oDoor_Collision_267");
        doorSamusCollision.SubstituteGMLCode("if (!open && ((lock == 11 && other.state == other.SPIDERBALL) || " +
                                             "(lock == 12 && global.screwattack && other.state == other.JUMPING && !other.vjump && !other.walljumping && (!other.inwater || global.currentsuit >= 2))))" +
                                             "event_user(1)");
        gmData.Code.Add(doorSamusCollision);
        var doorCollisionList = gmData.GameObjects.ByName("oDoor").Events[4];
        UndertaleGameObject.EventAction varDoorAction = new UndertaleGameObject.EventAction();
        varDoorAction.CodeId = doorSamusCollision;
        UndertaleGameObject.Event varDoorEvent = new UndertaleGameObject.Event();
        varDoorEvent.EventSubtype = 267; // 267 is oCharacter ID
        varDoorEvent.Actions.Add(varDoorAction);
        doorCollisionList.Add(varDoorEvent);

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

        // Fixes character step event for further modification
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("""
                                                                                 if (yVel < 0 && state == AIRBALL)
                                                                                 {
                                                                                     if (isCollisionUpRight() == 1 && kRight == 0)
                                                                                         x -= ((1 + statetime < 2) + statetime < 4)
                                                                                     if (isCollisionUpLeft() == 1 && kLeft == 0)
                                                                                         x += ((1 + statetime < 2) + statetime < 4)
                                                                                 }
                                                                             """, """
                                                                                      if (yVel < 0 && state == AIRBALL)
                                                                                      {
                                                                                  		var st1, st2;
                                                                                  		st1 = 0
                                                                                  		st2 = 0
                                                                                  		if (statetime < 2)
                                                                                  			st1 = 1
                                                                                  		if (statetime < 4)
                                                                                  			st2 = 1
                                                                                  		if (isCollisionUpRight() == 1 && kRight == 0)
                                                                                              x -= ((1 + st1) + st2)
                                                                                          if (isCollisionUpLeft() == 1 && kLeft == 0)
                                                                                              x += ((1 + st1) + st2)
                                                                                      }
                                                                                  """);

        // Add doors to gfs thoth bridge
        UndertaleCode thothLeftDoorCC = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_thothLeftDoor_Create") };
        UndertaleCode thothRightDoorCC = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_thothRightDoor_Create") };
        gmData.Code.Add(thothLeftDoorCC);
        gmData.Code.Add(thothRightDoorCC);
        gmData.Rooms.ByName("rm_a8a03").GameObjects.Add(new UndertaleRoom.GameObject
        {
            X = 24,
            Y = 96,
            ObjectDefinition = gmData.GameObjects.ByName("oDoorA8"),
            InstanceID = ThothBridgeLeftDoorID,
            ScaleY = 1,
            ScaleX = 1,
            CreationCode = thothLeftDoorCC
        });
        gmData.Rooms.ByName("rm_a8a03").GameObjects.Add(new UndertaleRoom.GameObject
        {
            X = 616,
            Y = 96,
            ObjectDefinition = gmData.GameObjects.ByName("oDoorA8"),
            InstanceID = ThothBridgeRightDoorID,
            ScaleX = -1,
            ScaleY = 1,
            CreationCode = thothRightDoorCC
        });

        // Make doors appear in front, so you can see them in door lock rando
        gmData.Code.ByName("gml_Room_rm_a8a03_Create").AppendGMLInCode("with (oDoor) depth = -200");


        // Add door from water turbine station to hydro station exterior
        UndertaleCode waterTurbineDoorCC = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_waterStationDoor_Create") };
        gmData.Code.Add(waterTurbineDoorCC);
        UndertaleRoom? rm_a2a08 = gmData.Rooms.ByName("rm_a2a08");
        rm_a2a08.GameObjects.Add(CreateRoomObject(24, 96, gmData.GameObjects.ByName("oDoor"), waterTurbineDoorCC, 1, 1, A2WaterTurbineLeftDoorID));


        UndertaleBackground? doorTileset = gmData.Backgrounds.ByName("tlDoor");
        rm_a2a08.Tiles.Add(CreateRoomTile(16, 144, -103, doorTileset, 112, 64));
        rm_a2a08.Tiles.Add(CreateRoomTile(16, 128, -103, doorTileset, 112, 32));
        rm_a2a08.Tiles.Add(CreateRoomTile(16, 112, -103, doorTileset, 112, 16));
        rm_a2a08.Tiles.Add(CreateRoomTile(16, 96, -103, doorTileset, 112, 0));
        rm_a2a08.Tiles.Add(CreateRoomTile(0, 144, -103, doorTileset, 96, 64));
        rm_a2a08.Tiles.Add(CreateRoomTile(0, 128, -103, doorTileset, 96, 32));
        rm_a2a08.Tiles.Add(CreateRoomTile(0, 112, -103, doorTileset, 96, 16));
        rm_a2a08.Tiles.Add(CreateRoomTile(0, 96, -103, doorTileset, 96, 0));


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
            gmData.Code.ByName(name).ReplaceGMLInCode("&& global.missiles == 0 && global.smissiles == 0", "");
        }

        // Replace Metroids counters with DNA counters
        UndertaleCode? drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
        drawGuiCode.ReplaceGMLInCode("global.monstersleft", "global.dna");
        drawGuiCode.ReplaceGMLInCode("global.monstersarea", "46 - global.dna");
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
            // Hideout
            UndertaleRoom? hideoutPipeRoom = gmData.Rooms.ByName("rm_a6a11");
            UndertaleBackground? hideoutPipeTileset = gmData.Backgrounds.ByName("tlWarpDepthsEntrance");
            UndertaleBackground? depthsEntrancePipeTileset = gmData.Backgrounds.ByName("tlWarpHideout");
            UndertaleBackground? depthsExitPipeTileset = gmData.Backgrounds.ByName("tlWarpWaterfall");
            UndertaleBackground? waterfallsPipeTileset = gmData.Backgrounds.ByName("tlWarpDepthsExit");
            UndertaleBackground? pipeBGTileset = gmData.Backgrounds.ByName("tlWarpPipes");
            UndertaleGameObject? solidObject = gmData.GameObjects.ByName("oSolid1");
            UndertaleGameObject? pipeObject = gmData.GameObjects.ByName("oWarpPipeTrigger");
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 176, 100, hideoutPipeTileset, 0, 48, 48, 48));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 176, -101, hideoutPipeTileset, 32, 0));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(368, 176, -101, hideoutPipeTileset, 48, 32));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(384, 176, -101, hideoutPipeTileset, 16, 0));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 192, -101, hideoutPipeTileset, 0, 32));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(352, 208, -101, hideoutPipeTileset, 32, 16));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(368, 208, -101, hideoutPipeTileset, 48, 48));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(384, 208, -101, hideoutPipeTileset, 16, 16));
            hideoutPipeRoom.Tiles.Add(CreateRoomTile(360, 80, 100, pipeBGTileset, 0, 32, 32, 96));

            hideoutPipeRoom.GameObjects.Add(CreateRoomObject(352, 176, solidObject, null, 3));
            hideoutPipeRoom.GameObjects.Add(CreateRoomObject(352, 192, solidObject));
            hideoutPipeRoom.GameObjects.Add(CreateRoomObject(352, 208, solidObject, null, 3));

            UndertaleCode hideoutPipeCode = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a6a11_pipe_Create") };
            hideoutPipeCode.SubstituteGMLCode("targetroom = 327; targetx = 216; targety = 400; direction = 90;");
            gmData.Code.Add(hideoutPipeCode);
            hideoutPipeRoom.GameObjects.Add(CreateRoomObject(368, 192, pipeObject, hideoutPipeCode, 1, 1, PipeInHideoutID));
            hideoutPipeRoom.CreationCodeId.AppendGMLInCode("global.darkness = 0; mus_change(mus_get_main_song());");

            // Nest
            UndertaleRoom? nestPipeRoom = gmData.Rooms.ByName("rm_a6b03");
            nestPipeRoom.Tiles.Add(CreateRoomTile(192, 368, 100, depthsEntrancePipeTileset, 0, 48, 48, 48));
            nestPipeRoom.Tiles.Add(CreateRoomTile(192, 368, -101, depthsEntrancePipeTileset, 0, 0));
            nestPipeRoom.Tiles.Add(CreateRoomTile(208, 368, -101, depthsEntrancePipeTileset, 48, 32));
            nestPipeRoom.Tiles.Add(CreateRoomTile(224, 368, -101, depthsEntrancePipeTileset, 48, 0));
            nestPipeRoom.Tiles.Add(CreateRoomTile(224, 384, -101, depthsEntrancePipeTileset, 16, 32));
            nestPipeRoom.Tiles.Add(CreateRoomTile(192, 400, -101, depthsEntrancePipeTileset, 0, 16));
            nestPipeRoom.Tiles.Add(CreateRoomTile(208, 400, -101, depthsEntrancePipeTileset, 48, 48));
            nestPipeRoom.Tiles.Add(CreateRoomTile(224, 400, -101, depthsEntrancePipeTileset, 48, 16));
            //nestPipeRoom.Tiles.Add(CreateRoomTile(360, 80, 100, pipeBGTileset, 0, 32, 32, 96));

            nestPipeRoom.GameObjects.Add(CreateRoomObject(192, 368, solidObject, null, 3));
            nestPipeRoom.GameObjects.Add(CreateRoomObject(224, 384, solidObject));
            nestPipeRoom.GameObjects.Add(CreateRoomObject(192, 400, solidObject, null, 3));

            nestPipeRoom.CreationCodeId.AppendGMLInCode("mus_change(musArea6A)");

            UndertaleCode nestPipeCode = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a6b03_pipe_Create") };
            nestPipeCode.SubstituteGMLCode("targetroom = 317; targetx = 376; targety = 208; direction = 270;");
            gmData.Code.Add(nestPipeCode);
            nestPipeRoom.GameObjects.Add(CreateRoomObject(208, 384, pipeObject, nestPipeCode, 1, 1, PipeInDepthsLowerID));

            // Change slope to solid to prevent oob issue
            nestPipeRoom.GameObjects.First(o => o.X == 176 && o.Y == 416).ObjectDefinition = solidObject;

            // Add shortcut between Depths and Waterfalls
            // Depths
            UndertaleRoom? depthsPipeRoom = gmData.Rooms.ByName("rm_a6b11");
            depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 160, 100, depthsExitPipeTileset, 0, 48, 48, 48));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 160, -101, depthsExitPipeTileset, 32, 0));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(96, 160, -101, depthsExitPipeTileset, 48, 32));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(112, 160, -101, depthsExitPipeTileset, 16, 0));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 176, -101, depthsExitPipeTileset, 0, 32));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 192, -101, depthsExitPipeTileset, 32, 16));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(96, 192, -101, depthsExitPipeTileset, 48, 48));
            depthsPipeRoom.Tiles.Add(CreateRoomTile(112, 192, -101, depthsExitPipeTileset, 16, 16));
            //depthsPipeRoom.Tiles.Add(CreateRoomTile(80, 80, 100, pipeBGTileset, 0, 32, 32, 96));

            // Clean up some tiles/collision
            depthsPipeRoom.Tiles.Remove(depthsPipeRoom.Tiles.First(t => t.X == 112 && t.Y == 160));
            depthsPipeRoom.Tiles.Remove(depthsPipeRoom.Tiles.First(t => t.X == 80 && t.Y == 192));
            depthsPipeRoom.GameObjects.First(o => o.X == 96 && o.Y == 208).ObjectDefinition = solidObject;

            depthsPipeRoom.GameObjects.Add(CreateRoomObject(80, 160, solidObject, null, 3));
            depthsPipeRoom.GameObjects.Add(CreateRoomObject(80, 176, solidObject));
            depthsPipeRoom.GameObjects.Add(CreateRoomObject(80, 192, solidObject, null, 3));

            depthsPipeRoom.CreationCodeId.AppendGMLInCode("mus_change(musArea6A);");

            UndertaleCode depthsPipeCode = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a6b11_pipe_Create") };
            depthsPipeCode.SubstituteGMLCode("targetroom = 348; targetx = 904; targety = 208; direction = 180;");
            gmData.Code.Add(depthsPipeCode);
            depthsPipeRoom.GameObjects.Add(CreateRoomObject(96, 176, pipeObject, depthsPipeCode, 1, 1, PipeInDepthsUpperID));

            // Waterfalls
            UndertaleRoom? waterfallsPipeRoom = gmData.Rooms.ByName("rm_a7a07");
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 176, 100, waterfallsPipeTileset, 0, 48, 48, 48));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 176, -101, waterfallsPipeTileset, 0, 0));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(896, 176, -101, waterfallsPipeTileset, 48, 32));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(912, 176, -101, waterfallsPipeTileset, 48, 0));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(912, 192, -101, waterfallsPipeTileset, 16, 32));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 208, -101, waterfallsPipeTileset, 0, 16));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(896, 208, -101, waterfallsPipeTileset, 48, 48));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(912, 208, -101, waterfallsPipeTileset, 48, 16));
            //nestPipeRoom.Tiles.Add(CreateRoomTile(360, 80, 100, pipeBGTileset, 0, 32, 32, 96));

            // Clean up some tiles/collision
            waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 912 && t.Y == 192));
            waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 912 && t.Y == 208));
            waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 896 && t.Y == 192));
            waterfallsPipeRoom.Tiles.Remove(waterfallsPipeRoom.Tiles.First(t => t.X == 880 && t.Y == 192));
            waterfallsPipeRoom.Tiles.Add(CreateRoomTile(880, 224, -100, gmData.Backgrounds.ByName("tlRock7A"), 0, 32, 32));

            waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(880, 176, solidObject, null, 3));
            waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(912, 192, solidObject));
            waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(880, 208, solidObject, null, 3));

            UndertaleCode waterfallsPipeCode = new UndertaleCode { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a7a07_pipe_Create") };
            waterfallsPipeCode.SubstituteGMLCode("targetroom = 335; targetx = 104; targety = 192; direction = 0;");
            gmData.Code.Add(waterfallsPipeCode);
            waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(896, 192, pipeObject, waterfallsPipeCode, 1, 1, PipeInWaterfallsID));

            waterfallsPipeRoom.CreationCodeId.AppendGMLInCode("global.darkness = 0");

            // Modify minimap for new pipes and purple in nest and waterfalls too
            // Hideout
            gmData.Code.ByName("gml_Script_map_init_04").ReplaceGMLInCode(@"global.map[21, 53] = ""1210100""", @"global.map[21, 53] = ""12104U0""");
            gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[20, 53] = ""1012100""", @"global.map[20, 53] = ""1012400""");
            // Depths lower
            gmData.Code.ByName("gml_Script_map_init_04").ReplaceGMLInCode("global.map[21, 44] = \"1102100\"\nglobal.map[21, 45] = \"0112100\"",
                "global.map[21, 44] = \"1102400\"\nglobal.map[21, 45] = \"01124D0\"");
            // Depths upper
            gmData.Code.ByName("gml_Script_map_init_02").ReplaceGMLInCode(@"global.map[16, 34] = ""1012100""", @"global.map[16, 34] = ""10124L0""");
            gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[17, 34] = ""1010100""", @"global.map[17, 34] = ""1010400""");
            gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[18, 34] = ""1020100""", @"global.map[18, 34] = ""1020400""");
            gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[19, 34] = ""1010100""", @"global.map[19, 34] = ""1010400""");
            gmData.Code.ByName("gml_Script_map_init_03").ReplaceGMLInCode(@"global.map[20, 34] = ""1210100""", @"global.map[20, 34] = ""1210400""");
            // Waterfalls
            gmData.Code.ByName("gml_Script_map_init_01").ReplaceGMLInCode(@"global.map[7, 34] = ""1012200""", @"global.map[7, 34] = ""1012400""");
            gmData.Code.ByName("gml_Script_map_init_17").ReplaceGMLInCode(@"global.map[8, 34] = ""1010200""", @"global.map[8, 34] = ""1010400""");
            gmData.Code.ByName("gml_Script_map_init_01").ReplaceGMLInCode(@"global.map[9, 34] = ""1010200""", @"global.map[9, 34] = ""10104R0""");
            gmData.Code.ByName("gml_Script_map_init_02").ReplaceGMLInCode(@"global.map[10, 34] = ""1210200""", @"global.map[10, 34] = ""1210400""");
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
        labsRoom.Tiles.Add(new UndertaleRoom.Tile
        {
            X = 64,
            Y = 96,
            TileDepth = -99,
            BackgroundDefinition = gmData.Backgrounds.ByName("tlArea7Outside"),
            InstanceID = gmData.GeneralInfo.LastTile++,
            SourceX = 0,
            SourceY = 208,
            Width = 32,
            Height = 32
        });
        labsRoom.Tiles.Add(new UndertaleRoom.Tile
        {
            X = 64,
            Y = 128,
            TileDepth = -99,
            BackgroundDefinition = gmData.Backgrounds.ByName("tlArea7Outside"),
            InstanceID = gmData.GeneralInfo.LastTile++,
            SourceX = 0,
            SourceY = 208,
            Width = 32,
            Height = 32
        });

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

        // TODO: change samus arm cannon to different sprite, when no missile launcher. This requires delving into state machine tho and that is *pain*
        // For that, also make her not arm the cannon if you have missile launcher but no missiles
        // ALTERNATIVE: if missile equipped, but no launcher, make EMP effect display that usually appears in gravity area

        // Have new variables for certain events because they are easier to debug via a switch than changing a ton of values
        characterVarsCode.PrependGMLInCode(
            "global.septoggHelpers = 0; global.skipCutscenes = 0; global.skipSaveCutscene = 0; global.skipItemFanfare = 0; global.respawnBombBlocks = 0; global.screwPipeBlocks = 0;" +
            "global.a3Block = 0; global.softlockPrevention = 0; global.unexploredMap = 0; global.unveilBlocks = 0; global.canUseSupersOnMissileDoors = 0;");

        // Set geothermal reactor to always be exploded
        characterVarsCode.AppendGMLInCode("global.event[203] = 9");

        // Set a bunch of metroid events to already be scanned
        characterVarsCode.AppendGMLInCode("global.event[301] = 1; global.event[305] = 1; global.event[306] = 1;");

        // Move Geothermal PB to big shaft
        gmData.Rooms.ByName("rm_a4b02a").CreationCodeId.AppendGMLInCode("instance_create(272, 400, scr_itemsopen(oControl.mod_253));");
        gmData.Rooms.ByName("rm_a4b02b").CreationCodeId.ReplaceGMLInCode("instance_create(314, 192, scr_itemsopen(oControl.mod_253))", "");

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

        // Decouple Major items from item locations
        characterVarsCode.PrependGMLInCode("global.dna = 0; global.hasBombs = 0; global.hasPowergrip = 0; global.hasSpiderball = 0; global.hasJumpball = 0; global.hasHijump = 0;" +
                                           "global.hasVaria = 0; global.hasSpacejump = 0; global.hasSpeedbooster = 0; global.hasScrewattack = 0; global.hasGravity = 0;" +
                                           "global.hasCbeam = 0; global.hasIbeam = 0; global.hasWbeam = 0; global.hasSbeam  = 0; global.hasPbeam = 0; global.hasMorph = 0;");

        // Make all item activation dependant on whether the main item is enabled.
        characterVarsCode.ReplaceGMLInCode("""
                                           global.morphball = 1
                                           global.jumpball = 0
                                           global.powergrip = 1
                                           global.spacejump = 0
                                           global.screwattack = 0
                                           global.hijump = 0
                                           global.spiderball = 0
                                           global.speedbooster = 0
                                           global.bomb = 0
                                           global.ibeam = 0
                                           global.wbeam = 0
                                           global.pbeam = 0
                                           global.sbeam = 0
                                           global.cbeam = 0
                                           """, """
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
                                                """);
        characterVarsCode.ReplaceGMLInCode("global.currentsuit = 0",
            "global.currentsuit = 0; if (global.hasGravity) global.currentsuit = 2; else if (global.hasVaria) global.currentsuit = 1;");

        // Fix spring showing up for a brief moment when killing arachnus
        gmData.Code.ByName("gml_Object_oArachnus_Alarm_11").ReplaceGMLInCode("if (temp_randitem == oItemJumpBall)", "if (false)");

        // Bombs
        UndertaleCode? subscreenMenuStep = gmData.Code.ByName("gml_Object_oSubscreenMenu_Step_0");
        subscreenMenuStep.ReplaceGMLInCode("global.item[0] == 0", "!global.hasBombs");
        UndertaleCode? subscreenMiscDaw = gmData.Code.ByName("gml_Object_oSubScreenMisc_Draw_0");
        subscreenMiscDaw.ReplaceGMLInCode("global.item[0]", "global.hasBombs");

        foreach (string code in new[]
                 {
                     "gml_Script_spawn_rnd_pickup", "gml_Script_spawn_rnd_pickup_at", "gml_Script_spawn_many_powerups",
                     "gml_Script_spawn_many_powerups_tank", "gml_RoomCC_rm_a2a06_4759_Create", "gml_RoomCC_rm_a2a06_4761_Create",
                     "gml_RoomCC_rm_a3h03_5279_Create", "gml_Room_rm_a3b08_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[0]", "global.hasBombs");
        }

        UndertaleGameObject? elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (UndertaleRoom.GameObject go in room.GameObjects.Where(go => go.ObjectDefinition == elderSeptogg && go.CreationCode is not null))
            {
                go.CreationCode.ReplaceGMLInCode("global.item[0]", "global.hasBombs", true);
            }
        }


        // Powergrip
        subscreenMiscDaw.ReplaceGMLInCode("global.item[1]", "global.hasPowergrip");
        subscreenMenuStep.ReplaceGMLInCode("global.item[1] == 0", "!global.hasPowergrip");

        // Spiderball
        subscreenMiscDaw.ReplaceGMLInCode("global.item[2]", "global.hasSpiderball");
        subscreenMenuStep.ReplaceGMLInCode("global.item[2] == 0", "!global.hasSpiderball");
        foreach (UndertaleCode code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                               c.Name.Content.Contains('2')) || c.Name.Content == "gml_RoomCC_rm_a0h25_4105_Create"))
        {
            code.ReplaceGMLInCode("global.item[2]", "global.hasSpiderball");
        }

        // Jumpball
        subscreenMiscDaw.ReplaceGMLInCode("global.item[3]", "global.hasJumpball");
        subscreenMenuStep.ReplaceGMLInCode("global.item[3] == 0", "!global.hasJumpball");
        gmData.Code.ByName("gml_RoomCC_rm_a2a06_4761_Create").ReplaceGMLInCode("global.item[3] == 0", "!global.hasJumpball");

        // Hijump
        UndertaleCode? subcreenBootsDraw = gmData.Code.ByName("gml_Object_oSubScreenBoots_Draw_0");
        subcreenBootsDraw.ReplaceGMLInCode("global.item[4]", "global.hasHijump");
        subscreenMenuStep.ReplaceGMLInCode("global.item[4] == 0", "!global.hasHijump");
        foreach (UndertaleCode? code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                                c.Name.Content.Contains('4')) || c.Name.Content == "gml_Room_rm_a3b08_Create" ||
                                                               c.Name.Content == "gml_RoomCC_rm_a5c17_7779_Create"))
        {
            code.ReplaceGMLInCode("global.item[4]", "global.hasHijump");
        }

        // Varia
        UndertaleCode? subscreenSuitDraw = gmData.Code.ByName("gml_Object_oSubScreenSuit_Draw_0");
        subscreenSuitDraw.ReplaceGMLInCode("global.item[5]", "global.hasVaria");
        subscreenMenuStep.ReplaceGMLInCode("global.item[5] == 0", "!global.hasVaria");
        foreach (string code in new[]
                 {
                     "gml_Script_characterStepEvent", "gml_Script_damage_player", "gml_Script_damage_player_push", "gml_Script_damage_player_knockdown",
                     "gml_Object_oQueenHead_Step_0"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[5]", "global.hasVaria");
        }

        // Spacejump
        subcreenBootsDraw.ReplaceGMLInCode("global.item[6]", "global.hasSpacejump");
        subscreenMenuStep.ReplaceGMLInCode("global.item[6] == 0", "!global.hasSpacejump");
        foreach (UndertaleCode? code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                                c.Name.Content.Contains('6')) || c.Name.Content.StartsWith("gml_RoomCC_rm_a5a03_") ||
                                                               c.Name.Content == "gml_RoomCC_rm_a0h25_4105_Create"))
        {
            code.ReplaceGMLInCode("global.item[6]", "global.hasSpacejump", true);
        }

        // Speedbooster
        subcreenBootsDraw.ReplaceGMLInCode("global.item[7]", "global.hasSpeedbooster");
        subscreenMenuStep.ReplaceGMLInCode("global.item[7] == 0", "!global.hasSpeedbooster");
        foreach (UndertaleCode? code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") &&
                                                                c.Name.Content.Contains('7')) || c.Name.Content.StartsWith("gml_RoomCC_rm_a5c08_")))
        {
            code.ReplaceGMLInCode("global.item[7]", "global.hasSpeedbooster", true);
        }


        // Screwattack
        subscreenMiscDaw.ReplaceGMLInCode("global.item[8]", "global.hasScrewattack");
        subscreenMenuStep.ReplaceGMLInCode("global.item[8] == 0", "!global.hasScrewattack");
        foreach (string code in new[]
                 {
                     "gml_Script_scr_septoggs_2468", "gml_Script_scr_septoggs_48", "gml_RoomCC_rm_a1a06_4447_Create",
                     "gml_RoomCC_rm_a1a06_4448_Create", "gml_RoomCC_rm_a1a06_4449_Create", "gml_RoomCC_rm_a3a04_5499_Create", "gml_RoomCC_rm_a3a04_5500_Create",
                     "gml_RoomCC_rm_a3a04_5501_Create", "gml_RoomCC_rm_a4a01_6476_Create", "gml_RoomCC_rm_a4a01_6477_Create", "gml_RoomCC_rm_a4a01_6478_Create",
                     "gml_RoomCC_rm_a5c13_7639_Create", "gml_RoomCC_rm_a5c13_7640_Create", "gml_RoomCC_rm_a5c13_7641_Create", "gml_RoomCC_rm_a5c13_7642_Create",
                     "gml_RoomCC_rm_a5c13_7643_Create", "gml_RoomCC_rm_a5c13_7644_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[8]", "global.hasScrewattack");
        }


        // Gravity
        subscreenSuitDraw.ReplaceGMLInCode("global.item[9]", "global.hasGravity");
        subscreenMenuStep.ReplaceGMLInCode("global.item[9] == 0", "!global.hasGravity");

        foreach (string code in new[]
                 {
                     "gml_Script_scr_variasuitswap", "gml_Object_oGravitySuitChangeFX_Step_0", "gml_Object_oGravitySuitChangeFX_Other_10",
                     "gml_RoomCC_rm_a2a06_4759_Create", "gml_RoomCC_rm_a2a06_4761_Create", "gml_RoomCC_rm_a5a03_8631_Create", "gml_RoomCC_rm_a5a03_8632_Create",
                     "gml_RoomCC_rm_a5a03_8653_Create", "gml_RoomCC_rm_a5a03_8654_Create", "gml_RoomCC_rm_a5a03_8655_Create", "gml_RoomCC_rm_a5a03_8656_Create",
                     "gml_RoomCC_rm_a5a03_8657_Create", "gml_RoomCC_rm_a5a03_8674_Create", "gml_RoomCC_rm_a5a05_8701_Create", "gml_RoomCC_rm_a5a06_8704_Create"
                 })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[9]", "global.hasGravity");
        }

        // Charge
        UndertaleCode? itemsSwapScript = gmData.Code.ByName("gml_Script_scr_itemsmenu_swap");
        itemsSwapScript.ReplaceGMLInCode("global.item[10]", "global.hasCbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[10] == 0", "!global.hasCbeam");

        // Ice
        itemsSwapScript.ReplaceGMLInCode("global.item[11]", "global.hasIbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[11] == 0", "!global.hasIbeam");
        foreach (string code in new[] { "gml_Object_oEris_Create_0", "gml_Object_oErisBody1_Create_0", "gml_Object_oErisHead_Create_0", "gml_Object_oErisSegment_Create_0" })
        {
            gmData.Code.ByName(code).ReplaceGMLInCode("global.item[11] == 0", "!global.hasIbeam");
        }

        // Wave
        itemsSwapScript.ReplaceGMLInCode("global.item[12]", "global.hasWbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[12] == 0", "!global.hasWbeam");

        // Spazer
        itemsSwapScript.ReplaceGMLInCode("global.item[13]", "global.hasSbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[13] == 0", "!global.hasSbeam");

        // Plasma
        itemsSwapScript.ReplaceGMLInCode("global.item[14]", "global.hasPbeam");
        subscreenMenuStep.ReplaceGMLInCode("global.item[14] == 0", "!global.hasPbeam");

        // Morph Ball
        subscreenMiscDaw.ReplaceGMLInCode("""
                                          draw_sprite(sSubScrButton, global.morphball, (x - 28), (y + 16))
                                          draw_text((x - 20), ((y + 15) + oControl.subScrItemOffset), morph)
                                          """, """
                                               if (global.hasMorph) {
                                                   draw_sprite(sSubScrButton, global.morphball, (x - 28), (y + 16))
                                                   draw_text((x - 20), ((y + 15) + oControl.subScrItemOffset), morph)
                                               }
                                               """);
        subscreenMenuStep.ReplaceGMLInCode("""
                                           if (global.curropt == 7 && (!global.hasIbeam))
                                                   global.curropt += 1
                                           """, """
                                                if (global.curropt == 7 && (!global.hasIbeam))
                                                        global.curropt += 1
                                                if (global.curropt == 8 && (!global.hasMorph))
                                                        global.curropt += 1
                                                """);
        subscreenMenuStep.ReplaceGMLInCode("""
                                           if (global.curropt == 7 && (!global.hasIbeam))
                                                   global.curropt -= 1
                                           """, """
                                                if (global.curropt == 8 && (!global.hasMorph))
                                                        global.curropt -= 1
                                                if (global.curropt == 7 && (!global.hasIbeam))
                                                        global.curropt -= 1
                                                """);
        subscreenMenuStep.ReplaceGMLInCode("""
                                               else
                                                   global.curropt = 14
                                           """, """
                                                    else
                                                        global.curropt = 14
                                                    if (global.curropt == 8 && (!global.hasMorph))
                                                        global.curropt += 1
                                                    if (global.curropt == 9 && (!global.hasSpiderball))
                                                        global.curropt += 1
                                                    if (global.curropt == 10 && (!global.hasJumpball))
                                                        global.curropt += 1
                                                    if (global.curropt == 11 && (!global.hasBombs))
                                                        global.curropt += 1
                                                    if (global.curropt == 12 && (!global.hasPowergrip))
                                                        global.curropt += 1
                                                    if (global.curropt == 13 && (!global.hasScrewattack))
                                                        global.curropt += 1
                                                """);

        subscreenMenuStep.ReplaceGMLInCode("""
                                               if (global.curropt > 16)
                                                   global.curropt = 8
                                           """, """
                                                    if (global.curropt > 16)
                                                        global.curropt = 8
                                                    if (global.curropt == 8 && (!global.hasMorph))
                                                            global.curropt = 0
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
                case ItemEnum.Nothing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            characterVarsCode.AppendGMLInCode($"global.collectedItems += \"{item.GetEnumMemberValue()}|{quantity},\"");
        }

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

        // Set starting location
        characterVarsCode.ReplaceGMLInCode("global.startingSave = 0", $"global.startingSave = {seedObject.StartingLocation.SaveRoom}");
        characterVarsCode.ReplaceGMLInCode("global.save_room = 0", $"global.save_room = {seedObject.StartingLocation.SaveRoom}");

        // Modify minimap for power plant because of pb movement
        gmData.Code.ByName("gml_Script_map_init_07").ReplaceGMLInCode("""
                                                                      global.map[35, 43] = "0112300"
                                                                      global.map[35, 44] = "1210300"
                                                                      global.map[35, 45] = "1210300"
                                                                      """, """
                                                                           global.map[35, 43] = "0101330"
                                                                           global.map[35, 44] = "0101300"
                                                                           global.map[35, 45] = "0101300"
                                                                           """);
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("&& itemid == 253", "&& false");
        // Removes map tiles of the inaccessible A4 basement/Reactor Core/Power Plant.
        gmData.Code.ByName("gml_Script_init_map").AppendGMLInCode("""
                                                                  // Remove A4 reactor core map tiles
                                                                  // Upper left
                                                                  i = 31
                                                                  repeat (4)
                                                                  {
                                                                      j = 43
                                                                      repeat (3)
                                                                      {
                                                                          global.map[i, j] = "0"
                                                                          j++
                                                                      }
                                                                      i++
                                                                  }

                                                                  // Mid section
                                                                  global.map[36, 44] = "0"
                                                                  global.map[36, 45] = "0"
                                                                  global.map[36, 46] = "0"
                                                                  global.map[37, 46] = "0"

                                                                  i = 34
                                                                  repeat (4)
                                                                  {
                                                                      j = 47
                                                                      repeat (6)
                                                                      {
                                                                          global.map[i, j] = "0"
                                                                          j++
                                                                      }
                                                                      i++
                                                                  }

                                                                  // Below A6
                                                                  i = 31
                                                                  repeat (8)
                                                                  {
                                                                      j = 54
                                                                      repeat (6)
                                                                      {
                                                                          global.map[i, j] = "0"
                                                                          j++
                                                                      }
                                                                      i++
                                                                  }
                                                                  """);


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
        gmData.Scripts.AddScript("get_flashlight", "global.flashlightLevel += argument0; with (oLightEngine) instance_destroy(); with (oFlashlight64) instance_destroy(); ApplyLightPreset()");
        gmData.Scripts.AddScript("get_blindfold", "global.flashlightLevel -= argument0; with (oLightEngine) instance_destroy(); with (oFlashlight64) instance_destroy(); ApplyLightPreset()");
        gmData.Scripts.AddScript("get_speed_booster_upgrade", "global.speedBoosterFramesReduction += argument0;");

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
                ItemEnum.MissileLauncher => "event_inherited(); if (active) " +
                                            $"{{ get_missile_launcher({pickup.Quantity}) }}",
                ItemEnum.SuperMissileExpansion => $"get_super_missile_expansion({pickup.Quantity})",
                ItemEnum.SuperMissileLauncher => "event_inherited(); if (active) " +
                                                 $"{{ get_super_missile_launcher({pickup.Quantity}) }}",
                ItemEnum.PBombExpansion => $"get_pb_expansion({pickup.Quantity})",
                ItemEnum.PBombLauncher => "event_inherited(); if (active) " +
                                          $"{{ get_pb_launcher({pickup.Quantity}) }}",
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
        // screw+pipes related
        if (seedObject.Patches.ScrewPipeBlocks) characterVarsCode.ReplaceGMLInCode("global.screwPipeBlocks = 0", "global.screwPipeBlocks = 1");

        // Screw blocks before normal pipe rooms
        foreach (string codeName in new[]
                 {
                     "gml_Room_rm_a1a06_Create", "gml_Room_rm_a2a08_Create", "gml_Room_rm_a2a09_Create", "gml_Room_rm_a2a12_Create", "gml_Room_rm_a3a04_Create",
                     "gml_Room_rm_a4h01_Create", "gml_Room_rm_a4a01_Create"
                 })
        {
            gmData.Code.ByName(codeName).AppendGMLInCode("if (!global.screwPipeBlocks) {with (oBlockScrew) instance_destroy();}");
        }

        foreach (string roomName in new[] { "rm_a1a06", "rm_a3a04", "rm_a4a01" })
        {
            foreach (UndertaleRoom.GameObject? gameObject in gmData.Rooms.ByName(roomName).GameObjects.Where(g => g.ObjectDefinition.Name.Content == "oBlockScrew"))
            {
                if (gameObject.CreationCode is null) continue;

                gameObject.CreationCode.ReplaceGMLInCode("global.hasScrewattack == 0", "false");
            }
        }

        // A bunch of tiles in a5c13 - screw blocks before pipe hub
        for (int i = 39; i <= 44; i++)
        {
            gmData.Code.ByName($"gml_RoomCC_rm_a5c13_76{i}_Create").SubstituteGMLCode("if (!global.screwPipeBlocks) instance_destroy();");
        }

        // Bomb block before a3 entry
        if (seedObject.Patches.A3EntranceBlocks)
        {
            characterVarsCode.ReplaceGMLInCode("global.a3Block = 0", "global.a3Block = 1;");
        }

        gmData.Code.ByName("gml_RoomCC_rm_a3h03_5279_Create").ReplaceGMLInCode(
            "if ((oControl.mod_randomgamebool == 1 || oControl.mod_splitrandom == 1) && global.hasBombs == 0 && global.ptanks == 0)",
            "if (!global.a3Block)");

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
            if (gameObject.X == 48 || gameObject.X == 64)
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
            if (gameObject.X == 96 || gameObject.X == 112)
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

        // A4 exterior top, always remove the bomb blocks when coming from that entrance
        foreach (string codeName in new[] { "gml_RoomCC_rm_a4h03_6341_Create", "gml_RoomCC_rm_a4h03_6342_Create" })
        {
            gmData.Code.ByName(codeName).ReplaceGMLInCode("oControl.mod_previous_room == 214 && global.spiderball == 0", "global.targetx == 416");
        }

        // Super Missile chamber - make first two crumble blocks shoot blocks
        gmData.Code.ByName("gml_Room_rm_a3a23a_Create").AppendGMLInCode("if (global.softlockPrevention) { with (119465) instance_destroy(); with (119465) instance_destroy(); instance_create(304, 96, oBlockShoot); instance_create(304, 112, oBlockShoot);}");

        // The bomb block puzzle in the room before varia dont need to be done anymore because it's already now covered by "dont regen bomb blocks" option
        gmData.Code.ByName("gml_RoomCC_rm_a2a06_4761_Create").ReplaceGMLInCode(
            "if (oControl.mod_randomgamebool == 1 && global.hasBombs == 0 && (!global.hasJumpball) && global.hasGravity == 0)",
            "if (false)");
        gmData.Code.ByName("gml_RoomCC_rm_a2a06_4759_Create").ReplaceGMLInCode("if (oControl.mod_randomgamebool == 1 && global.hasBombs == 0 && global.hasGravity == 0)",
            "if (false)");

        // When going down from thoth, make PB blocks disabled
        gmData.Code.ByName("gml_Room_rm_a0h13_Create").PrependGMLInCode("if (global.targety == 16) {global.event[176] = 1; with (oBlockPBombChain) event_user(0); }");

        // When coming from right side in Drill, always make drill event done
        gmData.Code.ByName("gml_Room_rm_a0h17e_Create").PrependGMLInCode("if (global.targety == 160) global.event[172] = 3");

        // Stop Bomb blocks from respawning
        if (seedObject.Patches.RespawnBombBlocks) characterVarsCode.ReplaceGMLInCode("global.respawnBombBlocks = 0", "global.respawnBombBlocks = 1");

        // The position here is for a puzzle in a2, that when not respawned makes it a tad hard.
        gmData.Code.ByName("gml_Object_oBlockBomb_Other_10").PrependGMLInCode("if (!global.respawnBombBlocks && !(room == rm_a2a06 && x == 624 && y == 128)) regentime = -1");

        // On start, make all rooms show being "unexplored" similar to prime/super rando
        ShowFullyUnexploredMap.Apply(gmData, decompileContext, seedObject);

        // Force all breakables (except the hidden super blocks) to be visible
        if (seedObject.Cosmetics.UnveilBlocks) characterVarsCode.ReplaceGMLInCode("global.unveilBlocks = 0", "global.unveilBlocks = 1");

        gmData.Code.ByName("gml_Object_oSolid_Alarm_5").AppendGMLInCode("if (global.unveilBlocks && sprite_index >= sBlockShoot && sprite_index <= sBlockSand)\n" +
                                                                        "{ event_user(1); visible = true; }");

        // Skip most cutscenes when enabled
        if (seedObject.Patches.SkipCutscenes) characterVarsCode.ReplaceGMLInCode("global.skipCutscenes = 0", "global.skipCutscenes = 1");

        // Skip Intro cutscene instantly
        gmData.Code.ByName("gml_Object_oIntroCutscene_Create_0").PrependGMLInCode("room_change(15, 0)");
        // First Alpha cutscene - event 0
        characterVarsCode.AppendGMLInCode("global.event[0] = global.skipCutscenes");
        // Gamma mutation cutscene - event 109
        gmData.Code.ByName("gml_Object_oMGammaFirstTrigger_Collision_267").PrependGMLInCode("""
                                                                                            if (global.skipCutscenes)
                                                                                            {
                                                                                                global.event[109] = 1;
                                                                                                mus_current_fadeout();
                                                                                                mutat = instance_create(144, 96, oMGammaMutate);
                                                                                                mutat.state = 3;
                                                                                                mutat.statetime = 90;
                                                                                                instance_destroy();
                                                                                                exit;
                                                                                            }
                                                                                            """);
        // Zeta mutation cutscene - event 205
        characterVarsCode.AppendGMLInCode("global.event[205] = global.skipCutscenes");
        // Omega Mutation cutscene - event 300
        characterVarsCode.AppendGMLInCode("global.event[300] = global.skipCutscenes");
        // Also still increase the metroid counters from the hatchling cutscene
        gmData.Code.ByName("gml_Object_oEggTrigger_Create_0").PrependGMLInCode("""
                                                                               if (global.skipCutscenes && !global.event[302])
                                                                               {
                                                                                    if (oControl.mod_monstersextremecheck == 1)
                                                                                       oControl.mod_monstersextreme = 1
                                                                                   global.event[302] = 1
                                                                                   global.monstersleft = 9
                                                                                   if (global.difficulty == 2)
                                                                                       global.monstersleft = 16
                                                                                   if (oControl.mod_fusion == 1)
                                                                                       global.monstersleft = 21
                                                                                   if (oControl.mod_monstersextreme == 1)
                                                                                       global.monstersleft = 47
                                                                                   if (!instance_exists(oScanMonster))
                                                                                   {
                                                                                       scan = instance_create(0, 0, oScanMonster)
                                                                                       scan.ammount = 9
                                                                                       if (global.difficulty == 2)
                                                                                           scan.ammount = 16
                                                                                       if (oControl.mod_fusion == 1)
                                                                                           scan.ammount = 21
                                                                                       if (oControl.mod_monstersextreme == 1)
                                                                                           scan.ammount = 47
                                                                                       scan.eventno = 700
                                                                                       scan.alarm[0] = 15
                                                                                   }
                                                                               }
                                                                               """);
        // Drill cutscene - event 172 to 3
        characterVarsCode.AppendGMLInCode("global.event[172] = global.skipCutscenes * 3");
        // 1 Orb cutscene
        gmData.Code.ByName("gml_Object_oClawOrbFirst_Other_11")
            .AppendGMLInCode(
                "if (global.skipCutscenes) {with (ecam) instance_destroy(); global.enablecontrol = 1; view_object[0] = oCamera; block2 = instance_create(768, 48, oSolid2x2); block2.material = 3; with (oA1MovingPlatform2) with (myblock) instance_destroy()}");
        // 3 Orb cutscene
        gmData.Code.ByName("gml_Object_oClawPuzzle_Alarm_0")
            .AppendGMLInCode(
                "if (global.skipCutscenes) {with (ecam) instance_destroy(); global.enablecontrol = 1; view_object[0] = oCamera; block2 = instance_create(608, 112, oSolid2x2); block2.material = 3; with (oA1MovingPlatform) with (myblock) instance_destroy()}");
        // Fix audio for the orb cutscenes
        gmData.Code.ByName("gml_Object_oMusicV2_Other_4").AppendGMLInCode("sfx_stop(sndStoneLoop)");
        // Skip baby collected cutscene
        gmData.Code.ByName("gml_Object_oHatchlingTrigger_Collision_267")
            .PrependGMLInCode("if (global.skipCutscenes) { global.event[304] = 1; instance_create(x, y, oHatchling); instance_destroy(); exit; }");
        // Skip A5 activation cutscene to not have to wait a long time
        gmData.Code.ByName("gml_Object_oA5MainSwitch_Step_0").ReplaceGMLInCode("""
                                                                                       if (oCharacter.x < 480)
                                                                                       {
                                                                                           with (oCharacter)
                                                                                               x += 1
                                                                                       }
                                                                               """, """
                                                                                            if (oCharacter.x < 480)
                                                                                            {
                                                                                                with (oCharacter)
                                                                                                    x += 1
                                                                                            }
                                                                                            if (oCharacter.x == 480 && global.skipCutscenes)
                                                                                                statetime = 119
                                                                                    """);
        gmData.Code.ByName("gml_Object_oA5MainSwitch_Step_0").ReplaceGMLInCode("instance_create(x, y, oA5BotSpawnCutscene)",
            "instance_create(x, y, oA5BotSpawnCutscene); if (global.skipCutscenes) statetime = 319");

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

        // Add patch to see room names on minimap
        DisplayRoomNamesOnMap.Apply(gmData, decompileContext, seedObject);

        // Adjust pause screen text to mention room names
        RoomFeatureMapText.Apply(gmData, decompileContext);


        // Add spoiler log in credits when finished game normally
        gmData.Code.ByName("gml_Object_oCreditsText_Create_0")
            .ReplaceGMLInCode("TEXT_ROWS = ", $"if (!global.creditsmenuopt) text = \"{seedObject.CreditsSpoiler}\" + text;\n TEXT_ROWS = ");


        // Multiworld stuff
        // Needed variables
        gmData.Scripts.AddScript("mw_debug", """
        var totalString = string(current_hour) + ":" + string(current_minute) + "." + string(current_second) + " - " + argument0
        show_debug_message(totalString);
        var f = file_text_open_append(working_directory + "/mw-debug-" + oControl.year + oControl.month + oControl.day + oControl.hour + oControl.minute + ".txt")
        file_text_write_string(f, totalString)
        file_text_writeln(f)
        file_text_close(f)
        """);

        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sDisconnected"), Height = 8, Width = 8, MarginRight = 7, MarginBottom = 7, OriginX = 0, OriginY = 8,
            Textures = GetTexturePageItemsForSpriteName("sDisconnected")
        });

        gmData.Code.ByName("gml_Object_oControl_Create_0").PrependGMLInCode($"""
        year = string(current_year)
        month = string(current_month)
        day = string(current_day)
        hour = string(current_hour)
        minute = string(current_minute)
        PACKET_HANDSHAKE=1
        PACKET_UUID=2
        PACKET_LOG=3
        PACKET_KEEP_ALIVE=4
        PACKET_NEW_INVENTORY=5
        PACKET_NEW_LOCATION=6
        PACKET_RECEIVED_PICKUPS=7
        PACKET_GAME_STATE=8
        PACKET_DISPLAY_MESSAGE=9
        PACKET_MALFORMED=10
        socketServer = network_create_server_raw(0, 2016, 1)
        if (socketServer < 0) mw_debug("The port 2016 was not able to be opened!")
        packetNumber = 0
        networkProtocolVersion = 1
        hasConnectedAtLeastOnce = false
        hasConnectedAlpha = 1
        hasConnectedAlphaDirection = 0
        currentGameUuid = "{seedObject.Identifier.WorldUUID}"
        clientState = 0
        CLIENT_DISCONNECTED = 0
        CLIENT_HANDSHAKE_CONNECTION = 1
        CLIENT_FULLY_CONNECTED = 2
        clientSocket = 0
        messageDisplay = ""
        messageDisplayTimer = 0
        MESSAGE_DISPLAY_TIMER_INITIAL = 360
        fetchPickupTimer = -1
        PICKUP_TIMER_INITIAL = 180
        global.lastOffworldNumber = 0
        global.collectedIndices = "locations:"
        global.collectedItems = "items:"
        tempTimer = 0
        mw_debug("Multiworld variables initialized");
        """);

        // Also add the save items to character vars
        characterVarsCode.PrependGMLInCode("""
        global.lastOffworldNumber = 0
        global.collectedIndices = "locations:"
        global.collectedItems = "items:"
        """);

        // Add script to send location and inventory info
        gmData.Scripts.AddScript("send_location_and_inventory_packet", """
        if (oControl.socketServer >= 0 && oControl.clientState >= oControl.CLIENT_FULLY_CONNECTED)
        {
            mw_debug("Sending location+inventory packet:")
            mw_debug("Current locations: " + global.collectedIndices)
            mw_debug("Current inventory: " + global.collectedItems)
            var collectedLocation = buffer_create(512, buffer_grow, 1)
            buffer_seek(collectedLocation, buffer_seek_start, 0)
            buffer_write(collectedLocation, buffer_u8, oControl.PACKET_NEW_LOCATION)
            var currentPos = buffer_tell(collectedLocation)
            buffer_write(collectedLocation, buffer_text, global.collectedIndices)
            var length = (buffer_tell(collectedLocation) - currentPos)
            buffer_seek(collectedLocation, buffer_seek_start, currentPos)
            buffer_write(collectedLocation, buffer_u16, length)
            buffer_write(collectedLocation, buffer_text, global.collectedIndices)
            network_send_raw(oControl.clientSocket, collectedLocation, buffer_get_size(collectedLocation))
            mw_debug("Sent location packet")
            buffer_delete(collectedLocation)
            var collectedItem = buffer_create(512, buffer_grow, 1)
            buffer_seek(collectedItem, buffer_seek_start, 0)
            buffer_write(collectedItem, buffer_u8, oControl.PACKET_NEW_INVENTORY)
            currentPos = buffer_tell(collectedItem)
            buffer_write(collectedItem, buffer_text, global.collectedItems)
            length = (buffer_tell(collectedItem) - currentPos)
            buffer_seek(collectedItem, buffer_seek_start, currentPos)
            buffer_write(collectedItem, buffer_u16, length)
            buffer_write(collectedItem, buffer_text, global.collectedItems)
            network_send_raw(oControl.clientSocket, collectedItem, buffer_get_size(collectedItem))
            mw_debug("Sent inventory packet")
            buffer_delete(collectedItem)
        }
        """);


        // Send collected item and location when collecting items
        gmData.Code.ByName("gml_Object_oItem_Other_10").ReplaceGMLInCode("instance_destroy()", """
        global.collectedIndices += (string(itemid) + ",")
        global.collectedItems += (((string(itemName) + "|") + string(itemQuantity)) + ",")
        mw_debug("Local item was collected: location " + string(itemid) + " , name " + string(itemName) + " quantity " + string(itemQuantity))
        send_location_and_inventory_packet();
        instance_destroy();
        """);

        // Send collected items and locations when loading saves
        gmData.Code.ByName("gml_Object_oLoadGame_Other_10").AppendGMLInCode("""
        mw_debug("Some save got loaded")
        send_room_info_packet(global.start_room);
        send_location_and_inventory_packet();
        """);

        // Send room info when transition rooms
        gmData.Scripts.AddScript("send_room_info_packet", """
        var rm = argument0;
        if (rm == undefined)
            rm = room
        if (oControl.socketServer >= 0 && oControl.clientState >= oControl.CLIENT_FULLY_CONNECTED)
        {
            mw_debug("Sending room info packet, with room " + string(room_get_name(rm)))
            var roomName = buffer_create(512, buffer_grow, 1)
            buffer_seek(roomName, buffer_seek_start, 0)
            buffer_write(roomName, buffer_u8, oControl.PACKET_GAME_STATE)
            var currentPos = buffer_tell(roomName)
            buffer_write(roomName, buffer_text, string(room_get_name(rm)))
            var length = (buffer_tell(roomName) - currentPos)
            buffer_seek(roomName, buffer_seek_start, currentPos)
            buffer_write(roomName, buffer_u16, length)
            buffer_write(roomName, buffer_text, string(room_get_name(rm)))
            network_send_raw(oControl.clientSocket, roomName, buffer_get_size(roomName))
            mw_debug("Sent room packet")
            buffer_delete(roomName)
        }
        """);

        gmData.Code.ByName("gml_Object_oControl_Other_4").AppendGMLInCode("""
        mw_debug("Player has changed their room")
        send_room_info_packet();
        """);

        // Periodically send what our last received offworld item was
        gmData.Code.ByName("gml_Object_oControl_Step_0").AppendGMLInCode("""
        if (socketServer >= 0 && clientState >= oControl.CLIENT_FULLY_CONNECTED && fetchPickupTimer == 0)
        {
            mw_debug("Sending PickupTimer packet. Our current value: " + string(global.lastOffworldNumber));
            fetchPickupTimer = PICKUP_TIMER_INITIAL
            var pickupBuffer = buffer_create(512, buffer_grow, 1)
            buffer_seek(pickupBuffer, buffer_seek_start, 0)
            buffer_write(pickupBuffer, buffer_u8, PACKET_RECEIVED_PICKUPS)
            var currentPos = buffer_tell(pickupBuffer)
            buffer_write(pickupBuffer, buffer_text, string(global.lastOffworldNumber))
            var length = (buffer_tell(pickupBuffer) - currentPos)
            buffer_seek(pickupBuffer, buffer_seek_start, currentPos)
            buffer_write(pickupBuffer, buffer_u16,length)
            buffer_write(pickupBuffer, buffer_text, string(global.lastOffworldNumber))
            network_send_raw(clientSocket, pickupBuffer, buffer_get_size(pickupBuffer))
            mw_debug("send receive pickup packet")
            buffer_delete(pickupBuffer)
        }
        fetchPickupTimer--
        tempTimer++
        if (tempTimer > 9999) tempTimer = 0
        """);

        // Show MW messages + cant open port message
        gmData.Code.ByName("gml_Script_draw_gui").AppendGMLInCode("""
                                             if (oControl.socketServer < 0)
                                             {
                                                 draw_set_font(global.fontGUI2)
                                                 draw_set_halign(fa_left)
                                                 draw_cool_text(0, 50, "Could not open Port!#Please try reopening#the game after 2min.", c_black, c_white, c_white, 1)
                                             }
                                             if (oControl.hasConnectedAtLeastOnce && (oControl.socketServer >= 0 && clientState == oControl.CLIENT_DISCONNECTED))
                                             {
                                                if (oControl.tempTimer % 60 == 0)
                                                    mw_debug("Showing 'game is disconnected' message.")
                                                draw_set_font(global.fontGUI2)
                                                draw_set_halign(fa_left)
                                                draw_sprite_ext(sDisconnected, 0, 5, 61, 1, 1, 0, c_white, oControl.hasConnectedAlpha);
                                                if (oControl.hasConnectedAlpha <= 0)
                                                    oControl.hasConnectedAlphaDirection = 0
                                                else if (oControl.hasConnectedAlpha >= 1.4)
                                                    oControl.hasConnectedAlphaDirection = 1
                                                if (oControl.hasConnectedAlphaDirection == 1)
                                                    oControl.hasConnectedAlpha -= 0.025
                                                else if (oControl.hasConnectedAlphaDirection == 0)
                                                    oControl.hasConnectedAlpha += 0.025
                                                draw_cool_text(15, 50, "Disconnected!", c_black, c_white, c_white, 1)
                                             }
                                             if (global.ingame && oControl.messageDisplay != "" && oControl.messageDisplayTimer > 0)
                                             {
                                                 if (oControl.tempTimer % 60 == 0)
                                                    mw_debug("Showing message display; message: " + string(oControl.messageDisplay) + " timer: " + string(oControl.messageDisplayTimer))
                                                 draw_set_font(global.fontGUI2)
                                                 draw_set_halign(fa_center)
                                                 draw_cool_text(160 + (widescreen_space / 2), 40, oControl.messageDisplay, c_black, c_white, c_white, 1)
                                                 oControl.messageDisplayTimer--
                                                 if (oControl.messageDisplayTimer <= 0)
                                                 {
                                                     oControl.messageDisplayTimer = 0
                                                     messageDisplay = ""
                                                     mw_debug("Timer ran out, resetting both display and timer to empty values")
                                                 }
                                                 draw_set_halign(fa_left)
                                                 draw_set_font(global.fontMenuTiny)
                                             }
                                             """);

        // Received network packet event.
        var oControlNetworkReceived = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oControl_Other_68") };
        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = oControlNetworkReceived.Name;
        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = gmData.Strings.MakeString("arguments");
        argsLocal.Index = 0;
        locals.Locals.Add(argsLocal);
        oControlNetworkReceived.LocalsCount = 1;
        gmData.CodeLocals.Add(locals);
        oControlNetworkReceived.SubstituteGMLCode($$"""
        mw_debug("Async Networking event entered")
        var type_event, _buffer, bufferSize, msgid, handshake, socket, malformed, protocolVer, length, currentPos, i, upperLimit;
        type_event = ds_map_find_value(async_load, "type")
        switch type_event
        {
            case 1:
                mw_debug("The client has done the initial connection to our socket")
                clientSocket = ds_map_find_value(async_load, "socket")
                break
            case 2:
                mw_debug("The client disconnected from the socket, resetting values")
                clientSocket = 0
                clientState = CLIENT_DISCONNECTED
                packetNumber = 0
                fetchPickupsTimer = -1
                break
            case 3:
                mw_debug("Client send us incoming data")
                socket = clientSocket
                if (socket == undefined || socket == 0)
                    exit
                mw_debug("Client socket is valid")
                mw_debug(("Socket: " + string(socket)))
                _buffer = ds_map_find_value(async_load, "buffer")
                if (_buffer == undefined)
                    exit
                mw_debug("Confirmed for Client to send us valid data")
                mw_debug(("Buffer id: " + string(_buffer)))
                bufferSize = buffer_get_size(_buffer)
                mw_debug("Buffer size: " + string(bufferSize))
                msgid = buffer_read(_buffer, buffer_u8)
                mw_debug(("Message ID of the packet: " + string(msgid)))
                switch msgid
                {
                    case PACKET_HANDSHAKE:
                        mw_debug("Entered Handshake handling.")
                        mw_debug(("Current client state: " + string(clientState)))
                        if (clientState == CLIENT_DISCONNECTED)
                        {
                            mw_debug("Client is not connected, initiating procedure")
                            clientState = CLIENT_HANDSHAKE_CONNECTION
                            mw_debug("Client has been internally set to initial connection")
                            var handshake = buffer_create(2, buffer_grow, 1)
                            buffer_seek(handshake, buffer_seek_start, 0)
                            buffer_write(handshake, buffer_u8, PACKET_HANDSHAKE)
                            buffer_write(handshake, buffer_u8, string(packetNumber))
                            network_send_raw(socket, handshake, buffer_get_size(handshake))
                            buffer_delete(handshake)
                            packetNumber = ((packetNumber + 1) % 256)
                            mw_debug("Internal packet number set to " + string(packetNumber))
                        }
                        else mw_debug("Client tried to initiate handshake while already connected")
                        break
                    case PACKET_UUID:
                        mw_debug("Client requested UUID")
                        if (clientState < CLIENT_HANDSHAKE_CONNECTION)
                        {
                            mw_debug("Client tried to request UUID while not being connected yet.")
                            exit
                        }
                        mw_debug("Client passed the handshake check")
                        protocolVer = buffer_create(1024, buffer_grow, 1)
                        buffer_seek(protocolVer, buffer_seek_start, 0)
                        buffer_write(protocolVer, buffer_u8, PACKET_UUID)
                        buffer_write(protocolVer, buffer_u8, string(packetNumber))
                        currentPos = buffer_tell(protocolVer)
                        buffer_write(protocolVer, buffer_text, string(networkProtocolVersion) + "," + string(currentGameUuid))
                        length = (buffer_tell(protocolVer) - currentPos)
                        buffer_seek(protocolVer, buffer_seek_start, currentPos)
                        buffer_write(protocolVer, buffer_u16, length)
                        buffer_write(protocolVer, buffer_text, string(networkProtocolVersion) + "," + string(currentGameUuid))
                        network_send_raw(socket, protocolVer, buffer_get_size(protocolVer))
                        mw_debug("Sent protocol packet; api version " + string(networkProtocolVersion) + " uuid " + string(currentGameUuid))
                        buffer_delete(protocolVer)
                        packetNumber = ((packetNumber + 1) % 256)
                        clientState = CLIENT_FULLY_CONNECTED
                        hasConnectedAtLeastOnce = true
                        mw_debug("Client has been connected at least once set, Client is fully connected now. Packet number: " + string(packetNumber))
                        fetchPickupTimer = PICKUP_TIMER_INITIAL
                        mw_debug("Pickup timer has been set to the initial timer")
                        send_room_info_packet();
                        send_location_and_inventory_packet();
                        break
                    case PACKET_DISPLAY_MESSAGE:
                        mw_debug("Client send us a message to show")
                        var tempMessage = buffer_read(_buffer, buffer_string)
                        var upperLimit = 45
                        if widescreen
                            upperLimit = 50
                        if (string_length(tempMessage) > upperLimit)
                            tempMessage = string_insert("-#", tempMessage, upperLimit)
                        messageDisplay = tempMessage
                        messageDisplayTimer = MESSAGE_DISPLAY_TIMER_INITIAL
                        mw_debug("Message that client wants us to show: " + messageDisplay)
                        break
                    case PACKET_RECEIVED_PICKUPS:
                        mw_debug("Client send us pickups that we should receive")
                        var message = buffer_read(_buffer, buffer_string)
                        mw_debug("Pickups are in the following message: " + string(message))
                        var splitBy = "|"
                        var splitted;
                        var i = 0
                        var total = string_count(splitBy, message)
                        while (i<=total)
                        {
                            var limit = string_length(message)+1
                            if (string_count(splitBy, message) > 0)
                                limit = string_pos("|", message)
                            var element = string_copy(message, 1, limit-1)
                            splitted[i] = element
                            message = string_copy(message, limit+1, string_length(message)-limit)
                            i++
                        }

                        var provider = splitted[0]
                        var name = splitted[1]
                        var model = splitted[2]
                        var quantity = real(splitted[3])
                        var remoteItemNumber = real(splitted[4])
                        mw_debug("prov:" + provider + " name:" + name + " model: " + model + " quant: " + string(quantity) + " remote num: " + string(remoteItemNumber))
                        if (remoteItemNumber <= global.lastOffworldNumber)
                        {
                            mw_debug("We have already received this item. Bailing out. Our current item is " + string(global.lastOffworldNumber))
                            break;
                        }
                        var knownItem = true
                        active = true       // workaround for some scripts
                        switch (name)
                        {
                            case "{{ItemEnum.EnergyTank.GetEnumMemberValue()}}":
                                get_etank()
                                break
                            case "{{ItemEnum.MissileExpansion.GetEnumMemberValue()}}":
                                get_missile_expansion(quantity)
                                break
                            case "{{ItemEnum.MissileLauncher.GetEnumMemberValue()}}":
                                get_missile_launcher(quantity)
                                break
                            case "{{ItemEnum.SuperMissileExpansion.GetEnumMemberValue()}}":
                                get_super_missile_expansion(quantity)
                                break
                            case "{{ItemEnum.SuperMissileLauncher.GetEnumMemberValue()}}":
                                get_super_missile_launcher(quantity)
                                break
                            case "{{ItemEnum.PBombExpansion.GetEnumMemberValue()}}":
                                get_pb_expansion(quantity)
                                break
                            case "{{ItemEnum.PBombLauncher.GetEnumMemberValue()}}":
                                get_pb_launcher(quantity)
                                break
                            case "{{ItemEnum.Bombs.GetEnumMemberValue()}}":
                                get_bombs()
                                break
                            case "{{ItemEnum.Powergrip.GetEnumMemberValue()}}":
                                get_power_grip()
                                break
                            case "{{ItemEnum.Spiderball.GetEnumMemberValue()}}":
                                get_spider_ball()
                                break
                            case "{{ItemEnum.Springball.GetEnumMemberValue()}}":
                                get_spring_ball()
                                break
                            case "{{ItemEnum.Screwattack.GetEnumMemberValue()}}":
                                get_screw_attack()
                                break
                            case "{{ItemEnum.Varia.GetEnumMemberValue()}}":
                                get_varia()
                                break
                            case "{{ItemEnum.Spacejump.GetEnumMemberValue()}}":
                                get_space_jump()
                                break
                            case "{{ItemEnum.Speedbooster.GetEnumMemberValue()}}":
                                get_speed_booster()
                                break
                            case "{{ItemEnum.Hijump.GetEnumMemberValue()}}":
                                get_hijump()
                                break
                            case "{{ItemEnum.ProgressiveJump.GetEnumMemberValue()}}":
                                get_progressive_jump()
                                break
                            case "{{ItemEnum.Gravity.GetEnumMemberValue()}}":
                                get_gravity()
                                break
                            case "{{ItemEnum.ProgressiveSuit.GetEnumMemberValue()}}":
                                get_progressive_suit()
                                break
                            case "{{ItemEnum.Charge.GetEnumMemberValue()}}":
                                get_charge_beam()
                                break
                            case "{{ItemEnum.Ice.GetEnumMemberValue()}}":
                                get_ice_beam()
                                break
                            case "{{ItemEnum.Wave.GetEnumMemberValue()}}":
                                get_wave_beam()
                                break
                            case "{{ItemEnum.Spazer.GetEnumMemberValue()}}":
                                get_spazer_beam()
                                break
                            case "{{ItemEnum.Plasma.GetEnumMemberValue()}}":
                                get_plasma_beam()
                                break
                            case "{{ItemEnum.Morphball.GetEnumMemberValue()}}":
                                get_morph_ball()
                                break
                            case "{{ItemEnum.SmallHealthDrop.GetEnumMemberValue()}}":
                                get_small_health_drop(quantity);
                                break
                            case "{{ItemEnum.BigHealthDrop.GetEnumMemberValue()}}":
                                get_big_health_drop(quantity)
                                break
                            case "{{ItemEnum.MissileDrop.GetEnumMemberValue()}}":
                                get_missile_drop(quantity)
                                break
                            case "{{ItemEnum.SuperMissileDrop.GetEnumMemberValue()}}":
                                get_super_missile_drop(quantity);
                                break
                            case "{{ItemEnum.PBombDrop.GetEnumMemberValue()}}":
                                get_power_bomb_drop(quantity)
                                break
                            case "{{ItemEnum.Flashlight.GetEnumMemberValue()}}":
                                get_flashlight(quantity);
                                break
                            case "{{ItemEnum.Blindfold.GetEnumMemberValue()}}":
                                get_blindfold(quantity);
                                break
                            case "{{ItemEnum.SpeedBoosterUpgrade.GetEnumMemberValue()}}":
                                get_speed_booster_upgrade(quantity)
                                break
                            default:
                                if (string_count("Metroid DNA", name) > 0)
                                {
                                    get_dna()
                                    break;
                                }
                                mw_debug("Have gotten unknown Item.")
                                knownItem = false
                                break;
                        }
                        var tempMessage = "Received " + name + " from " + provider
                        if (!knownItem)
                            tempMessage = "Unknown item from " + provider + "! Please report this!!!"
                        var upperLimit = 45
                        if widescreen
                            upperLimit = 50
                        if (string_length(tempMessage) > upperLimit)
                            tempMessage = string_insert("-#", tempMessage, upperLimit)
                        messageDisplay = tempMessage
                        messageDisplayTimer = MESSAGE_DISPLAY_TIMER_INITIAL
                        mw_debug("Message that we'll be showing: " + messageDisplay)
                        if (knownItem)
                        {
                            mw_debug("Item that we received was known to us. Attempting to increase inventory; current: " + string(global.collectedItems))
                            global.collectedItems += (name + "|" + string(quantity) + ",")
                            global.lastOffworldNumber = remoteItemNumber
                            mw_debug("Inventory and lastoffworld set. offworld: " + string(global.lastOffworldNumber) + " inventory: " + string(global.collectedItems))
                        }
                        send_location_and_inventory_packet()
                        active = false
                        mw_debug("End of Pickup receive handling.")
                        break;
                }
                break
            default:
                mw_debug("Unknown message id. Sending malformed packet as response")
                malformed = buffer_create(1024, buffer_grow, 1)
                buffer_seek(malformed, buffer_seek_start, 0)
                buffer_write(malformed, buffer_u8, PACKET_MALFORMED)
                buffer_write(malformed, buffer_u8, 0)
                network_send_raw(socket, malformed, buffer_get_size(malformed))
                buffer_delete(malformed)
                break
        }
        mw_debug("Leaving async networking event")
        """);
        gmData.Code.Add(oControlNetworkReceived);
        var oControlCollisionList = gmData.GameObjects.ByName("oControl").Events[7];
        var oControlAction = new UndertaleGameObject.EventAction();
        oControlAction.CodeId = oControlNetworkReceived;
        var oControlEvent = new UndertaleGameObject.Event();
        oControlEvent.EventSubtype = 68;
        oControlEvent.Actions.Add(oControlAction);
        oControlCollisionList.Add(oControlEvent);


        // Write back to disk
        using (FileStream fs = new FileInfo(outputAm2rPath).OpenWrite())
        {
            UndertaleIO.Write(fs, gmData, Console.WriteLine);
        }
    }
}
