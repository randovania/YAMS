using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YAMS_LIB;

// TODO: There are lots of horrible names in here
public class SeedObject
{
    [JsonInclude]
    [JsonPropertyName("configuration_identifier")]
    public ConfigurationIdentifier Identifier;
    
    [JsonInclude]
    [JsonPropertyName("game_patches")]
    public GamePatches Patches;

    [JsonInclude] [JsonPropertyName("door_locks")]
    public Dictionary<uint, DoorLock> DoorLocks;
    
    [JsonInclude]
    [JsonPropertyName("pickups")]
    public Dictionary<string, PickupObject> PickupObjects = new Dictionary<string, PickupObject>();
    
    [JsonInclude]
    [JsonPropertyName("rooms")]
    public Dictionary<string, RoomObject> RoomObjects = new Dictionary<string, RoomObject>();
    
    [JsonInclude]
    [JsonPropertyName("pipes")]
    public Dictionary<uint, PipeObject> PipeObjects = new Dictionary<uint, PipeObject>();

    [JsonInclude]
    [JsonPropertyName("starting_items")]
    public Dictionary<ItemEnum, int> StartingItems = new Dictionary<ItemEnum, int>();

    [JsonInclude]
    [JsonPropertyName("starting_location")]
    public StartingLocationObject StartingLocation;
    
    [JsonInclude]
    [JsonPropertyName("hints")]
    public Dictionary<HintLocationEnum, string> Hints = new Dictionary<HintLocationEnum, string>();
    
    [JsonInclude]
    [JsonPropertyName("cosmetics")]
    public GameCosmetics Cosmetics;
}

