using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class FixOverlappingSongs
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        // Fix annoying overlapping songs when fanfare is long song.
        gmData.Code.ByName("gml_Object_oMusicV2_Alarm_0").PrependGMLInCode("if (sfx_isplaying(musFanfare)) audio_stop_sound(musFanfare)");
        gmData.Code.ByName("gml_Script_mus_intro_fanfare").ReplaceGMLInCode("alarm[0] = 60", "alarm[0] = 330");
        // TODO: fix metroid fanfare
    }
}
