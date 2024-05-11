using System.Text.Json.Serialization;



namespace GenerateTexturePage;

// Needs to be synced with YAMS-LIB
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
