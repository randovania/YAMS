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
    LockedMissile = Missile,
    [EnumMember(Value = "Super Missiles")]
    SuperMissile,
    [EnumMember(Value = "Locked Super Missiles")]
    LockedSuperMissile = SuperMissile,
    [EnumMember(Value = "Power Bombs")]
    PBomb,
    [EnumMember(Value = "Locked Power Bombs")]
    LockedPBomb = PBomb,
    
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