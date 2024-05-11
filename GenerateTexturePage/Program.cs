// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Text.Json;
using GenerateTexturePage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var yamsLibDirectory = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<ProjectCompileDirectoryAttribute>()
    .ProjectCompileDirectory;
Console.WriteLine($"Identified YAMS-LIB directory as: {yamsLibDirectory}");

const int pageDimension = 1024;
var newTexturePage = new Image<Rgba32>(pageDimension, pageDimension);
var textureInfo = new List<PageItem>();
int lastUsedX = 0, lastUsedY = 0, currentShelfHeight = 0;

// TODO: Now that we have a seperate project for generating the pages, we should think about making this more optimized.
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
        if (String.IsNullOrWhiteSpace(extension) || extension == ".md" || extension == ".txt" || extension == ".gitignore" || extension == ".json")
            continue;
        if (filePath.EndsWith("texturepage.png"))
            continue;

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
        PageItem pageItem = new PageItem();
        pageItem.X = (ushort)xCoord;
        pageItem.Y = (ushort)yCoord;
        pageItem.Width = (ushort)sprite.Width;
        pageItem.Height = (ushort)sprite.Height;
        pageItem.Name = Path.GetFileNameWithoutExtension(filePath);
        textureInfo.Add(pageItem);
        lastUsedX += sprite.Width + 1; //One pixel padding
    }
}

AddAllSpritesFromDir(yamsLibDirectory + "/sprites");
newTexturePage.SaveAsPng(yamsLibDirectory + "/sprites/texturepage.png");
File.WriteAllText(yamsLibDirectory + "/sprites/texturepageiteminfo.json", JsonSerializer.Serialize(textureInfo));

Console.WriteLine("Written texture page and texture page info.");
