using System.Reflection;
using System.Text.Json;
using NaturalSort.Extension;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class Sprites
{
    // TODO: clean this up a little
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var nameToPageItemDict = new Dictionary<string, int>();
        UndertaleEmbeddedTexture? utTexturePage = new UndertaleEmbeddedTexture();

        using (MemoryStream ms = new MemoryStream())
        {
            var texturePage = Image.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/sprites/texturepage.png");
            utTexturePage.TextureWidth = texturePage.Width;
            utTexturePage.TextureHeight = texturePage.Height;
            texturePage.Save(ms, PngFormat.Instance);
            utTexturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
        }
        gmData.EmbeddedTextures.Add(utTexturePage);

        List<PageItem> pageItemInfo = JsonSerializer.Deserialize<List<PageItem>>(File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/sprites/texturepageiteminfo.json"));
        foreach (var item in pageItemInfo)
        {
            UndertaleTexturePageItem pageItem = new UndertaleTexturePageItem();
            pageItem.SourceX = item.X;
            pageItem.SourceY = item.Y;
            pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = item.Width;
            pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = item.Height;
            pageItem.TexturePage = utTexturePage;
            gmData.TexturePageItems.Add(pageItem);
            nameToPageItemDict.Add(item.Name, gmData.TexturePageItems.Count - 1);
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

        gmData.Backgrounds.Add(new UndertaleBackground { Name = gmData.Strings.MakeString("tlDoorsExtended"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlDoorsExtended"]] });

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
                Name = gmData.Strings.MakeString(name),
                Height = 16,
                Width = 16,
                MarginRight = 15,
                MarginBottom = 15,
                OriginX = 0,
                OriginY = 16,
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

        CreateAndAddItemSprite("sItemLongBeam");
        CreateAndAddItemSprite("sItemIBJ");
        CreateAndAddItemSprite("sItemWallJump");

        gmData.Sprites.Add(new UndertaleSprite
        {
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

        // Fix power grip sprite
        gmData.Sprites.ByName("sItemPowergrip").OriginX = 0;
        gmData.Sprites.ByName("sItemPowergrip").OriginY = 16;

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

        CreateAndAddItemSprite("sItemProgressiveJump");
        CreateAndAddItemSprite("sItemProgressiveSuit");

        // New sprites for dna septogg
        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sWisdomSeptogg"), Height = 35, Width = 47, MarginLeft = 14, MarginRight = 32, MarginBottom = 11, MarginTop = 6, OriginX = 23,
            OriginY = 35,
            Textures = GetTexturePageItemsForSpriteName("sWisdomSeptogg_")
        });

        // Sabre's new skippy design for Skippy the Bot
        if (seedObject.Patches.SabreSkippy)
        {
            foreach (string spriteName in new[] { "sAutoadP", "sAutoadPFang", "sAutoadPClaw" })
            {
                UndertaleSprite? sprite = gmData.Sprites.ByName(spriteName);
                sprite.Textures[0].Texture = gmData.TexturePageItems[nameToPageItemDict[spriteName]];
            }
        }

        // Multiworld Disconnected sprite
        gmData.Sprites.Add(new UndertaleSprite
        {
            Name = gmData.Strings.MakeString("sDisconnected"), Height = 8, Width = 8, MarginRight = 7, MarginBottom = 7, OriginX = 0, OriginY = 8,
            Textures = GetTexturePageItemsForSpriteName("sDisconnected")
        });


        #region MW sprites

        CreateAndAddItemSprite("sItemRDV");

        #region Prime1
        CreateAndAddItemSprite("sItemArtifactPrime");
        CreateAndAddItemSprite("sItemArtifactElderPrime");
        CreateAndAddItemSprite("sItemArtifactChozoPrime");
        CreateAndAddItemSprite("sItemArtifactLifegiverPrime");
        CreateAndAddItemSprite("sItemArtifactNaturePrime");
        CreateAndAddItemSprite("sItemArtifactNewbornPrime");
        CreateAndAddItemSprite("sItemArtifactSpiritPrime");
        CreateAndAddItemSprite("sItemArtifactStrengthPrime");
        CreateAndAddItemSprite("sItemArtifactSunPrime");
        CreateAndAddItemSprite("sItemArtifactTruthPrime");
        CreateAndAddItemSprite("sItemArtifactWarriorPrime");
        CreateAndAddItemSprite("sItemArtifactWildPrime");
        CreateAndAddItemSprite("sItemArtifactWorldPrime");
        CreateAndAddItemSprite("sItemBombsPrime");
        CreateAndAddItemSprite("sItemBoostBallPrime");
        CreateAndAddItemSprite("sItemChargeBeamPrime");
        CreateAndAddItemSprite("sItemCombatVisorPrime");
        CreateAndAddItemSprite("sItemEnergyTankPrime");
        CreateAndAddItemSprite("sItemFlamethrowerPrime");
        CreateAndAddItemSprite("sItemGrappleBeamPrime");
        CreateAndAddItemSprite("sItemGravitySuitPrime");
        CreateAndAddItemSprite("sItemIceBeamPrime");
        CreateAndAddItemSprite("sItemIceSpreaderPrime");
        CreateAndAddItemSprite("sItemMissileExpansionPrime");
        CreateAndAddItemSprite("sItemMorphBallPrime");
        CreateAndAddItemSprite("sItemPhazonSuitPrime");
        CreateAndAddItemSprite("sItemPlasmaBeamPrime");
        CreateAndAddItemSprite("sItemPowerBeamPrime");
        CreateAndAddItemSprite("sItemPowerBombLauncherPrime");
        CreateAndAddItemSprite("sItemScanVisorPrime");
        CreateAndAddItemSprite("sItemSpiderBallPrime");
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
        CreateAndAddItemSprite("sItemDiffusionBeamDread");
        CreateAndAddItemSprite("sItemEPartDread");
        CreateAndAddItemSprite("sItemETankDread");
        CreateAndAddItemSprite("sItemFlashShiftDread");
        CreateAndAddItemSprite("sItemGrappleBeamDread");
        CreateAndAddItemSprite("sItemIceMissilesDread");
        CreateAndAddItemSprite("sItemMissileLauncherDread");
        CreateAndAddItemSprite("sItemMissileTankDread");
        CreateAndAddItemSprite("sItemMissileTankPlusDread");
        CreateAndAddItemSprite("sItemMorphBallDread");
        CreateAndAddItemSprite("sItemPowerBombLauncherDread");
        CreateAndAddItemSprite("sItemPowerBombTankDread");
        CreateAndAddItemSprite("sItemPhantomCloakDread");
        CreateAndAddItemSprite("sItemPulseRadarDread");
        CreateAndAddItemSprite("sItemSpaceJumpDread");
        CreateAndAddItemSprite("sItemSpeedBoosterDread");
        CreateAndAddItemSprite("sItemSpeedBoosterUpgradeDread");
        CreateAndAddItemSprite("sItemSpiderMagnetDread");
        CreateAndAddItemSprite("sItemSpinBoostDread");
        CreateAndAddItemSprite("sItemStormMissilesDread");
        CreateAndAddItemSprite("sItemSuperMissileLauncherDread");
        CreateAndAddItemSprite("sItemVariaSuitDread");
        CreateAndAddItemSprite("sItemWideBeamDread");
        #endregion

        #region Cave Story
        CreateAndAddItemSprite("sItemGenericCS");
        #endregion

        #region Samus Returns
        CreateAndAddItemSprite("sItemAeionTankMSR");
        CreateAndAddItemSprite("sItemBombsMSR");
        CreateAndAddItemSprite("sItemBabyMetroidMSR");
        CreateAndAddItemSprite("sItemBeamBurstMSR");
        CreateAndAddItemSprite("sItemGrappleBeamMSR");
        CreateAndAddItemSprite("sItemLightningArmorMSR");
        CreateAndAddItemSprite("sItemMissileLauncherMSR");
        CreateAndAddItemSprite("sItemMorphMSR");
        CreateAndAddItemSprite("sItemPhaseDriftMSR");
        CreateAndAddItemSprite("sItemScanPulseMSR");
        CreateAndAddItemSprite("sItemSpiderBallMSR");
        #endregion

        #region Super Metroid
        CreateAndAddItemSprite("sItemGrappleBeamSuper");
        #endregion
        #endregion
    }
}
