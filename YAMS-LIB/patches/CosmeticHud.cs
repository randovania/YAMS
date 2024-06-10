using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class CosmeticHud
{
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
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight), seedObject.Cosmetics.EtankHUDRotation));
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
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight), seedObject.Cosmetics.HealthHUDRotation));
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
                tupleList.Add(new Tuple<Rectangle, int>(new Rectangle(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight), seedObject.Cosmetics.DNAHUDRotation));
                if (!wasInDict)
                    textureDict.Add(texture.TexturePage, null);

                textureDict[texture.TexturePage] = tupleList;}
        }

        foreach ((var texturePage, var rectangles) in textureDict)
        {
            RotateTextureAndSaveToTexturePage(texturePage, rectangles);
        }
    }
}
