using System.Globalization;
using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using SixLabors.ImageSharp.Formats.Png;

namespace YAMS_CLI // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const uint ThothBridgeLeftDoorID = 400000;
            const uint ThothBridgeRightDoorID = 400001;
            const uint A2WaterTurbineLeftDoorID = 400002;
            
            // Change this to not have to deal with floating point madness
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            
            var seedObject = JsonSerializer.Deserialize<SeedObject>(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "/example.json"));
            
            // TODO: lots of sanity checking
            
            // TODO: make insanity save stations enabled again by using jes' code
            
            // TODO: have some "FOOL!" thing.
            
            // Read 1.5.x data
            var debug = true;
            string am2rPath = "";
            string outputAm2rPath = "";
            if (debug)
            {
                am2rPath = @"/home/narr/Dokumente/am2r 1.5.5/assets/game.unx_older";
                outputAm2rPath = @"/home/narr/Dokumente/am2r 1.5.5/assets/game.unx";
            }
            else
            {
                Console.WriteLine("Please enter the full path to your 1.5.5 data.win");
                am2rPath = Console.ReadLine();
                Console.WriteLine("Please enter the output path where you want the randomized game to be");
                outputAm2rPath = Console.ReadLine();
            }
            var gmData = new UndertaleData();
            using (FileStream fs = new FileInfo(am2rPath).OpenRead())
            {
                gmData = UndertaleIO.Read(fs);
            }

            var decompileContext = new GlobalDecompileContext(gmData, false);

            void ReplaceGMLInCode(UndertaleCode code, string textToReplace, string replacementText, bool ignoreErrors = false)
            {
                var codeText = Decompiler.Decompile(code, decompileContext);
                if (!codeText.Contains(textToReplace) && !ignoreErrors)
                    throw new ApplicationException($"The text \"{textToReplace}\" was not found in \"{code.Name.Content}\"!");
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

            // Import new Sprites
            // TODO: add a bunch of sprites that'll be added into the future
            Dictionary<string, int> nameToPageItemDict = new Dictionary<string, int>();
            const int pageDimension = 256;
            int lastUsedX = 0, lastUsedY = 0, currentShelfHeight = 0;
            var newTexturePage = new Image<Rgba32>(pageDimension, pageDimension);
            var utTexturePage = new UndertaleEmbeddedTexture();
            utTexturePage.TextureHeight = utTexturePage.TextureWidth = pageDimension;
            gmData.EmbeddedTextures.Add(utTexturePage);
            foreach (var filePath in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/Sprites"))
            {
                var sprite = Image.Load(filePath);
                currentShelfHeight = Math.Max(currentShelfHeight, sprite.Height);
                if (lastUsedX + sprite.Width > pageDimension)
                {
                    lastUsedX = 0;
                    lastUsedY += currentShelfHeight;
                    currentShelfHeight = sprite.Height + 1; // One pixel padding
                }

                if (lastUsedY + sprite.Height > pageDimension)
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
                nameToPageItemDict.Add(Path.GetFileNameWithoutExtension(filePath), gmData.TexturePageItems.Count-1);
            }
            using (var ms = new MemoryStream())
            {
                newTexturePage.Save(ms, PngFormat.Instance);
                utTexturePage.TextureData = new UndertaleEmbeddedTexture.TexData() { TextureBlob = ms.ToArray() };
            }
            
            
            gmData.Sprites.ByName("sGUIMissile").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIMissile"]]});
            gmData.Sprites.ByName("sGUISMissile").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sGUISMissile"]]});
            gmData.Sprites.ByName("sGUIPBomb").Textures.Add(new UndertaleSprite.TextureEntry() {Texture = gmData.TexturePageItems[nameToPageItemDict["sGUIPBomb"]]});
            
            
            // TODO: double check margins in every sprite
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
                Name = gmData.Strings.MakeString("sItemUnknown"), Height = 16, Width = 16, MarginRight = 14, MarginBottom = 15, OriginX = 0, OriginY = 16,
                Textures =
                {
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_1"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_2"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_3"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_4"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_5"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_6"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_7"]] },
                    new UndertaleSprite.TextureEntry() {Texture =  gmData.TexturePageItems[nameToPageItemDict["sItemUnknown_8"]] },
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

            // For pause menu, draw now the same as equipment menu because doing determining what max total health/missiles/etc. are would be spoilery and insane to figure out
            var ssDraw = gmData.Code.ByName("gml_Object_oSS_Fg_Draw_0");
            ReplaceGMLInCode(ssDraw, "(string(global.etanks) + \"/10\")", "( string(global.playerhealth) + \"/\" + string(global.maxhealth) )");
            ReplaceGMLInCode(ssDraw, "(string(global.mtanks) + \"/44\")", "( string(global.missiles) + \"/\" + string(global.maxmissiles) )");
            ReplaceGMLInCode(ssDraw, "(string(global.stanks) + \"/10\")", "( string(global.smissiles) + \"/\" + string(global.maxsmissiles) )");
            ReplaceGMLInCode(ssDraw, " (string(global.ptanks) + \"/10\")", "( string(global.pbombs) + \"/\" + string(global.maxpbombs) )");
            foreach (var code in new[] {ssDraw.Name.Content, "gml_Script_scr_SubScrTop_swap", "gml_Script_scr_SubScrTop_swap2"})
            {
                ReplaceGMLInCode(gmData.Code.ByName(code), "global.stanks > 0", "true");
                ReplaceGMLInCode(gmData.Code.ByName(code), "global.ptanks > 0", "true");
            }
            
            // Fix plasma chamber having a missile door instead of normal after tester dead
            ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a4a09_6582_Create"), "lock = 1", "lock = 0;");
            
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

            // Lock these blocks behind a setting because they can make for some interesting changes
            ReplaceGMLInCode(gmData.Code.ByName("gml_Room_rm_a0h07_Create"), 
                "if (oControl.mod_purerandombool == 1 || oControl.mod_splitrandom == 1 || global.gamemode == 2)", 
                $"if ({(seedObject.Patches.RemoveGraveGrottoBlocks ? "true" : "false")})");

            // enable randomizer to be always on
            var newGameCode = gmData.Code.ByName("gml_Script_scr_newgame");
            ReplaceGMLInCode(newGameCode,"oControl.mod_randomgamebool = 0", "oControl.mod_randomgamebool = 1");
            
            // Fix local metroids
            ReplaceGMLInCode(newGameCode, "global.monstersleft = 47", "global.monstersleft = 47; global.monstersarea = 38");

            // Add main (super) missile / PB launcher
            // missileLauncher, SMissileLauncher, PBombLauncher
            // also add an item for them + amount of expansions they give
            var characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
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
            var drawGuiCode = gmData.Code.ByName("gml_Script_draw_gui");
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
            
            // TODO: make new variables for the rest of used events like breaking blocks etc.
            // Have new variables for certain events because they are easier to debug via a switch than changing a ton of values
            PrependGMLInCode(characterVarsCode, "global.septoggHelpers = 0; global.skipCutscenes = 0;");
            
            // Set geothermal reactor to always be exploded
            AppendGMLInCode(characterVarsCode, "global.event[203] = 9");
            
            // Move Geothermal PB to big shaft
            AppendGMLInCode(gmData.Rooms.ByName("rm_a4b02a").CreationCodeId, "instance_create(272, 400, scr_itemsopen(oControl.mod_253));");
            ReplaceGMLInCode(gmData.Rooms.ByName("rm_a4b02b").CreationCodeId, "instance_create(314, 192, scr_itemsopen(oControl.mod_253))", "");
            
            // Set lava state and the metroid scanned events
            AppendGMLInCode(characterVarsCode, "global.lavastate = 7; global.event[4] = 1; global.event[56] = 1;" +
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
            ReplaceGMLInCode(missileCharacterEvent, "popup_text(get_text(\"Notifications\", \"MissileTank\"))", "popup_text(text1)");
            
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
            ReplaceGMLInCode(superMissileCharacterEvent, "popup_text(get_text(\"Notifications\", \"SuperMissileTank\"))", "popup_text(text1)");
            
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
            ReplaceGMLInCode(pBombCharacterEvent, "popup_text(get_text(\"Notifications\", \"PowerBombTank\"))", "popup_text(text1)");
            
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
            ReplaceGMLInCode(eTankCharacterEvent, "popup_text(get_text(\"Notifications\", \"EnergyTank\"))", "popup_text(text1)");
            
            // Decouple Major items from item locations
            PrependGMLInCode(characterVarsCode, "global.hasBombs = 0; global.hasPowergrip = 0; global.hasSpiderball = 0; global.hasJumpball = 0; global.hasHijump = 0;" +
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
                                                        c.Name.Content.Contains('4')) || c.Name.Content == "gml_Room_rm_a3b08_Create"))
                ReplaceGMLInCode(code, "global.item[4]", "global.hasHijump");
            // Varia
            // TODO!!! gml_Script_characterStepEvent! needs fixing first, as otherwise it'll crash with current utmt setup
            var subscreenSuitDraw = gmData.Code.ByName("gml_Object_oSubScreenSuit_Draw_0");
            ReplaceGMLInCode(subscreenSuitDraw, "global.item[5]", "global.hasVaria");
            ReplaceGMLInCode(subscreenMenuStep, "global.item[5] == 0", "!global.hasVaria");
            foreach(var code in new[]{"gml_Script_damage_player", "gml_Script_damage_player_push", "gml_Script_damage_player_knockdown", "gml_Object_oQueenHead_Step_0"})
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
            var startNewGame = gmData.Code.ByName("gml_Script_start_new_game");
            ReplaceGMLInCode(startNewGame, """
                global.start_room = 21
                global.save_x = 3408
                global.save_y = 1184
                """, "global.save_room = 0; set_start_location();");
            
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
            
            var sv6loadDetails = gmData.Code.ByName("gml_Script_sv6_load_details");
            ReplaceGMLInCode(sv6loadDetails, "V7.0", "RDV V8.0");
            ReplaceGMLInCode(sv6loadDetails, "sv6_get_seed(fid)", "sv6_get_seed(fid); file_text_readln(fid);");

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
                    // TODO: what if more than 100 energy per tank? should cap?
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
                    // TODO: make a general arm weapon thing. see todo at very top somwewhere
                    case ItemEnum.Power:
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
            ReplaceGMLInCode(startNewGame, "global.save_room = 0", $"global.save_room = {seedObject.StartingLocation.SaveRoom}");
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

            // TODO: proper damage values, add to test
            
            // Door locks
            // TODO:
            
            // TODO: make doors automatically free their event when passing through them!
            
            // Modify every location item, to give the wished item, spawn the wished text and the wished sprite
            foreach ((var pickupName, PickupObject pickup) in seedObject.PickupObjects)
            {
                var gmObject = gmData.GameObjects.ByName(pickupName);
                gmObject.Sprite = gmData.Sprites.ByName(pickup.SpriteDetails.Name);
                // First 0 is for creation event
                var createCode = gmObject.Events[0][0].Actions[0].CodeId;
                AppendGMLInCode(createCode, $"image_speed = {pickup.SpriteDetails.Speed}; text1 = \"{pickup.Text.Header}\"; text2 = \"{pickup.Text.Description}\";" +
                                            $"btn1_name = \"\"; btn2_name = \"\";");
                
                // First 4 is for Collision event
                var collisionCode = gmObject.Events[4][0].Actions[0].CodeId;
                var collisionCodeToBe = pickup.ItemEffect switch
                {
                    
                    ItemEnum.EnergyTank => "scr_energytank_character_event()",
                    ItemEnum.MissileExpansion => "scr_missile_character_event()",
                    ItemEnum.MissileLauncher => "event_inherited(); if (active) " +
                                                "{{ global.missileLauncher = 1; global.maxmissiles += global.missileLauncherExpansion; global.missiles = global.maxmissiles; }}",
                    ItemEnum.SuperMissileExpansion => "scr_supermissile_character_event()",
                    ItemEnum.SuperMissileLauncher => "event_inherited(); if (active) " +
                                                "{{ global.SMissileLauncher = 1; global.maxsmissiles += global.SMissileLauncherExpansion; global.smissiles = global.maxsmissiles; }}",
                    ItemEnum.PBombExpansion=> "scr_powerbomb_character_event()",
                    ItemEnum.PBombLauncher => "event_inherited(); if (active) " +
                                                     "{{ global.PBombLauncher = 1; global.maxpbombs += global.PBombLauncherExpansion; global.pbombs = global.maxpbombs; }}",
                    ItemEnum.Bombs => "btn1_name = \"Fire\"; event_inherited(); if (active) {{ global.bomb = 1; global.hasBombs = 1; }}",
                    ItemEnum.Powergrip =>"event_inherited(); if (active) {{ global.powergrip = 1; global.hasPowergrip = 1; }}",
                    ItemEnum.Spiderball => "btn1_name = \"Aim\"; event_inherited(); if (active) {{ global.spiderball = 1; global.hasSpiderball = 1; }}",
                    ItemEnum.Springball => "btn1_name = \"Jump\"; event_inherited(); if (active) {{ global.jumpball = 1; global.hasJumpball = 1; }}",
                    ItemEnum.Screwattack => "event_inherited(); if (active) {{ global.screwattack = 1; global.hasScrewattack = 1; }} with (oCharacter) sfx_stop(spinjump_sound);",
                    ItemEnum.Varia => """
                        event_inherited()
                        global.hasVaria = 1;
                        global.SuitChange = 1;
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
                        global.SuitChange = 1;
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
    global.maxmissiles += {seedObject.PickupObjects.First(p => p.Value.ItemEffect == ItemEnum.MissileExpansion).Value.Quantity}
""");
            
            ReplaceGMLInCode(superMissileCharacterEvent, """
    if (global.difficulty < 2)
        global.maxsmissiles += 2
    if (global.difficulty == 2)
        global.maxsmissiles += 1
""",$"""
    global.maxsmissiles += {seedObject.PickupObjects.First(p => p.Value.ItemEffect == ItemEnum.SuperMissileExpansion).Value.Quantity}
""");
            
            ReplaceGMLInCode(pBombCharacterEvent, """
    if (global.difficulty < 2)
        global.maxpbombs += 2
    if (global.difficulty == 2)
        global.maxpbombs += 1
""",$"""
    global.maxpbombs += {seedObject.PickupObjects.First(p => p.Value.ItemEffect == ItemEnum.PBombExpansion).Value.Quantity}
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
                        "global.hasBombs && global.septoggHelpers", true);
            }
            ReplaceGMLInCode(gmData.Code.ByName("gml_RoomCC_rm_a0h25_4105_Create"), "else if (global.hasBombs == 1 || global.hasSpiderball == 1 || global.hasSpacejump == 1)",
                "else if (!global.septoggHelpers)");
            
            
            // TODO: ability to turn off the random room geometry changes!

            // TODO: instead of implementing power beam, implement arm cannon or something. is less insane than refactoring samus state machine and menu
            
            // TODO: ability to reset regentime for bomb blocks!

            // TODO: ability to skip cutscenes!
            if (seedObject.Patches.SkipCutscenes)
            {
                
            }
            
            // TODO: ability to skip mines crystal
            
            // Go through every room's creation code, and set popup_text(room_name)
            // TODO: make this an option
            foreach ((var internalName, var room) in seedObject.RoomObjects)
            {
                var roomName = room.DisplayName;
                if (String.IsNullOrWhiteSpace(roomName)) continue;
                
                var gmRoom = gmData.Rooms.ByName(internalName);
                AppendGMLInCode(gmRoom.CreationCodeId, $"popup_text(\"{roomName}\")");
            }
            
            // Display Seed hash
            AppendGMLInCode(gmData.Code.ByName("gml_Object_oGameSelMenu_Draw_0"), $"""
            draw_set_font(global.fontGUI2)
            draw_set_halign(fa_center)
            draw_cool_text(160, 10, "{seedObject.Identifier.WordHash} ({seedObject.Identifier.Hash})", c_black, c_white, c_white, 1)
            draw_set_halign(fa_left)
            """);

            // For room rando, go through each door and modify where it leads to
            // TODO: Implement this whenever room rando gets done.
            
            
            // Write back to disk
            using (FileStream fs = new FileInfo(outputAm2rPath).OpenWrite())
            {
                UndertaleIO.Write(fs, gmData, Console.WriteLine);
            }
        }
    }
}