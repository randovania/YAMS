using System;
using YAMS_LIB;

var debug = true;
string am2rPath = "";
string outputAm2rPath = "";
string jsonPath = "";
if (debug)
{
    am2rPath = @"/home/narr/Dokumente/am2r 1.5.5/assets/game.unx_older";
    outputAm2rPath = @"/home/narr/Dokumente/am2r 1.5.5/output/assets/game.unx";
    jsonPath = @"/home/narr/Dokumente/am2r 1.5.5/output/assets/yams-data.json";
}
else
{
    if (args.Length < 3)
    {
        Console.WriteLine("Insufficient arguments!");
        Console.WriteLine("Usage: ./YAMS [path-to-original-data-file] [path-to-output-data-file] [path-to-json-file]");
        return -1;
    }
    
    am2rPath = args[0];
    outputAm2rPath = args[1];
    jsonPath = args[2];
}

Patcher.Main(am2rPath, outputAm2rPath, jsonPath);
return 0;