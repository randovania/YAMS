using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.qol;

public class RoomFeatureMapText
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext)
    {
        gmData.Code.ByName("gml_Object_oSS_Fg_Create_0").ReplaceGMLInCode( "tip2text = get_text(\"Subscreen\", \"Marker_Tip\")", "tip2text = \"| - Marker & Room Names\"");
    }
}
