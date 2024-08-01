using BenchmarkDotNet.Attributes;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace BenchmarkTest;

[MemoryDiagnoser]
public class DecompileBenchmark
{
    public UndertaleData data;
    public GlobalDecompileContext decompileContext;

    [GlobalSetup]
    public void GlobalSetup()
    {
        using (FileStream fs = new FileInfo(@"/home/narr/Dokumente/am2r 1.5.5/assets/game.unx_older").OpenRead())
        {
            data = UndertaleIO.Read(fs);
        }
        decompileContext = new GlobalDecompileContext(data, false);
    }

    [Benchmark]
    public void AppendVariableToEveryEntry()
    {
        void AppendGML(UndertaleCode code, string appendedText)
        {
            var codeText = Decompiler.Decompile(code, decompileContext);
            codeText = codeText + appendedText + "\n";
            code.ReplaceGML(codeText, data);
        }

        foreach (UndertaleCode codeentry in data.Code.SkipLast((int)(data.Code.Count * 0.99)))
        {
            if (codeentry.Name.Content == "gml_Object_oRm_a5c11lock_Collision_267") continue;

            AppendGML(codeentry, "var this_variable_is_not_used_and_thus_completely_uselesss = 1; if (!this_variable_is_not_used_and_thus_completely_uselesss) this_variable_is_not_used_and_thus_completely_uselesss = 1");
        }
    }
}
