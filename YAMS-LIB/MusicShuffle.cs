using System.Diagnostics.CodeAnalysis;

namespace YAMS_LIB;

public class MusicShuffle
{
    public static void ShuffleMusic(string musicDirectory, Dictionary<string, string> musicDict)
    {
        if (musicDict.Count == 0)
            return;
        
        // First, we rename all songs to have a _ at the end
        DirectoryInfo musicPath = new DirectoryInfo(musicDirectory);
        foreach (var musicFile in musicPath.GetFiles("*.ogg"))
            musicFile.MoveTo(musicFile.FullName + "_");

        // Then we go through each song in the dictionary
        foreach ((var origSongName, var newSongName) in musicDict)
        {
            string finalSongName = newSongName;
            // These songs are optional, and may not be there in the directory
            if (newSongName == "mustester.ogg" && !File.Exists(musicDirectory + "mustester.ogg_"))
                finalSongName = "musancientguardian.ogg";
            
            if (newSongName == "musitemamb2.ogg" && !File.Exists(musicDirectory + "musitemamb2.ogg_"))
                finalSongName = "musitemamb.ogg";
            
            File.Move(musicDirectory + "/" + origSongName + "_", musicDirectory + "/" + finalSongName);
        }
        
        // Finally, if there are still songs with _ left, try to rename them back to what they should've been
        foreach (var musicFile in musicPath.GetFiles("*.ogg_"))
        {
            string origName = musicFile.FullName[..^1]; 
            if (!File.Exists(origName))
                musicFile.MoveTo(origName);
        }
    }
}