public class DoorLock
{
    [JsonInclude]
    [JsonPropertyName("lock")]
    public DoorLockType Lock;
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum DoorLockType
{
    [EnumMember(Value = "Normal Door")]
    Normal,
    [EnumMember(Value = "Missile Door")]
    Missile,
    [EnumMember(Value = "Super Missile Door")]
    SuperMissile,
    [EnumMember(Value = "Power Bomb Door")]
    PBomb,
    [EnumMember(Value = "Temporarily Locked Door")]
    TempLocked,
    [EnumMember(Value = "Charge Beam Door")]
    Charge,
    [EnumMember(Value = "Wave Beam Door")]
    Wave,
    [EnumMember(Value = "Spazer Beam Door")]
    Spazer,
    [EnumMember(Value = "Plasma Beam Door")]
    Plasma,
    [EnumMember(Value = "Ice Beam Door")]
    Ice,
    [EnumMember(Value = "Bomb Door")]
    Bomb,
    [EnumMember(Value = "Spider Ball Door")]
    Spider,
    [EnumMember(Value = "Screw Attack Door")]
    Screw,
    [EnumMember(Value = "Tower Energy Restored Door")]
    TowerEnabled,
    [EnumMember(Value = "Tester-Locked Door")]
    TesterDead,
    [EnumMember(Value = "Guardian-Locked Door")]
    GuardianDead,
    [EnumMember(Value = "Arachnus-Locked Door")]
    ArachnusDead,
    [EnumMember(Value = "Torizo-Locked Door")]
    TorizoDead,
    [EnumMember(Value = "Serris-Locked Door")]
    SerrisDead,
    [EnumMember(Value = "Genesis-Locked Door")]
    GenesisDead,
    [EnumMember(Value = "Queen-Locked Door")]
    QueenDead,
    [EnumMember(Value = "Distribution Center Energy Restored Door")]
    EMPActivated,
    [EnumMember(Value = "Golden Temple EMP Door")]
    EMPA1,
    [EnumMember(Value = "Hydro Station EMP Door")]
    EMPA2,
    [EnumMember(Value = "Industrial Complex EMP Door")]
    EMPA3,
    [EnumMember(Value = "Distribution Center EMP Ball Introduction EMP Door")]
    EMPA5Tutorial,
    [EnumMember(Value = "Distribution Center Robot Home EMP Door")]
    EMPA5RobotHome,
    [EnumMember(Value = "Distribution Center Energy Distribution Tower East EMP Door")]
    EMPA5NearZeta,
    [EnumMember(Value = "Distribution Center Bullet Hell Room Access EMP Door")]
    EMPA5BulletHell,
    [EnumMember(Value = "Distribution Center Pipe Hub Access EMP Door")]
    EMPA5PipeHub,
    [EnumMember(Value = "Distribution Center Exterior East Access EMP Door")]
    EMPA5RightExterior,
    [EnumMember(Value = "Locked Door")]
    Locked,
    [EnumMember(Value = "Hydro Station Water Turbine")]
    A2WaterTurbine,
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum HintLocationEnum
{
    [EnumMember(Value = "septogg_a0")]
    SeptoggA0,
    [EnumMember(Value = "septogg_a1")]
    SeptoggA1,
    [EnumMember(Value = "septogg_a2")]
    SeptoggA2,
    [EnumMember(Value = "septogg_a3")]
    SeptoggA3,
    [EnumMember(Value = "septogg_a4")]
    SeptoggA4,
    [EnumMember(Value = "septogg_a5")]
    SeptoggA5,
    [EnumMember(Value = "septogg_a6")]
    SeptoggA6,
    [EnumMember(Value = "chozo_labs")]
    ChozoLabs,
}

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum RoomNameHudEnum
{
    [EnumMember(Value = "NEVER")]
    Never,
    [EnumMember(Value = "WITH_FADE")]
    OnEntry,
    [EnumMember(Value = "ALWAYS")]
    Always,
}

public class GameCosmetics
{
    [JsonInclude]
    [JsonPropertyName("show_unexplored_map")]
    public bool ShowUnexploredMap;
    
    [JsonInclude]
    [JsonPropertyName("unveiled_blocks")]
    public bool UnveilBlocks;
    
    [JsonInclude]
    [JsonPropertyName("health_hud_rotation")]
    public int HealthHUDRotation;
    
    [JsonInclude]
    [JsonPropertyName("etank_hud_rotation")]
    public int EtankHUDRotation;
    
    [JsonInclude]
    [JsonPropertyName("dna_hud_rotation")]
    public int DNAHUDRotation;
    
    [JsonInclude]
    [JsonPropertyName("room_names_on_hud")]
    public RoomNameHudEnum RoomNameHud;
    
    [JsonInclude]
    [JsonPropertyName("music_shuffle")]
    public Dictionary<string, string> MusicShuffleDict = new Dictionary<string, string>();
}

public class GamePatches
{
    [JsonInclude]
    [JsonPropertyName("septogg_helpers")]
    public bool SeptoggHelpers;
    
    [JsonInclude]
    [JsonPropertyName("change_level_design")]
    public bool ChangeLevelDesign;

    [JsonInclude] [JsonPropertyName("grave_grotto_blocks")]
    public bool GraveGrottoBlocks;
    
    [JsonInclude]
    [JsonPropertyName("respawn_bomb_blocks")]
    public bool RespawnBombBlocks;
    
    [JsonInclude]
    [JsonPropertyName("skip_cutscenes")]
    public bool SkipCutscenes;
    
    [JsonInclude]
    [JsonPropertyName("skip_save_cutscene")]
    public bool SkipSaveCutscene;
    
    [JsonInclude]
    [JsonPropertyName("skip_item_cutscenes")]
    public bool SkipItemFanfares;
    
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
    
    [JsonInclude]
    [JsonPropertyName("fusion_mode")]
    public bool FusionMode;

    [JsonInclude] 
    [JsonPropertyName("supers_on_missile_doors")]
    public bool CanUseSupersOnMissileDoors = true;
    
    [JsonInclude]
    [JsonPropertyName("nest_pipes")]
    public bool NestPipes;
    
    [JsonInclude]
    [JsonPropertyName("softlock_prevention_blocks")]
    public bool SoftlockPrevention;
    
    [JsonInclude]
    [JsonPropertyName("a3_entrance_blocks")]
    public bool A3EntranceBlocks;
    
    [JsonInclude]
    [JsonPropertyName("screw_blocks")]
    public bool ScrewPipeBlocks;
    
    [JsonInclude]
    [JsonPropertyName("sabre_designed_skippy")]
    public bool SabreSkippy;
    
    [JsonInclude]
    [JsonPropertyName("locked_missile_text")]
    public TextDetails LockedMissileText;
    
    [JsonInclude]
    [JsonPropertyName("locked_super_text")]
    public TextDetails LockedSuperText;
    
    [JsonInclude]
    [JsonPropertyName("locked_pb_text")]
    public TextDetails LockedPBombText;
}

public class ConfigurationIdentifier
{
    [JsonInclude]
    [JsonPropertyName("hash")]
    public string Hash = "Quack";
    
    [JsonInclude]
    [JsonPropertyName("word_hash")]
    public string WordHash = "Have fun";
    
    [JsonInclude]
    [JsonPropertyName("randovania_version")]
    public string RDVVersion = "Randovania";
    
    [JsonInclude]
    [JsonPropertyName("patcher_version")]
    public string PatcherVersion = "YAMS";
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
    [EnumMember(Value = "Metroid DNA 1")]
    DNA1,
    [EnumMember(Value = "Metroid DNA 2")]
    DNA2,
    [EnumMember(Value = "Metroid DNA 3")]
    DNA3,
    [EnumMember(Value = "Metroid DNA 4")]
    DNA4,
    [EnumMember(Value = "Metroid DNA 5")]
    DNA5,
    [EnumMember(Value = "Metroid DNA 6")]
    DNA6,
    [EnumMember(Value = "Metroid DNA 7")]
    DNA7,
    [EnumMember(Value = "Metroid DNA 8")]
    DNA8,
    [EnumMember(Value = "Metroid DNA 9")]
    DNA9,
    [EnumMember(Value = "Metroid DNA 10")]
    DNA10,
    [EnumMember(Value = "Metroid DNA 11")]
    DNA11,
    [EnumMember(Value = "Metroid DNA 12")]
    DNA12,
    [EnumMember(Value = "Metroid DNA 13")]
    DNA13,
    [EnumMember(Value = "Metroid DNA 14")]
    DNA14,
    [EnumMember(Value = "Metroid DNA 15")]
    DNA15,
    [EnumMember(Value = "Metroid DNA 16")]
    DNA16,
    [EnumMember(Value = "Metroid DNA 17")]
    DNA17,
    [EnumMember(Value = "Metroid DNA 18")]
    DNA18,
    [EnumMember(Value = "Metroid DNA 19")]
    DNA19,
    [EnumMember(Value = "Metroid DNA 20")]
    DNA20,
    [EnumMember(Value = "Metroid DNA 21")]
    DNA21,
    [EnumMember(Value = "Metroid DNA 22")]
    DNA22,
    [EnumMember(Value = "Metroid DNA 23")]
    DNA23,
    [EnumMember(Value = "Metroid DNA 24")]
    DNA24,
    [EnumMember(Value = "Metroid DNA 25")]
    DNA25,
    [EnumMember(Value = "Metroid DNA 26")]
    DNA26,
    [EnumMember(Value = "Metroid DNA 27")]
    DNA27,
    [EnumMember(Value = "Metroid DNA 28")]
    DNA28,
    [EnumMember(Value = "Metroid DNA 29")]
    DNA29,
    [EnumMember(Value = "Metroid DNA 30")]
    DNA30,
    [EnumMember(Value = "Metroid DNA 31")]
    DNA31,
    [EnumMember(Value = "Metroid DNA 32")]
    DNA32,
    [EnumMember(Value = "Metroid DNA 33")]
    DNA33,
    [EnumMember(Value = "Metroid DNA 34")]
    DNA34,
    [EnumMember(Value = "Metroid DNA 35")]
    DNA35,
    [EnumMember(Value = "Metroid DNA 36")]
    DNA36,
    [EnumMember(Value = "Metroid DNA 37")]
    DNA37,
    [EnumMember(Value = "Metroid DNA 38")]
    DNA38,
    [EnumMember(Value = "Metroid DNA 39")]
    DNA39,
    [EnumMember(Value = "Metroid DNA 40")]
    DNA40,
    [EnumMember(Value = "Metroid DNA 41")]
    DNA41,
    [EnumMember(Value = "Metroid DNA 42")]
    DNA42,
    [EnumMember(Value = "Metroid DNA 43")]
    DNA43,
    [EnumMember(Value = "Metroid DNA 44")]
    DNA44,
    [EnumMember(Value = "Metroid DNA 45")]
    DNA45,
    [EnumMember(Value = "Metroid DNA 46")]
    DNA46,
    
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
    [EnumMember(Value = "Hi-Jump Boots")]
    Hijump,
    [EnumMember(Value = "Varia Suit")]
    Varia,
    [EnumMember(Value = "Space Jump")]
    Spacejump,
    [EnumMember(Value = "Progressive Jump")]
    ProgressiveJump,
    [EnumMember(Value = "Speed Booster")]
    Speedbooster,
    [EnumMember(Value = "Screw Attack")]
    Screwattack,
    [EnumMember(Value = "Gravity Suit")]
    Gravity,
    [EnumMember(Value = "Progressive Suit")]
    ProgressiveSuit,
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
    Nothing,
    [EnumMember(Value = "Small Health Drop")]
    SmallHealthDrop,
    [EnumMember(Value = "Big Health Drop")]
    BigHealthDrop,
    [EnumMember(Value = "Missile Drop")]
    MissileDrop,
    [EnumMember(Value = "Super Missile Drop")]
    SuperMissileDrop,
    [EnumMember(Value = "Power Bomb Drop")]
    PBombDrop,
    [EnumMember(Value = "Flashlight")]
    Flashlight,
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
    
    [JsonInclude]
    [JsonPropertyName("region_name")]
    public string RegionName = "";
    
    [JsonInclude]
    [JsonPropertyName("minimap_data")]
    public List<Coordinate> MinimapData = new List<Coordinate>();
}

public class PipeObject
{
    [JsonInclude] 
    [JsonPropertyName("dest_x")]
    public int XPosition;

    [JsonInclude] 
    [JsonPropertyName("dest_y")]
    public int YPosition;
    
    [JsonInclude] 
    [JsonPropertyName("dest_room")]
    public string Room = "";
}

public struct Coordinate
{
    [JsonInclude] 
    [JsonPropertyName("x")]
    public int X;

    [JsonInclude] 
    [JsonPropertyName("y")]
    public int Y;
}
