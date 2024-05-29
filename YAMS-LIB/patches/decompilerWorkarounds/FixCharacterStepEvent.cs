using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches.decompilerWorkarounds;

public class FixCharacterStepEvent
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        gmData.Code.ByName("gml_Script_characterStepEvent").ReplaceGMLInCode("""
                                                                                 if (yVel < 0 && state == AIRBALL)
                                                                                 {
                                                                                     if (isCollisionUpRight() == 1 && kRight == 0)
                                                                                         x -= ((1 + statetime < 2) + statetime < 4)
                                                                                     if (isCollisionUpLeft() == 1 && kLeft == 0)
                                                                                         x += ((1 + statetime < 2) + statetime < 4)
                                                                                 }
                                                                             """, """
                                                                                      if (yVel < 0 && state == AIRBALL)
                                                                                      {
                                                                                  		var st1, st2;
                                                                                  		st1 = 0
                                                                                  		st2 = 0
                                                                                  		if (statetime < 2)
                                                                                  			st1 = 1
                                                                                  		if (statetime < 4)
                                                                                  			st2 = 1
                                                                                  		if (isCollisionUpRight() == 1 && kRight == 0)
                                                                                              x -= ((1 + st1) + st2)
                                                                                          if (isCollisionUpLeft() == 1 && kLeft == 0)
                                                                                              x += ((1 + st1) + st2)
                                                                                      }
                                                                                  """);
    }
}
