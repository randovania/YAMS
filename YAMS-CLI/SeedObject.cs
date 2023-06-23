using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YAMS_CLI;

public class SeedObject
{
    [JsonInclude]
    [JsonPropertyName("configuration_identifier")]
    public ConfigurationIdentifier Identifier;
    
    [JsonInclude]
    [JsonPropertyName("game_patches")]
    public GamePatches Patches;
    
    [JsonInclude]
    [JsonPropertyName("pickups")]
    public Dictionary<string, PickupObject> PickupObjects = new Dictionary<string, PickupObject>();
    
    [JsonInclude]
    [JsonPropertyName("rooms")]
    public Dictionary<string, RoomObject> RoomObjects = new Dictionary<string, RoomObject>();

    [JsonInclude]
    [JsonPropertyName("starting_items")]
    public Dictionary<ItemEnum, int> StartingItems = new Dictionary<ItemEnum, int>();

    [JsonInclude]
    [JsonPropertyName("starting_location")]
    public StartingLocationObject StartingLocation;
}

public class GamePatches
{
    [JsonInclude]
    [JsonPropertyName("septogg_helpers")]
    public bool SeptoggHelpers;
    
    [JsonInclude]
    [JsonPropertyName("change_level_design")]
    public bool ChangeLevelDesign;

    [JsonInclude] [JsonPropertyName("remove_grave_grotto_blocks")]
    public bool RemoveGraveGrottoBlocks;
    
    [JsonInclude]
    [JsonPropertyName("respawn_bomb_blocks")]
    public bool RespawnBombBlocks;
    
    [JsonInclude]
    [JsonPropertyName("skip_cutscenes")]
    public bool SkipCutscenes;
    
    [JsonInclude]
    [JsonPropertyName("energy_per_tank")]
    public int EnergyPerTank;
    
    [JsonInclude]
    [JsonPropertyName("require_missile_launcher")]
    public bool RequireMissileLauncher;
    
    [JsonInclude]
    [JsonPropertyName("require_super_launcher")]
    public bool RequireSuperLauncher;
    
    [JsonInclude]
    [JsonPropertyName("require_pb_launcher")]
    public bool RequirePBLauncher;
    
}

public class ConfigurationIdentifier
{
    [JsonInclude]
    [JsonPropertyName("hash")]
    public string Hash = "";
    
    [JsonInclude]
    [JsonPropertyName("word_hash")]
    public string WordHash = "";
}


