using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using SixLabors.ImageSharp.Formats.Png;

namespace YAMS_LIB;

public class Patcher
{
    private static string CreateVersionString()
    {
        Version? assembly = Assembly.GetExecutingAssembly().GetName().Version;
        if (assembly is null) return "";

        return $"{assembly.Major}.{assembly.Minor}.{assembly.Build}";
    }

    public static string Version = CreateVersionString();
    
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
            
        var seedObject = JsonSerializer.Deserialize<SeedObject>(File.ReadAllText(jsonPath));
            
        // TODO: lots of code cleanup and sanity checking 
            
        // TODO: make insanity save stations enabled again by using jes' code
            
        // TODO: add missile/super drops in spots where you can get sotflocked via ammo (ice, a3 entrance maybe more)
        
        // Read 1.5.x data
        var gmData = new UndertaleData();
            
        using (FileStream fs = new FileInfo(am2rPath).OpenRead())
        {
            gmData = UndertaleIO.Read(fs);
        }
        var decompileContext = new GlobalDecompileContext(gmData, false);

        // Check for 1.5.5 before doing *anything*
        var controlCreate = GetGMLCode(gmData.Code.ByName("gml_Object_oControl_Create_0"));
        if (!controlCreate.Contains("global.am2r_version = \"V1.5.5\""))
            throw new InvalidAM2RVersionException("The selected game is not AM2R 1.5.5!");
        

        string GetGMLCode(UndertaleCode code)
        {
            return Decompiler.Decompile(code, decompileContext);
        }
        
        void ReplaceGMLInCode(UndertaleCode code, string textToReplace, string replacementText, bool ignoreErrors = false)
        {
            var codeText = Decompiler.Decompile(code, decompileContext);
            if (!codeText.Contains(textToReplace))
            {
                if (ignoreErrors)
                    return;
                
                throw new ApplicationException($"The text \"{textToReplace}\" was not found in \"{code.Name.Content}\"!");
            }
            codeText = codeText.Replace(textToReplace, replacementText);
            code.ReplaceGML(codeText, gmData);
        }
            
        void PrependGMLInCode(UndertaleCode code, string prependedText)
        {
            var codeText = Decompiler.Decompile(code, decompileContext);
            codeText = prependedText + "\n" + codeText;
            code.ReplaceGML(codeText, gmData);
        }
            
        void AppendGMLInCode(UndertaleCode code, string appendedText)
        {
            var codeText = Decompiler.Decompile(code, decompileContext);
            codeText = codeText + appendedText + "\n";
            code.ReplaceGML(codeText, gmData);
        }

        void SubstituteGMLCode(UndertaleCode code, string newGMLCode)
        {
            code.ReplaceGML(newGMLCode, gmData);
        }

        UndertaleRoom.Tile CreateRoomTile(int x, int y, int depth, UndertaleBackground tileset, uint sourceX, uint sourceY, uint width = 16, uint height = 16, uint? id = null)
        {
            id ??= gmData.GeneralInfo.LastTile++;
            return new UndertaleRoom.Tile()
            {
                X = x,
                Y = y,
                TileDepth = depth,
                BackgroundDefinition = tileset,
                SourceX = sourceX,
                SourceY = sourceY,
                InstanceID = id.Value,
                Width = width,
                Height = height
            };
        }

        UndertaleRoom.GameObject CreateRoomObject(int x, int y, UndertaleGameObject gameObject, UndertaleCode? creationCode = null, int scaleX = 1, int scaleY = 1, uint? id = null)
        {
            id ??= gmData.GeneralInfo.LastObj++;
            return new UndertaleRoom.GameObject()
            {
                X = x,
                Y = y,
                ObjectDefinition = gameObject,
                CreationCode = creationCode,
                ScaleX = scaleX,
                ScaleY = scaleY,
                InstanceID = id.Value
            };
        }

        // Import new Sprites
        Dictionary<string, int> nameToPageItemDict = new Dictionary<string, int>();
        const int pageDimension = 1024;
        int lastUsedX = 0, lastUsedY = 0, currentShelfHeight = 0;
        var newTexturePage = new Image<Rgba32>(pageDimension, pageDimension);
        var utTexturePage = new UndertaleEmbeddedTexture();
        utTexturePage.TextureHeight = utTexturePage.TextureWidth = pageDimension;
        gmData.EmbeddedTextures.Add(utTexturePage);

        void AddAllSpritesFromDir(string dirPath)
        {
            // Recursively add sprites from subdirs
            foreach (string subDir in Directory.GetDirectories(dirPath))
            {
                AddAllSpritesFromDir(subDir);
            }
            
            foreach (var filePath in Directory.GetFiles(dirPath))
            {
                var sprite = Image.Load(filePath);
                currentShelfHeight = Math.Max(currentShelfHeight, sprite.Height);
                if ((lastUsedX + sprite.Width) > pageDimension)
                {
                    lastUsedX = 0;
                    lastUsedY += currentShelfHeight;
                    currentShelfHeight = sprite.Height + 1; // One pixel padding

                    if (sprite.Width > pageDimension)
                        throw new NotSupportedException($"Currently a sprite ({filePath}) is bigger than the max size of a {pageDimension} texture page!");
                }

                if ((lastUsedY + sprite.Height) > pageDimension)
                    throw new NotSupportedException($"Currently all the sprites would be above a {pageDimension} texture page!");

                int xCoord = lastUsedX;
                int yCoord = lastUsedY;
                newTexturePage.Mutate(i => i.DrawImage(sprite, new Point(xCoord, yCoord), 1));
                var pageItem = new UndertaleTexturePageItem();
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
        using (var ms = new MemoryStream())
        {
            newTexturePage.Save(ms, PngFormat.Instance);
            utTexturePage.TextureData = new UndertaleEmbeddedTexture.TexData() { TextureBlob = ms.ToArray() };
        }

        // Replace A4 doors
        {
            var a4DoorTex = gmData.TexturePageItems[nameToPageItemDict["newA4Doors"]];
            var a4DoorImage = Image.Load(a4DoorTex.TexturePage.TextureData.TextureBlob);
            a4DoorImage.Mutate((i => i.Crop(new Rectangle(a4DoorTex.SourceX, a4DoorTex.SourceY, a4DoorTex.SourceWidth, a4DoorTex.SourceHeight))));
            var a4Tex = gmData.Backgrounds.ByName("tlArea4Tech").Texture;
            var a4PageImage = Image.Load(a4Tex.TexturePage.TextureData.TextureBlob);
            a4PageImage.Mutate(i => i.DrawImage(a4DoorImage, new Point(a4Tex.SourceX + 104, a4Tex.SourceY), 1));
            using (var ms = new MemoryStream())
            {
                a4PageImage.Save(ms, PngFormat.Instance);
                a4Tex.TexturePage.TextureData.TextureBlob = ms.ToArray();
            }
                
            var a4door2Tex = gmData.TexturePageItems[nameToPageItemDict["newA4Doors2"]];
            var a4Door2Image = Image.Load(a4door2Tex.TexturePage.TextureData.TextureBlob);
            a4Door2Image.Mutate((i => i.Crop(new Rectangle(a4door2Tex.SourceX, a4door2Tex.SourceY, a4door2Tex.SourceWidth, a4door2Tex.SourceHeight))));
            var a4Tex2 = gmData.Backgrounds.ByName("tlArea4Tech2").Texture;
            var a4Page2Image = Image.Load(a4Tex2.TexturePage.TextureData.TextureBlob);
            a4Page2Image.Mutate(i => i.DrawImage(a4Door2Image, new Point(a4Tex2.SourceX + 104, a4Tex2.SourceY), 1));
            using (var ms = new MemoryStream())
            {
                a4Page2Image.Save(ms, PngFormat.Instance);
                a4Tex2.TexturePage.TextureData.TextureBlob = ms.ToArray();
            }
        }

        gmData.Backgrounds.ByName("bg_MapBottom2").Texture = gmData.TexturePageItems[nameToPageItemDict["bg_MapBottom2"]];
        gmData.Backgrounds.ByName("bgGUIMetCountBG1").Texture = gmData.TexturePageItems[nameToPageItemDict["bgGUIMetCountBG2"]];
        gmData.Backgrounds.ByName("bgGUIMetCountBG2").Texture = gmData.TexturePageItems[nameToPageItemDict["bgGUIMetCountBG2"]];
        gmData.Backgrounds.ByName("bgGUIMetCountBG2ELM").Texture = gmData.TexturePageItems[nameToPageItemDict["bgGUIMetCountBG2ELM"]];
        gmData.Backgrounds.ByName("bgLogImg44B").Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogIce"]];
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA0"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA0"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA1"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA1"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA2"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA2"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA3"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA3"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA4"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA4"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA5"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA5"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("bgLogDNA6"), Texture = gmData.TexturePageItems[nameToPageItemDict["bgLogDNA6"]]});
            
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("tlWarpHideout"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpHideout"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("tlWarpDepthsEntrance"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpDepthsEntrance"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("tlWarpDepthsExit"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpDepthsExit"]]});
        gmData.Backgrounds.Add(new UndertaleBackground() {Name = gmData.Strings.MakeString("tlWarpWaterfall"), Texture = gmData.TexturePageItems[nameToPageItemDict["tlWarpWaterfall"]]});
            
            
        gmData.Sprites.ByName("sGUIMissile").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIMissile"]]});
        gmData.Sprites.ByName("sGUISMissile").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sGUISMissile"]]});
        gmData.Sprites.ByName("sGUIPBomb").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIPBomb"]]});
            
        // Replace existing door sprites
        gmData.Sprites.ByName("sDoorA5Locks").Textures[0].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorBlue"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[1].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorMissile"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[2].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSuper"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[3].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorPBomb"]];
        gmData.Sprites.ByName("sDoorA5Locks").Textures[4].Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTempLocked"]];
            
