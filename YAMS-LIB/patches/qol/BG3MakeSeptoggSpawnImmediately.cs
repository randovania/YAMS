using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static YAMS_LIB.ExtensionMethods;

namespace YAMS_LIB.patches.qol;

public class BG3MakeSeptoggSpawnImmediately
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        var gammaChecker = new UndertaleGameObject() { Name = gmData.Strings.MakeString("BG3GammaSeptoggChecker"), Sprite = gmData.Sprites.ByName("ssEDFX")};
        gammaChecker.EventHandlerFor(EventType.Step, gmData).SubstituteGMLCode(
            """
            if ((!instance_exists(oMGammaA3BTrigger)) && (!instance_exists(oMGamma)))
            {
                with (oElderSeptogg)
                {
                    xstart = 224
                    ystart = 352
                }
                instance_destroy()
            }
            """
            );
        gmData.GameObjects.Add(gammaChecker);

        gmData.Rooms.ByName("rm_a3b08").GameObjects.Add(CreateRoomObject(0, 0, gammaChecker));
    }
}
