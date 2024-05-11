using System.Text.Json.Serialization;
namespace YAMS_LIB;

// Needs to be synced with GenerateTexturePage
public class PageItem
{
    [JsonInclude]
    public string Name = "";
    [JsonInclude]
    public ushort X;
    [JsonInclude]
    public ushort Y;
    [JsonInclude]
    public ushort Width;
    [JsonInclude]
    public ushort Height;
}
