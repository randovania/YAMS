using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace YAMS_LIB.patches;

public class ArmCannonItem
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        UndertaleCode? characterVarsCode = gmData.Code.ByName("gml_Script_load_character_vars");
        UndertaleCode? fireCode = gmData.Code.ByName("gml_Script_chStepFire");
        // Add Arm cannon as an item
        characterVarsCode.PrependGMLInCode("global.hasArmCannon = 0;");

        var beamnoise = new UndertaleGameObject() { Name = gmData.Strings.MakeString("oBeamNoise")};
        var drawEvent = beamnoise.EventHandlerFor(EventType.Draw, gmData);
        drawEvent.SubstituteGMLCode("");
        var createEvent = beamnoise.EventHandlerFor(EventType.Create, gmData);
        createEvent.SubstituteGMLCode("xoff = 0; yoff = 0; alarm[0] = 1; image_alpha = 1 - (0.5 * global.sensitivitymode);");
        var alarm0 = beamnoise.EventHandlerFor(EventType.Alarm, (uint)0, gmData);
        alarm0.SubstituteGMLCode("""
        with (oCharacter)
        {
            idle = 0;
            if ((turning == 0 && morphing == 0 && unmorphing == 0 && walljumping == 0 && (state == STANDING || state == RUNNING || state == DUCKING || (state == JUMPING && vjump == 1))) || (state == GRIP && ((facing == RIGHT && aimdirection != 0) || (facing == LEFT && aimdirection != 1))))
            {
                empspark = instance_create(oCharacter.x + oCharacter.aspr2x, oCharacter.y + oCharacter.aspr2y, oFXAnimSpark);
                empspark.sprite_index = sBatterySpark;
                empspark.image_speed = 1;
                empspark.additive = 1;
                empspark.image_xscale = choose(1, -1);
                empspark.image_yscale = choose(1, -1);
                empspark.image_angle = random(360);
                empspark.depth = -10;
            }
        }

        alarm[0] = 5;
        """);
        var step = beamnoise.EventHandlerFor(EventType.Step, gmData);
        step.SubstituteGMLCode("if (!global.sensitivitymode) { xoff = irandom(128); yoff = irandom(128); } image_alpha -= 0.1; if (image_alpha <= 0) instance_destroy(); with (oCharacter) chargebeam = 0;");
        gmData.GameObjects.Add(beamnoise);

        
        gmData.Code.ByName("gml_Script_characterStepEvent").PrependGMLInCode("if (!global.hasArmCannon) { if (!instance_exists(oBeamNoise)) instance_create(0, 0, oBeamNoise); else with (oBeamNoise) image_alpha = 1 - (0.5 * global.sensitivitymode) }");


        fireCode.ReplaceGMLInCode("walljumping == 0 && monster_drain == 0 && !instance_exists(oEMPNoise)",
        "walljumping == 0 && monster_drain == 0 && !instance_exists(oEMPNoise) && !instance_exists(oBeamNoise)");    
        fireCode.ReplaceGMLInCode("global.cbeam == 1 && monster_drain == 0 && !instance_exists(oEMPNoise)",
        "global.cbeam == 1 && monster_drain == 0 && !instance_exists(oEMPNoise) && !instance_exists(oBeamNoise)");
    }
}