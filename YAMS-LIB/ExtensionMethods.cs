using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using System.Reflection;
using System.Runtime.Serialization;

namespace YAMS_LIB;

public static class ExtensionMethods
{
    private static UndertaleData? gmData = null;
    private static GlobalDecompileContext? decompileContext = null;

    static ExtensionMethods()
    {
        gmData = Patcher.gmData;
        decompileContext = Patcher.decompileContext;
    }


    public static string GetGMLCode(this UndertaleCode code)
    {
        return Decompiler.Decompile(code, decompileContext);
    }

    public static void ReplaceGMLInCode(this UndertaleCode code, string textToReplace, string replacementText, bool ignoreErrors = false)
    {
        var codeText = Decompiler.Decompile(code, decompileContext);
        if (!codeText.Contains(textToReplace))
        {
            if (ignoreErrors)
                return;

            throw new ApplicationException($"The text \"{textToReplace}\" was not found in \"{code.Name.Content}\"!");
        }
        codeText = codeText.Replace(textToReplace, replacementText);
        code.ReplaceGML(codeText, gmData);
    }

    public static void PrependGMLInCode(this UndertaleCode code, string prependedText)
    {
        var codeText = Decompiler.Decompile(code, decompileContext);
        codeText = prependedText + "\n" + codeText;
        code.ReplaceGML(codeText, gmData);
    }

    public static void AppendGMLInCode(this UndertaleCode code, string appendedText)
    {
        var codeText = Decompiler.Decompile(code, decompileContext);
        codeText = codeText + appendedText + "\n";
        code.ReplaceGML(codeText, gmData);
    }

    public static void SubstituteGMLCode(this UndertaleCode code, string newGMLCode)
    {
        code.ReplaceGML(newGMLCode, gmData);
    }

    public static UndertaleRoom.Tile CreateRoomTile(int x, int y, int depth, UndertaleBackground tileset, uint sourceX, uint sourceY, uint width = 16, uint height = 16, uint? id = null)
    {
        id ??= gmData.GeneralInfo.LastTile++;
        return new UndertaleRoom.Tile()
        {
            X = x,
            Y = y,
            TileDepth = depth,
            BackgroundDefinition = tileset,
            SourceX = sourceX,
            SourceY = sourceY,
            InstanceID = id.Value,
            Width = width,
            Height = height
        };
    }

    public static UndertaleRoom.GameObject CreateRoomObject(int x, int y, UndertaleGameObject gameObject, UndertaleCode? creationCode = null, int scaleX = 1, int scaleY = 1, uint? id = null)
    {
        id ??= gmData.GeneralInfo.LastObj++;
        return new UndertaleRoom.GameObject()
        {
            X = x,
            Y = y,
            ObjectDefinition = gameObject,
            CreationCode = creationCode,
            ScaleX = scaleX,
            ScaleY = scaleY,
            InstanceID = id.Value
        };
    }

    public static string? GetEnumMemberValue<T>(this T value)
        where T : Enum
    {
        return typeof(T)
            .GetTypeInfo()
            .DeclaredMembers
            .SingleOrDefault(x => x.Name == value.ToString())
            ?.GetCustomAttribute<EnumMemberAttribute>(false)
            ?.Value;
    }

    public static void AddScript(this IList<UndertaleScript> list, string name, string code)
    {
        var codeEntry = new UndertaleCode() { Name = gmData.Strings.MakeString($"gml_Script_{name}") };
        gmData.Code.Add(codeEntry);
        // add locals
        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = codeEntry.Name;
        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = gmData.Strings.MakeString("arguments");
        argsLocal.Index = 0;
        locals.Locals.Add(argsLocal);
        codeEntry.LocalsCount = 1;
        gmData.CodeLocals.Add(locals);

        codeEntry.SubstituteGMLCode(code);
        var script = new UndertaleScript() { Code = codeEntry, Name = gmData.Strings.MakeString(name)};
        gmData.Scripts.Add(script);
    }

    public static UndertaleCode AddCodeEntry(this IList<UndertaleCode> list, string name, string code)
    {
        var codeEntry = new UndertaleCode() { Name = gmData.Strings.MakeString(name) };
        gmData.Code.Add(codeEntry);
        // Add locals
        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = codeEntry.Name;
        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = gmData.Strings.MakeString("arguments");
        argsLocal.Index = 0;
        locals.Locals.Add(argsLocal);
        codeEntry.LocalsCount = 1;
        gmData.CodeLocals.Add(locals);

        codeEntry.SubstituteGMLCode(code);
        return codeEntry;
    }
}
