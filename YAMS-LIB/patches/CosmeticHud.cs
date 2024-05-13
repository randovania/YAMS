using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class CosmeticHud
{
    static void RotateTextureAndSaveToTexturePage(int rotation, UndertaleTexturePageItem texture)
    {
        using Image texturePage = Image.Load(texture.TexturePage.TextureData.TextureBlob);
        texturePage.Mutate(im => im.Hue(rotation, new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight)));

        using MemoryStream ms = new MemoryStream();
        texturePage.Save(ms, PngFormat.Instance);
        texture.TexturePage.TextureData.TextureBlob = ms.ToArray();
    }


    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
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
    }
}