        // Add new sprites for doors
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorChargeBeam"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorWaveBeam"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSpazerBeam"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorPlasmaBeam"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorIceBeam"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorBomb"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSpider"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorScrew"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTowerEnabled"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTester"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorGuardian"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorArachnus"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorTorizo"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorSerris"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorGenesis"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorQueen"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPActivated"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPA1"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPA2"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorEMPA3"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearTotem"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPRobotHome"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearSave"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearBulletHell"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPNearPipeHub"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorA5EMPRightExterior"]]});
        gmData.Sprites.ByName("sDoorA5Locks").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorLocked"]]});
            
        // New sprites for door animation
        gmData.Sprites.ByName("sDoorA5").Textures.Clear();
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_1"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_2"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_3"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_4"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_5"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_6"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_7"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_8"]]});
        gmData.Sprites.ByName("sDoorA5").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sDoorAnim_9"]]});
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemShinyMissile"), Height = 16, Width = 16, 
            MarginLeft = 3, MarginRight = 12, MarginBottom = 12, MarginTop = 1, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyMissile_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyMissile_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyMissile_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyMissile_4"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            // TODO: sprite is offset by a bit
            Name = gmData.Strings.MakeString("sItemNothing"), Height = 16, Width = 16, MarginRight = 15, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemNothing_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemNothing_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemNothing_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemNothing_4"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemShinyNothing"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyNothing_8"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemShinyScrewAttack"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_9"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_10"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_11"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_12"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_13"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_14"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_15"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_16"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_17"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemScrewAttacker_18"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemShinyIceBeam"), Height = 16, Width = 16, 
            MarginLeft = 3, MarginRight = 12, MarginBottom = 12, MarginTop = 1, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyIceBeam_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyIceBeam_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyIceBeam_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyIceBeam_4"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemShinyHijump"), Height = 16, Width = 16, 
            MarginLeft = 3, MarginRight = 12, MarginBottom = 12, MarginTop = 1, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyHijump_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyHijump_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyHijump_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemShinyHijump_4"]] },
            }
        });
            
        gmData.Sprites.ByName("sItemPowergrip").Textures.Clear();
        gmData.Sprites.ByName("sItemPowergrip").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPowergrip_1"]]});
        gmData.Sprites.ByName("sItemPowergrip").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPowergrip_2"]]});
        gmData.Sprites.ByName("sItemPowergrip").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPowergrip_3"]]});
        gmData.Sprites.ByName("sItemPowergrip").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPowergrip_4"]]});
                                    
        gmData.Sprites.ByName("sItemMorphBall").Textures.Clear();
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_1"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_2"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_3"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_4"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_5"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_6"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_7"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_8"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_9"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_10"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_11"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_12"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_13"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_14"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_15"]]});
        gmData.Sprites.ByName("sItemMorphBall").Textures.Add(new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMorphBall_16"]]});

        gmData.Sprites.ByName("sMapSP").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sMapHint"]]});
        
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sMapBlockUnexplored"), Height = 8, Width = 8,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapBlockUnexplored"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sMapCornerUnexplored"), Height = 8, Width = 8,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[0].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[1].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[2].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[3].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[4].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[5].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[6].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[7].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[8].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[9].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[10].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[11].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[12].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[13].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[14].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[15].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.Sprites.ByName("sMapCorner").Textures[16].Texture },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_0"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_0"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_0"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_1"]] },
                    
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_5"]] },
                    
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_9"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_9"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sMapCornerUnexplored_9"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemMissileLauncher"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_9"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_10"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_11"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemMissileLauncher_12"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemSMissileLauncher"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_9"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_10"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_11"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemSMissileLauncher_12"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemPBombLauncher"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_9"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_10"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_11"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemPBombLauncher_12"]] },
            }
        });
            
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sItemDNA"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_4"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_5"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_6"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_7"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_8"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemDNA_9"]] },
            }
        });
            
        // New sprites for dna septogg
        gmData.Sprites.Add(new UndertaleSprite()
        {
            Name = gmData.Strings.MakeString("sWisdomSeptogg"), Height = 35, Width = 47, MarginLeft = 14, MarginRight = 32, MarginBottom = 11, MarginTop = 6, OriginX = 23, OriginY = 35,
            Textures =
            {
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sWisdomSeptogg_1"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sWisdomSeptogg_2"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sWisdomSeptogg_3"]] },
                new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sWisdomSeptogg_4"]] },
            }
        });
        
        void RotateTextureAndSaveToTexturePage(int rotation, UndertaleTexturePageItem texture)
        {
            var texturePage = Image.Load(texture.TexturePage.TextureData.TextureBlob);
            texturePage.Mutate(im => im.Hue(rotation, new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight)));

            using var ms = new MemoryStream();
            texturePage.Save(ms, PngFormat.Instance);
            texture.TexturePage.TextureData.TextureBlob = ms.ToArray();
        }
        
        // Hue shift etanks
        if (seedObject.Cosmetics.EtankHUDRotation != 0)
        {
            foreach (var textureEntry in gmData.Sprites.ByName("sGUIETank").Textures)
            {
                RotateTextureAndSaveToTexturePage(seedObject.Cosmetics.EtankHUDRotation, textureEntry.Texture);
            }
        }

        // Hue shift health numbers
        if (seedObject.Cosmetics.HealthHUDRotation != 0)
        {
            foreach (var textureEntry in gmData.Sprites.ByName("sGUIFont1").Textures.Concat(gmData.Sprites.ByName("sGUIFont1A").Textures))
            {
                RotateTextureAndSaveToTexturePage(seedObject.Cosmetics.HealthHUDRotation, textureEntry.Texture);
            }
        }

        // Hue shift dna icon
        if (seedObject.Cosmetics.DNAHUDRotation != 0)
        {
            foreach (var bg in new List<UndertaleBackground>()
                         { gmData.Backgrounds.ByName("bgGUIMetCountBG1"), gmData.Backgrounds.ByName("bgGUIMetCountBG2ELM") })
            {
                RotateTextureAndSaveToTexturePage(seedObject.Cosmetics.DNAHUDRotation, bg.Texture);
            }
        }
        
        // Sabre's new skippy design for Skippy the Bot
        if (seedObject.Patches.SabreSkippy)
        {
            foreach (var spriteName in new[] {"sAutoadP", "sAutoadPFang", "sAutoadPClaw"})
            {
                var sprite = gmData.Sprites.ByName(spriteName);
                sprite.Textures[0].Texture = gmData.TexturePageItems[nameToPageItemDict[spriteName]];
            }
        }


        // Create new wisdom septogg object
        var oWisdomSeptogg = new UndertaleGameObject()
        {
            Name = gmData.Strings.MakeString("oWisdomSeptogg"),
            Sprite = gmData.Sprites.ByName("sWisdomSeptogg"),
            Depth = 90
        };
        var wisdomSeptoggCreate = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oWisdomSeptogg_Create_0") };
        SubstituteGMLCode(wisdomSeptoggCreate, "image_speed = 0.1666; origY = y; timer = 0;");
        gmData.Code.Add(wisdomSeptoggCreate);
        var wisdomSeptoggCreateList = oWisdomSeptogg.Events[0];
        var wisdomSeptoggAction = new UndertaleGameObject.EventAction();
        wisdomSeptoggAction.CodeId = wisdomSeptoggCreate;
        var wisdomSeptoggEvent = new UndertaleGameObject.Event();
        wisdomSeptoggEvent.EventSubtype = 0;
        wisdomSeptoggEvent.Actions.Add(wisdomSeptoggAction);
        wisdomSeptoggCreateList.Add(wisdomSeptoggEvent);
        var wisdomSeptoggStep = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oWisdomSeptogg_Step_0") };
        SubstituteGMLCode(wisdomSeptoggStep, "y = origY + (sin((timer) * 0.08) * 2); timer++; if (timer > 9990) timer = 0;");
        gmData.Code.Add(wisdomSeptoggStep);
        var wisdomSeptoggStepList = oWisdomSeptogg.Events[3];
        wisdomSeptoggAction = new UndertaleGameObject.EventAction();
        wisdomSeptoggAction.CodeId = wisdomSeptoggStep;
        wisdomSeptoggEvent = new UndertaleGameObject.Event();
        wisdomSeptoggEvent.EventSubtype = 0;
        wisdomSeptoggEvent.Actions.Add(wisdomSeptoggAction);
        wisdomSeptoggStepList.Add(wisdomSeptoggEvent);
        gmData.GameObjects.Add(oWisdomSeptogg);
            
        var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
            
        // Fix power grip sprite
        gmData.Sprites.ByName("sItemPowergrip").OriginX = 0;
        gmData.Sprites.ByName("sItemPowergrip").OriginY = 16;
            
        // Remove other game modes, rename "normal" to "Randovania"
        var gameSelMenuStepCode = gmData.Code.ByName("gml_Object_oGameSelMenu_Step_0");
        ReplaceGMLInCode(gameSelMenuStepCode, "if (global.mod_gamebeaten == 1)", "if (false)");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oSlotMenu_normal_only_Create_0"), 
            "d0str = get_text(\"Title-Additions\", \"GameSlot_NewGame_NormalGame\")", "d0str = \"Randovania\";");

        // Unlock fusion etc. by default
        var unlockStuffCode = gmData.Code.ByName("gml_Object_oControl_Other_2");
        AppendGMLInCode(unlockStuffCode, "global.mod_fusion_unlocked = 1; global.mod_gamebeaten = 1;");
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oSS_Fg_Create_0"), "itemcollunlock = 1;");

        // Make fusion only a damage multiplier, leaving the fusion stuff up to a setting
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oControl_Step_0"), "mod_fusion = 0;");
            
        // Fix varia cutscene
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oSuitChangeFX_Step_0"), "bg1alpha = 0", "bg1alpha = 0; instance_create(x, y, oSuitChangeFX2);");
            
        // Make beams not instantly despawn when out of screen
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oBeam_Step_0"), 
            "if (x < ((view_xview[0] - 48) - (oControl.widescreen_space / 2)) || x > (((view_xview[0] + view_wview[0]) + 48) + (oControl.widescreen_space / 2)) || y < (view_yview[0] - 48) || y > ((view_yview[0] + view_hview[0]) + 48))",
            "if (x > (room_width + 80) || x < -80 || y > (room_height + 80) || y < -160)");
                
        // Fix arachnus event value doing x coordinate BS
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oArachnus_Alarm_11"), "global.event[103] = x", "global.event[103] = 1");
        // Make arachnus item location always spawn in center
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a2a04_Create"), "instance_create(global.event[103]", "instance_create(room_width / 2");
        
        //No more Out of Bounds oSmallsplash crashes
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oSmallSplash_Step_0"), "if (global.watertype == 0)", "if (global.watertype == 0 && instance_exists(oWater))");

        // Killing queen should not lock you out of the rest of the game
        AppendGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a0h01_3762_Create"), "instance_destroy()");
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a0h01_Create"), "tile_layer_delete(-119)");
        
        // For pause menu, draw now the same as equipment menu because doing determining what max total health/missiles/etc. are would be spoilery and insane to figure out
        var ssDraw = gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0");
        ReplaceGMLInCode(ssDraw, "(string(global.etanks) + \"/10\")", "( string(ceil(global.playerhealth)) + \"/\" + string(global.maxhealth) )");
        ReplaceGMLInCode(ssDraw, "(string(global.mtanks) + \"/44\")", "( string(global.missiles) + \"/\" + string(global.maxmissiles) )");
        ReplaceGMLInCode(ssDraw, "(string(global.stanks) + \"/10\")", "( string(global.smissiles) + \"/\" + string(global.maxsmissiles) )");
        ReplaceGMLInCode(ssDraw, " (string(global.ptanks) + \"/10\")", "( string(global.pbombs) + \"/\" + string(global.maxpbombs) )");
        foreach (var code in new[] {ssDraw.Name.Content, "gml_Script_scr_SubScrTop_swap", "gml_Script_scr_SubScrTop_swap2"})
        {
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.stanks > 0", "true");
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.ptanks > 0", "true");
        }
            
        // Make doors automatically free their event when passing through them!...
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Alarm_0"), "event_user(2)", 
            "{ event_user(2); if(event > 0 && lock < 5) global.event[event] = 1; }");
        // ...But don't make them automatically opened for non-ammo doors!
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Alarm_0"), "lock = 0", "if (lock < 4) lock = 0;");
            
        // Make doors when unlocked, go to the type they were before except for ammo doors
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Create_0"), "originalLock = lock;");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Other_13"), "lock = 0", "lock = originalLock; if (originalLock < 4) lock = 0");
            
        // Fix doors unlocking in arachnus/torizo/tester/genesis
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a2a04_Create"), "if (!global.event[103]) {with (oDoor) lock = 4;}");
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a3a01_Create"), "if (!global.event[152]) {with (oDoor) lock = 4;}");
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a4a05_Create"), "if (!global.event[207]) {with (oDoor) lock = 4;}");
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a8a11_Create"), "if (!global.event[307]) {with (oDoor) lock = 4;}");
            
        // Fix doors in tester to be always blue
        foreach (var codeName in new[] {"gml_RoomCC_rm_a4a05_6510_Create", "gml_RoomCC_rm_a4a05_6511_Create"})
            SubstituteGMLCode(gmData.Code.ByName(codeName), "lock = 0;");
            
        // Fix Tower activation unlocking right door for door lock rando
        if (seedObject.DoorLocks.ContainsKey(127890))
            ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oArea4PowerSwitch_Step_0"), "lock = 0", "lock = lock;");
            
        // Fix tester being fought in darkness / proboscums being disabled on not activated tower
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oTesterBossTrigger_Other_10"), 
            "global.darkness = 0; with (oLightEngine) instance_destroy(); with (oFlashlight64); instance_destroy()");
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oProboscum_Create_0"), "active = true; image_index = 0;");

        // Fix tester events sharing an event with tower activated - moved tester to 207
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a4a04_6496_Create"), "global.event[200] < 2", "!global.event[207]");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oTesterBossTrigger_Create_0"), "global.event[200] != 1", "global.event[207]");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oTester_Step_0"), "global.event[200] = 2", "global.event[207] = 1;");
            
        // Fix doors in labs, by making them always blue, and the metroid listener lock/unlock them
        foreach (var codeName in new[] {"gml_RoomCC_rm_a7b05_9400_Create", "gml_RoomCC_rm_a7b06_9413_Create", "gml_RoomCC_rm_a7b06_9414_Create", 
                     "gml_RoomCC_rm_a7b06A_9421_Create", "gml_RoomCC_rm_a7b06A_9420_Create", "gml_RoomCC_rm_a7b07_9437_Create", "gml_RoomCC_rm_a7b07_9438_Create",
                     "gml_RoomCC_rm_a7b08_9455_Create", "gml_RoomCC_rm_a7b08_9454_Create", "gml_RoomCC_rm_a7b08A_9467_Create", "gml_RoomCC_rm_a7b08A_9470_Create"
                 })
            SubstituteGMLCode(gmData.Code.ByName(codeName), "");
        SubstituteGMLCode(gmData.Code.ByName("gml_Object_oMonsterDoorControl_Alarm_0"), "if (instance_number(oMonster) > 0) { with (oDoor) lock = 4 }");
            
        // Have option for missile doors to not open by supers
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Collision_438"), "lock == 1", "((lock == 1 && !other.smissile) || (lock == 1 && other.smissile && global.canUseSupersOnMissileDoors))");
        
        // Implement new beam doors (charge = 5, wave = 6, spazer = 7, plasma = 8, ice = 9)
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Collision_439"), "lock == 0", "(lock == 0) || (lock == 5 && other.chargebeam) ||" +
                                                                                            "(lock == 6 && other.wbeam) || (lock == 7 && other.sbeam) || " +
                                                                                            "(lock == 7 && other.sbeam) || (lock == 8 && other.pbeam) || " +
                                                                                            "(lock == 9 && other.ibeam)");
            
            
        // Implement other weapon doors (bomb = 10, spider = 11, screw = 12)
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oDoor_Collision_435"), "lock == 0", "(lock == 0 || lock == 10 )");
        var doorSamusCollision = new UndertaleCode();
        doorSamusCollision.Name = gmData.Strings.MakeString($"gml_Object_oDoor_Collision_267");
        SubstituteGMLCode(doorSamusCollision, "if (!open && ((lock == 11 && other.state == other.SPIDERBALL) || " +
                                              "(lock == 12 && global.screwattack && other.state == other.JUMPING && !other.vjump && !other.walljumping && (!other.inwater || global.currentsuit >= 2))))" +
                                              "event_user(1)");
        gmData.Code.Add(doorSamusCollision);
        var doorCollisionList = gmData.GameObjects.ByName("oDoor").Events[4];
        var varDoorAction = new UndertaleGameObject.EventAction();
        varDoorAction.CodeId = doorSamusCollision;
        var varDoorEvent = new UndertaleGameObject.Event();
        varDoorEvent.EventSubtype = 267; // 267 is oCharacter ID
        varDoorEvent.Actions.Add(varDoorAction);
        doorCollisionList.Add(varDoorEvent);
            
        // Implement tower activated (13), tester dead doors (14), guardian doors (15), arachnus (16), torizo (17), serris (18), genesis (19), queen (20)
        // Also implement emp events - emp active (21), emp a1 (22), emp a2 (23), emp a3 (24), emp tutorial (25), emp robot home (26), emp near zeta (27),
        // emp near bullet hell (28), emp near pipe hub (29), emp near right exterior (30).
        // perma locked is doesnt have a number and is never set here as being openable.
        var newDoorReplacementText = "(lock == 0) || (global.event[200] && lock == 13)" +
                                     "|| (global.event[207] && lock == 14) || (global.event[51] && lock == 15)" +
                                     "|| (global.event[103] && lock == 16) || (global.event[152] && lock == 17)" +
                                     "|| (global.event[261] && lock == 18) || (global.event[307] && lock == 19)" +
                                     "|| (global.event[303] && lock == 20) || (global.event[250] && lock == 21)" +
                                     "|| (global.event[57] && lock == 22) || (global.event[110] && lock == 23)" +
                                     "|| (global.event[163] && lock == 24) || (global.event[251] && lock == 25) || (global.event[252] && lock == 26) || (global.event[253] && lock == 27)" +
                                     "|| (global.event[256] && lock == 28) || (global.event[254] && lock == 29) || (global.event[262] && lock == 30)";
        // beams, missile explosion, pbomb explosion, bomb explosion
        foreach (var codeName in new[] {"gml_Object_oDoor_Collision_439", "gml_Object_oDoor_Collision_438", "gml_Object_oDoor_Collision_437", "gml_Object_oDoor_Collision_435"})
            ReplaceGMLInCode(gmData.Code.ByName(codeName), "lock == 0", newDoorReplacementText);    
            

        // Fix Emp devices unlocking all doors automatically!
        string empBatteryCellCondition = "false";
        foreach (var doorID in new uint[] {108539, 111778, 115149, 133836, 133903, 133914, 133911, 134711, 134426, 135330})
        {
            if (!seedObject.DoorLocks.ContainsKey(doorID))
                empBatteryCellCondition += $" || id == {doorID}";
        }
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oBatterySlot_Alarm_0"), """
            with (oDoor)
                event_user(3)
            """,
            $"with (oDoor) {{ if ({empBatteryCellCondition}) event_user(3) }}");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oBatterySlot_Alarm_1"), """
                with (oDoor)
                    lock = 0
            """,
            $"with (oDoor) {{ if ({empBatteryCellCondition}) lock = 0 }}");

        string a5ActivateCondition = "false";
        foreach (var doorID in new uint[] {133732, 133731})
        {
            if (!seedObject.DoorLocks.ContainsKey(doorID))
                a5ActivateCondition += $" || id == {doorID}";
        }
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oA5MainSwitch_Step_0"), """
                    with (oDoor)
                        event_user(3)
            """,
            $"with (oDoor) {{ if ({a5ActivateCondition}) event_user(3) }}");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oA5MainSwitch_Alarm_0"), """
                with (oDoor)
                    lock = 0
            """,
            $"with (oDoor) {{ if ({a5ActivateCondition}) lock = 0 }}");
            
                
        //Destroy turbines and set the event to fully complete if entering "Water Turbine Station" at bottom doors
        PrependGMLInCode(gmData.Code.ByName("gml_Room_rm_a2a08_Create"), "if (global.targety > 240) { with (oA2SmallTurbine) instance_destroy(); global.event[101] = 4; }");
        //Remove setting of turbine event from adjacent rooms
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a2a09_Create"), "global.event[101] = 4", "");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a2a19_Create"), "global.event[101] = 4", "");
            
        // Fix plasma chamber having a missile door instead of normal after tester dead
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a4a09_6582_Create"), "lock = 1", "lock = 0;");
        
        // Fix lab log not displaying progress bar
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a7b04A_Create"), "create_log_trigger(0, 44, 440, 111, 0, 0)", "create_log_trigger(0, 44, 438, 111, -60, 1)");
            
        // Fix skreek street not actually having skreeks
        PrependGMLInCode(gmData.Code.ByName("gml_Script_scr_skreeks_destroy"), "exit");
            
        // Rename "fusion" difficulty to brutal, in order to be less confusing
        foreach (var codeName in new[] { "gml_Object_oMenuSaveSlot_Other_10", "gml_Object_oSlotMenu_Fusion_Create_0" })
            ReplaceGMLInCode(gmData.Code.ByName(codeName), @"get_text(""Title-Additions"", ""GameSlot_NewGame_Fusion"")", "\"Brutal\"");
        // Implement a fix, where every save shows "Brutal" as the difficulty when global.mod_fusion is enabled
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oGameSelMenu_Other_12"), "if (oControl.mod_fusion == 1)", "if (oControl.mod_diffmult == 4)");
            
        // Make the popup text display during the pause for item acquisitions for less awkwardness
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oItemCutscene_Create_0"), "sfx_play(sndMessage)",
            "popup_text(global.itmtext1); sfx_play(sndMessage);");
        
        // Fixes character step event for further modification
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_characterStepEvent"), """
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
        var thothLeftDoorCC = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_thothLeftDoor_Create")};
        var thothRightDoorCC = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_thothRightDoor_Create") };
        gmData.Code.Add(thothLeftDoorCC);
        gmData.Code.Add(thothRightDoorCC);
        gmData.Rooms.ByName("rm_a8a03").GameObjects.Add(new UndertaleRoom.GameObject()
        {
            X = 24,
            Y = 96,
            ObjectDefinition = gmData.GameObjects.ByName("oDoorA8"),
            InstanceID = ThothBridgeLeftDoorID,
            ScaleY = 1,
            ScaleX = 1,
            CreationCode = thothLeftDoorCC
        });
        gmData.Rooms.ByName("rm_a8a03").GameObjects.Add(new UndertaleRoom.GameObject()
        {
            X = 616,
            Y = 96,
            ObjectDefinition = gmData.GameObjects.ByName("oDoorA8"),
            InstanceID = ThothBridgeRightDoorID,
            ScaleX = -1,
            ScaleY = 1,
            CreationCode = thothRightDoorCC
        });
            
        // Add door from water turbine station to hydro station exterior
        var waterTurbineDoorCC = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_waterStationDoor_Create") };
        gmData.Code.Add(waterTurbineDoorCC);
        var rm_a2a08 = gmData.Rooms.ByName("rm_a2a08");
        rm_a2a08.GameObjects.Add(new UndertaleRoom.GameObject()
        {
            X = 24,
            Y = 96,
            ObjectDefinition = gmData.GameObjects.ByName("oDoor"),
            InstanceID = A2WaterTurbineLeftDoorID,
            ScaleX = 1,
            ScaleY = 1,
            CreationCode = waterTurbineDoorCC
        });
            
        var tempTile = rm_a2a08.Tiles.First(t => t.InstanceID == 10040174);
        tempTile.X = 16;
        var doorTileset = gmData.Backgrounds.ByName("tlDoor");
        tempTile.BackgroundDefinition = doorTileset;
        tempTile.SourceX = 112;
        tempTile.SourceY = 64;
            
        tempTile = rm_a2a08.Tiles.First(t => t.InstanceID == 10040175);
        tempTile.X = 16;
        tempTile.BackgroundDefinition = doorTileset;
        tempTile.SourceX = 112;
        tempTile.SourceY = 32;
            
        tempTile = rm_a2a08.Tiles.First(t => t.InstanceID == 10040176);
        tempTile.X = 16;
        tempTile.BackgroundDefinition = doorTileset;
        tempTile.SourceX = 112;
        tempTile.SourceY = 16;
            
        tempTile = rm_a2a08.Tiles.First(t => t.InstanceID == 10040177);
        tempTile.X = 16;
        tempTile.BackgroundDefinition = doorTileset;
        tempTile.SourceX = 112;
        tempTile.SourceY = 0;

        tempTile = new UndertaleRoom.Tile()
        {
            X = 0,
            Y = 144,
            BackgroundDefinition = doorTileset,
            SourceX = 96,
            SourceY = 64,
            Width = 16,
            Height = 16,
            InstanceID = gmData.GeneralInfo.LastTile++
        };
        rm_a2a08.Tiles.Add(tempTile);
            
        tempTile = new UndertaleRoom.Tile()
        {
            X = 0,
            Y = 128,
            BackgroundDefinition = doorTileset,
            SourceX = 96,
            SourceY = 32,
            Width = 16,
            Height = 16,
            InstanceID = gmData.GeneralInfo.LastTile++
        };
        rm_a2a08.Tiles.Add(tempTile);
            
        tempTile = new UndertaleRoom.Tile()
        {
            X = 0,
            Y = 112,
            BackgroundDefinition = doorTileset,
            SourceX = 96,
            SourceY = 16,
            Width = 16,
            Height = 16,
            InstanceID = gmData.GeneralInfo.LastTile++
        };
        rm_a2a08.Tiles.Add(tempTile);
            
        tempTile = new UndertaleRoom.Tile()
        {
            X = 0,
            Y = 96,
            BackgroundDefinition = doorTileset,
            SourceX = 96,
            SourceY = 0,
            Width = 16,
            Height = 16,
            InstanceID = gmData.GeneralInfo.LastTile++
        };
        rm_a2a08.Tiles.Add(tempTile);
            
        // Implement dna item
        var enemyObject = gmData.GameObjects.ByName("oItem");
        for (int i = 350; i <= 395; i++)
        {
            var go = new UndertaleGameObject();
            go.Name = gmData.Strings.MakeString("oItemDNA_" + i);
            go.ParentId = enemyObject;
            // Add create event
            var create = new UndertaleCode();
            create.Name = gmData.Strings.MakeString($"gml_Object_oItemDNA_{i}_Create_0");
            SubstituteGMLCode(create, "event_inherited(); itemid = " + i + ";");
            gmData.Code.Add(create);
            var createEventList = go.Events[0];
            var action = new UndertaleGameObject.EventAction();
            action.CodeId = create;
            var gEvent = new UndertaleGameObject.Event();
            gEvent.Actions.Add(action);
            createEventList.Add(gEvent);
                
            var collision = new UndertaleCode();
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
        ReplaceGMLInCode(characterVarsCode, """
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
        ReplaceGMLInCode( gmData.Code.ByName("gml_Script_sv6_add_items"), "350", "400");
        ReplaceGMLInCode( gmData.Code.ByName("gml_Script_sv6_get_items"), "350", "400");
            
        // Metroid ID to DNA map
        var scrDNASpawn = new UndertaleCode();
        scrDNASpawn.Name = gmData.Strings.MakeString("gml_Script_scr_DNASpawn");
        SubstituteGMLCode(scrDNASpawn, """
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
        gmData.Scripts.Add(new UndertaleScript() {Name = gmData.Strings.MakeString("scr_DNASpawn"), Code = scrDNASpawn});
            
        // Make DNA count show on map
        ReplaceGMLInCode(ssDraw, "draw_text((view_xview[0] + 18), ((view_yview[0] + 198) + rectoffset), timetext)", 
            "draw_text((view_xview[0] + 18), ((view_yview[0] + 198) + rectoffset), timetext); draw_text((view_xview[0] + 158), ((view_yview[0] + 198) + rectoffset), string(global.dna) + \"/46\")");
        ReplaceGMLInCode(ssDraw, "draw_text((view_xview[0] + 17), ((view_yview[0] + 197) + rectoffset), timetext)", 
            "draw_text((view_xview[0] + 17), ((view_yview[0] + 197) + rectoffset), timetext); draw_text((view_xview[0] + 157), ((view_yview[0] + 197) + rectoffset), string(global.dna) + \"/46\")");
            
        // Fix item percentage now that more items have been added
        foreach (var name in new[] {"gml_Object_oGameSelMenu_Other_12", "gml_Object_oSS_Fg_Draw_0", "gml_Object_oScoreScreen_Create_0", "gml_Object_oScoreScreen_Other_10", "gml_Object_oIGT_Step_0", })
            ReplaceGMLInCode(gmData.Code.ByName(name), "/ 88", "/ 134");
            
        // Make Charge Beam always hit metroids
        foreach (string name in new[] { "gml_Object_oMAlpha_Collision_439", "gml_Object_oMGamma_Collision_439", "gml_Object_oMZeta_Collision_439", "gml_Object_oMZetaBodyMask_Collision_439", "gml_Object_oMOmegaMask2_Collision_439", "gml_Object_oMOmegaMask3_Collision_439"})
            ReplaceGMLInCode(gmData.Code.ByName(name), "&& global.missiles == 0 && global.smissiles == 0", "");
            
        // Replace Metroids counters with DNA counters
        var drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
        ReplaceGMLInCode(drawGuiCode, "global.monstersleft", "global.dna");
        ReplaceGMLInCode(drawGuiCode, "global.monstersarea", "46 - global.dna");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_14"), "get_text(\"OptionsDisplay\", \"MonsterCounter\")", "\"DNA Counter\"");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_10"), "get_text(\"OptionsDisplay\", \"MonsterCounter\")", "\"DNA Counter\"");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_13"), "get_text(\"OptionsDisplay\", \"MonsterCounter_Tip\")", 
            "\"Switches the type of the HUD DNA Counter\"");
        var optionsDisplayUser2 = gmData.Code.ByName("gml_Object_oOptionsDisplay_Other_12");
        ReplaceGMLInCode(optionsDisplayUser2, "get_text(\"OptionsDisplay\", \"MonsterCounter_Local\")", "\"Until Labs\"");
        ReplaceGMLInCode(optionsDisplayUser2, "get_text(\"OptionsDisplay\", \"MonsterCounter_Global\")", "\"Current\"");
        ReplaceGMLInCode(optionsDisplayUser2, "get_text(\"OptionsDisplay\", \"MonsterCounter_Disabled_Tip\")", "\"Don't show the DNA Counter\"");
        ReplaceGMLInCode(optionsDisplayUser2, "get_text(\"OptionsDisplay\", \"MonsterCounter_Local_Tip\")", "\"Show the remaining DNA until you can access the Genetics Laboratory\"");
        ReplaceGMLInCode(optionsDisplayUser2, "get_text(\"OptionsDisplay\", \"MonsterCounter_Global_Tip\")", "\"Show the currently collected DNA\"");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oGameSelMenu_Other_12"), "global.monstersleft", "global.dna");
            
        // Add shortcut between nest and hideout
        if (seedObject.Patches.NestPipes)
        {
            // Hideout
            var hideoutPipeRoom = gmData.Rooms.ByName("rm_a6a11");
            var hideoutPipeTileset = gmData.Backgrounds.ByName("tlWarpDepthsEntrance");
            var depthsEntrancePipeTileset = gmData.Backgrounds.ByName("tlWarpHideout");
            var depthsExitPipeTileset = gmData.Backgrounds.ByName("tlWarpWaterfall");
            var waterfallsPipeTileset = gmData.Backgrounds.ByName("tlWarpDepthsExit");
            var pipeBGTileset = gmData.Backgrounds.ByName("tlWarpPipes");
            var solidObject = gmData.GameObjects.ByName("oSolid1");
            var pipeObject = gmData.GameObjects.ByName("oWarpPipeTrigger");
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

            var hideoutPipeCode = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a6a11_pipe_Create") };
            SubstituteGMLCode(hideoutPipeCode, "targetroom = 327; targetx = 216; targety = 400; direction = 90;");
            gmData.Code.Add(hideoutPipeCode);
            hideoutPipeRoom.GameObjects.Add(CreateRoomObject(368, 192, pipeObject, hideoutPipeCode, 1, 1, PipeInHideoutID));
            AppendGMLInCode(hideoutPipeRoom.CreationCodeId, "global.darkness = 0; mus_change(mus_get_main_song());");

            // Nest
            var nestPipeRoom = gmData.Rooms.ByName("rm_a6b03");
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

            AppendGMLInCode(nestPipeRoom.CreationCodeId, "mus_change(musArea6A)");

            var nestPipeCode = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a6b03_pipe_Create") };
            SubstituteGMLCode(nestPipeCode, "targetroom = 317; targetx = 376; targety = 208; direction = 270;");
            gmData.Code.Add(nestPipeCode);
            nestPipeRoom.GameObjects.Add(CreateRoomObject(208, 384, pipeObject, nestPipeCode, 1, 1, PipeInDepthsLowerID));
                
            // Change slope to solid to prevent oob issue
            nestPipeRoom.GameObjects.First(o => o.X == 176 && o.Y == 416).ObjectDefinition = solidObject;

            // Add shortcut between Depths and Waterfalls
            // Depths
            var depthsPipeRoom = gmData.Rooms.ByName("rm_a6b11");
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

            AppendGMLInCode(depthsPipeRoom.CreationCodeId, "mus_change(musArea6A);");

            var depthsPipeCode = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a6b11_pipe_Create") };
            SubstituteGMLCode(depthsPipeCode, "targetroom = 348; targetx = 904; targety = 208; direction = 180;");
            gmData.Code.Add(depthsPipeCode);
            depthsPipeRoom.GameObjects.Add(CreateRoomObject(96, 176, pipeObject, depthsPipeCode, 1, 1, PipeInDepthsUpperID));

            // Waterfalls
            var waterfallsPipeRoom = gmData.Rooms.ByName("rm_a7a07");
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

            var waterfallsPipeCode = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_rm_a7a07_pipe_Create") };
            SubstituteGMLCode(waterfallsPipeCode, "targetroom = 335; targetx = 104; targety = 192; direction = 0;");
            gmData.Code.Add(waterfallsPipeCode);
            waterfallsPipeRoom.GameObjects.Add(CreateRoomObject(896, 192, pipeObject, waterfallsPipeCode, 1, 1, PipeInWaterfallsID));

            AppendGMLInCode(waterfallsPipeRoom.CreationCodeId, "global.darkness = 0");
                
            // Modify minimap for new pipes and purple in nest and waterfalls too
            // Hideout
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_04"), @"global.map[21, 53] = ""1210100""", @"global.map[21, 53] = ""12104U0""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_03"), @"global.map[20, 53] = ""1012100""", @"global.map[20, 53] = ""1012400""");
            // Depths lower
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_04"), "global.map[21, 44] = \"1102100\"\nglobal.map[21, 45] = \"0112100\"", "global.map[21, 44] = \"1102400\"\nglobal.map[21, 45] = \"01124D0\"");
            // Depths upper
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_02"), @"global.map[16, 34] = ""1012100""", @"global.map[16, 34] = ""10124L0""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_03"), @"global.map[17, 34] = ""1010100""", @"global.map[17, 34] = ""1010400""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_03"), @"global.map[18, 34] = ""1020100""", @"global.map[18, 34] = ""1020400""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_03"), @"global.map[19, 34] = ""1010100""", @"global.map[19, 34] = ""1010400""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_03"), @"global.map[20, 34] = ""1210100""", @"global.map[20, 34] = ""1210400""");
            // Waterfalls
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_01"), @"global.map[7, 34] = ""1012200""", @"global.map[7, 34] = ""1012400""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_17"), @"global.map[8, 34] = ""1010200""", @"global.map[8, 34] = ""1010400""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_01"), @"global.map[9, 34] = ""1010200""", @"global.map[9, 34] = ""10104R0""");
            ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_02"), @"global.map[10, 34] = ""1210200""", @"global.map[10, 34] = ""1210400""");
        }

        // Make metroids drop an item onto you on death and increase music timer to not cause issues
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oMAlpha_Other_10"), "check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; } with (oMusicV2) { if (alarm[1] >= 0) alarm[1] = 120; }");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oMGamma_Other_10"), "check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; } with (oMusicV2) { if (alarm[2] >= 0) alarm[2] = 120; }");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oMZeta_Other_10"), "check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; } with (oMusicV2) { if (alarm[3] >= 0) alarm[3] = 120; }");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oMOmega_Other_10"), "check_areaclear()",
            "check_areaclear(); with (instance_create(oCharacter.x, oCharacter.y, scr_DNASpawn(myid))) { active = 1; itemtype = 1; } with (oMusicV2) { if (alarm[4] >= 0) alarm[4] = 120; }");

        // Make new global.lavastate 11 that requires 46 dna to be collected
        SubstituteGMLCode(gmData.Code.ByName("gml_Script_check_areaclear"), "if (global.lavastate == 11) { if (global.dna >= 46) { instance_create(0, 0, oBigQuake); global.lavastate = 12; } }");
            
        // Check lavastate at labs
        var labsRoom = gmData.Rooms.ByName("rm_a7b04A");
        var labBlock = new UndertaleRoom.GameObject();
        labBlock.X = 64;
        labBlock.Y = 96;
        labBlock.ScaleX = 2;
        labBlock.ScaleY = 4;
        labBlock.InstanceID = gmData.GeneralInfo.LastObj++;
        labBlock.ObjectDefinition = gmData.GameObjects.ByName("oSolid1");
        var labBlockCode = new UndertaleCode();
        labBlockCode.Name = gmData.Strings.MakeString("gml_RoomCC_rm_a7b04A_labBlock_Create");
        SubstituteGMLCode(labBlockCode, "if (global.lavastate > 11) {  tile_layer_delete(-99); instance_destroy(); }");
        gmData.Code.Add(labBlockCode);
        labBlock.CreationCode = labBlockCode;
        labsRoom.GameObjects.Add(labBlock);
        labsRoom.Tiles.Add(new UndertaleRoom.Tile()
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
        labsRoom.Tiles.Add(new UndertaleRoom.Tile()
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
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a6a09_8945_Create"), "if (global.lavastate > 8)", "y = 320; if (false)");

        // Lock these blocks behind a setting because they can make for some interesting changes
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a0h07_Create"), 
            "if (oControl.mod_purerandombool == 1 || oControl.mod_splitrandom == 1 || global.gamemode == 2)", 
            $"if ({(!seedObject.Patches.GraveGrottoBlocks).ToString().ToLower()})");

        // enable randomizer to be always on
        var newGameCode = gmData.Code.ByName("gml_Script_scr_newgame");
        ReplaceGMLInCode(newGameCode,"oControl.mod_randomgamebool = 0", "oControl.mod_randomgamebool = 1");
            
        // Fix local metroids
        ReplaceGMLInCode(newGameCode, "global.monstersleft = 47", "global.monstersleft = 47; global.monstersarea = 44");
            
        // Fix larvas dropping either missiles or supers instead of what's needed
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oMonster_Other_10"), "pickup == 1", "true");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oMonster_Other_10"), "pickup == 0", "true");

        // Make it in oItem, that itemtype one's automatically spawn a popup
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oItem_Other_10"), "global.itemtype = itemtype", 
            "if (itemtype == 1) {popup_text(text1);} global.itemtype = itemtype");
            
        // Add main (super) missile / PB launcher
        // missileLauncher, SMissileLauncher, PBombLauncher
        // also add an item for them + amount of expansions they give
            
        PrependGMLInCode(characterVarsCode, "global.missileLauncher = 0; global.SMissileLauncher = 0; global.PBombLauncher = 0;" +
                                            "global.missileLauncherExpansion = 30; global.SMissileLauncherExpansion = 2; global.PBombLauncherExpansion = 2;");
            
            
            
        // Make expansion set to default values
        ReplaceGMLInCode(characterVarsCode, "global.missiles = oControl.mod_Mstartingcount", "global.missiles = 0;");
        ReplaceGMLInCode(characterVarsCode, "global.maxmissiles = oControl.mod_Mstartingcount", "global.maxmissiles = global.missiles;");
        ReplaceGMLInCode(characterVarsCode, "global.smissiles = 0", "global.smissiles = 0;");
        ReplaceGMLInCode(characterVarsCode, "global.maxsmissiles = 0", "global.maxsmissiles = global.smissiles;");
        ReplaceGMLInCode(characterVarsCode, "global.pbombs = 0", "global.pbombs = 0;");
        ReplaceGMLInCode(characterVarsCode, "global.maxpbombs = 0", "global.maxpbombs = global.pbombs;");
        ReplaceGMLInCode(characterVarsCode, "global.maxhealth = 99", "global.maxhealth = global.playerhealth;");
            
        // Make main (super) missile / PB launcher required for firing
        var shootMissileCode = gmData.Code.ByName("gml_Script_shoot_missile");
        ReplaceGMLInCode(shootMissileCode, 
            "if ((global.currentweapon == 1 && global.missiles > 0) || (global.currentweapon == 2 && global.smissiles > 0))",
            "if ((global.currentweapon == 1 && global.missiles > 0 && global.missileLauncher) || (global.currentweapon == 2 && global.smissiles > 0 && global.SMissileLauncher))");
        var chStepFireCode = gmData.Code.ByName("gml_Script_chStepFire");
        ReplaceGMLInCode(chStepFireCode, "&& global.pbombs > 0", "&& global.pbombs > 0 && global.PBombLauncher");
            
        // Change GUI For toggle, use a red item sprite instead of green, for hold use a red instead of yellow
        // Replace Missile GUI
        ReplaceGMLInCode(drawGuiCode, """
                if (oCharacter.armmsl == 1)
                    draw_sprite(sGUIMissile, 2, ((0 + xoff) + 1), 4)
""","""
                if (oCharacter.armmsl == 1 && global.missileLauncher)
                    draw_sprite(sGUIMissile, 2, ((0 + xoff) + 1), 4)
                else if (oCharacter.armmsl == 1 && !global.missileLauncher)
                    draw_sprite(sGUIMissile, 3, ((0 + xoff) + 1), 4)
""");
        ReplaceGMLInCode(drawGuiCode, """
            if (global.currentweapon == 1)
                draw_sprite(sGUIMissile, 1, ((0 + xoff) + 1), 4)
""", """
            if (global.currentweapon == 1 && global.missileLauncher)
                draw_sprite(sGUIMissile, 1, ((0 + xoff) + 1), 4)
            else if (global.currentweapon == 1 && !global.missileLauncher)
                draw_sprite(sGUIMissile, 3, ((0 + xoff) + 1), 4)
""");
            
        // Replace Super GUI
        ReplaceGMLInCode(drawGuiCode, """
                if (oCharacter.armmsl == 1)
                    draw_sprite(sGUISMissile, 2, (xoff + 1), 4)
""","""
                if (oCharacter.armmsl == 1 && global.SMissileLauncher)
                    draw_sprite(sGUISMissile, 2, (xoff + 1), 4)
                else if (oCharacter.armmsl == 1 && !global.SMissileLauncher)
                    draw_sprite(sGUISMissile, 3, (xoff + 1), 4)
""");
        ReplaceGMLInCode(drawGuiCode, """
            if (global.currentweapon == 2)
                draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
""", """
            if (global.currentweapon == 2 && global.SMissileLauncher)
                draw_sprite(sGUISMissile, 1, (xoff + 1), 4)
            else if (global.currentweapon == 2 && !global.SMissileLauncher)
                draw_sprite(sGUISMissile, 3, (xoff + 1), 4)
""");
            
        // Replace PB GUI
        ReplaceGMLInCode(drawGuiCode, """
                if (oCharacter.armmsl == 1)
                    draw_sprite(sGUIPBomb, 2, (xoff + 1), 4)
""","""
                if (oCharacter.armmsl == 1 && global.PBombLauncher)
                    draw_sprite(sGUIPBomb, 2, (xoff + 1), 4)
                else if (oCharacter.armmsl == 1 && !global.PBombLauncher)
                    draw_sprite(sGUIPBomb, 3, (xoff + 1), 4)
""");
        ReplaceGMLInCode(drawGuiCode, """
            if (global.currentweapon == 3)
                draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
""", """
            if (global.currentweapon == 3 && global.PBombLauncher)
                draw_sprite(sGUIPBomb, 1, (xoff + 1), 4)
            else if (global.currentweapon == 3 && !global.PBombLauncher)
                draw_sprite(sGUIPBomb, 3, (xoff + 1), 4)
""");
            
        // Fix weapon selection with toggle
        var chStepControlCode = gmData.Code.ByName("gml_Script_chStepControl");
        ReplaceGMLInCode(chStepControlCode, "if (kMissile && kMissilePushedSteps == 1 && global.maxmissiles > 0", "if (kMissile && kMissilePushedSteps == 1");
        ReplaceGMLInCode(chStepControlCode, "if (global.currentweapon == 1 && global.missiles == 0)", "if (global.currentweapon == 1 && (global.maxmissiles == 0 || global.missiles == 0))");
            
        // Fix weapon selection with hold
        ReplaceGMLInCode(chStepControlCode, """
                if (global.currentweapon == 0)
                    global.currentweapon = 1
            """, """
            if (global.currentweapon == 0)
            {
                if (global.maxmissiles > 0) global.currentweapon = 1;
                else if (global.maxsmissiles > 0) global.currentweapon = 2;
            }
            """);
        ReplaceGMLInCode(chStepControlCode, "if (global.maxmissiles > 0 && (state", "if ((state");
            
        // TODO: change samus arm cannon to different sprite, when no missile launcher. This requires delving into state machine tho and that is *pain*
        // For that, also make her not arm the cannon if you have missile launcher but no missiles
        // ALTERNATIVE: if missile equipped, but no launcher, make EMP effect display that usually appears in gravity area
            
        // Have new variables for certain events because they are easier to debug via a switch than changing a ton of values
        PrependGMLInCode(characterVarsCode, "global.septoggHelpers = 0; global.skipCutscenes = 0; global.skipItemFanfare = 0; global.respawnBombBlocks = 0; global.screwPipeBlocks = 0;" +
                                            "global.a3Block = 0; global.softlockPrevention = 0; global.unexploredMap = 0; global.unveilBlocks = 0; global.canUseSupersOnMissileDoors = 0;");
            
        // Set geothermal reactor to always be exploded
        AppendGMLInCode(characterVarsCode, "global.event[203] = 9");
            
        // Set a bunch of metroid events to already be scanned
        AppendGMLInCode(characterVarsCode, "global.event[301] = 1; global.event[305] = 1; global.event[306] = 1;");
            
        // Move Geothermal PB to big shaft
        AppendGMLInCode(gmData.Rooms.ByName("rm_a4b02a").CreationCodeId, "instance_create(272, 400, scr_itemsopen(oControl.mod_253));");
        ReplaceGMLInCode(gmData.Rooms.ByName("rm_a4b02b").CreationCodeId, "instance_create(314, 192, scr_itemsopen(oControl.mod_253))", "");
            
        // Set lava state and the metroid scanned events
        AppendGMLInCode(characterVarsCode, "global.lavastate = 11; global.event[4] = 1; global.event[56] = 1;" +
                                           " global.event[155] = 1; global.event[173] = 1; global.event[204] = 1; global.event[259] = 1");
            
        // Improve when expansions trigger big pickup text and popup_text
        PrependGMLInCode(characterVarsCode, "global.firstMissileCollected = 0; global.firstSMissileCollected = 0; " +
                                            "global.firstPBombCollected = 0; global.firstETankCollected = 0;");
        var missileCharacterEvent = gmData.Code.ByName("gml_Script_scr_missile_character_event");
        ReplaceGMLInCode(missileCharacterEvent, """
    if (global.maxmissiles == oControl.mod_Mstartingcount)
        event_inherited()
""", """
    if (!global.firstMissileCollected) {
        event_inherited();
        global.firstMissileCollected = 1;
    }
""");
        ReplaceGMLInCode(missileCharacterEvent, "popup_text(get_text(\"Notifications\", \"MissileTank\"))", "");
        
        var superMissileCharacterEvent = gmData.Code.ByName("gml_Script_scr_supermissile_character_event");
        ReplaceGMLInCode(superMissileCharacterEvent, """
    if (global.maxsmissiles == 0)
        event_inherited()
""", """
    if (!global.firstSMissileCollected) {
        event_inherited();
        global.firstSMissileCollected = 1;
    }
""");
        ReplaceGMLInCode(superMissileCharacterEvent, "popup_text(get_text(\"Notifications\", \"SuperMissileTank\"))", "");
            
        var pBombCharacterEvent = gmData.Code.ByName("gml_Script_scr_powerbomb_character_event");
        ReplaceGMLInCode(pBombCharacterEvent, """
    if (global.maxpbombs == 0)
        event_inherited()
""", """
    if (!global.firstPBombCollected) {
        event_inherited();
        global.firstPBombCollected = 1;
    }
""");
        ReplaceGMLInCode(pBombCharacterEvent, "popup_text(get_text(\"Notifications\", \"PowerBombTank\"))", "");
            
        var eTankCharacterEvent = gmData.Code.ByName("gml_Script_scr_energytank_character_event");
        ReplaceGMLInCode(eTankCharacterEvent, """
    if (global.maxhealth < 100)
        event_inherited()
""", """
    if (!global.firstETankCollected) {
        event_inherited();
        global.firstETankCollected = 1;
    }
""");
        ReplaceGMLInCode(eTankCharacterEvent, "popup_text(get_text(\"Notifications\", \"EnergyTank\"))", "");
            
        // Decouple Major items from item locations
        PrependGMLInCode(characterVarsCode, "global.dna = 0; global.hasBombs = 0; global.hasPowergrip = 0; global.hasSpiderball = 0; global.hasJumpball = 0; global.hasHijump = 0;" +
                                            "global.hasVaria = 0; global.hasSpacejump = 0; global.hasSpeedbooster = 0; global.hasScrewattack = 0; global.hasGravity = 0;" +
                                            "global.hasCbeam = 0; global.hasIbeam = 0; global.hasWbeam = 0; global.hasSbeam  = 0; global.hasPbeam = 0; global.hasMorph = 0;");
            
        // Make all item activation dependant on whether the main item is enabled.
        ReplaceGMLInCode(characterVarsCode, """
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
        ReplaceGMLInCode(characterVarsCode, "global.currentsuit = 0", 
            "global.currentsuit = 0; if (global.hasGravity) global.currentsuit = 2; else if (global.hasVaria) global.currentsuit = 1;");
            
        // Fix spring showing up for a brief moment when killing arachnus
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oArachnus_Alarm_11"), "if (temp_randitem == oItemJumpBall)", "if (false)");

        // Bombs
        var subscreenMenuStep = gmData.Code.ByName("gml_Object_oSubscreenMenu_Step_0");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[0] == 0", "!global.hasBombs");
        var subscreenMiscDaw = gmData.Code.ByName("gml_Object_oSubScreenMisc_Draw_0");
        ReplaceGMLInCode(subscreenMiscDaw, "global.item[0]", "global.hasBombs");
            
        foreach(var code in new[]{"gml_Script_spawn_rnd_pickup", "gml_Script_spawn_rnd_pickup_at", "gml_Script_spawn_many_powerups", 
                    "gml_Script_spawn_many_powerups_tank", "gml_RoomCC_rm_a2a06_4759_Create", "gml_RoomCC_rm_a2a06_4761_Create",
                    "gml_RoomCC_rm_a3h03_5279_Create", "gml_Room_rm_a3b08_Create"
                })
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.item[0]", "global.hasBombs");
        var elderSeptogg = gmData.GameObjects.ByName("oElderSeptogg");
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (UndertaleRoom.GameObject go in room.GameObjects.Where(go => go.ObjectDefinition == elderSeptogg && go.CreationCode is not null))
                ReplaceGMLInCode(go.CreationCode, "global.item[0]", "global.hasBombs", true);
        }
            
            
        // Powergrip
        ReplaceGMLInCode(subscreenMiscDaw, "global.item[1]", "global.hasPowergrip");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[1] == 0", "!global.hasPowergrip");
            
        // Spiderball
        ReplaceGMLInCode(subscreenMiscDaw, "global.item[2]", "global.hasSpiderball");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[2] == 0", "!global.hasSpiderball");
        foreach (UndertaleCode code in gmData.Code.Where(c => c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && 
                     c.Name.Content.Contains('2') || c.Name.Content == "gml_RoomCC_rm_a0h25_4105_Create"))
            ReplaceGMLInCode(code, "global.item[2]", "global.hasSpiderball");
            
        // Jumpball
        ReplaceGMLInCode(subscreenMiscDaw, "global.item[3]", "global.hasJumpball");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[3] == 0", "!global.hasJumpball");
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a2a06_4761_Create"), "global.item[3] == 0", "!global.hasJumpball");
            
        // Hijump
        var subcreenBootsDraw = gmData.Code.ByName("gml_Object_oSubScreenBoots_Draw_0");
        ReplaceGMLInCode(subcreenBootsDraw, "global.item[4]", "global.hasHijump");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[4] == 0", "!global.hasHijump");
        foreach(var code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && 
                                                    c.Name.Content.Contains('4')) || c.Name.Content == "gml_Room_rm_a3b08_Create" || c.Name.Content == "gml_RoomCC_rm_a5c17_7779_Create"))
            ReplaceGMLInCode(code, "global.item[4]", "global.hasHijump");
        
        // Varia
        var subscreenSuitDraw = gmData.Code.ByName("gml_Object_oSubScreenSuit_Draw_0");
        ReplaceGMLInCode(subscreenSuitDraw, "global.item[5]", "global.hasVaria");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[5] == 0", "!global.hasVaria");
        foreach(var code in new[]{"gml_Script_characterStepEvent", "gml_Script_damage_player", "gml_Script_damage_player_push", "gml_Script_damage_player_knockdown", "gml_Object_oQueenHead_Step_0"})
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.item[5]", "global.hasVaria");
            
        // Spacejump
        ReplaceGMLInCode(subcreenBootsDraw, "global.item[6]", "global.hasSpacejump");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[6] == 0", "!global.hasSpacejump");
        foreach(var code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && 
                                                    c.Name.Content.Contains('6')) || c.Name.Content.StartsWith("gml_RoomCC_rm_a5a03_") || 
                                                   c.Name.Content == "gml_RoomCC_rm_a0h25_4105_Create"))
            ReplaceGMLInCode(code, "global.item[6]", "global.hasSpacejump", true);
            
        // Speedbooster
        ReplaceGMLInCode(subcreenBootsDraw, "global.item[7]", "global.hasSpeedbooster");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[7] == 0", "!global.hasSpeedbooster");
        foreach(var code in gmData.Code.Where(c => (c.Name.Content.StartsWith("gml_Script_scr_septoggs_") && 
                                                    c.Name.Content.Contains('7')) || c.Name.Content.StartsWith("gml_RoomCC_rm_a5c08_")))
            ReplaceGMLInCode(code, "global.item[7]", "global.hasSpeedbooster", true);

            
        // Screwattack
        ReplaceGMLInCode(subscreenMiscDaw, "global.item[8]", "global.hasScrewattack");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[8] == 0", "!global.hasScrewattack");
        foreach(var code in new[]{"gml_Script_scr_septoggs_2468", "gml_Script_scr_septoggs_48", "gml_RoomCC_rm_a1a06_4447_Create", 
                    "gml_RoomCC_rm_a1a06_4448_Create", "gml_RoomCC_rm_a1a06_4449_Create", "gml_RoomCC_rm_a3a04_5499_Create", "gml_RoomCC_rm_a3a04_5500_Create",
                    "gml_RoomCC_rm_a3a04_5501_Create", "gml_RoomCC_rm_a4a01_6476_Create", "gml_RoomCC_rm_a4a01_6477_Create", "gml_RoomCC_rm_a4a01_6478_Create",
                    "gml_RoomCC_rm_a5c13_7639_Create", "gml_RoomCC_rm_a5c13_7640_Create", "gml_RoomCC_rm_a5c13_7641_Create", "gml_RoomCC_rm_a5c13_7642_Create",
                    "gml_RoomCC_rm_a5c13_7643_Create", "gml_RoomCC_rm_a5c13_7644_Create"
                })
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.item[8]", "global.hasScrewattack");

            
        // Gravity
        ReplaceGMLInCode(subscreenSuitDraw, "global.item[9]", "global.hasGravity");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[9] == 0", "!global.hasGravity");
            
        foreach(var code in new[]{"gml_Script_scr_variasuitswap", "gml_Object_oGravitySuitChangeFX_Step_0", "gml_Object_oGravitySuitChangeFX_Other_10",
                    "gml_RoomCC_rm_a2a06_4759_Create", "gml_RoomCC_rm_a2a06_4761_Create", "gml_RoomCC_rm_a5a03_8631_Create", "gml_RoomCC_rm_a5a03_8632_Create",
                    "gml_RoomCC_rm_a5a03_8653_Create", "gml_RoomCC_rm_a5a03_8654_Create", "gml_RoomCC_rm_a5a03_8655_Create", "gml_RoomCC_rm_a5a03_8656_Create",
                    "gml_RoomCC_rm_a5a03_8657_Create", "gml_RoomCC_rm_a5a03_8674_Create", "gml_RoomCC_rm_a5a05_8701_Create", "gml_RoomCC_rm_a5a06_8704_Create"
                })
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.item[9]", "global.hasGravity");

        // Charge
        var itemsSwapScript = gmData.Code.ByName("gml_Script_scr_itemsmenu_swap");
        ReplaceGMLInCode(itemsSwapScript, "global.item[10]", "global.hasCbeam");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[10] == 0", "!global.hasCbeam");
            
        // Ice
        ReplaceGMLInCode(itemsSwapScript, "global.item[11]", "global.hasIbeam");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[11] == 0", "!global.hasIbeam");
        foreach(var code in new[]{"gml_Object_oEris_Create_0", "gml_Object_oErisBody1_Create_0", "gml_Object_oErisHead_Create_0", "gml_Object_oErisSegment_Create_0"})
            ReplaceGMLInCode(gmData.Code.ByName(code), "global.item[11] == 0", "!global.hasIbeam");

        // Wave
        ReplaceGMLInCode(itemsSwapScript, "global.item[12]", "global.hasWbeam");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[12] == 0", "!global.hasWbeam");
            
        // Spazer
        ReplaceGMLInCode(itemsSwapScript, "global.item[13]", "global.hasSbeam");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[13] == 0", "!global.hasSbeam");
            
        // Plasma
        ReplaceGMLInCode(itemsSwapScript, "global.item[14]", "global.hasPbeam");
        ReplaceGMLInCode(subscreenMenuStep, "global.item[14] == 0", "!global.hasPbeam");
            
        // Morph Ball
        ReplaceGMLInCode(subscreenMiscDaw, """
                draw_sprite(sSubScrButton, global.morphball, (x - 28), (y + 16))
                draw_text((x - 20), ((y + 15) + oControl.subScrItemOffset), morph)
                """, """
                if (global.hasMorph) {
                    draw_sprite(sSubScrButton, global.morphball, (x - 28), (y + 16))
                    draw_text((x - 20), ((y + 15) + oControl.subScrItemOffset), morph)
                }
                """);
        ReplaceGMLInCode(subscreenMenuStep, """
                if (global.curropt == 7 && (!global.hasIbeam))
                        global.curropt += 1
                ""","""
                if (global.curropt == 7 && (!global.hasIbeam))
                        global.curropt += 1
                if (global.curropt == 8 && (!global.hasMorph))
                        global.curropt += 1
                """);
        ReplaceGMLInCode(subscreenMenuStep, """
                if (global.curropt == 7 && (!global.hasIbeam))
                        global.curropt -= 1
                ""","""
                if (global.curropt == 8 && (!global.hasMorph))
                        global.curropt -= 1
                if (global.curropt == 7 && (!global.hasIbeam))
                        global.curropt -= 1
                """);
        ReplaceGMLInCode(subscreenMenuStep, """
                    else
                        global.curropt = 14
                ""","""
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
            
        ReplaceGMLInCode(subscreenMenuStep, """
                if (global.curropt > 16)
                    global.curropt = 8
            """, """
                if (global.curropt > 16)
                    global.curropt = 8
                if (global.curropt == 8 && (!global.hasMorph))
                        global.curropt = 0 
            """);
            
        // Save current hash seed, so we can compare saves later
        PrependGMLInCode(characterVarsCode, $"global.gameHash = \"{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})\"");
            
        // modify gravity pod room to *always* spawn an item
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a5a07_Create"), "if (oControl.mod_gravity != 9)", "");
        SubstituteGMLCode(gmData.Code.ByName("gml_Object_oGravityPodTrigger_Create_0"), "instance_destroy()");
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oGravityPod_Create_0"), "closed = 1; xoff = 0;");
            
        // Always enable long range activation, for consistent zips
        foreach (var room in gmData.Rooms.Where(r => r.Name.Content.StartsWith("rm_a")))
            AppendGMLInCode(room.CreationCodeId, "global.objdeactivate = 0");
            
        // Make new game not hardcode separate starting values
        PrependGMLInCode(characterVarsCode, "global.startingSave = 0;");
        var startNewGame = gmData.Code.ByName("gml_Script_start_new_game");
        ReplaceGMLInCode(startNewGame, """
                global.start_room = 21
                global.save_x = 3408
                global.save_y = 1184
                """, "load_character_vars(); global.save_room = global.startingSave; set_start_location();");
            
        // Modify main menu to have a "restart from starting save" option
        SubstituteGMLCode(gmData.Code.ByName("gml_Object_oPauseMenuOptions_Other_10"), """
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
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oPauseMenuOptions_Create_0"), "lastitem = 3", "lastitem = 4;");
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oPauseMenuOptions_Create_0"), """
            tip[0] = get_text("PauseMenu", "Resume_Tip");
            tip[1] = get_text("PauseMenu", "Restart_Tip");
            tip[2] = "Abandon the current game and load from Starting Area";
            tip[3] = get_text("PauseMenu", "Options_Tip");
            tip[4] = get_text("PauseMenu", "Quit_Tip");
            global.tiptext = tip[global.curropt];
            """);
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oPauseMenuOptions_Step_0"), """
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
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oPauseMenuOptions_Other_11"), """
            if instance_exists(op5)
            {
                with (op5)
                    instance_destroy()
            }

            """);
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oControl_Create_0"), "global.shouldLoadFromStart = 0;");
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oLoadGame_Other_10"), """
            if (global.shouldLoadFromStart)
            {
              global.save_room = global.startingSave;
              set_start_location();
              room_change(global.start_room, 1)
              global.shouldLoadFromStart = 0;  
            }
            """);
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oOptionsReload_Step_0"), "instance_create(50, 92, oPauseMenuOptions)", 
            "instance_create(50, 92, oPauseMenuOptions); global.shouldLoadFromStart = 0;");

        // Modify save scripts to load our new globals / stuff we modified
        var saveGlobalsCode = new UndertaleCode();
        saveGlobalsCode.Name = gmData.Strings.MakeString("gml_Script_sv6_add_newglobals");
        SubstituteGMLCode(saveGlobalsCode, """
            var list, str_list, comment;
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
            comment = "gives me some leeway in case i need to add more"
            repeat (15)
            {
                ds_list_add(list, 0)
                i += 1
            }
            str_list = ds_list_write(list)
            ds_list_clear(list)
            return str_list;
            """);
        gmData.Code.Add(saveGlobalsCode);
        gmData.Scripts.Add(new UndertaleScript() {Name = gmData.Strings.MakeString("sv6_add_newglobals"), Code = saveGlobalsCode});

        var loadGlobalsCode = new UndertaleCode();
        loadGlobalsCode.Name = gmData.Strings.MakeString("gml_Script_sv6_get_newglobals");
        SubstituteGMLCode(loadGlobalsCode, """
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
            global.startingSave = readline();
            ds_list_clear(list)
            """);
        gmData.Code.Add(loadGlobalsCode);
        gmData.Scripts.Add(new UndertaleScript(){Name = gmData.Strings.MakeString("sv6_get_newglobals"), Code = loadGlobalsCode});
            
        var sv6Save = gmData.Code.ByName("gml_Script_sv6_save");
        ReplaceGMLInCode(sv6Save, "save_str[10] = sv6_add_seed()", "save_str[10] = sv6_add_seed(); save_str[11] = sv6_add_newglobals()");
        ReplaceGMLInCode(sv6Save, "V7.0", "RDV V8.0");
        ReplaceGMLInCode(sv6Save, "repeat (10)", "repeat (11)");

        var sv6load = gmData.Code.ByName("gml_Script_sv6_load");
        ReplaceGMLInCode(sv6load, "V7.0", "RDV V8.0");
        ReplaceGMLInCode(sv6load, "sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid); sv6_get_newglobals(fid);");
        ReplaceGMLInCode(sv6load, "global.maxhealth = (99 + ((global.etanks * 100) * oControl.mod_etankhealthmult))", "");
        ReplaceGMLInCode(sv6load, """
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
        PrependGMLInCode(sv6load, $"var uniqueGameHash = \"{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})\"");
        ReplaceGMLInCode(sv6load, "global.playerhealth = global.maxhealth", 
            "if (global.gameHash != uniqueGameHash) { " +
            "show_message(\"Save file is from another seed! (\" + global.gameHash + \")\"); " +
            "file_text_close(fid); file_delete((filename + \"d\")); room_goto(titleroom); exit;" +
            "} global.playerhealth = global.maxhealth");
        // TODO: instead of just show_messsage, have an actual proper in-game solution
        // reference: https://cdn.discordapp.com/attachments/914294505107251231/1121816654385516604/image.png
            
        var sv6loadDetails = gmData.Code.ByName("gml_Script_sv6_load_details");
        ReplaceGMLInCode(sv6loadDetails, "V7.0", "RDV V8.0");
        ReplaceGMLInCode(sv6loadDetails, "sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid); sv6_get_newglobals(fid);");

        foreach (var code in new[] {"gml_Script_save_stats", "gml_Script_save_stats2", "gml_Script_load_stats", "gml_Script_load_stats2"})
            ReplaceGMLInCode(gmData.Code.ByName(code), "V7.0", "RDV V8.0");
            
        // Change to custom save directory
        gmData.GeneralInfo.Name = gmData.Strings.MakeString("AM2R_RDV");
        gmData.GeneralInfo.FileName = gmData.Strings.MakeString("AM2R_RDV");
            
        // Change starting health and energy per tank
        ReplaceGMLInCode(characterVarsCode, "global.playerhealth = 99", $"global.playerhealth = {seedObject.Patches.EnergyPerTank-1};");
        ReplaceGMLInCode(eTankCharacterEvent, "global.maxhealth += (100 * oControl.mod_etankhealthmult)", $"global.maxhealth += {seedObject.Patches.EnergyPerTank}");
            
        // Set starting items
        foreach ((var item, var quantity) in seedObject.StartingItems)
        {
            switch (item)
            {
                case ItemEnum.EnergyTank:
                    ReplaceGMLInCode(characterVarsCode, "global.etanks = 0", $"global.etanks = {quantity};");
                    ReplaceGMLInCode(characterVarsCode, $"global.playerhealth = {seedObject.Patches.EnergyPerTank-1}",
                        $"global.playerhealth = {(seedObject.Patches.EnergyPerTank + (seedObject.Patches.EnergyPerTank * quantity)-1)};");
                    break;
                case ItemEnum.Missile:
                    ReplaceGMLInCode(characterVarsCode, "global.missiles = 0", $"global.missiles = {quantity};");
                    break;
                case ItemEnum.SuperMissile:
                    ReplaceGMLInCode(characterVarsCode, "global.smissiles = 0", $"global.smissiles = {quantity};");
                    break;
                case ItemEnum.PBomb:
                    ReplaceGMLInCode(characterVarsCode, "global.pbombs = 0", $"global.pbombs = {quantity};");
                    break;
                    
                case ItemEnum.LockedMissile:
                case ItemEnum.LockedSuperMissile:
                case ItemEnum.LockedPBomb:
                    break;
                    
                case ItemEnum.MissileLauncher:
                case ItemEnum.SuperMissileLauncher:
                case ItemEnum.PBombLauncher:
                    // Are handled further down
                    break;
                    
                case var x when x.ToString().StartsWith("DNA"):
                    ReplaceGMLInCode(characterVarsCode, "global.dna =", "global.dna = 1 +");
                    break;
                    
                case ItemEnum.Bombs:
                    ReplaceGMLInCode(characterVarsCode, "global.hasBombs = 0", $"global.hasBombs = {quantity};");
                    break;
                case ItemEnum.Powergrip:
                    ReplaceGMLInCode(characterVarsCode, "global.hasPowergrip = 0", $"global.hasPowergrip = {quantity};");
                    break;
                case ItemEnum.Spiderball:
                    ReplaceGMLInCode(characterVarsCode, "global.hasSpiderball = 0", $"global.hasSpiderball = {quantity};");
                    break;
                case ItemEnum.Springball:
                    ReplaceGMLInCode(characterVarsCode, "global.hasJumpball = 0", $"global.hasJumpball = {quantity};");
                    break;
                case ItemEnum.Hijump:
                    ReplaceGMLInCode(characterVarsCode, "global.hasHijump = 0", $"global.hasHijump = {quantity};");
                    break;
                case ItemEnum.Varia:
                    ReplaceGMLInCode(characterVarsCode, "global.hasVaria = 0", $"global.hasVaria = {quantity};");
                    break;
                case ItemEnum.Spacejump:
                    ReplaceGMLInCode(characterVarsCode, "global.hasSpacejump = 0", $"global.hasSpacejump = {quantity};");
                    break;
                case ItemEnum.Speedbooster:
                    ReplaceGMLInCode(characterVarsCode, "global.hasSpeedbooster = 0", $"global.hasSpeedbooster = {quantity};");
                    break;
                case ItemEnum.Screwattack:
                    ReplaceGMLInCode(characterVarsCode, "global.hasScrewattack = 0", $"global.hasScrewattack = {quantity};");
                    break;
                case ItemEnum.Gravity:
                    ReplaceGMLInCode(characterVarsCode, "global.hasGravity = 0", $"global.hasGravity = {quantity};");
                    break;
                case ItemEnum.Power:
                    // Stubbed for now, may get a purpose in the future
                    break;
                case ItemEnum.Charge:
                    ReplaceGMLInCode(characterVarsCode, "global.hasCbeam = 0", $"global.hasCbeam = {quantity};");
                    break;
                case ItemEnum.Ice:
                    ReplaceGMLInCode(characterVarsCode, "global.hasIbeam = 0", $"global.hasIbeam = {quantity};");
                    break;
                case ItemEnum.Wave:
                    ReplaceGMLInCode(characterVarsCode, "global.hasWbeam = 0", $"global.hasWbeam = {quantity};");
                    break;
                case ItemEnum.Spazer:
                    ReplaceGMLInCode(characterVarsCode, "global.hasSbeam = 0", $"global.hasSbeam = {quantity};");
                    break;
                case ItemEnum.Plasma:
                    ReplaceGMLInCode(characterVarsCode, "global.hasPbeam = 0", $"global.hasPbeam = {quantity};");
                    break;
                case ItemEnum.Morphball:
                    ReplaceGMLInCode(characterVarsCode, "global.hasMorph = 0", $"global.hasMorph = {quantity};");
                    break;
                case ItemEnum.Nothing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
            
        // Check whether option has been set for non-main launchers or if starting with them, if yes enable the main launchers in character var
        if (!seedObject.Patches.RequireMissileLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.MissileLauncher))
            ReplaceGMLInCode(characterVarsCode, "global.missileLauncher = 0", "global.missileLauncher = 1");
        if (!seedObject.Patches.RequireSuperLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.SuperMissileLauncher))
            ReplaceGMLInCode(characterVarsCode, "global.SMissileLauncher = 0", "global.SMissileLauncher = 1");
        if (!seedObject.Patches.RequirePBLauncher || seedObject.StartingItems.ContainsKey(ItemEnum.PBombLauncher))
            ReplaceGMLInCode(characterVarsCode, "global.PBombLauncher = 0", "global.PBombLauncher = 1");
            
        // Set starting location
        ReplaceGMLInCode(characterVarsCode, "global.startingSave = 0", $"global.startingSave = {seedObject.StartingLocation.SaveRoom}");
        ReplaceGMLInCode(characterVarsCode, "global.save_room = 0", $"global.save_room = {seedObject.StartingLocation.SaveRoom}");
            
        // Modify minimap for power plant because of pb movement
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_07"), """
            global.map[35, 43] = "0112300"
            global.map[35, 44] = "1210300"
            global.map[35, 45] = "1210300"
            """, """
            global.map[35, 43] = "0101330"
            global.map[35, 44] = "0101300"
            global.map[35, 45] = "0101300"
            """);
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oItem_Other_10"), "&& itemid == 253", "&& false");
        // Removes map tiles of the inaccessible A4 basement/Reactor Core/Power Plant.
        AppendGMLInCode(gmData.Code.ByName("gml_Script_init_map"), """
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
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oItem_Other_10"), "if (distance_to_object(oItem) > 180)", 
            "if ((distance_to_object(oItem) > 180) && (instance_number(oMAlpha) <= 0) && (instance_number(oMGamma) <= 0) && (instance_number(oMZeta) <= 0) && (instance_number(oMOmega) <= 0))");
            
        // Door locks
        // Adjust global event array to be 700
        ReplaceGMLInCode(characterVarsCode, """
            i = 350
            repeat (350)
            {
                i -= 1
                global.event[i] = 0
            }
            """, """
            i = 700
            repeat (700)
            {
                i -= 1
                global.event[i] = 0
            }
            """);
        ReplaceGMLInCode( gmData.Code.ByName("gml_Script_sv6_add_events"), "350", "700");
        ReplaceGMLInCode( gmData.Code.ByName("gml_Script_sv6_get_events"), "350", "700");
            
        // Replace every normal, a4 and a8 door with an a5 door for consistency
        var a5Door = gmData.GameObjects.ByName("oDoorA5");
        foreach (var room in gmData.Rooms)
        {
            foreach (var door in room.GameObjects.Where(go => go.ObjectDefinition.Name.Content.StartsWith("oDoor")))
            {
                door.ObjectDefinition = a5Door;
            }
        }
        // Also fix depth value for them
        a5Door.Depth = -99;
            
            
        var doorEventIndex = 350;
        foreach ((var id, var doorLock) in seedObject.DoorLocks)
        {
            bool found = false;
            foreach (var room in gmData.Rooms)
            {
                foreach (var gameObject in room.GameObjects)
                {
                    if (gameObject.InstanceID != id) continue;

                    if (!gameObject.ObjectDefinition.Name.Content.StartsWith("oDoor"))
                        throw new NotSupportedException($"The 'door' instance {id} is not actually a door!");

                    found = true;
                    if (gameObject.CreationCode is null)
                    {
                        var code = new UndertaleCode() { Name = gmData.Strings.MakeString($"gml_RoomCC_{room.Name.Content}_{id}_Create") };
                        gmData.Code.Add(code);
                        gameObject.CreationCode = code;
                    }
                        
                    string codeText = doorLock.Lock switch
                    {
                        DoorLockType.Normal => "lock = 0; event = -1;",
                        DoorLockType.Missile => $"lock = 1; originalLock = lock; event = {doorEventIndex};",
                        DoorLockType.SuperMissile => $"lock = 2; originalLock = lock; event = {doorEventIndex};",
                        DoorLockType.PBomb => $"lock = 3; originalLock = lock; event = {doorEventIndex};",
                        DoorLockType.TempLocked => $"lock = 4; originalLock = lock; event = -1;",
                        DoorLockType.Charge => $"lock = 5; originalLock = lock; event = -1;",
                        DoorLockType.Wave => $"lock = 6; originalLock = lock; event = -1;",
                        DoorLockType.Spazer => $"lock = 7; originalLock = lock; event = -1;",
                        DoorLockType.Plasma => $"lock = 8; originalLock = lock; event = -1;",
                        DoorLockType.Ice => $"lock = 9; originalLock = lock; event = -1;",
                        DoorLockType.Bomb => "lock = 10; originalLock = lock; event = -1;",
                        DoorLockType.Spider => "lock = 11; originalLock = lock; event = -1;",
                        DoorLockType.Screw => "lock = 12; originalLock = lock; event = -1;",
                        DoorLockType.TowerEnabled => "lock = 13; originalLock = lock; event = -1;",
                        DoorLockType.TesterDead => "lock = 14; originalLock = lock; event = -1;",
                        DoorLockType.GuardianDead => "lock = 15; originalLock = lock; event = -1;",
                        DoorLockType.ArachnusDead => "lock = 16; originalLock = lock; event = -1;",
                        DoorLockType.TorizoDead => "lock = 17; originalLock = lock; event = -1;",
                        DoorLockType.SerrisDead => "lock = 18; originalLock = lock; event = -1;",
                        DoorLockType.GenesisDead => "lock = 19; originalLock = lock; event = -1;",
                        DoorLockType.QueenDead => "lock = 20; originalLock = lock; event = -1;",
                        DoorLockType.EMPActivated => "lock = 21; originalLock = lock; event = -1;",
                        DoorLockType.EMPA1 => "lock = 22; originalLock = lock; event = -1;",
                        DoorLockType.EMPA2 => "lock = 23; originalLock = lock; event = -1;",
                        DoorLockType.EMPA3 => "lock = 24; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5Tutorial => "lock = 25; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5RobotHome => "lock = 26; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5NearZeta => "lock = 27; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5BulletHell => "lock = 28; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5PipeHub => "lock = 29; originalLock = lock; event = -1;",
                        DoorLockType.EMPA5RightExterior => "lock = 30; originalLock = lock; event = -1;",
                        DoorLockType.Locked => "lock = 31; originalLock = lock; event = -1;",
                        _ => throw new NotSupportedException($"Door {id} has an unsupported door lock ({doorLock.Lock})!")
                    };
                        
                    AppendGMLInCode(gameObject.CreationCode, codeText);
                    doorEventIndex++;
                    break;
                }

                if (found) break;
            }

            if (!found)
                throw new NotSupportedException($"There is no door with ID {id}!");
        }
            

        // Modify every location item, to give the wished item, spawn the wished text and the wished sprite
        foreach ((var pickupName, PickupObject pickup) in seedObject.PickupObjects)
        {
            var gmObject = gmData.GameObjects.ByName(pickupName);
            gmObject.Sprite = gmData.Sprites.ByName(pickup.SpriteDetails.Name);
            if (gmObject.Sprite is null)
                throw new NotSupportedException($"The sprite for {pickupName} ({gmObject.Name.Content}) cannot be null!");
            // First 0 is for creation event
            var createCode = gmObject.Events[0][0].Actions[0].CodeId;
            AppendGMLInCode(createCode, $"image_speed = {pickup.SpriteDetails.Speed}; text1 = \"{pickup.Text.Header}\"; text2 = \"{pickup.Text.Description}\";" +
                                        $"btn1_name = \"\"; btn2_name = \"\";");

            // First 4 is for Collision event
            var collisionCode = gmObject.Events[4][0].Actions[0].CodeId;
            var collisionCodeToBe = pickup.ItemEffect switch
            {
                ItemEnum.EnergyTank => "scr_energytank_character_event()",
                ItemEnum.MissileExpansion => $"if (!global.missileLauncher) {{ text1 = \"{seedObject.Patches.LockedMissileText.Header}\"; "+ 
                                             $"text2 = \"{seedObject.Patches.LockedMissileText.Description}\" }} scr_missile_character_event()",
                ItemEnum.MissileLauncher => "event_inherited(); if (active) " +
                                            "{ global.missileLauncher = 1; global.maxmissiles += global.missileLauncherExpansion; global.missiles = global.maxmissiles; }",
                ItemEnum.SuperMissileExpansion => $"if (!global.SMissileLauncher) {{ text1 = \"{seedObject.Patches.LockedSuperText.Header}\"; " +
                                                  $"text2 = \"{seedObject.Patches.LockedSuperText.Description}\" }} scr_supermissile_character_event()",
                ItemEnum.SuperMissileLauncher => "event_inherited(); if (active) " +
                                                 "{ global.SMissileLauncher = 1; global.maxsmissiles += global.SMissileLauncherExpansion; global.smissiles = global.maxsmissiles; }",
                ItemEnum.PBombExpansion=> $"if (!global.PBombLauncher) {{ text1 = \"{seedObject.Patches.LockedPBombText.Header}\"; " + 
                                          $"text2 = \"{seedObject.Patches.LockedPBombText.Description}\" }} scr_powerbomb_character_event()",
                ItemEnum.PBombLauncher => "event_inherited(); if (active) " +
                                          "{ global.PBombLauncher = 1; global.maxpbombs += global.PBombLauncherExpansion; global.pbombs = global.maxpbombs; }",
                var x when x.ToString().StartsWith("DNA") => "event_inherited(); if (active) { global.dna++; check_areaclear(); }",
                ItemEnum.Bombs => "btn1_name = \"Fire\"; event_inherited(); if (active) { global.bomb = 1; global.hasBombs = 1; }",
                ItemEnum.Powergrip =>"event_inherited(); if (active) { global.powergrip = 1; global.hasPowergrip = 1; }",
                ItemEnum.Spiderball => "btn1_name = \"Aim\"; event_inherited(); if (active) { global.spiderball = 1; global.hasSpiderball = 1; }",
                ItemEnum.Springball => "btn1_name = \"Jump\"; event_inherited(); if (active) { global.jumpball = 1; global.hasJumpball = 1; }",
                ItemEnum.Screwattack => "event_inherited(); if (active) { global.screwattack = 1; global.hasScrewattack = 1; } with (oCharacter) sfx_stop(spinjump_sound);",
                ItemEnum.Varia => """
                        event_inherited()
                        global.hasVaria = 1;
                        global.SuitChange = !global.skipItemFanfare;
                        if collision_line((x + 8), (y - 8), (x + 8), (y - 32), oSolid, false, true)
                            global.SuitChange = 0;
                        if (!(collision_point((x + 8), (y + 8), oSolid, 0, 1)))
                            global.SuitChange = 0;
                        global.SuitChangeX = x;
                        global.SuitChangeY = y;
                        global.SuitChangeGravity = 0;
                        if (active)
                        {
                            with (oCharacter)
                                alarm[1] = 1;
                        }
                    """,
                ItemEnum.Spacejump => "event_inherited(); if (active) { global.spacejump = 1; global.hasSpacejump = 1; } with (oCharacter) sfx_stop(spinjump_sound);",
                ItemEnum.Speedbooster => "event_inherited(); if (active) { global.speedbooster = 1; global.hasSpeedbooster = 1; }",
                ItemEnum.Hijump => "event_inherited(); if (active) { global.hijump = 1; global.hasHijump = 1; }",
                ItemEnum.Gravity => """
                        event_inherited();
                        global.hasGravity = 1;
                        global.SuitChange = !global.skipItemFanfare;
                        if (collision_line((x + 8), (y - 8), (x + 8), (y - 32), oSolid, false, true))
                            global.SuitChange = 0;
                        if (!(collision_point((x + 8), (y + 8), oSolid, 0, 1)))
                            global.SuitChange = 0;
                        global.SuitChangeX = x;
                        global.SuitChangeY = y;
                        global.SuitChangeGravity = 1;
                        if (active)
                        {
                            with (oCharacter)
                                alarm[4] = 1;
                        }
                    """,
                ItemEnum.Charge => "btn1_name = \"Fire\"; event_inherited(); if (active) { global.cbeam = 1; global.hasCbeam = 1; }",
                ItemEnum.Ice => "event_inherited(); if (active) { global.ibeam = 1; global.hasIbeam = 1; }",
                ItemEnum.Wave => "event_inherited(); if (active) { global.wbeam = 1; global.hasWbeam = 1; }",
                ItemEnum.Spazer => "event_inherited(); if (active) { global.sbeam = 1; global.hasSbeam = 1; }",
                ItemEnum.Plasma => "event_inherited(); if (active) { global.pbeam = 1; global.hasPbeam = 1; }",
                ItemEnum.Morphball => "event_inherited(); if (active) { global.morphball = 1; global.hasMorph = 1; }",
                ItemEnum.Nothing => "event_inherited();",
                _ => throw new NotSupportedException("Unsupported item! " + pickup.ItemEffect)
            };
            SubstituteGMLCode(collisionCode, collisionCodeToBe);
        }
            
        // Modify how much expansions give
        ReplaceGMLInCode(missileCharacterEvent, """
    if (global.difficulty < 2)
        global.maxmissiles += 5
    if (global.difficulty == 2)
        global.maxmissiles += 2
""",$"""
    global.maxmissiles += {seedObject.PickupObjects.FirstOrDefault(p => p.Value.ItemEffect == ItemEnum.MissileExpansion).Value?.Quantity ?? 0}
""");
            
        ReplaceGMLInCode(superMissileCharacterEvent, """
    if (global.difficulty < 2)
        global.maxsmissiles += 2
    if (global.difficulty == 2)
        global.maxsmissiles += 1
""",$"""
    global.maxsmissiles += {seedObject.PickupObjects.FirstOrDefault(p => p.Value.ItemEffect == ItemEnum.SuperMissileExpansion).Value?.Quantity ?? 0}
""");
            
        ReplaceGMLInCode(pBombCharacterEvent, """
    if (global.difficulty < 2)
        global.maxpbombs += 2
    if (global.difficulty == 2)
        global.maxpbombs += 1
""",$"""
    global.maxpbombs += {seedObject.PickupObjects.FirstOrDefault(p => p.Value.ItemEffect == ItemEnum.PBombExpansion).Value?.Quantity ?? 0}
""");
            
            
        // Set how much items the launchers give
        if (seedObject.PickupObjects.Any(p => p.Value.ItemEffect == ItemEnum.MissileLauncher))
            ReplaceGMLInCode(characterVarsCode, "global.missileLauncherExpansion = 30", 
                $"global.missileLauncherExpansion = {seedObject.PickupObjects.First(p => p.Value.ItemEffect == ItemEnum.MissileLauncher).Value.Quantity};");
            
        if (seedObject.PickupObjects.Any(p => p.Value.ItemEffect == ItemEnum.SuperMissileLauncher))
            ReplaceGMLInCode(characterVarsCode, "global.SMissileLauncherExpansion = 2", 
                $"global.SMissileLauncherExpansion = {seedObject.PickupObjects.First(p => p.Value.ItemEffect == ItemEnum.SuperMissileLauncher).Value.Quantity};");
            
        if (seedObject.PickupObjects.Any(p => p.Value.ItemEffect == ItemEnum.PBombLauncher))
            ReplaceGMLInCode(characterVarsCode, "global.PBombLauncherExpansion = 2", 
                $"global.PBombLauncherExpansion = {seedObject.PickupObjects.First(p => p.Value.ItemEffect == ItemEnum.PBombLauncher).Value.Quantity};");
            
            
        // Also change how gui health is drawn
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_gui_health"), """
            if (ceil(guih) == 100)
                guih = 99
            """, $"""
            guih = ceil((global.playerhealth % {seedObject.Patches.EnergyPerTank}));
            if (ceil(guih) == {seedObject.Patches.EnergyPerTank})
                guih = {seedObject.Patches.EnergyPerTank-1};
            """);
            
        // Draw_gui has a huge fucking block that does insane etank shenanigans
        // because i dont want to copypaste the whole thing into here, i'll get the index where it starts, where it ends, and replace that section with my own
        var drawGuiText = Decompiler.Decompile(drawGuiCode, decompileContext);
        int drawStartIndex = drawGuiText.IndexOf("if (global.etanks >= 1)");
        int drawEndIndex = drawGuiText.IndexOf("draw_set_font(global.guifont2)");
        var etankSnippet = drawGuiText.Substring(drawStartIndex, drawEndIndex - drawStartIndex);
        ReplaceGMLInCode(drawGuiCode, etankSnippet, $$"""
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
              if (global.playerhealth > ({{seedObject.Patches.EnergyPerTank-0.01}} + ((i-1)*{{seedObject.Patches.EnergyPerTank}})))
                etankIndex = 1;
              var drawXOff = (floor((i-1)/2) * 6) + (floor((i-1) / 10) * 3) 
              var drawYOff = 4;
              if (i % 2 == 0) drawYOff = 10
              draw_sprite(sGUIETank, etankIndex, (0+etankxoff+drawXOff), drawYOff)
            }

            """);
            
        // Turn off Septoggs if the wished configuration
        if (seedObject.Patches.SeptoggHelpers)
            ReplaceGMLInCode(characterVarsCode, "global.septoggHelpers = 0", "global.septoggHelpers = 1");
        foreach (var code in gmData.Code.Where(c => c.Name.Content.StartsWith("gml_Script_scr_septoggs_")))
            PrependGMLInCode(code, "if (!global.septoggHelpers) return true;");
            
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (UndertaleRoom.GameObject go in room.GameObjects.Where(go => go.ObjectDefinition == elderSeptogg && go.CreationCode is not null))
                ReplaceGMLInCode(go.CreationCode, "oControl.mod_septoggs_bombjumps_easy == 0 && global.hasBombs == 1", 
                    "!global.septoggHelpers", true);
        }
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a0h25_4105_Create"), "else if (global.hasBombs == 1 || global.hasSpiderball == 1 || global.hasSpacejump == 1)",
            "else if (!global.septoggHelpers)");
            
            
        // Options to turn off the random room geometry changes!
        // screw+pipes related
        if (seedObject.Patches.ScrewPipeBlocks)
            ReplaceGMLInCode(characterVarsCode, "global.screwPipeBlocks = 0", "global.screwPipeBlocks = 1");
        // Screw blocks before normal pipe rooms
        foreach (var codeName in new[] {"gml_Room_rm_a1a06_Create", "gml_Room_rm_a2a08_Create", "gml_Room_rm_a2a09_Create", "gml_Room_rm_a2a12_Create", "gml_Room_rm_a3a04_Create", "gml_Room_rm_a4h01_Create", "gml_Room_rm_a4a01_Create"})
            AppendGMLInCode(gmData.Code.ByName(codeName), "if (!global.screwPipeBlocks) {with (oBlockScrew) instance_destroy();}");
        foreach (var roomName in new[] {"rm_a1a06", "rm_a3a04", "rm_a4a01"})
        {
            foreach (var gameObject in gmData.Rooms.ByName(roomName).GameObjects.Where(g => g.ObjectDefinition.Name.Content == "oBlockScrew"))
            {
                if (gameObject.CreationCode is null) continue;
                
                ReplaceGMLInCode(gameObject.CreationCode, "instance_destroy()", "while (false) {}");
            }
        }
        // A bunch of tiles in a5c13 - screw blocks before pipe hub
        for (int i = 39; i <= 44; i++)
            SubstituteGMLCode(gmData.Code.ByName($"gml_RoomCC_rm_a5c13_76{i}_Create"), "if (!global.screwPipeBlocks) instance_destroy();");            
            
        // Bomb block before a3 entry
        if (seedObject.Patches.A3EntranceBlocks)
            ReplaceGMLInCode(characterVarsCode, "global.a3Block = 0", "global.a3Block = 1;");
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a3h03_5279_Create"), "if ((oControl.mod_randomgamebool == 1 || oControl.mod_splitrandom == 1) && global.hasBombs == 0 && global.ptanks == 0)",
            "if (!global.a3Block)");
            
        // Softlock prevention blocks
        if (seedObject.Patches.SoftlockPrevention)
            ReplaceGMLInCode(characterVarsCode, "global.softlockPrevention = 0", "global.softlockPrevention = 1;");

        // gml_Room_rm_a3b08_Create - some shot / solid blocks in BG3
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a3b08_Create"), """
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
        foreach (var gameObject in gmData.Rooms.ByName("rm_a5c08").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockSpeed"))
        {
            // Y 32 is the top row of speed blocks
            if (gameObject.Y == 32)
                ReplaceGMLInCode(gameObject.CreationCode, """
                if (oControl.mod_randomgamebool == 1 && global.hasSpeedbooster == 0)
                    instance_destroy()
                """, "");
                
            // Y 80 is the bottom row of speed blocks
            if (gameObject.Y == 80) 
                AppendGMLInCode(gameObject.CreationCode, "if (global.softlockPrevention) instance_destroy();");
        }

        // screw blocks in bullet hell room
        foreach (var gameObject in gmData.Rooms.ByName("rm_a5c22").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockScrew"))
        {
            if (gameObject.X == 48 || gameObject.X == 64)
                ReplaceGMLInCode(gameObject.CreationCode, "oControl.mod_previous_room == 268 && global.screwattack == 0 && global.item[scr_itemchange(8)] == 1",
                    "global.softlockPrevention");
        }
            
        // Crumble blocks before Ice chamber
        foreach (var gameObject in gmData.Rooms.ByName("rm_a5c31").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockStep"))
        {
            ReplaceGMLInCode(gameObject.CreationCode, "oControl.mod_previous_room == 277 && global.ibeam == 0 && global.item[scr_itemchange(11)] == 1",
                "global.softlockPrevention");
        }
            
        // Crumble blocks in gravity area one way room
        foreach (var gameObject in gmData.Rooms.ByName("rm_a5a03").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockStep"))
        {
            if (gameObject.X == 96 || gameObject.X == 112)
                ReplaceGMLInCode(gameObject.CreationCode, "oControl.mod_previous_room == 298 && (global.hasGravity == 0 || global.hasSpacejump == 0)",
                    "global.softlockPrevention");
        }
            
        foreach (var gameObject in gmData.Rooms.ByName("rm_a5a06").GameObjects.Where(o => o.ObjectDefinition.Name.Content == "oBlockBombChain"))
        {
            // Top bomb block
            if (gameObject.Y == 64)
                ReplaceGMLInCode(gameObject.CreationCode, """
                if (oControl.mod_randomgamebool == 1 && oControl.mod_previous_room == 301 && global.hasGravity == 0 && global.item[oControl.mod_gravity] == 1 && global.ptanks == 0)
                    instance_destroy()
                else
                """, "");
                
            // Bottom bomb block
            if (gameObject.Y == 176)
                AppendGMLInCode(gameObject.CreationCode, "if (global.softlockPrevention) instance_destroy();");
        }
            
            
        // A4 exterior top, always remove the bomb blocks when coming from that entrance
        foreach (string codeName in new[] {"gml_RoomCC_rm_a4h03_6341_Create", "gml_RoomCC_rm_a4h03_6342_Create"})
            ReplaceGMLInCode(gmData.Code.ByName(codeName), "oControl.mod_previous_room == 214 && global.spiderball == 0", "global.targetx == 416");
            
        // The bomb block puzzle in the room before varia dont need to be done anymore because it's already now covered by "dont regen bomb blocks" option
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a2a06_4761_Create"), "if (oControl.mod_randomgamebool == 1 && global.hasBombs == 0 && (!global.hasJumpball) && global.hasGravity == 0)",
            "if (false)");
        ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a2a06_4759_Create"), "if (oControl.mod_randomgamebool == 1 && global.hasBombs == 0 && global.hasGravity == 0)",
            "if (false)");
            
        // When going down from thoth, make PB blocks disabled
        PrependGMLInCode(gmData.Code.ByName("gml_Room_rm_a0h13_Create"), "if (global.targety == 16) {global.event[176] = 1; with (oBlockPBombChain) event_user(0); }");
            
        // Stop Bomb blocks from respawning
        if (seedObject.Patches.RespawnBombBlocks)
            ReplaceGMLInCode(characterVarsCode, "global.respawnBombBlocks = 0", "global.respawnBombBlocks = 1");
        foreach (UndertaleRoom room in gmData.Rooms)
        {
            foreach (var go in room.GameObjects)
            {
                // Instance ID here is for a puzzle in a2, that when not respawned makes it a tad hard.
                if (!go.ObjectDefinition.Name.Content.StartsWith("oBlockBomb") || go.InstanceID == 110602) continue;
                    
                if (go.CreationCode is null)
                {
                    var code = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_RoomCC_" + room.Name.Content + "_" + go.InstanceID + "_Create")};
                    gmData.Code.Add(code);
                    go.CreationCode = code;
                }
                AppendGMLInCode(go.CreationCode, "if (!global.respawnBombBlocks) regentime = -1");
            }
        }

        // On start, make all rooms show being "unexplored" similar to primes/super
        // Replaces all mentions of sMapBlock and sMapCorner with local variables.
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "sMapBlock", "blockSprite");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "sMapCorner", "cornerSprite");

        // Redefine the sprite variables to the Unexplored sprites, if map tile hasn't been revealed to player.
        if (seedObject.Cosmetics.ShowUnexploredMap)
            ReplaceGMLInCode(characterVarsCode, "global.unexploredMap = 0", "global.unexploredMap = 1;");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "if (argument8 > 0)", """
            //variables for swapping map color and corner sprites
            var blockSprite, cornerSprite;

            //default sprites
            blockSprite = sMapBlock;
            cornerSprite = sMapCorner;

            //Don't draw if map tile is a fast travel pipe
            if (argument7 != "H" && argument7 != "V" && argument7 != "C")
            {
                //Sprite variables changed to unexplored variants if map not revealed
                if (argument8 == 0 && global.unexploredMap)
                {
                    blockSprite = sMapBlockUnexplored;
                    cornerSprite = sMapCornerUnexplored;
                }
                else if (argument8 == 0 && !global.unexploredMap)
                    exit;
            }
            """);
        // Don't ever draw the debug pipe tiles
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), """
            if (argument7 == "H")
                draw_sprite(sMapSP, 12, argument0, argument1)
            if (argument7 == "V")
                draw_sprite(sMapSP, 13, argument0, argument1)
            if (argument7 == "C")
                draw_sprite(sMapSP, 14, argument0, argument1)
            """, "");
        // Also show item pickups and metroids
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "if (argument7 == \"3\" && argument8 == 1)", "if (argument7 == \"3\" && (argument8 == 1 || argument8 == 0))");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "if (argument7 == \"4\" && argument8 == 1)", "if (argument7 == \"4\" && (argument8 == 1 || argument8 == 0))");
        // Add hint icons to minimap
        AppendGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "if (argument7 == \"W\") draw_sprite(sMapSP, 15, argument0, argument1)");
        //Add "M" condition to Metroid alive icon check
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_draw_mapblock"), "if (argument8 == 10)", """if (argument8 == 10 || (argument7 == "M" && global.unexploredMap))""");
        //Draw metroid alive icon on undiscovered map
        AppendGMLInCode(gmData.Code.ByName("gml_Script_init_map"), """
        global.map[14, 34] = "10111M0"
        global.map[15, 29] = "10111M0"
        global.map[18, 50] = "10101M0"
        global.map[19, 24] = "00112M0"
        global.map[21, 15] = "10111M0"
        global.map[25, 27] = "10122M0"
        global.map[25, 36] = "10112M0"
        global.map[25, 42] = "10112M0"
        global.map[25, 47] = "10101M0"
        global.map[27, 31] = "11101M0"
        global.map[30, 17] = "10112M0"
        global.map[30, 24] = "11002M0"
        global.map[30, 26] = "11002M0"
        global.map[35, 19] = "11103M0"
        global.map[35, 23] = "21103M0"
        global.map[35, 31] = "11103M0"
        global.map[37, 33] = "11012M0"
        global.map[41, 28] = "11102M0"
        global.map[42, 21] = "10102M0"
        global.map[42, 37] = "11102M0"
        global.map[43, 24] = "10103M0"
        global.map[49, 38] = "01101M0"
        global.map[54, 16] = "10112M0"
        global.map[54, 27] = "10102M0"
        global.map[58, 25] = "10112M0"
        global.map[58, 28] = "12102M0"
        global.map[58, 31] = "00102M0"
        global.map[59, 38] = "11001M0"
        global.map[60, 16] = "11102M0"
        global.map[60, 25] = "11122M0"
        global.map[63, 15] = "11102M0"
        global.map[64, 33] = "01112M0"
        global.map[64, 47] = "10103M0"
        global.map[65, 24] = "01013M0"
        global.map[67, 25] = "10103M0"
        global.map[67, 45] = "10013M0"
        global.map[68, 46] = "01103M0"
        global.map[69, 9] = "11102M0"
        global.map[71, 48] = "11003M0"
        global.map[73, 24] = "11102M0"
        global.map[73, 31] = "11102M0"
        """);
        
        // Fix BG3 surprise gamma map tile
        SubstituteGMLCode(gmData.Code.ByName("gml_Object_oMGamma_Alarm_9"), """
        myposx = floor((x / 320))
        myposy = floor(((y - 8) / 240))
        mapposx = (myposx + global.mapoffsetx)
        mapposy = (myposy + global.mapoffsety)
        if (myid == 20)
        {
            mapposx = 58
            mapposy = 31
        }
        global.dmap[mapposx, mapposy] = 10
        with (oControl)
            event_user(2)
        """);
        
        // Force all breakables (except the hidden super blocks) to be visible
        if (seedObject.Cosmetics.UnveilBlocks)
            ReplaceGMLInCode(characterVarsCode, "global.unveilBlocks = 0", "global.unveilBlocks = 1");
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oSolid_Alarm_5"), "if (global.unveilBlocks && sprite_index >= sBlockShoot && sprite_index <= sBlockSand)\n" +
                                                                         "{ event_user(1); visible = true; }");
        
        // Skip most cutscenes when enabled
        if (seedObject.Patches.SkipCutscenes)
        {
            ReplaceGMLInCode(characterVarsCode, "global.skipCutscenes = 0", "global.skipCutscenes = 1");
        }

        // Skip Intro cutscene instantly
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oIntroCutscene_Create_0"), "room_change(15, 0)");
        // First Alpha cutscene - event 0
        AppendGMLInCode(characterVarsCode, "global.event[0] = global.skipCutscenes");
        // Gamma mutation cutscene - event 109
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oMGammaFirstTrigger_Collision_267"), """
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
        AppendGMLInCode(characterVarsCode, "global.event[205] = global.skipCutscenes");
        // Omega Mutation cutscene - event 300
        AppendGMLInCode(characterVarsCode, "global.event[300] = global.skipCutscenes");
        // Hatchling cutscene - 302
        AppendGMLInCode(characterVarsCode, "global.event[302] = global.skipCutscenes");
        // Also still increase the metroid counters from the hatchling cutscene
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oEggTrigger_Create_0"), """
            if (global.skipCutscenes)
            {
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
        AppendGMLInCode(characterVarsCode, "global.event[172] = global.skipCutscenes * 3");
        // 1 Orb cutscene
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oClawOrbFirst_Other_11"), "if (global.skipCutscenes) {with (ecam) instance_destroy(); global.enablecontrol = 1; view_object[0] = oCamera; block2 = instance_create(768, 48, oSolid2x2); block2.material = 3; with (oA1MovingPlatform2) with (myblock) instance_destroy()}");
        // 3 Orb cutscene
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oClawPuzzle_Alarm_0"), "if (global.skipCutscenes) {with (ecam) instance_destroy(); global.enablecontrol = 1; view_object[0] = oCamera; block2 = instance_create(608, 112, oSolid2x2); block2.material = 3; with (oA1MovingPlatform) with (myblock) instance_destroy()}");
        // Fix audio for the orb cutscenes
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oMusicV2_Other_4"), "sfx_stop(sndStoneLoop)");
        // Shorten save animation
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_characterStepEvent"), """
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
                if (!global.skipCutscenes)
                {
                    instance_create(x, y, oSaveFX)
                    instance_create(x, y, oSaveSparks)
                }
                popup_text(get_text("Notifications", "GameSaved"))
                save_game(("save" + string((global.saveslot + 1))))
                refill_heath_ammo()
            }
            if ((statetime == 230 && !global.skipCutscenes) || (statetime == 10 && global.skipCutscenes))
                state = IDLE
        """);

        // Skip Item acquisition fanfares
        if (seedObject.Patches.SkipItemFanfares)
            ReplaceGMLInCode(characterVarsCode, "global.skipItemFanfare = 0", "global.skipItemFanfare = 1");
        
        // Put all items as type one
        PrependGMLInCode(gmData.Code.ByName("gml_Object_oItem_Other_10"), "if (global.skipItemFanfare) itemtype = 1;");
        
        // Show popup text only when we skip the cutscene
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oItem_Other_10"), "if (itemtype == 1)", "if (global.skipItemFanfare)");
        // Removes cutscenes for type 1's
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oItem_Other_10"), "display_itemmsg", "if (!global.skipItemFanfare) display_itemmsg");
        
        
        // Patch to add room name display near health
        StringBuilder roomNameDSMapBuilder = new StringBuilder();
        foreach ((var roomName, var roomData) in seedObject.RoomObjects)
            roomNameDSMapBuilder.Append($"ds_map_add(roomNames, \"{roomName}\", \"{roomData.DisplayName}\");\n");
        string roomNameDSMapString = roomNameDSMapBuilder.ToString();
        
        var roomNameHudCC = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Create_0") };
        SubstituteGMLCode(roomNameHudCC, $"""
        rnh_surface = surface_create((320 + oControl.widescreen_space), 240)
        fadeout = 0
        roomtime = 0
        textToDisplay = ""
        image_alpha = 0
        offsetY = 0
        displayMode = {(int)seedObject.Cosmetics.RoomNameHud}
        textBGColor = 0
        textColor0 = c_white
        textColor1 = c_silver
        roomNames = ds_map_create()
        {roomNameDSMapString}
        """);
        var roomNameHudDestroy = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Destroy_0") };
        SubstituteGMLCode(roomNameHudDestroy, "surface_free(rnh_surface); ds_map_destroy(roomNames)");
        var roomNameHudDrawGui = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Draw_64") };
        SubstituteGMLCode(roomNameHudDrawGui, """
        var d;
        d = application_get_position()
        if (room != rm_transition && instance_exists(oCharacter) && global.ingame && displayMode != 0 && global.opshowhud)
            draw_surface_ext(rnh_surface, (oControl.displayx - d[0]), (oControl.displayy - d[1]), oControl.display_scale, oControl.display_scale, 0, -1, 1)
        """);
        var roomNameHudRoomStart = new UndertaleCode() {Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Other_4")};
        SubstituteGMLCode(roomNameHudRoomStart, """
        var newRoomName = ds_map_find_value(roomNames, room_get_name(room))
        if (global.ingame && textToDisplay != newRoomName)
        {
            fadeout = 0
            roomtime = 0
            image_alpha = 0
            textToDisplay = newRoomName
        }
        """);
        var roomNameHudStep = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oRoomNameHUD_Step_0") };
        SubstituteGMLCode(roomNameHudStep, """
        if (displayMode == 0)
            exit;
        offsetY = 0
        if instance_exists(oIGT)
            offsetY = 11
        if global.ingame
        {
            if (!fadeout)
            {
                if (image_alpha < 1)
                    image_alpha += 0.2
                else
                    fadeout = 1
            }
            else if (displayMode == 1 && roomtime > 200 && image_alpha > 0)
                image_alpha -= 0.02
            roomtime++
        }
        if (surface_exists(rnh_surface) && surface_get_width(rnh_surface) < (320 + oControl.widescreen_space))
            surface_free(rnh_surface)
        if (!surface_exists(rnh_surface))
            rnh_surface = surface_create((320 + oControl.widescreen_space), 240)
        if surface_exists(rnh_surface)
        {
            surface_set_target(rnh_surface)
            draw_clear_alpha(c_black, 0)
            draw_set_font(global.fontGUI2)
            draw_set_halign(fa_left)
            draw_set_alpha(1)
            draw_set_color(c_white)
            draw_cool_text(4, (16 + offsetY), textToDisplay, textBGColor, textColor0, textColor1, image_alpha)
            surface_reset_target()
        }
        """);
        gmData.Code.Add(roomNameHudCC);
        gmData.Code.Add(roomNameHudDestroy);
        gmData.Code.Add(roomNameHudDrawGui);
        gmData.Code.Add(roomNameHudRoomStart);
        gmData.Code.Add(roomNameHudStep);
        // Create Object and add events
        
        var roomHudObject = new UndertaleGameObject() { Name = gmData.Strings.MakeString("oRoomNameHUD"), Depth = -9999999, Persistent = true};
        gmData.GameObjects.Add(roomHudObject);
        AppendGMLInCode(gmData.Code.ByName("gml_Script_load_character_vars"), "if (!instance_exists(oRoomNameHUD))\ninstance_create(0, 0, oRoomNameHUD)");
        // Create
        var roomHudCollisionList = roomHudObject.Events[0];
        var roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudCC;
        var roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 0;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Destroy
        roomHudCollisionList = roomHudObject.Events[1];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudDestroy;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 0;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Step
        roomHudCollisionList = roomHudObject.Events[3];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudStep;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 0;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Room start
        roomHudCollisionList = roomHudObject.Events[7];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudRoomStart;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 4;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);
        // Draw GUI
        roomHudCollisionList = roomHudObject.Events[8];
        roomHudAction = new UndertaleGameObject.EventAction();
        roomHudAction.CodeId = roomNameHudDrawGui;
        roomHudEvent = new UndertaleGameObject.Event();
        roomHudEvent.EventSubtype = 64;
        roomHudEvent.Actions.Add(roomHudAction);
        roomHudCollisionList.Add(roomHudEvent);

        // Set fusion mode value
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oControl_Step_0"), "mod_fusion = 0", $"mod_fusion = {(seedObject.Patches.FusionMode ? 1 : 0)}");
        
        // Display Seed hash
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oGameSelMenu_Draw_0"), $"""
            draw_set_font(global.fontGUI2)
            draw_set_halign(fa_center)
            draw_cool_text(160, 5, "{seedObject.Identifier.RDVVersion} - {seedObject.Identifier.PatcherVersion}", c_black, c_white, c_white, 1)
            draw_cool_text(160, 15, "{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})", c_black, c_white, c_white, 1)
            draw_set_halign(fa_left)
            """);

        // Set option on whether supers can destroy missile doors
        if (seedObject.Patches.CanUseSupersOnMissileDoors)
            ReplaceGMLInCode(characterVarsCode, "global.canUseSupersOnMissileDoors = 0", "global.canUseSupersOnMissileDoors = 1");
        
        // TODO: For the future, with room rando, go through each door and modify where it leads to
            
        // Hints
        // Ice Beam Hints
        // Make log in lab always appear
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a7b04A_Create"), "oControl.chozomessage >= 10", "true");
        // Fix hint dissappearing when visiting room right after baby scan
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oChozoLogMarker_Step_0"), "instance_exists(oNotification)", "instance_exists(oNotification) && oNotification.log == 1");
        // Change text to be hint
        AppendGMLInCode(gmData.Code.ByName("gml_Script_load_logs_list"), $"lbl[44] = \"Ice Beam Hint\"; txt[44, 0] = \"{seedObject.Hints[HintLocationEnum.ChozoLabs]}\"; pic[44, 0] = bgLogImg44B");
        // Remove second scanning
        ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a0h01_Create"), "scan_log(44, get_text(\"Misc\", \"Translation\"), 180, 1)", "if (false) {}");
            
        // Septogg hints
        // Prep work:
        // Increase log array size to 100
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_sv6_add_logs"), "repeat (50)", "repeat (100)");
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_sv6_get_logs"), "repeat (50)", "repeat (100)");
        // Replace reset lists with simplified new array range
        SubstituteGMLCode(gmData.Code.ByName("gml_Script_reset_logs"), """
            var i = 99
            repeat (100)
            {
                global.log[i] = 0
                i -= 1
            }
            i = 7
            repeat (8)
            {
                global.trooperlog[i] = 0
                i -= 1
            }
            global.log[0] = 1
            global.log[1] = 1
            global.log[4] = 1
            global.log[5] = 1
            global.log[10] = 1
            global.log[20] = 1
            global.log[30] = 1
            """);
        //Another array extension	
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_reset_logs_list"), """
            i = 49
            repeat (50)
            """, """
            i = 99
            repeat (100)
            """);
        //Add 7 new Hint logs to the new category 5
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_create_log_category"), "dlognum = 0", """
            if (argument0 == 5)
            {
                clognum = 7
                min_log = 50
            }
            dlognum = 0
            """);
        //add categories
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oLogScreen_Create_0"), "create_log_category(category)", """
            if (global.gotolog >= 50 && global.gotolog < 60)
                category = 5
            create_log_category(category)
            """);
        //This stuff is for the menu. The array thing might not be needed, but did it anyway, increasing by the same amount as the global.log arrays.
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oLogScreenControl_Create_0"), """
            i = 59
            repeat (60)
            """, """
            i = 109
            repeat (110)
            """);
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oLogScreenControl_Create_0"), """
            j += 1
            create_log_label(cat[4])
            create_log_entry(41)
            create_log_entry(42)
            create_log_entry(43)
            create_log_entry(45)
            """, """
            if (global.log[41] > 0 || global.log[42] > 0 || global.log[43] > 0 || global.log[45] > 0)
            {
                j += 1
                create_log_label(cat[4])
                create_log_entry(41)
                create_log_entry(42)
                create_log_entry(43)
                create_log_entry(45)
            }
            if (global.log[50] > 0 || global.log[51] > 0 || global.log[52] > 0 || global.log[53] > 0 || global.log[54] > 0 || global.log[55] > 0 || global.log[56] > 0)
            {
                j += 1
                create_log_label(cat[5])
                create_log_entry(50)
                create_log_entry(51)
                create_log_entry(52)
                create_log_entry(53)
                create_log_entry(54)
                create_log_entry(55)
                create_log_entry(56)
            }
            """);
        //Defines the new septogg hint entries
        AppendGMLInCode(gmData.Code.ByName("gml_Script_load_logs_list"), $"""
            cat[5] = "DNA Hints"
            lbl[50] = "Main Caves"
            txt[50, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA0]}"
            pic[50, 0] = bgLogDNA0
            lbl[51] = "Golden Temple"
            txt[51, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA1]}"
            pic[51, 0] = bgLogDNA1
            lbl[52] = "Hydro Station"
            txt[52, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA2]}" 
            pic[52, 0] = bgLogDNA2
            lbl[53] = "Industrial Complex"
            txt[53, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA3]}"
            pic[53, 0] = bgLogDNA3
            lbl[54] = "The Tower"
            txt[54, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA4]}"
            pic[54, 0] = bgLogDNA4
            lbl[55] = "Distribution Center"
            txt[55, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA5]}"
            pic[55, 0] = bgLogDNA5
            lbl[56] = "The Nest"
            txt[56, 0] = "{seedObject.Hints[HintLocationEnum.SeptoggA6]}"
            pic[56, 0] = bgLogDNA6
            """);

        // Add wisdom septoggs into rooms
        // A0
        gmData.Rooms.ByName("rm_a0h13").GameObjects.Add(CreateRoomObject(55, 194, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a0h13_Create"), "create_log_trigger(0, 50, 55, 194, -35, 1)");
        // A1
        gmData.Rooms.ByName("rm_a1b02").GameObjects.Add(CreateRoomObject(144, 670, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a1b02_Create"), "create_log_trigger(0, 51, 144, 670, -35, 1)");
        // A2
        gmData.Rooms.ByName("rm_a2c05").GameObjects.Add(CreateRoomObject(115, 310, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a2c05_Create"), "create_log_trigger(0, 52, 115, 310, -35, 1)");
        // A3
        gmData.Rooms.ByName("rm_a3b10").GameObjects.Add(CreateRoomObject(768, 396, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a3b10_Create"), "create_log_trigger(0, 53, 768, 396, -35, 1)");
        // A4
        gmData.Rooms.ByName("rm_a4h03").GameObjects.Add(CreateRoomObject(88, 523, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a4h03_Create"), "create_log_trigger(0, 54, 88, 523, -35, 1)");
        // A5
        gmData.Rooms.ByName("rm_a5c17").GameObjects.Add(CreateRoomObject(246, 670, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a5c17_Create"), "create_log_trigger(0, 55, 246, 670, -35, 1)");
        // A6
        gmData.Rooms.ByName("rm_a6b02").GameObjects.Add(CreateRoomObject(240, 400, oWisdomSeptogg));
        AppendGMLInCode(gmData.Code.ByName("gml_Room_rm_a6b02_Create"), "create_log_trigger(0, 56, 240, 400, -35, 1)");
        
        // A0
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_09"), "global.map[41, 24] = \"2201100\"", "global.map[41, 24] = \"22011W0\"");
        
        // A1
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_12"), "global.map[58, 16] = \"0212200\"", "global.map[58, 16] = \"02122W0\"");

        // A2
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_04"), "global.map[24, 26] = \"0101200\"", "global.map[24, 26] = \"01012W0\"");
        
        // A3
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_14"), "global.map[63, 28] = \"0011200\"", "global.map[63, 28] = \"00112W0\"");
        
        // A4
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_05"), "global.map[32, 29] = \"0021200\"", "global.map[32, 29] = \"00212W0\"");

        // A5
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_16"), "global.map[69, 45] = \"0112300\"", "global.map[69, 45] = \"01123W0\"");
        
        // A6
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_03"), "global.map[20, 40] = \"0112100\"", "global.map[20, 40] = \"01121W0\"");
        
        // Ice
        ReplaceGMLInCode(gmData.Code.ByName("gml_Script_map_init_01"), "global.map[8, 22] = \"1210300\"", "global.map[8, 22] = \"12103W0\"");
        
        // Pipe rando
        // TODO: optimization could be made here, by letting rdv provide the room where the instance id is, thus not neeeding to crawl over every room.
        // TODO: for this (And for entrance rando) i need to go through each room, and set the correct global.darkness, global.water and music value.
        foreach (var pipe in seedObject.PipeObjects)
        {
            foreach (var room in gmData.Rooms)
            {
                foreach (var gameObject in room.GameObjects)
                {
                    if (gameObject.InstanceID != pipe.Key) continue;

                    AppendGMLInCode(gameObject.CreationCode, $"targetx = {pipe.Value.XPosition}; targety = {pipe.Value.YPosition}; targetroom = {pipe.Value.Room};");
                }
            }
        }
        
        // Add patch to see room names on minimap
        StringBuilder dsMapCordBuilder = new StringBuilder("room_names_coords = ds_map_create()");
        foreach ((string key, RoomObject value) in seedObject.RoomObjects)
        {
            foreach (Coordinate coord in value.MinimapData)
            {
                dsMapCordBuilder.Append($"ds_map_add(room_names_coords, \"{coord.X}|{coord.Y}\", \"{value.RegionName} - {value.DisplayName}\");\n");
            }
        }
        string DSMapCoordRoomname = dsMapCordBuilder.ToString();
        AppendGMLInCode(gmData.Code.ByName("gml_Object_oSS_Fg_Create_0"), DSMapCoordRoomname);
        ReplaceGMLInCode(gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0"), """
            draw_text((view_xview[0] + 161), ((view_yview[0] + 30) - rectoffset), maptext)
            draw_set_color(c_white)
            draw_text((view_xview[0] + 160), ((view_yview[0] + 29) - rectoffset), maptext)
        ""","""
            draw_set_font(global.fontMenuSmall2)
            var titleText = maptext;
            if (instance_exists(oMapCursor) && oMapCursor.active && oMapCursor.state == 1)
            {
                titleText = ds_map_find_value(room_names_coords, string(global.mapmarkerx) + "|" + string(global.mapmarkery));
                if (titleText == undefined)
                    titleText = maptext;
            }
            draw_text((view_xview[0] + 161), ((view_yview[0] + 29) - rectoffset), titleText);
            draw_set_color(c_white);
            draw_text((view_xview[0] + 160), ((view_yview[0] + 28) - rectoffset), titleText);
        """);
        var ssFGDestroyCode = new UndertaleCode() { Name = gmData.Strings.MakeString("gml_Object_oSS_Fg_Destroy_0") };
        SubstituteGMLCode(ssFGDestroyCode, "ds_map_destroy(room_names_coords)");
        gmData.Code.Add(ssFGDestroyCode);
        var ssFGCollisionList = gmData.GameObjects.ByName("oSS_Fg").Events[1];
        var ssFGAction = new UndertaleGameObject.EventAction();
        ssFGAction.CodeId = ssFGDestroyCode;
        var ssFGEvent = new UndertaleGameObject.Event();
        ssFGEvent.EventSubtype = 0;
        ssFGEvent.Actions.Add(ssFGAction);
        ssFGCollisionList.Add(ssFGEvent);


        // TODO: rewrite log rendering to have color
        
        // Write back to disk
        using (FileStream fs = new FileInfo(outputAm2rPath).OpenWrite())
        {
            UndertaleIO.Write(fs, gmData, Console.WriteLine);
        }
    }
}