public class StartingLocationObject
{
    [JsonInclude]
    [JsonPropertyName("save_room")]
    public int SaveRoom;
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ItemEnum
{
    [EnumMember(Value = "Artifact1")]
    Artifact1,
    [EnumMember(Value = "Artifact2")]
    Artifact2,
    [EnumMember(Value = "Artifact3")]
    Artifact3,
    [EnumMember(Value = "Artifact4")]
    Artifact4,
    [EnumMember(Value = "Artifact5")]
    Artifact5,
    [EnumMember(Value = "Artifact6")]
    Artifact6,
    [EnumMember(Value = "Artifact7")]
    Artifact7,
    [EnumMember(Value = "Artifact8")]
    Artifact8,
    [EnumMember(Value = "Artifact9")]
    Artifact9,
    [EnumMember(Value = "Artifact10")]
    Artifact10,
    [EnumMember(Value = "Artifact11")]
    Artifact11,
    [EnumMember(Value = "Artifact12")]
    Artifact12,
    [EnumMember(Value = "Artifact13")]
    Artifact13,
    [EnumMember(Value = "Artifact14")]
    Artifact14,
    [EnumMember(Value = "Artifact15")]
    Artifact15,
    [EnumMember(Value = "Artifact16")]
    Artifact16,
    [EnumMember(Value = "Artifact17")]
    Artifact17,
    [EnumMember(Value = "Artifact18")]
    Artifact18,
    [EnumMember(Value = "Artifact19")]
    Artifact19,
    [EnumMember(Value = "Artifact20")]
    Artifact20,
    [EnumMember(Value = "Artifact21")]
    Artifact21,
    [EnumMember(Value = "Artifact22")]
    Artifact22,
    [EnumMember(Value = "Artifact23")]
    Artifact23,
    [EnumMember(Value = "Artifact24")]
    Artifact24,
    [EnumMember(Value = "Artifact25")]
    Artifact25,
    [EnumMember(Value = "Artifact26")]
    Artifact26,
    [EnumMember(Value = "Artifact27")]
    Artifact27,
    [EnumMember(Value = "Artifact28")]
    Artifact28,
    [EnumMember(Value = "Artifact29")]
    Artifact29,
    [EnumMember(Value = "Artifact30")]
    Artifact30,
    [EnumMember(Value = "Artifact31")]
    Artifact31,
    [EnumMember(Value = "Artifact32")]
    Artifact32,
    [EnumMember(Value = "Artifact33")]
    Artifact33,
    [EnumMember(Value = "Artifact34")]
    Artifact34,
    [EnumMember(Value = "Artifact35")]
    Artifact35,
    [EnumMember(Value = "Artifact36")]
    Artifact36,
    [EnumMember(Value = "Artifact37")]
    Artifact37,
    [EnumMember(Value = "Artifact38")]
    Artifact38,
    [EnumMember(Value = "Artifact39")]
    Artifact39,
    [EnumMember(Value = "Artifact40")]
    Artifact40,
    [EnumMember(Value = "Artifact41")]
    Artifact41,
    [EnumMember(Value = "Artifact42")]
    Artifact42,
    [EnumMember(Value = "Artifact43")]
    Artifact43,
    [EnumMember(Value = "Artifact44")]
    Artifact44,
    [EnumMember(Value = "Artifact45")]
    Artifact45,
    [EnumMember(Value = "Artifact46")]
    Artifact46,
    
    [EnumMember(Value = "Missile Expansion")]
    MissileExpansion,
    [EnumMember(Value = "Super Missile Expansion")]
    SuperMissileExpansion,
    [EnumMember(Value = "Power Bomb Expansion")]
    PBombExpansion,
    [EnumMember(Value = "Energy Tank")]
    EnergyTank,
    
    [EnumMember(Value = "Missiles")]
    Missile,
    [EnumMember(Value = "Locked Missiles")]
    LockedMissile,
    [EnumMember(Value = "Super Missiles")]
    SuperMissile,
    [EnumMember(Value = "Locked Super Missiles")]
    LockedSuperMissile,
    [EnumMember(Value = "Power Bombs")]
    PBomb,
    [EnumMember(Value = "Locked Power Bombs")]
    LockedPBomb,
    
    [EnumMember(Value = "Missile Launcher")]
    MissileLauncher,
    [EnumMember(Value = "Super Missile Launcher")]
    SuperMissileLauncher,
    [EnumMember(Value = "Power Bomb Launcher")]
    PBombLauncher,
    
    [EnumMember(Value = "Bombs")]
    Bombs,
    [EnumMember(Value = "Power Grip")]
    Powergrip,
    [EnumMember(Value = "Spider Ball")]
    Spiderball,
    [EnumMember(Value = "Spring Ball")]
    Springball,
    [EnumMember(Value = "Hi-Jump")]
    Hijump,
    [EnumMember(Value = "Varia Suit")]
    Varia,
    [EnumMember(Value = "Space Jump")]
    Spacejump,
    [EnumMember(Value = "Speed Booster")]
    Speedbooster,
    [EnumMember(Value = "Screw Attack")]
    Screwattack,
    [EnumMember(Value = "Gravity Suit")]
    Gravity,
    [EnumMember(Value = "Power Beam")]
    Power,
    [EnumMember(Value = "Charge Beam")]
    Charge,
    [EnumMember(Value = "Ice Beam")]
    Ice,
    [EnumMember(Value = "Wave Beam")]
    Wave,
    [EnumMember(Value = "Spazer Beam")]
    Spazer,
    [EnumMember(Value = "Plasma Beam")]
    Plasma,
    [EnumMember(Value = "Morph Ball")]
    Morphball,
    [EnumMember(Value = "Nothing")]
    Nothing
    
}

public class PickupObject
{
    [JsonInclude]
    [JsonPropertyName("item_id")]
    public string ItemID;
    [JsonInclude]
    [JsonPropertyName("sprite_details")]
    public SpriteDetails SpriteDetails;
    [JsonInclude]
    [JsonPropertyName("item_effect")]
    public ItemEnum ItemEffect;
    [JsonInclude]
    [JsonPropertyName("quantity")]
    public int Quantity;

    [JsonInclude] 
    [JsonPropertyName("text")]
    public TextDetails Text;

}

public class SpriteDetails
{
    [JsonInclude]
    [JsonPropertyName("name")]
    public string Name = "sItemUnknown";
    [JsonInclude]
    [JsonPropertyName("speed")]
    public decimal Speed;
}

public class TextDetails
{
    [JsonInclude] 
    [JsonPropertyName("header")]
    public string Header = "INVALID TEXT";
    
    [JsonInclude] 
    [JsonPropertyName("description")]
    public string Description = "INVALID DESCRIPTION";
}

public class RoomObject
{
    [JsonInclude]
    [JsonPropertyName("display_name")]
    public string DisplayName = "";
}